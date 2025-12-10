using Serilog;
using WpfClipboard = System.Windows.Clipboard;

namespace RightClicks.Services;

/// <summary>
/// Service for monitoring clipboard changes and detecting video URLs.
/// Polls the clipboard periodically (like the legacy app) to detect changes.
/// </summary>
public class ClipboardMonitorService : IDisposable
{
    private readonly VideoDownloaderService _videoDownloader;
    private readonly System.Threading.Timer _clipboardTimer;
    private string _previousClipboardText = string.Empty;
    private readonly HashSet<string> _downloadedUrls = new(); // Track downloaded URLs to avoid duplicates
    private bool _isEnabled = true;
    private bool _isDisposed = false;

    /// <summary>
    /// Polling interval in milliseconds.
    /// Default: 1000ms (1 second) - matches legacy app.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Event fired when a video URL is detected in the clipboard.
    /// </summary>
    public event EventHandler<string>? VideoUrlDetected;

    /// <summary>
    /// Event fired when a video download is triggered.
    /// </summary>
    public event EventHandler<VideoDownloadEventArgs>? DownloadTriggered;

    public ClipboardMonitorService(VideoDownloaderService videoDownloader, bool enabled = true)
    {
        _videoDownloader = videoDownloader;
        _isEnabled = enabled;

        // Subscribe to download events for logging
        _videoDownloader.DownloadStarted += (s, e) => 
            Log.Information("Download started: {Platform} - {Url}", e.Platform, e.Url);
        _videoDownloader.DownloadCompleted += (s, e) => 
            Log.Information("Download completed: {Platform} - saved to {Folder}", e.Platform, e.DownloadFolder);
        _videoDownloader.DownloadFailed += (s, e) => 
            Log.Error("Download failed: {Platform} - {Error}", e.Platform, e.Error);

        // Initialize the clipboard with current content
        try
        {
            if (WpfClipboard.ContainsText())
            {
                _previousClipboardText = WpfClipboard.GetText();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read initial clipboard content");
        }

        // Start polling timer
        _clipboardTimer = new System.Threading.Timer(
            callback: _ => CheckClipboard(),
            state: null,
            dueTime: TimeSpan.FromMilliseconds(PollingIntervalMs),
            period: TimeSpan.FromMilliseconds(PollingIntervalMs)
        );

        Log.Information("ClipboardMonitorService initialized. Monitoring enabled: {Enabled}", _isEnabled);
    }

    /// <summary>
    /// Enable or disable clipboard monitoring.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            Log.Information("Clipboard monitoring {Status}", value ? "enabled" : "disabled");
        }
    }

    /// <summary>
    /// Set enabled state (convenience method for UI binding).
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    /// <summary>
    /// Check clipboard for video URLs.
    /// Called periodically by the timer.
    /// </summary>
    private void CheckClipboard()
    {
        if (!_isEnabled || _isDisposed) return;

        try
        {
            // Must run on STA thread for clipboard access
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (!WpfClipboard.ContainsText()) return;

                var currentText = WpfClipboard.GetText()?.Trim() ?? string.Empty;

                // Check if clipboard changed
                if (currentText == _previousClipboardText) return;

                _previousClipboardText = currentText;
                Log.Debug("Clipboard changed: {Text}", 
                    currentText.Length > 100 ? currentText.Substring(0, 100) + "..." : currentText);

                // Check if it's a supported video URL
                if (!_videoDownloader.IsEnabledVideoUrl(currentText)) return;

                // Check if we've already downloaded this URL
                if (_downloadedUrls.Contains(currentText))
                {
                    Log.Debug("URL already downloaded, skipping: {Url}", currentText);
                    return;
                }

                var platform = _videoDownloader.DetectPlatform(currentText);
                Log.Information("Video URL detected from {Platform}: {Url}", platform, currentText);

                VideoUrlDetected?.Invoke(this, currentText);

                // Add to downloaded set before starting download
                _downloadedUrls.Add(currentText);

                // Trigger download asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var folder = _videoDownloader.GetDownloadFolder(platform!);
                        DownloadTriggered?.Invoke(this, new VideoDownloadEventArgs(currentText, platform!, folder));

                        var success = await _videoDownloader.DownloadVideoAsync(currentText);
                        if (!success)
                        {
                            // Remove from downloaded set if failed so user can retry
                            _downloadedUrls.Remove(currentText);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error downloading video: {Url}", currentText);
                        _downloadedUrls.Remove(currentText);
                    }
                });
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error checking clipboard");
        }
    }

    /// <summary>
    /// Clear the list of downloaded URLs (allows re-downloading).
    /// </summary>
    public void ClearDownloadHistory()
    {
        _downloadedUrls.Clear();
        Log.Information("Download history cleared");
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _clipboardTimer.Dispose();
        Log.Information("ClipboardMonitorService disposed");
    }
}

