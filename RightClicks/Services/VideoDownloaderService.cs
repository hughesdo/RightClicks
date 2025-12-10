using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using RightClicks.Models;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Service for downloading videos using yt-dlp.exe.
/// Supports YouTube, X/Twitter, and other platforms.
/// </summary>
public class VideoDownloaderService
{
    private readonly string _ytDlpPath;
    private VideoDownloaderSettings _settings;

    /// <summary>
    /// Platform regex patterns for URL detection.
    /// Key: Platform name, Value: Regex pattern
    /// </summary>
    public static readonly Dictionary<string, string> PlatformPatterns = new()
    {
        // YouTube: youtube.com/watch?v=xxx, youtu.be/xxx, m.youtube.com/watch?v=xxx, youtube.com/shorts/xxx
        { "YouTube", @"^https?:\/\/(www\.|m\.)?(youtube\.com\/(watch\?v=|shorts\/)[\w-]+|youtu\.be\/[\w-]+)" },
        // X/Twitter: x.com/user/status/xxx, twitter.com/user/status/xxx
        { "X", @"^https?:\/\/(x\.com|twitter\.com)\/\w+\/status\/\d+" },
        // TikTok: tiktok.com/@user/video/xxx, vm.tiktok.com/xxx
        { "TikTok", @"^https?:\/\/(www\.|vm\.)?tiktok\.com\/" },
        // Instagram: instagram.com/p/xxx, instagram.com/reel/xxx
        { "Instagram", @"^https?:\/\/(www\.)?instagram\.com\/(p|reel|tv)\/[\w-]+" },
        // Facebook: facebook.com/xxx/videos/xxx, fb.watch/xxx
        { "Facebook", @"^https?:\/\/(www\.|m\.)?(facebook\.com\/.*\/videos\/|fb\.watch\/)" },
        // Reddit: reddit.com/r/xxx/comments/xxx
        { "Reddit", @"^https?:\/\/(www\.|old\.)?reddit\.com\/r\/\w+\/comments\/" },
        // Vimeo: vimeo.com/xxx
        { "Vimeo", @"^https?:\/\/(www\.|player\.)?vimeo\.com\/\d+" },
        // Twitch: twitch.tv/videos/xxx, clips.twitch.tv/xxx
        { "Twitch", @"^https?:\/\/(www\.|clips\.)?twitch\.tv\/(videos\/\d+|\w+\/clip\/)" }
    };

    /// <summary>
    /// Event fired when a download starts.
    /// </summary>
    public event EventHandler<VideoDownloadEventArgs>? DownloadStarted;

    /// <summary>
    /// Event fired when a download completes.
    /// </summary>
    public event EventHandler<VideoDownloadEventArgs>? DownloadCompleted;

    /// <summary>
    /// Event fired when a download fails.
    /// </summary>
    public event EventHandler<VideoDownloadEventArgs>? DownloadFailed;

    public VideoDownloaderService(VideoDownloaderSettings settings)
    {
        _settings = settings;

        // Find yt-dlp.exe in the bin folder
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        _ytDlpPath = Path.Combine(appDir, "bin", "yt-dlp.exe");

        if (!File.Exists(_ytDlpPath))
        {
            // Try fallback location (same folder as app)
            _ytDlpPath = Path.Combine(appDir, "yt-dlp.exe");
        }

        if (File.Exists(_ytDlpPath))
        {
            Log.Information("VideoDownloaderService initialized. yt-dlp.exe found at: {Path}", _ytDlpPath);
        }
        else
        {
            Log.Warning("yt-dlp.exe not found. Video downloads will not work.");
        }
    }

    /// <summary>
    /// Update settings (called when user changes configuration).
    /// </summary>
    public void UpdateSettings(VideoDownloaderSettings settings)
    {
        _settings = settings;
        Log.Information("VideoDownloaderService settings updated. Enabled platforms: {Platforms}",
            string.Join(", ", settings.Platforms.Where(p => p.Value).Select(p => p.Key)));
    }

    /// <summary>
    /// Check if yt-dlp.exe is available.
    /// </summary>
    public bool IsYtDlpAvailable => File.Exists(_ytDlpPath);

    /// <summary>
    /// Detect which platform a URL belongs to.
    /// </summary>
    /// <returns>Platform name or null if not recognized.</returns>
    public string? DetectPlatform(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        foreach (var (platform, pattern) in PlatformPatterns)
        {
            if (Regex.IsMatch(url.Trim(), pattern, RegexOptions.IgnoreCase))
            {
                return platform;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a URL is a supported video URL and the platform is enabled.
    /// </summary>
    public bool IsEnabledVideoUrl(string url)
    {
        var platform = DetectPlatform(url);
        if (platform == null) return false;

        return _settings.Platforms.TryGetValue(platform, out var enabled) && enabled;
    }

    /// <summary>
    /// Get the download folder path for a platform.
    /// Format: %USERPROFILE%\Videos\YYYY-MM-DD_Platform
    /// </summary>
    public string GetDownloadFolder(string platform)
    {
        var basePath = Environment.ExpandEnvironmentVariables(_settings.DownloadPath);
        var dateFolder = DateTime.Now.ToString("yyyy-MM-dd") + "_" + platform;
        return Path.Combine(basePath, dateFolder);
    }

    /// <summary>
    /// Download a video from a URL.
    /// </summary>
    public async Task<bool> DownloadVideoAsync(string url, CancellationToken cancellationToken = default)
    {
        var platform = DetectPlatform(url);
        if (platform == null)
        {
            Log.Warning("URL not recognized as a supported video platform: {Url}", url);
            return false;
        }

        if (!IsYtDlpAvailable)
        {
            Log.Error("yt-dlp.exe not found. Cannot download video.");
            return false;
        }

        var downloadFolder = GetDownloadFolder(platform);
        Directory.CreateDirectory(downloadFolder);

        Log.Information("Starting video download from {Platform}: {Url}", platform, url);
        Log.Information("Download folder: {Folder}", downloadFolder);

        DownloadStarted?.Invoke(this, new VideoDownloadEventArgs(url, platform, downloadFolder));

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = $"\"{url}\"",
                WorkingDirectory = downloadFolder,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                Log.Information("Video download completed successfully from {Platform}", platform);
                DownloadCompleted?.Invoke(this, new VideoDownloadEventArgs(url, platform, downloadFolder));
                return true;
            }
            else
            {
                Log.Error("Video download failed. Exit code: {ExitCode}. Error: {Error}", 
                    process.ExitCode, error);
                DownloadFailed?.Invoke(this, new VideoDownloadEventArgs(url, platform, downloadFolder, error));
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception during video download from {Platform}: {Url}", platform, url);
            DownloadFailed?.Invoke(this, new VideoDownloadEventArgs(url, platform, downloadFolder, ex.Message));
            return false;
        }
    }
}

/// <summary>
/// Event args for video download events.
/// </summary>
public class VideoDownloadEventArgs : EventArgs
{
    public string Url { get; }
    public string Platform { get; }
    public string DownloadFolder { get; }
    public string? Error { get; }

    public VideoDownloadEventArgs(string url, string platform, string downloadFolder, string? error = null)
    {
        Url = url;
        Platform = platform;
        DownloadFolder = downloadFolder;
        Error = error;
    }
}

