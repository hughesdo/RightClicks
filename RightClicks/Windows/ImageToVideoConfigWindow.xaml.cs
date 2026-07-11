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
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfColor = System.Windows.Media.Color;

namespace RightClicks.Windows;

/// <summary>
/// Configuration window for Image-to-Video generation.
/// Allows user to select model, configure parameters, and submit API request.
/// </summary>
public partial class ImageToVideoConfigWindow : Window
{
    private string _imagePath;
    private readonly string _initialDirectory;
    private readonly string? _preselectModelId;
    private Dictionary<string, ModelConfig>? _modelConfigs;
    private ModelConfig? _currentModelConfig;
    private Dictionary<string, object> _parameterValues = new();
    private WpfTextBox? _promptTextBox;
    private WpfCheckBox? _generateAudioCheckBox;

    // Public properties to expose configuration data
    public string ImagePath => _imagePath;
    public string? SelectedModelId => _currentModelConfig?.model_id;
    public string? SelectedModelDisplayName => _currentModelConfig?.display_name;

    /// <summary>
    /// The parameter name the selected model expects the image URL under.
    /// Kling v3 calls it "start_image_url"; everything else calls it "image_url".
    /// </summary>
    public string? SelectedImageParameter => _currentModelConfig?.image_parameter;

    public Dictionary<string, object> Parameters => _parameterValues;

