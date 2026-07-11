using FFMpegCore;
using RightClicks.Models.FalType;
using RightClicks.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfButton = System.Windows.Controls.Button;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfColor = System.Windows.Media.Color;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;

namespace RightClicks.Windows;

/// <summary>
/// ONE config window shared by all the NEW fal.ai category types (Audio-to-Video, Video-to-Video,
/// Swaps, Text-to-Video). It renders itself from the category's JSON model files: file input slots,
/// a model dropdown, the dynamic parameter form, and (when the model supports it) a LoRA selector.
/// The Image-to-Video window is a separate, frozen file and is not affected by this one.
/// </summary>
public partial class FalTypeConfigWindow : Window
{
    private static readonly string[] ImageExts = { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".avif" };
    private static readonly string[] AudioExts = { ".wav", ".mp3", ".m4a", ".aac", ".ogg", ".flac" };
    private static readonly string[] VideoExts = { ".mp4", ".mov", ".webm", ".m4v", ".avi", ".mkv", ".gif" };

    private readonly string _categoryFolder;
    private readonly string _triggerFilePath;
    private readonly string _triggerRole;
    private readonly string? _preselectModelId;

    private Dictionary<string, FalTypeModelConfig> _models = new();
    private FalTypeModelConfig? _currentModel;

    // Collected values.
    private readonly Dictionary<string, WpfTextBox> _fileInputBoxes = new(); // fal param -> textbox holding path/url
    private readonly Dictionary<string, object> _parameterValues = new();

    // LoRA widget state (only present when the model supports loras).
    private WpfComboBox? _loraCombo;
    private WpfTextBox? _loraScaleBox;
    private WpfComboBox? _loraTransformerCombo;
    private List<LoraPreset> _loraPresets = new();
    private bool _loraHasNone = true; // false when the model requires a LoRA (no "(none)" option)

    public string? SelectedModelId => _currentModel?.model_id;
    public string SelectedOutputType => _currentModel?.output_type ?? "video";
    public string? ReattachAudioFrom => _currentModel?.reattach_audio_from;
    public Dictionary<string, string> FileInputs { get; private set; } = new();
    public Dictionary<string, object> Parameters => _parameterValues;

    public FalTypeConfigWindow(string categoryFolder, string categoryTitle, string? preselectModelId, string triggerFilePath)
    {
        InitializeComponent();

        _categoryFolder = categoryFolder;
        _triggerFilePath = triggerFilePath;
        _preselectModelId = preselectModelId;
        _triggerRole = RoleOf(triggerFilePath);
        Title = $"{categoryTitle} - API Config";

        Log.Information("FalTypeConfigWindow init: category={Category}, trigger={File} (role={Role}), preselect={Preselect}",
            categoryFolder, triggerFilePath, _triggerRole, preselectModelId ?? "(none)");

        // Tray app has no owner window; come up Topmost then release so we land in front but don't pin.
        Loaded += (_, _) => { WindowState = WindowState.Normal; Activate(); Focus(); };
        Activated += (_, _) => Topmost = false;

        LoadModels();
        PopulateModelDropdown();
    }

