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
    /// Whether the feature is enabled.
    /// Disabled features are not shown in context menus.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

