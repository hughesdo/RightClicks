namespace RightClicks.Models;

/// <summary>
/// Application configuration loaded from config.json.
/// Shared between main app and shell hook manager.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Application version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// List of features and their enabled/disabled state.
    /// </summary>
    public List<FeatureConfig> Features { get; set; } = new();

    /// <summary>
    /// API key configuration (environment variable names, not actual keys).
    /// Key: API name (e.g., "openAI")
    /// Value: Environment variable name (e.g., "OPENAI_API_KEY")
    /// </summary>
    public Dictionary<string, string> ApiKeys { get; set; } = new();

    /// <summary>
    /// Application settings.
    /// </summary>
    public AppSettings Settings { get; set; } = new();
}

/// <summary>
/// Application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Logging level: Debug, Info, Warning, Error.
    /// </summary>
    public string LogLevel { get; set; } = "Info";

    /// <summary>
    /// Maximum number of jobs that can run simultaneously.
    /// Default: 3
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 3;

    /// <summary>
    /// Number of days to retain job history.
    /// Default: 7 days
    /// </summary>
    public int JobHistoryDays { get; set; } = 7;

    /// <summary>
    /// Number of days to retain log files.
    /// Default: 7 days
    /// </summary>
    public int LogRetentionDays { get; set; } = 7;

    /// <summary>
    /// Whether to check for updates on startup.
    /// Default: true
    /// </summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>
    /// Output path for processed files (optional).
    /// If null/empty, files are saved in the same directory as the source file.
    /// Supports environment variables like %USERPROFILE%.
    /// </summary>
    public string? OutputPath { get; set; }
}

