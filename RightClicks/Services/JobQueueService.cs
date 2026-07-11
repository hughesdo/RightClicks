using RightClicks.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.IO;

namespace RightClicks.Services;

/// <summary>
/// Service for managing job queue with concurrent execution.
/// Handles job lifecycle: Pending → Running → Completed/Failed/Cancelled
/// </summary>
public class JobQueueService
{
    private readonly ObservableCollection<Job> _jobs = new();
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly ConcurrentQueue<Job> _pendingJobs = new();
    private readonly object _lock = new();
    private readonly System.Threading.Timer _cleanupTimer;
    private readonly System.Threading.Timer _processingTimer;
    private int _maxConcurrentJobs;
    private bool _isProcessing = false;

    /// <summary>
    /// Event fired when a job's status changes.
    /// </summary>
    public event EventHandler<Job>? JobStatusChanged;

    /// <summary>
    /// Event fired when a job is added to the queue.
    /// </summary>
    public event EventHandler<Job>? JobAdded;

    /// <summary>
    /// Event fired when a job is removed from the queue.
    /// </summary>
    public event EventHandler<Job>? JobRemoved;

    /// <summary>
    /// Observable collection of all jobs (for UI binding).
    /// </summary>
    public ObservableCollection<Job> Jobs => _jobs;

    public JobQueueService(int maxConcurrentJobs = 3)
    {
        _maxConcurrentJobs = maxConcurrentJobs;
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrentJobs, maxConcurrentJobs);

        Log.Information("JobQueueService initialized with max concurrent jobs: {MaxJobs}", maxConcurrentJobs);

        // Setup cleanup timer - runs every hour to remove old jobs
        _cleanupTimer = new System.Threading.Timer(
            callback: _ => CleanupOldJobs(),
            state: null,
            dueTime: TimeSpan.FromMinutes(5), // First run after 5 minutes
            period: TimeSpan.FromHours(1)     // Then every hour
        );