    /// <param name="imagePath">The image the user right-clicked.</param>
    /// <param name="preselectModelId">
    /// The model to open on - the fal model_id of the feature the user picked from the context menu.
    /// The user can still switch models here, and their choice is what gets called.
    /// </param>
    public ImageToVideoConfigWindow(string imagePath, string? preselectModelId = null)
    {
        InitializeComponent();

        _imagePath = imagePath;
        _preselectModelId = preselectModelId;
        _initialDirectory = Path.GetDirectoryName(imagePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Log.Information("ImageToVideoConfigWindow: Initializing with image={ImagePath}, preselect={PreselectModelId}",
            imagePath, preselectModelId ?? "(none)");

        // RightClicks lives in the tray with no visible main window, so this dialog has no owner to
        // sit above and Windows won't hand a background process the foreground - it lands minimized
        // or behind Explorer. Come up Topmost to force it in front, then drop Topmost once we have
        // focus so it doesn't stay pinned over everything else.
        Loaded += (_, _) =>
        {
            WindowState = WindowState.Normal;
            Activate();
            Focus();
        };

        Activated += (_, _) => Topmost = false;

        LoadImage();
        LoadModelConfigurations();
        PopulateModelDropdown();
    }

    /// <summary>
    /// Load and display the source image.
    /// </summary>
    private void LoadImage()
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(_imagePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            SourceImage.Source = bitmap;
            ImagePathText.Text = _imagePath;

            Log.Information("Source image loaded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load source image");
            WpfMessageBox.Show($"Failed to load image: {ex.Message}", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Load model configurations from JSON files in ImageToVideo folder.
    /// </summary>
    private void LoadModelConfigurations()
    {
        _modelConfigs = new Dictionary<string, ModelConfig>();

        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var configDir = Path.Combine(appDir, "ImageToVideo");

            if (!Directory.Exists(configDir))
            {
                Log.Error("ImageToVideo configuration directory not found: {Path}", configDir);
                WpfMessageBox.Show($"Configuration directory not found: {configDir}", "Configuration Error", 
                    WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
                return;
            }

            var jsonFiles = Directory.GetFiles(configDir, "*.json");
            Log.Information("Found {Count} model configuration files", jsonFiles.Length);

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(jsonFile);
                    var config = JsonSerializer.Deserialize<ModelConfig>(json);
                    if (config != null && !string.IsNullOrEmpty(config.display_name))
                    {
                        _modelConfigs[config.display_name] = config;
                        Log.Information("Loaded model config: {ModelName} from {FileName}", 
                            config.display_name, Path.GetFileName(jsonFile));
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to load model config from {FileName}", Path.GetFileName(jsonFile));
                }
            }

            if (_modelConfigs.Count == 0)
            {
                Log.Error("No model configurations loaded!");
                WpfMessageBox.Show("No model configurations found. Please ensure JSON files exist in the ImageToVideo directory.",
                    "Configuration Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load model configurations");
            WpfMessageBox.Show($"Failed to load model configurations: {ex.Message}", "Error", 
                WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Populate the model dropdown with available models.
    /// </summary>
    private void PopulateModelDropdown()
    {
        if (_modelConfigs == null || _modelConfigs.Count == 0)
        {
            Log.Warning("No model configurations available to populate dropdown");
            return;
        }

        ModelComboBox.Items.Clear();

        // Sort models by display name
        foreach (var modelName in _modelConfigs.Keys.OrderBy(k => k))
        {
            ModelComboBox.Items.Add(modelName);
        }

        if (ModelComboBox.Items.Count == 0)
        {
            return;
        }

        // Open on the model the user actually right-clicked, falling back to the first entry.
        var selectedIndex = 0;

        if (!string.IsNullOrWhiteSpace(_preselectModelId))
        {
            var match = _modelConfigs.Values.FirstOrDefault(m =>
                string.Equals(m.model_id, _preselectModelId, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                var index = ModelComboBox.Items.IndexOf(match.display_name);
                if (index >= 0)
                {
                    selectedIndex = index;
                    Log.Information("Preselected model: {DisplayName} ({ModelId})", match.display_name, match.model_id);
                }
            }
            else
            {
                Log.Warning("Preselect model '{PreselectModelId}' has no matching JSON config - defaulting to first model. " +
                            "The feature's DefaultModelId and the ImageToVideo\\*.json model_ids are out of sync.",
                    _preselectModelId);
            }
        }

        ModelComboBox.SelectedIndex = selectedIndex;

        Log.Information("Populated model dropdown with {Count} models", ModelComboBox.Items.Count);
    }

    /// <summary>
    /// Handle model selection change - rebuild dynamic form.
    /// </summary>
    private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelComboBox.SelectedItem == null || _modelConfigs == null)
            return;

        var selectedModelName = ModelComboBox.SelectedItem.ToString();
        if (string.IsNullOrEmpty(selectedModelName) || !_modelConfigs.ContainsKey(selectedModelName))
            return;

        _currentModelConfig = _modelConfigs[selectedModelName];
        Log.Information("Model selected: {ModelName} ({ModelId})",
            _currentModelConfig.display_name, _currentModelConfig.model_id);

        // Update model info text
        ModelInfoText.Text = $"{_currentModelConfig.description}\nPricing: {_currentModelConfig.pricing} | Processing: {_currentModelConfig.processing_time}";

        // Rebuild dynamic form
        BuildDynamicForm();
    }

    /// <summary>
    /// Build dynamic form controls based on current model's parameters.
    /// </summary>
    private void BuildDynamicForm()
    {
        DynamicFormPanel.Children.Clear();
        _parameterValues.Clear();

        if (_currentModelConfig?.parameters == null)
        {
            Log.Warning("No parameters defined for current model");
            return;
        }

        Log.Information("Building dynamic form with {Count} parameters", _currentModelConfig.parameters.Count);

        foreach (var param in _currentModelConfig.parameters)
        {
            var paramName = param.Key;
            var paramConfig = param.Value;

            // Create label
            var label = new WpfTextBlock
            {
                Text = paramConfig.label ?? paramName,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0x1F, 0x1F, 0x1F)),
                Margin = new Thickness(0, 10, 0, 5)
            };
            DynamicFormPanel.Children.Add(label);

            // Create description if available
            if (!string.IsNullOrEmpty(paramConfig.description))
            {
                var description = new WpfTextBlock
                {
                    Text = paramConfig.description,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(0x60, 0x5E, 0x5C)),
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap
                };
                DynamicFormPanel.Children.Add(description);
            }

            // Create appropriate control based on parameter type
            switch (paramConfig.type)
            {
                case "text":
                    CreateTextControl(paramName, paramConfig);
                    break;
                case "dropdown":
                    CreateDropdownControl(paramName, paramConfig);
                    break;
                case "checkbox":
                    CreateCheckboxControl(paramName, paramConfig);
                    break;
                case "number":
                    CreateNumberControl(paramName, paramConfig);
                    break;
                case "slider":
                    CreateSliderControl(paramName, paramConfig);
                    break;
                default:
                    Log.Warning("Unknown parameter type: {Type} for {ParamName}", paramConfig.type, paramName);
                    break;
            }
        }

        Log.Information("Dynamic form built successfully");
    }

    /// <summary>
    /// Create text input control (TextBox).
    /// </summary>
    private void CreateTextControl(string paramName, ParameterConfig config)
    {
        var textBox = new WpfTextBox
        {
            FontSize = 13,
            Padding = new Thickness(8),
            Text = config.@default?.ToString() ?? "",
            AcceptsReturn = config.multiline,
            TextWrapping = config.multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Height = config.multiline ? (config.rows * 20 + 20) : double.NaN,
            VerticalScrollBarVisibility = config.multiline ? WpfScrollBarVisibility.Auto : WpfScrollBarVisibility.Disabled
        };

        // Store reference to prompt textbox
        if (paramName == "prompt")
        {
            _promptTextBox = textBox;
        }

        // Handle clear_on_focus
        if (config.clear_on_focus)
        {
            var defaultText = textBox.Text;
            textBox.GotFocus += (s, e) =>
            {
                if (textBox.Text == defaultText)
                {
                    textBox.Text = "";
                }
            };
        }

        textBox.TextChanged += (s, e) => _parameterValues[paramName] = textBox.Text;
        DynamicFormPanel.Children.Add(textBox);

        // Set initial value
        _parameterValues[paramName] = textBox.Text;
    }

    /// <summary>
    /// Convert a control's displayed text to the CLR type fal expects for this field, so it
    /// serializes as the right JSON type. Dropdown options are always shown as strings, but a
    /// model may declare the underlying field as an integer (Wan's duration) or a float.
    /// Returns null if the text can't be parsed, which causes the field to be omitted.
    /// </summary>
    private object? CoerceValue(string? text, ParameterConfig config)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        switch (config.value_type?.ToLowerInvariant())
        {
            case "int":
                return int.TryParse(text, out var i) ? i : (object?)null;

            case "number":
                return double.TryParse(text, out var d) ? d : (object?)null;

            case "bool":
                return bool.TryParse(text, out var b) ? b : (object?)null;

            default:
                return text;
        }
    }

    /// <summary>
    /// Create dropdown control (ComboBox).
    /// </summary>
    private void CreateDropdownControl(string paramName, ParameterConfig config)
    {
        var comboBox = new WpfComboBox
        {
            FontSize = 13,
            Padding = new Thickness(8)
        };

        if (config.options != null)
        {
            foreach (var option in config.options)
            {
                comboBox.Items.Add(option);
            }
        }

        // Set default value
        if (config.@default != null)
        {
            var defaultValue = config.@default.ToString();
            var index = comboBox.Items.IndexOf(defaultValue);
            if (index >= 0)
            {
                comboBox.SelectedIndex = index;
            }
        }
        else if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }

        comboBox.SelectionChanged += (s, e) =>
        {
            var coerced = CoerceValue(comboBox.SelectedItem?.ToString(), config);
            if (coerced != null)
            {
                _parameterValues[paramName] = coerced;
            }
        };

        DynamicFormPanel.Children.Add(comboBox);

        // Set initial value
        var initial = CoerceValue(comboBox.SelectedItem?.ToString(), config);
        if (initial != null)
        {
            _parameterValues[paramName] = initial;
        }
    }