    private static string RoleOf(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ImageExts.Contains(ext)) return "image";
        if (AudioExts.Contains(ext)) return "audio";
        if (VideoExts.Contains(ext)) return "video";
        return "";
    }

    private void LoadModels()
    {
        _models = new Dictionary<string, FalTypeModelConfig>();
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _categoryFolder);
            if (!Directory.Exists(dir))
            {
                Log.Error("Category folder not found: {Dir}", dir);
                WpfMessageBox.Show($"Configuration folder not found: {dir}", "Configuration Error",
                    WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
                return;
            }

            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var model = JsonSerializer.Deserialize<FalTypeModelConfig>(File.ReadAllText(file));
                    if (model != null && !string.IsNullOrEmpty(model.display_name))
                    {
                        _models[model.display_name] = model;
                        Log.Information("Loaded model {Name} ({Id}) from {File}", model.display_name, model.model_id, Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to load model config {File}", Path.GetFileName(file));
                }
            }

            if (_models.Count == 0)
                WpfMessageBox.Show($"No model configurations found in {dir}.", "Configuration Error",
                    WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load category models");
            WpfMessageBox.Show($"Failed to load models: {ex.Message}", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
    }

    private void PopulateModelDropdown()
    {
        ModelComboBox.Items.Clear();
        foreach (var name in _models.Keys.OrderBy(k => k))
            ModelComboBox.Items.Add(name);

        if (ModelComboBox.Items.Count == 0)
            return;

        var selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(_preselectModelId))
        {
            var match = _models.Values.FirstOrDefault(m =>
                string.Equals(m.model_id, _preselectModelId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                var idx = ModelComboBox.Items.IndexOf(match.display_name);
                if (idx >= 0) selectedIndex = idx;
            }
            else
            {
                Log.Warning("Preselect model '{Id}' not found among {Folder} JSONs - defaulting to first",
                    _preselectModelId, _categoryFolder);
            }
        }

        ModelComboBox.SelectedIndex = selectedIndex;
    }

    private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelComboBox.SelectedItem is not string name || !_models.TryGetValue(name, out var model))
            return;

        _currentModel = model;
        ModelInfoText.Text = $"{model.description}\nPricing: {model.pricing} | Processing: {model.processing_time} | Output: {model.output_type}";

        BuildInputs();
        BuildDynamicForm();
    }

    /// <summary>Render the file input slots. The slot matching the right-clicked file is pre-filled.</summary>
    private void BuildInputs()
    {
        InputsPanel.Children.Clear();
        _fileInputBoxes.Clear();

        if (_currentModel == null)
            return;

        // Text-to-Video style: no file slots; the trigger file's text pre-fills a prompt param instead.
        if (_currentModel.input_slots.Count == 0)
        {
            var note = new WpfTextBlock
            {
                Text = string.IsNullOrEmpty(_currentModel.prefill_prompt_from_file)
                    ? "This model takes no file input."
                    : $"Prompt pre-filled from: {Path.GetFileName(_triggerFilePath)}",
                FontSize = 12,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x60, 0x5E, 0x5C))
            };
            InputsPanel.Children.Add(note);
            return;
        }

        var autoFilled = false;
        foreach (var slot in _currentModel.input_slots)
        {
            var label = new WpfTextBlock
            {
                Text = slot.required ? $"{slot.label} *" : slot.label,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x1F, 0x1F, 0x1F)),
                Margin = new Thickness(0, 8, 0, 3)
            };
            InputsPanel.Children.Add(label);

            if (!string.IsNullOrEmpty(slot.description))
            {
                InputsPanel.Children.Add(new WpfTextBlock
                {
                    Text = slot.description,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(0x60, 0x5E, 0x5C)),
                    Margin = new Thickness(0, 0, 0, 3),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            var row = new WpfStackPanel { Orientation = WpfOrientation.Horizontal };

            var box = new WpfTextBox { FontSize = 12, Padding = new Thickness(6), Width = 640 };
            var browse = new WpfButton { Content = "Browse...", Width = 90, Margin = new Thickness(8, 0, 0, 0) };
            var slotCopy = slot;
            browse.Click += (_, _) => BrowseForSlot(slotCopy, box);

            // Auto-fill the first matching slot with the right-clicked file.
            if (!autoFilled && string.Equals(slot.role, _triggerRole, StringComparison.OrdinalIgnoreCase))
            {
                box.Text = _triggerFilePath;
                autoFilled = true;
            }

            row.Children.Add(box);
            row.Children.Add(browse);
            InputsPanel.Children.Add(row);

            _fileInputBoxes[slot.param] = box;

            // Small live preview for image slots.
            if (string.Equals(slot.role, "image", StringComparison.OrdinalIgnoreCase))
            {
                var preview = new System.Windows.Controls.Image { Height = 120, Stretch = Stretch.Uniform, Margin = new Thickness(0, 5, 0, 0), HorizontalAlignment = WpfHorizontalAlignment.Left };
                InputsPanel.Children.Add(preview);
                void Refresh() => preview.Source = TryLoadBitmap(box.Text);
                box.TextChanged += (_, _) => Refresh();
                Refresh();
            }
        }
    }

    private void BrowseForSlot(FalTypeInputSlot slot, WpfTextBox target)
    {
        var exts = slot.accept ?? DefaultExtsForRole(slot.role);
        var filter = exts.Length > 0
            ? $"{slot.role} files|{string.Join(";", exts.Select(x => "*" + x))}|All files|*.*"
            : "All files|*.*";

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter,
            InitialDirectory = Path.GetDirectoryName(_triggerFilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog(this) == true)
            target.Text = dlg.FileName;
    }

    private static string[] DefaultExtsForRole(string role) => role switch
    {
        "image" => ImageExts,
        "audio" => AudioExts,
        "video" => VideoExts,
        _ => Array.Empty<string>()
    };

    private static BitmapImage? TryLoadBitmap(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            return bmp;
        }
        catch { return null; }
    }

    private void BuildDynamicForm()
    {
        DynamicFormPanel.Children.Clear();
        _parameterValues.Clear();
        _loraCombo = null;
        _loraScaleBox = null;
        _loraTransformerCombo = null;

        if (_currentModel == null)
            return;

        foreach (var (paramName, cfg) in _currentModel.parameters)
        {
            DynamicFormPanel.Children.Add(new WpfTextBlock
            {
                Text = cfg.label ?? paramName,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x1F, 0x1F, 0x1F)),
                Margin = new Thickness(0, 10, 0, 5)
            });

            if (!string.IsNullOrEmpty(cfg.description))
            {
                DynamicFormPanel.Children.Add(new WpfTextBlock
                {
                    Text = cfg.description,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(0x60, 0x5E, 0x5C)),
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            switch (cfg.type)
            {
                case "text": CreateText(paramName, cfg); break;
                case "dropdown": CreateDropdown(paramName, cfg); break;
                case "checkbox": CreateCheckbox(paramName, cfg); break;
                case "number": CreateNumber(paramName, cfg); break;
                case "slider": CreateSlider(paramName, cfg); break;
                case "resolution": CreateResolution(paramName, cfg); break;
                default: Log.Warning("Unknown parameter type {Type} for {Param}", cfg.type, paramName); break;
            }
        }

        if (_currentModel.supports_loras)
            BuildLoraSelector();
    }

    private void CreateText(string paramName, FalTypeParameterConfig cfg)
    {
        var box = new WpfTextBox
        {
            FontSize = 13,
            Padding = new Thickness(8),
            Text = cfg.@default?.ToString() ?? "",
            AcceptsReturn = cfg.multiline,
            TextWrapping = cfg.multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Height = cfg.multiline ? (cfg.rows * 20 + 20) : double.NaN,
            VerticalScrollBarVisibility = cfg.multiline ? WpfScrollBarVisibility.Auto : WpfScrollBarVisibility.Disabled
        };

        // Text-to-Video: pre-fill the prompt box from the right-clicked .txt.
        if (!string.IsNullOrEmpty(_currentModel?.prefill_prompt_from_file)
            && string.Equals(paramName, _currentModel!.prefill_prompt_from_file, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(_triggerFilePath))
                    box.Text = File.ReadAllText(_triggerFilePath).Trim();
            }
            catch (Exception ex) { Log.Warning(ex, "Failed to pre-fill prompt from {File}", _triggerFilePath); }
        }
        else if (cfg.clear_on_focus)
        {
            var def = box.Text;
            box.GotFocus += (_, _) => { if (box.Text == def) box.Text = ""; };
        }

        box.TextChanged += (_, _) => _parameterValues[paramName] = box.Text;
        DynamicFormPanel.Children.Add(box);
        _parameterValues[paramName] = box.Text;
    }

    private void CreateDropdown(string paramName, FalTypeParameterConfig cfg)
    {
        var combo = new WpfComboBox { FontSize = 13, Padding = new Thickness(8) };
        if (cfg.options != null)
            foreach (var opt in cfg.options) combo.Items.Add(opt);

        if (cfg.@default != null)
        {
            var idx = combo.Items.IndexOf(cfg.@default.ToString());
            combo.SelectedIndex = idx >= 0 ? idx : (combo.Items.Count > 0 ? 0 : -1);
        }
        else if (combo.Items.Count > 0) combo.SelectedIndex = 0;

        combo.SelectionChanged += (_, _) =>
        {
            var coerced = Coerce(combo.SelectedItem?.ToString(), cfg);
            if (coerced != null) _parameterValues[paramName] = coerced;
        };
        DynamicFormPanel.Children.Add(combo);

        var initial = Coerce(combo.SelectedItem?.ToString(), cfg);
        if (initial != null) _parameterValues[paramName] = initial;
    }

    private void CreateCheckbox(string paramName, FalTypeParameterConfig cfg)
    {
        var defaultChecked = cfg.@default switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.True => true,
            JsonElement je when je.ValueKind == JsonValueKind.False => false,
            bool b => b,
            _ => false
        };

        var check = new WpfCheckBox
        {
            Content = cfg.label ?? paramName,
            FontSize = 13,
            IsChecked = defaultChecked,
            Margin = new Thickness(0, 5, 0, 0)
        };
        check.Checked += (_, _) => _parameterValues[paramName] = true;
        check.Unchecked += (_, _) => _parameterValues[paramName] = false;
        DynamicFormPanel.Children.Add(check);
        _parameterValues[paramName] = check.IsChecked == true;
    }

    private void CreateNumber(string paramName, FalTypeParameterConfig cfg)
    {
        var box = new WpfTextBox { FontSize = 13, Padding = new Thickness(8), Text = cfg.@default?.ToString() ?? "" };
        box.PreviewTextInput += (s, e) => e.Handled = !int.TryParse(e.Text, out _);
        box.TextChanged += (_, _) =>
        {
            if (int.TryParse(box.Text, out var value))
            {
                if (cfg.min.HasValue && value < cfg.min.Value) { value = (int)cfg.min.Value; box.Text = value.ToString(); }
                if (cfg.max.HasValue && value > cfg.max.Value) { value = (int)cfg.max.Value; box.Text = value.ToString(); }
                _parameterValues[paramName] = value;
            }
            else _parameterValues.Remove(paramName);
        };
        DynamicFormPanel.Children.Add(box);
        if (int.TryParse(box.Text, out var initial)) _parameterValues[paramName] = initial;
    }

    private void CreateSlider(string paramName, FalTypeParameterConfig cfg)
    {
        var panel = new WpfStackPanel { Orientation = WpfOrientation.Horizontal };
        var isInt = string.Equals(cfg.value_type, "int", StringComparison.OrdinalIgnoreCase);
        var step = cfg.step ?? (isInt ? 1 : 0.05);
        var format = isInt ? "F0" : "F2";

        var def = cfg.@default is JsonElement je && je.TryGetDouble(out var jd) ? jd : cfg.min ?? 0;

        var slider = new Slider
        {
            Minimum = cfg.min ?? 0,
            Maximum = cfg.max ?? 100,
            Value = def,
            Width = 300,
            TickFrequency = step,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center
        };
        var valueLabel = new WpfTextBlock
        {
            Text = slider.Value.ToString(format),
            FontSize = 13,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 40
        };

        void Store(double raw)
        {
            valueLabel.Text = raw.ToString(format);
            _parameterValues[paramName] = isInt ? (int)raw : Math.Round(raw, 3);
        }
        slider.ValueChanged += (_, _) => Store(slider.Value);

        panel.Children.Add(slider);
        panel.Children.Add(valueLabel);
        DynamicFormPanel.Children.Add(panel);
        Store(slider.Value);
    }

    /// <summary>
    /// Resolution dropdown that emits the nested { "width": W, "height": H } object fal wants.
    /// JSON declares options as "WxH" strings, e.g. ["1024x1024", "1280x720"].
    /// </summary>
    private void CreateResolution(string paramName, FalTypeParameterConfig cfg)
    {
        var combo = new WpfComboBox { FontSize = 13, Padding = new Thickness(8) };
        if (cfg.options != null)
            foreach (var opt in cfg.options) combo.Items.Add(opt);

        if (cfg.@default != null)
        {
            var idx = combo.Items.IndexOf(cfg.@default.ToString());
            combo.SelectedIndex = idx >= 0 ? idx : (combo.Items.Count > 0 ? 0 : -1);
        }
        else if (combo.Items.Count > 0) combo.SelectedIndex = 0;

        void Store(string? text)
        {
            var res = ParseResolution(text);
            if (res != null) _parameterValues[paramName] = res;
        }
        combo.SelectionChanged += (_, _) => Store(combo.SelectedItem?.ToString());
        DynamicFormPanel.Children.Add(combo);
        Store(combo.SelectedItem?.ToString());
    }

    private static Dictionary<string, object>? ParseResolution(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var parts = text.ToLowerInvariant().Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out var w) && int.TryParse(parts[1].Trim(), out var h))
            return new Dictionary<string, object> { ["width"] = w, ["height"] = h };
        return null;
    }

    /// <summary>
    /// The LoRA selector: dropdown of presets valid for this endpoint + scale + transformer + hint.
    /// On submit these become a loras[] array of { path, scale, transformer }.
    /// </summary>
    private void BuildLoraSelector()
    {
        _loraPresets = LoraRegistryService.GetForEndpoint(_currentModel!.model_id);
        _loraHasNone = !_currentModel.loras_required;

        DynamicFormPanel.Children.Add(new WpfTextBlock
        {
            Text = _currentModel.loras_required ? "LoRA *" : "LoRA",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0x1F, 0x1F, 0x1F)),
            Margin = new Thickness(0, 14, 0, 5)
        });

        _loraCombo = new WpfComboBox { FontSize = 13, Padding = new Thickness(8) };
        if (_loraHasNone) _loraCombo.Items.Add("(none)");
        foreach (var p in _loraPresets) _loraCombo.Items.Add(p.name);
        DynamicFormPanel.Children.Add(_loraCombo);

        var hint = new WpfTextBlock
        {
            FontSize = 11,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0x60, 0x5E, 0x5C)),
            Margin = new Thickness(0, 4, 0, 5),
            TextWrapping = TextWrapping.Wrap
        };
        DynamicFormPanel.Children.Add(hint);

        var scaleLabel = new WpfTextBlock { Text = "LoRA Scale (1.2–1.5 typical; higher = stronger)", FontSize = 12, Margin = new Thickness(0, 6, 0, 3) };
        DynamicFormPanel.Children.Add(scaleLabel);
        _loraScaleBox = new WpfTextBox { FontSize = 13, Padding = new Thickness(8), Text = "1.2", Width = 120, HorizontalAlignment = WpfHorizontalAlignment.Left };
        DynamicFormPanel.Children.Add(_loraScaleBox);

        var transLabel = new WpfTextBlock { Text = "Transformer", FontSize = 12, Margin = new Thickness(0, 6, 0, 3) };
        DynamicFormPanel.Children.Add(transLabel);
        _loraTransformerCombo = new WpfComboBox { FontSize = 13, Padding = new Thickness(8), Width = 160, HorizontalAlignment = WpfHorizontalAlignment.Left };
        foreach (var t in new[] { "both", "high", "low" }) _loraTransformerCombo.Items.Add(t);
        _loraTransformerCombo.SelectedIndex = 0;
        DynamicFormPanel.Children.Add(_loraTransformerCombo);

        _loraCombo.SelectionChanged += (_, _) =>
        {
            var i = SelectedLoraPresetIndex();
            if (i >= 0 && i < _loraPresets.Count)
            {
                var p = _loraPresets[i];
                hint.Text = p.use_hint;
                _loraCombo.ToolTip = p.use_hint;
                _loraScaleBox!.Text = p.default_scale.ToString("0.0#");
                var ti = _loraTransformerCombo!.Items.IndexOf(p.default_transformer);
                if (ti >= 0) _loraTransformerCombo.SelectedIndex = ti;
            }
            else hint.Text = "";
        };

        // Default: first preset when required, otherwise "(none)". Firing this populates scale/hint.
        if (_loraCombo.Items.Count > 0)
            _loraCombo.SelectedIndex = 0;
    }

    /// <summary>Index into <see cref="_loraPresets"/> of the current selection, or -1 if "(none)".</summary>
    private int SelectedLoraPresetIndex()
    {
        if (_loraCombo == null) return -1;
        return _loraCombo.SelectedIndex - (_loraHasNone ? 1 : 0);
    }

    private static object? Coerce(string? text, FalTypeParameterConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return cfg.value_type?.ToLowerInvariant() switch
        {
            "int" => int.TryParse(text, out var i) ? i : (object?)null,
            "number" => double.TryParse(text, out var d) ? d : (object?)null,
            "bool" => bool.TryParse(text, out var b) ? b : (object?)null,
            _ => text
        };
    }

    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentModel == null)
            return;

        // Validate required file slots.
        FileInputs = new Dictionary<string, string>();
        foreach (var slot in _currentModel.input_slots)
        {
            var text = _fileInputBoxes.TryGetValue(slot.param, out var box) ? box.Text?.Trim() ?? "" : "";
            if (slot.required && string.IsNullOrWhiteSpace(text))
            {
                WpfMessageBox.Show($"'{slot.label}' is required.", "Validation", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
                return;
            }
            if (!string.IsNullOrWhiteSpace(text))
                FileInputs[slot.param] = text;
        }

        // Validate required params.
        foreach (var (name, cfg) in _currentModel.parameters)
        {
            if (!cfg.required) continue;
            var has = _parameterValues.TryGetValue(name, out var val)
                      && !(val is string s && string.IsNullOrWhiteSpace(s));
            if (!has)
            {
                WpfMessageBox.Show($"'{cfg.label ?? name}' is required.", "Validation", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
                return;
            }
        }

        // Guard fal's 481-frame cap: match_audio_length can't be honored for long audio.
        if (!GuardMatchAudioLength())
            return;

        // Assemble loras[] if a preset was chosen (and enforce it when the endpoint requires one).
        if (_currentModel.supports_loras)
        {
            var presetIndex = SelectedLoraPresetIndex();
            if (presetIndex >= 0 && presetIndex < _loraPresets.Count)
            {
                var preset = _loraPresets[presetIndex];
                double.TryParse(_loraScaleBox?.Text, out var scale);
                if (scale <= 0) scale = preset.default_scale;

                _parameterValues["loras"] = new List<Dictionary<string, object>>
                {
                    new()
                    {
                        ["path"] = preset.url,
                        ["scale"] = scale,
                        ["transformer"] = _loraTransformerCombo?.SelectedItem?.ToString() ?? "both"
                    }
                };
            }
            else if (_currentModel.loras_required)
            {
                WpfMessageBox.Show("This model requires a LoRA. Please choose one.", "Validation",
                    WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
                return;
            }
        }

        Log.Information("FalTypeConfigWindow submitted: model={Model}, files={Files}, params={Params}",
            _currentModel.model_id, FileInputs.Count, _parameterValues.Count);

        DialogResult = true;
        Close();
    }

    private const int LtxMaxFrames = 481;

    /// <summary>
    /// If match_audio_length is on and the chosen audio exceeds fal's 481-frame cap at the selected
    /// fps, warn and auto-uncheck it (so fal renders a default-length clip instead of hard-failing).
    /// Returns true to proceed with submit. Never blocks on a probe failure.
    /// </summary>
    private bool GuardMatchAudioLength()
    {
        if (_currentModel == null) return true;
        if (!_parameterValues.TryGetValue("match_audio_length", out var mal) || mal is not true)
            return true;

        var audioSlot = _currentModel.input_slots.FirstOrDefault(s =>
            string.Equals(s.role, "audio", StringComparison.OrdinalIgnoreCase));
        if (audioSlot == null || !_fileInputBoxes.TryGetValue(audioSlot.param, out var box))
            return true;

        var audioPath = box.Text?.Trim();
        if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
            return true; // can't probe a URL/missing file — let fal decide

        var fps = _parameterValues.TryGetValue("frames_per_second", out var f) && f is int fi ? fi : 24;

        double seconds;
        try
        {
            seconds = FFProbe.Analyse(audioPath).Duration.TotalSeconds;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not probe audio duration for {Path} - skipping match_audio_length guard", audioPath);
            return true;
        }

        var frames = (int)Math.Ceiling(seconds * fps);
        if (frames <= LtxMaxFrames)
            return true;

        var maxSeconds = LtxMaxFrames / (double)fps;
        WpfMessageBox.Show(
            $"'Match audio length' can't be used here: your audio is {seconds:F1}s (~{frames} frames at {fps} fps), " +
            $"but this model caps at {LtxMaxFrames} frames (~{maxSeconds:F0}s at {fps} fps).\n\n" +
            "I've turned off 'Match audio length' for this run so it doesn't fail. The video will be a " +
            "default length and the source audio will be trimmed to fit. To match the full clip, trim the audio first.",
            "Audio too long for match-length", WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);

        _parameterValues["match_audio_length"] = false;
        Log.Information("Auto-unchecked match_audio_length: {Sec}s * {Fps}fps = {Frames} frames > {Max}", seconds, fps, frames, LtxMaxFrames);
        return true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
