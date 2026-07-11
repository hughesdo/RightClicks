using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Service for loading and managing First+Last Frame model configurations.
/// Reads JSON config files from the FirstLastFrame folder.
/// </summary>
public static class FirstLastFrameConfigService
{
    private static readonly string ConfigFolder;
    private static List<FirstLastFrameModelConfig>? _cachedConfigs;

    static FirstLastFrameConfigService()
    {
        // Config folder is in the application directory
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        ConfigFolder = Path.Combine(appDir, "FirstLastFrame");
        Log.Debug("FirstLastFrame config folder: {ConfigFolder}", ConfigFolder);
    }

    /// <summary>
    /// Gets all available First+Last Frame model configurations.
    /// </summary>
    public static List<FirstLastFrameModelConfig> GetAllConfigs()
    {
        if (_cachedConfigs != null)
        {
            return _cachedConfigs;
        }

        _cachedConfigs = LoadAllConfigs();
        return _cachedConfigs;
    }

    /// <summary>
    /// Gets a specific model configuration by model ID.
    /// </summary>
    public static FirstLastFrameModelConfig? GetConfig(string modelId)
    {
        return GetAllConfigs().FirstOrDefault(c => c.ModelId == modelId);
    }

    /// <summary>
    /// Clears the cached configurations, forcing a reload on next access.
    /// </summary>
    public static void ClearCache()
    {
        _cachedConfigs = null;
    }

    private static List<FirstLastFrameModelConfig> LoadAllConfigs()
    {
        var configs = new List<FirstLastFrameModelConfig>();

        if (!Directory.Exists(ConfigFolder))
        {
            Log.Warning("FirstLastFrame config folder not found: {ConfigFolder}", ConfigFolder);
            return configs;
        }

        var jsonFiles = Directory.GetFiles(ConfigFolder, "*.json");
        Log.Information("Found {Count} First+Last Frame config files", jsonFiles.Length);

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var config = JsonSerializer.Deserialize<FirstLastFrameModelConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config != null)
                {
                    configs.Add(config);
                    Log.Debug("Loaded config: {DisplayName} ({ModelId})", config.DisplayName, config.ModelId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load config file: {File}", file);
            }
        }

        // Sort by display name
        configs = configs.OrderBy(c => c.DisplayName).ToList();
        Log.Information("Loaded {Count} First+Last Frame model configurations", configs.Count);

        return configs;
    }
}

/// <summary>
/// Configuration for a First+Last Frame model.
/// </summary>
public class FirstLastFrameModelConfig
{
    [JsonPropertyName("model_id")]
    public string ModelId { get; set; } = "";

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("pricing")]
    public string Pricing { get; set; } = "";

    [JsonPropertyName("processing_time")]
    public string ProcessingTime { get; set; } = "";

    [JsonPropertyName("image_param_names")]
    public ImageParamNames ImageParamNames { get; set; } = new();

    [JsonPropertyName("parameters")]
    public Dictionary<string, ParameterConfig> Parameters { get; set; } = new();
}

/// <summary>
/// Names of the image URL parameters for the API.
/// </summary>
public class ImageParamNames
{
    [JsonPropertyName("start")]
    public string Start { get; set; } = "start_image_url";

    [JsonPropertyName("end")]
    public string End { get; set; } = "end_image_url";
}

/// <summary>
/// Configuration for a single parameter.
/// </summary>
public class ParameterConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    [JsonPropertyName("min")]
    public int? Min { get; set; }

    [JsonPropertyName("max")]
    public int? Max { get; set; }

    [JsonPropertyName("step")]
    public int? Step { get; set; }

    [JsonPropertyName("multiline")]
    public bool Multiline { get; set; }

    [JsonPropertyName("rows")]
    public int Rows { get; set; } = 1;

    [JsonPropertyName("clear_on_focus")]
    public bool ClearOnFocus { get; set; }
}