    /// <summary>
    /// Create checkbox control (CheckBox).
    /// </summary>
    private void CreateCheckboxControl(string paramName, ParameterConfig config)
    {
        // System.Text.Json hands us a JsonElement here, never a bool, so an "is bool" test would
        // always fail and quietly force every checkbox default to false.
        var defaultChecked = config.@default switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.True => true,
            JsonElement je when je.ValueKind == JsonValueKind.False => false,
            bool b => b,
            _ => false
        };

        var checkBox = new WpfCheckBox
        {
            Content = config.label ?? paramName,
            FontSize = 13,
            IsChecked = defaultChecked,
            Margin = new Thickness(0, 5, 0, 0)
        };

        // Store reference to generate_audio checkbox
        if (paramName == "generate_audio")
        {
            _generateAudioCheckBox = checkBox;
        }

        checkBox.Checked += (s, e) => _parameterValues[paramName] = true;
        checkBox.Unchecked += (s, e) => _parameterValues[paramName] = false;

        DynamicFormPanel.Children.Add(checkBox);

        // Set initial value
        _parameterValues[paramName] = checkBox.IsChecked == true;
    }

    /// <summary>
    /// Create number input control (TextBox with numeric validation).
    /// </summary>
    private void CreateNumberControl(string paramName, ParameterConfig config)
    {
        var textBox = new WpfTextBox
        {
            FontSize = 13,
            Padding = new Thickness(8),
            Text = config.@default?.ToString() ?? ""
        };

        textBox.PreviewTextInput += (s, e) =>
        {
            // Allow only numbers
            e.Handled = !int.TryParse(e.Text, out _);
        };

        textBox.TextChanged += (s, e) =>
        {
            if (int.TryParse(textBox.Text, out var value))
            {
                // Validate min/max
                if (config.min.HasValue && value < config.min.Value)
                {
                    value = (int)config.min.Value;
                    textBox.Text = value.ToString();
                }
                if (config.max.HasValue && value > config.max.Value)
                {
                    value = (int)config.max.Value;
                    textBox.Text = value.ToString();
                }

                _parameterValues[paramName] = value;
            }
            else
            {
                // Blank means "leave it out" (an optional seed, say) - not "send null".
                _parameterValues.Remove(paramName);
            }
        };

        DynamicFormPanel.Children.Add(textBox);

        // Set initial value
        if (int.TryParse(textBox.Text, out var initialValue))
        {
            _parameterValues[paramName] = initialValue;
        }
    }

    /// <summary>
    /// Create slider control (Slider with value display).
    /// </summary>
    private void CreateSliderControl(string paramName, ParameterConfig config)
    {
        var panel = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal
        };

        // Fractional sliders (Kling's cfg_scale runs 0-1 in steps of 0.05) need a sub-1 tick,
        // so the step comes from the config rather than being pinned to 1.
        var isInteger = string.Equals(config.value_type, "int", StringComparison.OrdinalIgnoreCase);
        var step = config.step ?? (isInteger ? 1 : 0.05);
        var format = isInteger ? "F0" : "F2";

        var defaultValue = config.@default is JsonElement je && je.TryGetDouble(out var jsonDefault)
            ? jsonDefault
            : config.min ?? 0;

        var slider = new Slider
        {
            Minimum = config.min ?? 0,
            Maximum = config.max ?? 100,
            Value = defaultValue,
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
            _parameterValues[paramName] = isInteger ? (int)raw : Math.Round(raw, 3);
        }

        slider.ValueChanged += (s, e) => Store(slider.Value);

        panel.Children.Add(slider);
        panel.Children.Add(valueLabel);
        DynamicFormPanel.Children.Add(panel);

        // Set initial value
        Store(slider.Value);
    }

    /// <summary>
    /// Handle submit button - validate and close with OK result.
    /// </summary>
    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required parameters
        if (_currentModelConfig?.parameters != null)
        {
            foreach (var param in _currentModelConfig.parameters)
            {
                var paramName = param.Key;
                var paramConfig = param.Value;

                if (paramConfig.required && !_parameterValues.ContainsKey(paramName))
                {
                    WpfMessageBox.Show($"Required parameter '{paramConfig.label ?? paramName}' is missing.",
                        "Validation Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
                    return;
                }

                // Check if required text field is empty
                if (paramConfig.required && paramConfig.type == "text")
                {
                    var value = _parameterValues[paramName]?.ToString();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        WpfMessageBox.Show($"Required parameter '{paramConfig.label ?? paramName}' cannot be empty.",
                            "Validation Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
                        return;
                    }
                }
            }
        }

        Log.Information("ImageToVideoConfigWindow: User submitted configuration");
        Log.Information("  Model: {ModelId}", SelectedModelId);
        Log.Information("  Parameters: {ParamCount}", _parameterValues.Count);

        DialogResult = true;
        Close();
    }

    /// <summary>
    /// Handle cancel button - close without submitting.
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("ImageToVideoConfigWindow: User cancelled");
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// Model configuration loaded from JSON file. These files are the single source of truth for
/// which fal endpoint gets called and what it accepts - generated from fal's OpenAPI schemas.
/// </summary>
public class ModelConfig
{
    public string model_id { get; set; } = string.Empty;
    public string display_name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string pricing { get; set; } = string.Empty;
    public string processing_time { get; set; } = string.Empty;

    /// <summary>Parameter name this model expects the image URL under (e.g. "image_url", "start_image_url").</summary>
    public string image_parameter { get; set; } = "image_url";

    public Dictionary<string, ParameterConfig> parameters { get; set; } = new();
}

/// <summary>
/// Parameter configuration for a single model parameter.
/// </summary>
public class ParameterConfig
{
    public string type { get; set; } = string.Empty; // text, dropdown, checkbox, number, slider

    /// <summary>
    /// The JSON type fal expects for this field: "string", "int", "number", or "bool".
    /// Separate from <see cref="type"/>, which only decides which control to draw. Wan and Veo both
    /// show a duration dropdown, but Wan needs the integer 5 and Veo needs the string "8s".
    /// </summary>
    public string value_type { get; set; } = "string";

    public string label { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public bool required { get; set; }
    public object? @default { get; set; }
    public bool multiline { get; set; }
    public int rows { get; set; }
    public bool clear_on_focus { get; set; }
    public string[]? options { get; set; }
    public double? min { get; set; }
    public double? max { get; set; }
    public double? step { get; set; }
}
