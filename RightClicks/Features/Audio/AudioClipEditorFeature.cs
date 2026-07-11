using System.Diagnostics;
using System.IO;
using RightClicks.Models;
using Serilog;

namespace RightClicks.Features.Audio;

/// <summary>
/// Launches the standalone audio clip editor for sample-accurate clipping
/// </summary>
public class AudioClipEditorFeature : IFileFeature
{
    public string Id => "AudioClipEditor";
    public string DisplayName => "Audio Clip Editor...";
    public string Description => "Open audio in clip editor for sample-accurate clipping";
    public string[] SupportedExtensions => new[] { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".ogg", ".wma", ".opus" };
    public bool IsCloudBased => false;

    public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            Log.Information("Launching audio clip editor for: {FilePath}", filePath);

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
                Arguments = $"--audio \"{filePath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(clipEditorPath)
            };

            var process = Process.Start(startInfo);

            if (process == null)
            {
                Log.Error("Failed to start clip editor process");
                return FeatureResult.CreateFailure("Failed to launch clip editor.");
            }

            Log.Information("Audio clip editor launched successfully (PID: {ProcessId})", process.Id);

            // Return informational result - no job created since this is a separate process
            return FeatureResult.CreateInformational("Audio clip editor launched");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to launch audio clip editor");
            return FeatureResult.CreateFailure($"Failed to launch clip editor: {ex.Message}", ex);
        }
    }
}

