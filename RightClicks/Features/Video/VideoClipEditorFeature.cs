using System.Diagnostics;
using System.IO;
using RightClicks.Models;
using Serilog;

namespace RightClicks.Features.Video;

/// <summary>
/// Launches the standalone video clip editor for frame-accurate clipping
/// </summary>
public class VideoClipEditorFeature : IFileFeature
{
    public string Id => "VideoClipEditor";
    public string DisplayName => "Video Clip Editor...";
    public string Description => "Open video in clip editor for frame-accurate clipping";
    public string[] SupportedExtensions => new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg" };
    public bool IsCloudBased => false;

    public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            Log.Information("Launching video clip editor for: {FilePath}", filePath);

            var clipEditorPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "RightClicksClipEditor.exe");

            if (!File.Exists(clipEditorPath))
            {
                Log.Error("Clip editor not found at: {Path}", clipEditorPath);
                return FeatureResult.CreateFailure(
                    "Clip editor not found. Please reinstall RightClicks.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = clipEditorPath,
                Arguments = $"--video \"{filePath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(clipEditorPath)
            };

            var process = Process.Start(startInfo);

            if (process == null)
            {
                Log.Error("Failed to start clip editor process");
                return FeatureResult.CreateFailure("Failed to launch clip editor.");
            }

            Log.Information("Video clip editor launched successfully (PID: {ProcessId})", process.Id);

            // Return informational result - no job created since this is a separate process
            return FeatureResult.CreateInformational("Video clip editor launched");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to launch video clip editor");
            return FeatureResult.CreateFailure($"Failed to launch clip editor: {ex.Message}", ex);
        }
    }
}

