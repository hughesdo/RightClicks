using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Keeps yt-dlp.exe up to date. yt-dlp becomes outdated frequently (roughly monthly) as
/// video sites change, so we check for updates at startup before the app becomes available.
///
/// NOTE: We deliberately do NOT use yt-dlp's built-in updater (<c>yt-dlp --update</c>).
/// Its embedded Python uses its own certifi CA bundle, which does not trust TLS-inspecting
/// antivirus/proxy roots (e.g. Avast Web Shield re-signs HTTPS with its own root cert that
/// only exists in the Windows cert store). That makes --update fail permanently with
/// CERTIFICATE_VERIFY_FAILED on such machines. Instead we download via HttpClient, which
/// uses the Windows TLS stack (Schannel) and therefore trusts the same roots the OS does.
///
/// The check runs during startup (before the tray becomes available). If it fails for any
/// reason (no network, download error, timeout), the error is logged and startup continues
/// with the existing yt-dlp.exe.
/// </summary>
public class YtDlpUpdateService
{
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
    private const string DownloadUrlTemplate = "https://github.com/yt-dlp/yt-dlp/releases/download/{0}/yt-dlp.exe";

    /// <summary>
    /// Sanity floor for a downloaded yt-dlp.exe. Real builds are ~15-20 MB; anything smaller
    /// is a truncated download or an error page and must not replace the working binary.
    /// </summary>
    private const long MinValidExeBytes = 5 * 1024 * 1024;

    /// <summary>Maximum time for the version check (local --version + GitHub API call).</summary>
    private static readonly TimeSpan VersionCheckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Maximum time for downloading the new executable (~15-20 MB).</summary>
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Resolve the path to yt-dlp.exe using the same lookup order as
    /// <see cref="VideoDownloaderService"/>: the bin subfolder first, then the app folder.
    /// Returns null if the executable cannot be found.
    /// </summary>
    public static string? ResolveYtDlpPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;

        var binPath = Path.Combine(appDir, "bin", "yt-dlp.exe");
        if (File.Exists(binPath))
        {
            return binPath;
        }

        var appPath = Path.Combine(appDir, "yt-dlp.exe");
        if (File.Exists(appPath))
        {
            return appPath;
        }

        return null;
    }

    /// <summary>
    /// Check whether the installed yt-dlp.exe is current and, if not, download and install the
    /// latest release. Never throws - all failures are logged and swallowed so that startup can
    /// proceed with whatever version is already installed.
    /// </summary>
    public async Task CheckAndUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var ytDlpPath = ResolveYtDlpPath();
            if (ytDlpPath == null)
            {
                Log.Warning("yt-dlp.exe not found - skipping update check.");
                return;
            }

            Log.Information("Checking for yt-dlp updates: {Path}", ytDlpPath);

            using var checkCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            checkCts.CancelAfter(VersionCheckTimeout);

            var installedVersion = await GetInstalledVersionAsync(ytDlpPath, checkCts.Token).ConfigureAwait(false);
            if (installedVersion == null)
            {
                Log.Warning("Could not determine installed yt-dlp version - skipping update check.");
                return;
            }

            var latestVersion = await GetLatestVersionAsync(checkCts.Token).ConfigureAwait(false);
            if (latestVersion == null)
            {
                Log.Warning("Could not determine latest yt-dlp version from GitHub - continuing with existing version {Installed}.",
                    installedVersion);
                return;
            }

            if (string.Equals(installedVersion, latestVersion, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("yt-dlp is up to date (version {Version}).", installedVersion);
                return;
            }

            Log.Information("yt-dlp is outdated (installed: {Installed}, latest: {Latest}) - downloading update...",
                installedVersion, latestVersion);

            using var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            downloadCts.CancelAfter(DownloadTimeout);

            await DownloadAndReplaceAsync(ytDlpPath, latestVersion, downloadCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Warning("yt-dlp update check timed out - continuing with existing version.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check for yt-dlp updates - continuing with existing version.");
        }
    }

    /// <summary>
    /// Get the installed yt-dlp version by running "yt-dlp --version" (no network access).
    /// Output is a bare version string like "2026.03.13".
    /// </summary>
    private static async Task<string?> GetInstalledVersionAsync(string ytDlpPath, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = "--version",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        var output = (await outputTask.ConfigureAwait(false)).Trim();
        return process.ExitCode == 0 && !string.IsNullOrEmpty(output) ? output : null;
    }

    /// <summary>
    /// Query the GitHub releases API for the latest yt-dlp release tag (e.g. "2026.07.04").
    /// </summary>
    private static async Task<string?> GetLatestVersionAsync(CancellationToken cancellationToken)
    {
        using var http = CreateHttpClient();

        var json = await http.GetStringAsync(LatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
        var tag = JObject.Parse(json)["tag_name"]?.ToString();

        return string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();
    }

    /// <summary>
    /// Download the latest yt-dlp.exe to a temp file next to the target, sanity-check it,
    /// then swap it into place. The previous binary is kept as yt-dlp.exe.old and restored
    /// if the swap fails partway.
    /// </summary>
    private static async Task DownloadAndReplaceAsync(string ytDlpPath, string version, CancellationToken cancellationToken)
    {
        var downloadUrl = string.Format(DownloadUrlTemplate, version);
        // Stage the download in the same directory so the final move is atomic (same volume).
        var tempPath = ytDlpPath + ".new";
        var backupPath = ytDlpPath + ".old";

        using var http = CreateHttpClient();

        Log.Information("Downloading yt-dlp {Version} from {Url}", version, downloadUrl);

        using (var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();

            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        }

        var downloadedSize = new FileInfo(tempPath).Length;
        if (downloadedSize < MinValidExeBytes)
        {
            Log.Warning("Downloaded yt-dlp.exe is suspiciously small ({Bytes} bytes) - discarding and keeping existing version.",
                downloadedSize);
            TryDelete(tempPath);
            return;
        }

        // Swap: current -> .old, .new -> current. Restore on failure.
        TryDelete(backupPath);
        File.Move(ytDlpPath, backupPath);
        try
        {
            File.Move(tempPath, ytDlpPath);
        }
        catch
        {
            // Put the old binary back so downloads keep working.
            File.Move(backupPath, ytDlpPath);
            throw;
        }

        // Verify the new binary actually runs before declaring success.
        var newVersion = await GetInstalledVersionAsync(ytDlpPath, cancellationToken).ConfigureAwait(false);
        if (newVersion == null)
        {
            Log.Warning("Updated yt-dlp.exe failed to run - rolling back to previous version.");
            TryDelete(ytDlpPath);
            File.Move(backupPath, ytDlpPath);
            return;
        }

        TryDelete(backupPath);
        Log.Information("yt-dlp updated successfully to version {Version}.", newVersion);
    }

    private static HttpClient CreateHttpClient()
    {
        // HttpClient uses the Windows certificate store, so TLS-inspecting AV roots
        // (e.g. Avast) are trusted - unlike yt-dlp's own Python/certifi stack.
        var http = new HttpClient();
        // GitHub API rejects requests without a User-Agent.
        http.DefaultRequestHeaders.UserAgent.ParseAdd("RightClicks/1.0");
        return http;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not delete file: {Path}", path);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to kill timed-out yt-dlp process.");
        }
    }
}
