using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
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
/// Configuration window for First + Last Frame video generation.
/// Allows user to select model, configure parameters, and submit API request.
/// </summary>
public partial class FirstLastFrameConfigWindow : Window
{
    private string _firstImagePath;
    private string _lastImagePath;
    private readonly string _initialDirectory;
    private Dictionary<string, ModelConfig>? _modelConfigs;
    private ModelConfig? _currentModelConfig;
    private Dictionary<string, object> _parameterValues = new();
    private WpfTextBox? _promptTextBox;
    private WpfCheckBox? _generateAudioCheckBox;

    // Public properties to expose configuration data
    public string FirstImagePath => _firstImagePath;
    public string LastImagePath => _lastImagePath;
    public string? SelectedModelId => _currentModelConfig?.model_id;
    public string? SelectedModelDisplayName => _currentModelConfig?.display_name;
    public Dictionary<string, object> Parameters => _parameterValues;

    public FirstLastFrameConfigWindow(string firstImagePath, string lastImagePath)
    {
        InitializeComponent();

        _firstImagePath = firstImagePath;
        _lastImagePath = lastImagePath;
        _initialDirectory = Path.GetDirectoryName(firstImagePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        Log.Information("FirstLastFrameConfigWindow: Initializing with first={FirstImage}, last={LastImage}", 
            firstImagePath, lastImagePath);

        LoadImages();
        LoadModelConfigurations();
        PopulateModelDropdown();
    }

    /// <summary>
    /// Load and display the selected images.
    /// </summary>
    private void LoadImages()
    {
        try
        {
            // Load first frame
            var firstBitmap = new BitmapImage();
            firstBitmap.BeginInit();
            firstBitmap.UriSource = new Uri(_firstImagePath, UriKind.Absolute);
            firstBitmap.CacheOption = BitmapCacheOption.OnLoad;
            firstBitmap.EndInit();
            FirstFrameImage.Source = firstBitmap;

            // Load last frame
            var lastBitmap = new BitmapImage();
            lastBitmap.BeginInit();
            lastBitmap.UriSource = new Uri(_lastImagePath, UriKind.Absolute);
            lastBitmap.CacheOption = BitmapCacheOption.OnLoad;
            lastBitmap.EndInit();
            LastFrameImage.Source = lastBitmap;

            Log.Information("FirstLastFrameConfigWindow: Images loaded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "FirstLastFrameConfigWindow: Failed to load images");
            WpfMessageBox.Show($"Failed to load images: {ex.Message}", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Load model configurations from JSON files.
    /// </summary>
    private void LoadModelConfigurations()
    {
        _modelConfigs = new Dictionary<string, ModelConfig>();

        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Load wan-flf2v.txt
            var wanConfigPath = Path.Combine(appDir, "wan-flf2v.txt");
            if (File.Exists(wanConfigPath))
            {
                var wanJson = File.ReadAllText(wanConfigPath);
                var wanConfig = JsonSerializer.Deserialize<ModelConfig>(wanJson);
                if (wanConfig != null)
                {
                    _modelConfigs[wanConfig.display_name] = wanConfig;
                    Log.Information("Loaded model config: {ModelName}", wanConfig.display_name);
                }
            }
            else
            {
                Log.Warning("Model config file not found: {Path}", wanConfigPath);
            }

            // Load first-last-frame-to-video.txt
            var veoConfigPath = Path.Combine(appDir, "first-last-frame-to-video.txt");
            if (File.Exists(veoConfigPath))
            {
                var veoJson = File.ReadAllText(veoConfigPath);
                var veoConfig = JsonSerializer.Deserialize<ModelConfig>(veoJson);
                if (veoConfig != null)
                {
                    _modelConfigs[veoConfig.display_name] = veoConfig;
                    Log.Information("Loaded model config: {ModelName}", veoConfig.display_name);
                }
            }
            else
            {
                Log.Warning("Model config file not found: {Path}", veoConfigPath);
            }

            if (_modelConfigs.Count == 0)
            {
                Log.Error("No model configurations loaded!");
                WpfMessageBox.Show("No model configurations found. Please ensure wan-flf2v.txt and first-last-frame-to-video.txt exist in the application directory.",
                    "Configuration Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load model configurations");
            WpfMessageBox.Show($"Failed to load model configurations: {ex.Message}", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Populate the model dropdown with available models.
    /// </summary>
    private void PopulateModelDropdown()
    {
        if (_modelConfigs == null || _modelConfigs.Count == 0)
        {
            Log.Warning("No model configs available to populate dropdown");
            return;
        }

        foreach (var modelName in _modelConfigs.Keys)
        {
            ModelComboBox.Items.Add(modelName);
        }

        // Select first model by default
        if (ModelComboBox.Items.Count > 0)
        {
            ModelComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Handle model selection change - rebuild form with new model's parameters.
    /// </summary>
    private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModelComboBox.SelectedItem == null || _modelConfigs == null)
            return;

        var selectedModelName = ModelComboBox.SelectedItem.ToString();
        if (selectedModelName == null || !_modelConfigs.ContainsKey(selectedModelName))
            return;

        _currentModelConfig = _modelConfigs[selectedModelName];
        Log.Information("Model selected: {ModelName}", selectedModelName);

        BuildDynamicForm();
    }

    /// <summary>
    /// Build the dynamic form based on the selected model's parameters.
    /// </summary>
    private void BuildDynamicForm()
    {
        DynamicFormPanel.Children.Clear();
        _parameterValues.Clear();

        if (_currentModelConfig == null || _currentModelConfig.parameters == null)
        {
            Log.Warning("No model config or parameters available");
            return;
        }

        Log.Information("Building form for model: {ModelName} with {ParamCount} parameters",
            _currentModelConfig.display_name, _currentModelConfig.parameters.Count);

        foreach (var param in _currentModelConfig.parameters)
        {
            var paramName = param.Key;
            var paramConfig = param.Value;

            // Create label
            var label = new WpfTextBlock
            {
                Text = paramConfig.label,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(31, 31, 31)),
                Margin = new Thickness(0, 10, 0, 5)
            };
            DynamicFormPanel.Children.Add(label);

            // Create description
            if (!string.IsNullOrEmpty(paramConfig.description))
            {
                var description = new WpfTextBlock
                {
                    Text = paramConfig.description,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(96, 94, 92)),
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap
                };
                DynamicFormPanel.Children.Add(description);
            }

            // Create input control based on type
            UIElement? inputControl = paramConfig.type switch
            {
                "text" => CreateTextInput(paramName, paramConfig),
                "number" => CreateNumberInput(paramName, paramConfig),
                "slider" => CreateSliderInput(paramName, paramConfig),
                "dropdown" => CreateDropdownInput(paramName, paramConfig),
                "checkbox" => CreateCheckboxInput(paramName, paramConfig),
                _ => null
            };

            if (inputControl != null)
            {
                DynamicFormPanel.Children.Add(inputControl);
            }
        }
    }

    /// <summary>
    /// Create a text input control.
    /// </summary>
    private UIElement CreateTextInput(string paramName, ParameterConfig config)
    {
        var textBox = new WpfTextBox
        {
            FontSize = 13,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 10),
            Text = config.@default?.ToString() ?? "",
            AcceptsReturn = config.multiline,
            TextWrapping = config.multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            VerticalScrollBarVisibility = config.multiline ? WpfScrollBarVisibility.Auto : WpfScrollBarVisibility.Disabled,
            Height = config.multiline ? (config.rows * 20 + 20) : double.NaN
        };

        // Store reference to prompt textbox for auto-enable-on-quotes behavior
        if (paramName == "prompt")
        {
            _promptTextBox = textBox;
        }

        // Handle clear_on_focus behavior
        bool hasBeenCleared = false;
        if (config.clear_on_focus)
        {
            textBox.GotFocus += (s, e) =>
            {
                if (!hasBeenCleared && textBox.Text == config.@default?.ToString())
                {
                    textBox.Text = "";
                    hasBeenCleared = true;
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = config.@default?.ToString() ?? "";
                    hasBeenCleared = false;
                }
            };
        }

        textBox.TextChanged += (s, e) =>
        {
            _parameterValues[paramName] = textBox.Text;

            // Auto-enable generate_audio if prompt contains quotes
            if (paramName == "prompt" && _generateAudioCheckBox != null)
            {
                bool containsQuotes = textBox.Text.Contains("\"") || textBox.Text.Contains("'");
                if (containsQuotes && _generateAudioCheckBox.IsChecked == false)
                {
                    _generateAudioCheckBox.IsChecked = true;
                }
            }
        };

        _parameterValues[paramName] = textBox.Text;

        return textBox;
    }

    /// <summary>
    /// Create a number input control.
    /// </summary>
    private UIElement CreateNumberInput(string paramName, ParameterConfig config)
    {
        var stackPanel = new WpfStackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

        var textBox = new WpfTextBox
        {
            FontSize = 13,
            Padding = new Thickness(8),
            Width = 150,
            Text = config.@default?.ToString() ?? ""
        };

        textBox.TextChanged += (s, e) =>
        {
            var text = textBox.Text.Trim();

            // Special handling for "Random" - don't include in parameters (API will generate random seed)
            if (string.Equals(text, "Random", StringComparison.OrdinalIgnoreCase))
            {
                // Remove from parameters dictionary so it's not sent to API
                _parameterValues.Remove(paramName);
            }
            else if (double.TryParse(text, out var value))
            {
                _parameterValues[paramName] = (int)value; // Convert to int for seed
            }
            else if (string.IsNullOrWhiteSpace(text))
            {
                // Empty field - remove from parameters
                _parameterValues.Remove(paramName);
            }
        };

        // Initialize parameter value
        if (config.@default != null)
        {
            var defaultStr = config.@default.ToString();
            if (!string.Equals(defaultStr, "Random", StringComparison.OrdinalIgnoreCase))
            {
                _parameterValues[paramName] = config.@default;
            }
            // If default is "Random", don't add to parameters dictionary
        }

        stackPanel.Children.Add(textBox);

        // Add range info if available
        if (config.min.HasValue || config.max.HasValue)
        {
            var rangeText = new WpfTextBlock
            {
                Text = $" (Range: {config.min ?? 0} - {config.max ?? 999999})",
                FontSize = 11,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(96, 94, 92)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            stackPanel.Children.Add(rangeText);
        }

        return stackPanel;
    }

    /// <summary>
    /// Create a slider input control.
    /// </summary>
    private UIElement CreateSliderInput(string paramName, ParameterConfig config)
    {
        var stackPanel = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Parse default value safely (handles JsonElement from System.Text.Json)
        double defaultValue = 0;
        if (config.@default != null)
        {
            if (config.@default is System.Text.Json.JsonElement jsonElement)
            {
                defaultValue = jsonElement.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.Number => jsonElement.GetDouble(),
                    System.Text.Json.JsonValueKind.String => double.TryParse(jsonElement.GetString(), out var d) ? d : 0,
                    _ => 0
                };
            }
            else
            {
                defaultValue = Convert.ToDouble(config.@default);
            }
        }

        // Slider control - ALL sliders are integer-only (no decimals)
        var slider = new System.Windows.Controls.Slider
        {
            Minimum = config.min ?? 0,
            Maximum = config.max ?? 100,
            Value = Math.Round(defaultValue), // Round to nearest integer
            TickFrequency = 1, // Always 1 for integer steps
            IsSnapToTickEnabled = true,
            Width = 250,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };

        // Value label - shows current integer value
        var valueLabel = new WpfTextBlock
        {
            Text = ((int)slider.Value).ToString(),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 50,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(0, 120, 215)) // Blue color for visibility
        };

        // Update value label when slider changes
        slider.ValueChanged += (s, e) =>
        {
            int intValue = (int)Math.Round(slider.Value);
            valueLabel.Text = intValue.ToString();
            _parameterValues[paramName] = intValue; // Always store as integer
        };

        // Initialize parameter value as integer
        _parameterValues[paramName] = (int)Math.Round(slider.Value);

        stackPanel.Children.Add(slider);
        stackPanel.Children.Add(valueLabel);

        return stackPanel;
    }

    /// <summary>
    /// Create a dropdown input control.
    /// </summary>
    private UIElement CreateDropdownInput(string paramName, ParameterConfig config)
    {
        var comboBox = new WpfComboBox
        {
            FontSize = 13,
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 10),
            IsEnabled = !config.@readonly
        };

        if (config.options != null)
        {
            foreach (var option in config.options)
            {
                comboBox.Items.Add(option);
            }

            // Select default value
            if (config.@default != null)
            {
                var defaultValue = config.@default.ToString();
                comboBox.SelectedItem = defaultValue;
                _parameterValues[paramName] = defaultValue ?? "";
            }
        }

        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem != null)
            {
                _parameterValues[paramName] = comboBox.SelectedItem.ToString()!;
            }
        };

        return comboBox;
    }

    /// <summary>
    /// Create a checkbox input control.
    /// </summary>
    private UIElement CreateCheckboxInput(string paramName, ParameterConfig config)
    {
        var checkBox = new WpfCheckBox
        {
            Content = config.label,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 10),
            IsChecked = config.@default as bool? ?? false
        };

        // Store reference to generate_audio checkbox for auto-enable behavior
        if (paramName == "generate_audio" && config.auto_enable_on_quotes)
        {
            _generateAudioCheckBox = checkBox;
        }

        checkBox.Checked += (s, e) => _parameterValues[paramName] = true;
        checkBox.Unchecked += (s, e) => _parameterValues[paramName] = false;

        _parameterValues[paramName] = checkBox.IsChecked ?? false;

        return checkBox;
    }

