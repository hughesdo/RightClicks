namespace RightClicks.Models;

/// <summary>
/// Interface that all file processing features must implement.
/// Features are automatically discovered via reflection at startup.
/// </summary>
public interface IFileFeature
{
    /// <summary>
    /// Unique identifier for the feature (e.g., "ExtractMp3").
    /// Must match the ID in config.json.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Display name shown in UI and context menus (e.g., "Extract MP3").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of what the feature does (shown in UI as help text).
    /// </summary>
    string Description { get; }

    /// <summary>
    /// File extensions this feature supports (e.g., [".mp4", ".avi", ".mkv"]).
    /// Used to determine which context menu items to show for each file type.
    /// </summary>
    string[] SupportedExtensions { get; }

    /// <summary>
    /// Execute the feature on the specified file.
    /// </summary>
    /// <param name="filePath">Full path to the file to process.</param>
    /// <param name="cancellationToken">Token to support job cancellation.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken);
}

