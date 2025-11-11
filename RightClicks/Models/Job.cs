namespace RightClicks.Models;

/// <summary>
/// Represents a queued job for feature execution.
/// Jobs are displayed in the Queued Jobs tab and retained for 7 days.
/// </summary>
public class Job
{
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the feature to execute (e.g., "ExtractMp3").
    /// </summary>
    public string FeatureId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the feature (e.g., "Extract MP3").
    /// </summary>
    public string FeatureName { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the input file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the job.
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>
    /// When the job was created/queued.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// When the job started executing (null if not started yet).
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job completed (null if not completed yet).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Result message from feature execution.
    /// </summary>
    public string? ResultMessage { get; set; }

    /// <summary>
    /// Path to the output file created (if applicable).
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    /// Error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Cancellation token source for this job.
    /// Used to cancel the job if user clicks Cancel button.
    /// </summary>
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// Duration of job execution in milliseconds.
    /// </summary>
    public long DurationMs
    {
        get
        {
            if (StartedAt.HasValue && CompletedAt.HasValue)
            {
                return (long)(CompletedAt.Value - StartedAt.Value).TotalMilliseconds;
            }
            return 0;
        }
    }
}

/// <summary>
/// Status of a job in the queue.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is waiting to be executed.
    /// </summary>
    Pending,

    /// <summary>
    /// Job is currently being executed.
    /// </summary>
    Running,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Job was cancelled by the user.
    /// </summary>
    Cancelled
}