    /// <summary>
    /// Handle browse button for first frame.
    /// </summary>
    private void BrowseFirstFrameButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Select First Frame Image",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.avif|All Files|*.*",
            InitialDirectory = _initialDirectory
        };

        if (dialog.ShowDialog() == true)
        {
            _firstImagePath = dialog.FileName;
            LoadImages();
            Log.Information("First frame changed to: {Path}", _firstImagePath);
        }
    }

    /// <summary>
    /// Handle browse button for last frame.
    /// </summary>
    private void BrowseLastFrameButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Select Last Frame Image",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.avif|All Files|*.*",
            InitialDirectory = _initialDirectory
        };

        if (dialog.ShowDialog() == true)
        {
            _lastImagePath = dialog.FileName;
            LoadImages();
            Log.Information("Last frame changed to: {Path}", _lastImagePath);
        }
    }

    /// <summary>
    /// Handle cancel button - close window without submitting.
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("FirstLastFrameConfigWindow: User cancelled");
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Handle submit button - validate and submit API request.
    /// </summary>
    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Information("FirstLastFrameConfigWindow: User clicked Submit");

        if (_currentModelConfig == null)
        {
            WpfMessageBox.Show("Please select a model.", "Validation Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        // Validate required parameters
        foreach (var param in _currentModelConfig.parameters)
        {
            if (param.Value.required && !_parameterValues.ContainsKey(param.Key))
            {
                WpfMessageBox.Show($"Required parameter '{param.Value.label}' is missing.",
                    "Validation Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
                return;
            }
        }

        Log.Information("Configuration validated successfully");
        Log.Information("Model: {ModelId}", _currentModelConfig.model_id);
        Log.Information("First Image: {FirstImage}", _firstImagePath);
        Log.Information("Last Image: {LastImage}", _lastImagePath);
        Log.Information("Parameters: {Parameters}", JsonSerializer.Serialize(_parameterValues));

        // Close window with success - feature will handle API submission
        DialogResult = true;
        Close();
    }

    #region Model Configuration Classes

    public class ModelConfig
    {
        public string model_id { get; set; } = "";
        public string display_name { get; set; } = "";
        public string description { get; set; } = "";
        public string pricing { get; set; } = "";
        public string processing_time { get; set; } = "";
        public Dictionary<string, ParameterConfig> parameters { get; set; } = new();
    }

    public class ParameterConfig
    {
        public string type { get; set; } = "";
        public string label { get; set; } = "";
        public string description { get; set; } = "";
        public bool required { get; set; }
        public object? @default { get; set; }
        public bool multiline { get; set; }
        public int rows { get; set; }
        public bool clear_on_focus { get; set; }
        public string[]? options { get; set; }
        public bool @readonly { get; set; }
        public double? min { get; set; }
        public double? max { get; set; }
        public double? step { get; set; }
        public bool auto_enable_on_quotes { get; set; }
    }

    #endregion
}

