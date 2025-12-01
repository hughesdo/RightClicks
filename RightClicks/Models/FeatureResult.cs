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
    /// Whether to suppress notification for this result.
    /// Used for informational results that don't represent actual job completion.
    /// Example: First click in a two-click workflow (muxing feature).
    /// </summary>
    public bool SuppressNotification { get; set; }

    /// <summary>
    /// Whether this result represents an informational state change that should not create a job.
    /// Used for multi-step workflows where only the final step should create a job.
    /// Example: First click in muxing feature (just stores state, no job needed).
    /// When true, the feature execution completes immediately without creating a job entry.
    /// </summary>
    public bool IsInformational { get; set; }

    /// <summary>
    /// Create a successful result.
    /// </summary>
    public static FeatureResult CreateSuccess(string message, string? outputFilePath = null, long durationMs = 0, bool suppressNotification = false)
    {
        return new FeatureResult
        {
            Success = true,
            Message = message,
            OutputFilePath = outputFilePath,
            DurationMs = durationMs,
            SuppressNotification = suppressNotification
        };
    }

    /// <summary>
    /// Create an informational result (no job created, no notification).
    /// Used for multi-step workflows where this is just a state change, not actual work.
    /// </summary>
    public static FeatureResult CreateInformational(string message, long durationMs = 0)
    {
        return new FeatureResult
        {
            Success = true,
            Message = message,
            OutputFilePath = null,
            DurationMs = durationMs,
            SuppressNotification = true,
            IsInformational = true
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

