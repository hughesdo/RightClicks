namespace RightClicks.Models;

/// <summary>
/// A feature that needs to collect settings from the user before it can run.
///
/// The queue must not see the job until the user has actually submitted the dialog, so
/// <see cref="Configure"/> is called first, on the UI thread, and the job is only created if it
/// returns a configuration. Cancelling the dialog leaves nothing behind in the queue.
///
/// The resulting configuration rides along on the Job rather than being stashed on the feature
/// instance, because feature instances are shared singletons and several jobs can run at once.
/// </summary>
public interface IConfigurableFeature : IFileFeature
{
    /// <summary>
    /// Prompt the user for settings. Called on the UI thread before the job is queued.
    /// </summary>
    /// <param name="filePath">Full path to the file the user selected.</param>
    /// <returns>The configuration to run with, or null if the user cancelled.</returns>
    object? Configure(string filePath);

    /// <summary>
    /// Execute using the configuration collected by <see cref="Configure"/>.
    /// </summary>
    Task<FeatureResult> ExecuteAsync(string filePath, object? configuration, CancellationToken cancellationToken);
}