        // Setup processing timer - checks for pending jobs every 500ms
        _processingTimer = new System.Threading.Timer(
            callback: _ => ProcessPendingJobs(),
            state: null,
            dueTime: TimeSpan.FromMilliseconds(500),
            period: TimeSpan.FromMilliseconds(500)
        );
    }

    /// <summary>
    /// Update the maximum concurrent jobs setting.
    /// </summary>
    public void UpdateMaxConcurrentJobs(int maxJobs)
    {
        if (maxJobs < 1 || maxJobs > 10)
        {
            Log.Warning("Invalid max concurrent jobs value: {MaxJobs}. Must be between 1 and 10.", maxJobs);
            return;
        }

        lock (_lock)
        {
            _maxConcurrentJobs = maxJobs;
            Log.Information("Max concurrent jobs updated to: {MaxJobs}", maxJobs);
        }
    }

    /// <summary>
    /// Add a job to the queue.
    /// </summary>
    public void AddJob(Job job)
    {
        lock (_lock)
        {
            _jobs.Add(job);
            _pendingJobs.Enqueue(job);

            Log.Information("Job added to queue: {JobId} - {FeatureName} on {FileName}",
                job.Id, job.FeatureName, Path.GetFileName(job.FilePath));

            JobAdded?.Invoke(this, job);
        }

        // Trigger processing
        ProcessPendingJobs();
    }

    /// <summary>
    /// Cancel a running job.
    /// </summary>
    public bool CancelJob(Guid jobId)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == jobId);
            if (job == null)
            {
                Log.Warning("Cannot cancel job {JobId}: Job not found", jobId);
                return false;
            }

            if (job.Status != JobStatus.Running)
            {
                Log.Warning("Cannot cancel job {JobId}: Job is not running (Status: {Status})", jobId, job.Status);
                return false;
            }

            // Request cancellation
            job.CancellationTokenSource?.Cancel();
            Log.Information("Cancellation requested for job: {JobId} - {FeatureName}", job.Id, job.FeatureName);

            return true;
        }
    }

    /// <summary>
    /// Remove a pending job from the queue before it starts.
    /// </summary>
    public bool RemoveJob(Guid jobId)
    {
        lock (_lock)
        {
            var job = _jobs.FirstOrDefault(j => j.Id == jobId);
            if (job == null)
            {
                Log.Warning("Cannot remove job {JobId}: Job not found", jobId);
                return false;
            }

            if (job.Status != JobStatus.Pending)
            {
                Log.Warning("Cannot remove job {JobId}: Job is not pending (Status: {Status})", jobId, job.Status);
                return false;
            }

            _jobs.Remove(job);
            Log.Information("Job removed from queue: {JobId} - {FeatureName}", job.Id, job.FeatureName);

            JobRemoved?.Invoke(this, job);
            return true;
        }
    }

    /// <summary>
    /// Clear all completed, failed, and cancelled jobs.
    /// </summary>
    public void ClearCompleted()
    {
        List<Job> completedJobs;

        lock (_lock)
        {
            completedJobs = _jobs
                .Where(j => j.Status == JobStatus.Completed ||
                           j.Status == JobStatus.Failed ||
                           j.Status == JobStatus.Cancelled)
                .ToList();

            foreach (var job in completedJobs)
            {
                _jobs.Remove(job);
            }

            Log.Information("Cleared {Count} completed/failed/cancelled jobs", completedJobs.Count);
        }

        // Trigger JobRemoved event for each cleared job (outside lock to avoid deadlock)
        foreach (var job in completedJobs)
        {
            JobRemoved?.Invoke(this, job);
        }
    }

    /// <summary>
    /// Get all jobs (for display purposes).
    /// </summary>
    public List<Job> GetAllJobs()
    {
        lock (_lock)
        {
            return _jobs.ToList();
        }
    }

    /// <summary>
    /// Remove jobs older than 7 days.
    /// </summary>
    private void CleanupOldJobs()
    {
        try
        {
            lock (_lock)
            {
                var cutoffDate = DateTime.Now.AddDays(-7);
                var oldJobs = _jobs
                    .Where(j => j.CreatedAt < cutoffDate &&
                               (j.Status == JobStatus.Completed ||
                                j.Status == JobStatus.Failed ||
                                j.Status == JobStatus.Cancelled))
                    .ToList();

                foreach (var job in oldJobs)
                {
                    _jobs.Remove(job);
                }

                if (oldJobs.Count > 0)
                {
                    Log.Information("Cleaned up {Count} old jobs (older than 7 days)", oldJobs.Count);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during job cleanup");
        }
    }

    /// <summary>
    /// Process pending jobs respecting concurrency limit.
    /// </summary>
    private void ProcessPendingJobs()
    {
        // Prevent multiple simultaneous processing attempts
        if (_isProcessing) return;

        try
        {
            _isProcessing = true;

            // Try to start jobs up to the concurrency limit
            while (_concurrencySemaphore.CurrentCount > 0 && _pendingJobs.TryDequeue(out var job))
            {
                // Double-check job is still pending (might have been removed)
                lock (_lock)
                {
                    if (!_jobs.Contains(job) || job.Status != JobStatus.Pending)
                    {
                        continue; // Skip this job
                    }
                }

                // Start the job
                _ = ExecuteJobAsync(job);
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// Execute a job asynchronously.
    /// </summary>
    private async Task ExecuteJobAsync(Job job)
    {
        // Wait for a slot in the semaphore
        await _concurrencySemaphore.WaitAsync();

        try
        {
            // Update job status to Running
            lock (_lock)
            {
                job.Status = JobStatus.Running;
                job.StartedAt = DateTime.Now;
                job.CancellationTokenSource = new CancellationTokenSource();
            }

            Log.Information("Job started: {JobId} - {FeatureName} on {FileName}",
                job.Id, job.FeatureName, Path.GetFileName(job.FilePath));

            JobStatusChanged?.Invoke(this, job);

            // Get the feature instance
            var feature = FeatureDiscoveryService.GetFeatures()
                .FirstOrDefault(f => f.Id == job.FeatureId);

            if (feature == null)
            {
                throw new InvalidOperationException($"Feature not found: {job.FeatureId}");
            }

            // Execute the feature. Configurable features already prompted the user before this job
            // was queued, so hand them the settings that were collected then rather than asking again.
            var result = feature is IConfigurableFeature configurable
                ? await configurable.ExecuteAsync(job.FilePath, job.Configuration, job.CancellationTokenSource.Token)
                : await feature.ExecuteAsync(job.FilePath, job.CancellationTokenSource.Token);

            // Check if this is an informational result (e.g., first click in two-click workflow)
            // If so, remove the job from the queue without notification
            if (result.IsInformational)
            {
                lock (_lock)
                {
                    _jobs.Remove(job);
                    Log.Information("Job removed (informational result, no actual work): {JobId} - {FeatureName} - {Message}",
                        job.Id, job.FeatureName, result.Message);
                }

                // Trigger JobRemoved event so UI updates
                JobRemoved?.Invoke(this, job);
                return;
            }

            // Update job with result
            lock (_lock)
            {
                job.CompletedAt = DateTime.Now;
                job.ResultMessage = result.Message;
                job.OutputFilePath = result.OutputFilePath;
                job.SuppressNotification = result.SuppressNotification;

                if (result.Success)
                {
                    job.Status = JobStatus.Completed;

                    if (result.SuppressNotification)
                    {
                        Log.Information("Job completed (notification suppressed): {JobId} - {FeatureName} - {Message}",
                            job.Id, job.FeatureName, result.Message);
                    }
                    else
                    {
                        Log.Information("Job completed successfully: {JobId} - {FeatureName} (Duration: {Duration}ms)",
                            job.Id, job.FeatureName, job.DurationMs);
                    }
                }
                else
                {
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = result.Message;
                    Log.Error("Job failed: {JobId} - {FeatureName} - {Error}",
                        job.Id, job.FeatureName, result.Message);
                }
            }

            JobStatusChanged?.Invoke(this, job);
        }
        catch (OperationCanceledException)
        {
            // Job was cancelled
            lock (_lock)
            {
                job.Status = JobStatus.Cancelled;
                job.CompletedAt = DateTime.Now;
                job.ErrorMessage = "Job was cancelled by user";
            }

            Log.Information("Job cancelled: {JobId} - {FeatureName}", job.Id, job.FeatureName);
            JobStatusChanged?.Invoke(this, job);
        }
        catch (Exception ex)
        {
            // Unexpected error
            lock (_lock)
            {
                job.Status = JobStatus.Failed;
                job.CompletedAt = DateTime.Now;
                job.ErrorMessage = $"Unexpected error: {ex.Message}";
            }

            Log.Error(ex, "Job failed with exception: {JobId} - {FeatureName}", job.Id, job.FeatureName);
            JobStatusChanged?.Invoke(this, job);
        }
        finally
        {
            // Release the semaphore slot
            _concurrencySemaphore.Release();

            // Dispose cancellation token source
            lock (_lock)
            {
                job.CancellationTokenSource?.Dispose();
                job.CancellationTokenSource = null;
            }
        }
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _processingTimer?.Dispose();
        _concurrencySemaphore?.Dispose();

        // Cancel all running jobs
        lock (_lock)
        {
            foreach (var job in _jobs.Where(j => j.Status == JobStatus.Running))
            {
                job.CancellationTokenSource?.Cancel();
            }
        }

        Log.Information("JobQueueService disposed");
    }
}
