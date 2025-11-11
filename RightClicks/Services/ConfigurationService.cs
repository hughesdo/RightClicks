using Newtonsoft.Json;
using RightClicks.Models;
using Serilog;
using System.IO;

namespace RightClicks.Services;

/// <summary>
/// Service for loading and saving application configuration.
/// Handles JSON config file read/write and environment variable resolution.
/// </summary>
public static class ConfigurationService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightClicks"
    );

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

    private static AppConfig? _cachedConfig;

    /// <summary>
    /// Load configuration from config.json.
    /// Creates default config if file doesn't exist.
    /// </summary>
    public static AppConfig LoadConfig()
    {
        try
        {
            // Create directory if it doesn't exist
            Directory.CreateDirectory(ConfigDirectory);

            // If config file doesn't exist, create default
            if (!File.Exists(ConfigFilePath))
            {
                Log.Information("Config file not found, creating default: {ConfigPath}", ConfigFilePath);
                var defaultConfig = CreateDefaultConfig();
                SaveConfig(defaultConfig);
                _cachedConfig = defaultConfig;
                return defaultConfig;
            }

            // Read and parse config file
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonConvert.DeserializeObject<AppConfig>(json);

            if (config == null)
            {
                Log.Warning("Failed to deserialize config, using default");
                config = CreateDefaultConfig();
            }

            _cachedConfig = config;
            Log.Information("Configuration loaded from: {ConfigPath}", ConfigFilePath);
            Log.Debug("Config: {FeatureCount} features, LogLevel={LogLevel}, MaxConcurrentJobs={MaxJobs}",
                config.Features.Count, config.Settings.LogLevel, config.Settings.MaxConcurrentJobs);

            return config;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load configuration from {ConfigPath}", ConfigFilePath);
            Log.Warning("Using default configuration");
            var defaultConfig = CreateDefaultConfig();
            _cachedConfig = defaultConfig;
            return defaultConfig;
        }
    }

    /// <summary>
    /// Save configuration to config.json.
    /// </summary>
    public static void SaveConfig(AppConfig config)
    {
        try
        {
            // Create directory if it doesn't exist
            Directory.CreateDirectory(ConfigDirectory);

            // Serialize with indentation for readability
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);

            // Write to file
            File.WriteAllText(ConfigFilePath, json);

            _cachedConfig = config;
            Log.Information("Configuration saved to: {ConfigPath}", ConfigFilePath);
            Log.Debug("Saved config: {FeatureCount} features", config.Features.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save configuration to {ConfigPath}", ConfigFilePath);
            throw;
        }
    }

    /// <summary>
    /// Get the cached configuration without reloading from disk.
    /// Returns null if config hasn't been loaded yet.
    /// </summary>
    public static AppConfig? GetCachedConfig()
    {
        return _cachedConfig;
    }

    /// <summary>
    /// Resolve an API key from environment variables.
    /// </summary>
    /// <param name="envVarName">Name of the environment variable to read.</param>
    /// <returns>The API key value, or null if not found.</returns>
    public static string? ResolveApiKey(string envVarName)
    {
        try
        {
            var value = Environment.GetEnvironmentVariable(envVarName);
            
            if (string.IsNullOrWhiteSpace(value))
            {
                Log.Warning("Environment variable not found or empty: {EnvVarName}", envVarName);
                return null;
            }

            Log.Debug("Resolved API key from environment variable: {EnvVarName}", envVarName);
            return value;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to resolve environment variable: {EnvVarName}", envVarName);
            return null;
        }
    }

    /// <summary>
    /// Create default configuration with all features enabled.
    /// </summary>
    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            Version = "1.0.0",
            Features = new List<FeatureConfig>
            {
                new() { Id = "ExtractMp3", Enabled = true },
                new() { Id = "ExtractWav", Enabled = true },
                new() { Id = "LastFrameToJpg", Enabled = true },
                new() { Id = "FirstFrameToJpg", Enabled = true }
            },
            ApiKeys = new Dictionary<string, string>
            {
                { "openAI", "OPENAI_API_KEY" }
            },
            Settings = new AppSettings
            {
                LogLevel = "Info",
                MaxConcurrentJobs = 3,
                JobHistoryDays = 7,
                LogRetentionDays = 7,
                CheckForUpdates = true,
                OutputPath = null // null means output next to source file
            }
        };
    }
}

