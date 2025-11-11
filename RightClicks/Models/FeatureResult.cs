namespace RightClicks.Models;

/// <summary>
/// Result returned by feature execution.
/// Contains success/failure status and details.
/// </summary>
public class FeatureResult
{
    /// <summary>
    /// Whether the feature executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Human-readable message describing the result.
    /// For success: "Successfully extracted MP3 from video.mp4"
    /// For failure: "Failed to extract MP3: FFmpeg not found"
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Path to the output file created by the feature (if applicable).
    /// Example: "C:\Users\Don\Videos\vacation.mp3"
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    /// Exception that occurred during execution (if any).
    /// Used for detailed error logging.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Duration of feature execution in milliseconds.
    /// Used for performance monitoring and logging.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Create a successful result.
    /// </summary>
    public static FeatureResult CreateSuccess(string message, string? outputFilePath = null, long durationMs = 0)
    {
        return new FeatureResult
        {
            Success = true,
            Message = message,
            OutputFilePath = outputFilePath,
            DurationMs = durationMs
        };
    }

    /// <summary>
    /// Create a failure result.
    /// </summary>
    public static FeatureResult CreateFailure(string message, Exception? exception = null, long durationMs = 0)
    {
        return new FeatureResult
        {
            Success = false,
            Message = message,
            Exception = exception,
            DurationMs = durationMs
        };
    }
}

