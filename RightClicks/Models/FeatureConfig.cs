namespace RightClicks.Models;

/// <summary>
/// Configuration for a single feature.
/// Stored in config.json features array.
/// </summary>
public class FeatureConfig
{
    /// <summary>
    /// Unique identifier for the feature (e.g., "ExtractMp3").
    /// Must match the IFileFeature.Id property.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown in context menu (e.g., "Extract MP3").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the feature does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// File extensions this feature supports (e.g., [".mp4", ".avi", ".mkv"]).
    /// </summary>
    public string[] SupportedExtensions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether the feature is enabled.
    /// Disabled features are not shown in context menus.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

