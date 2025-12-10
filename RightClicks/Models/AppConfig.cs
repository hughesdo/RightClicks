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
    /// Cloudinary configuration for file hosting.
    /// </summary>
    public CloudinaryConfig Cloudinary { get; set; } = new();

    /// <summary>
    /// Application settings.
    /// </summary>
    public AppSettings Settings { get; set; } = new();

    /// <summary>
    /// Video downloader settings for clipboard monitoring.
    /// </summary>
    public VideoDownloaderSettings VideoDownloader { get; set; } = new();
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

/// <summary>
/// Cloudinary configuration for file hosting.
/// </summary>
public class CloudinaryConfig
{
    /// <summary>
    /// Cloudinary cloud name (e.g., "do15ttvsq").
    /// </summary>
    public string CloudName { get; set; } = "do15ttvsq";

    /// <summary>
    /// Environment variable name for Cloudinary API key.
    /// Default: "CLOUDINARY_API_KEY"
    /// </summary>
    public string ApiKeyEnvVar { get; set; } = "CLOUDINARY_API_KEY";

    /// <summary>
    /// Environment variable name for Cloudinary API secret.
    /// Default: "CLOUDINARY_API_SECRET"
    /// </summary>
    public string ApiSecretEnvVar { get; set; } = "CLOUDINARY_API_SECRET";
}

/// <summary>
/// Video downloader settings for clipboard monitoring and auto-download.
/// </summary>
public class VideoDownloaderSettings
{
    /// <summary>
    /// Whether clipboard monitoring is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Download destination folder.
    /// Default: %USERPROFILE%\Videos
    /// </summary>
    public string DownloadPath { get; set; } = "%USERPROFILE%\\Videos";

    /// <summary>
    /// Per-platform enable/disable settings.
    /// Key: Platform name (e.g., "YouTube", "X")
    /// Value: Whether downloads from this platform are enabled
    /// </summary>
    public Dictionary<string, bool> Platforms { get; set; } = new()
    {
        { "YouTube", true },
        { "X", true },
        { "TikTok", true },
        { "Instagram", true },
        { "Facebook", true },
        { "Reddit", true },
        { "Vimeo", true },
        { "Twitch", true }
    };
}

