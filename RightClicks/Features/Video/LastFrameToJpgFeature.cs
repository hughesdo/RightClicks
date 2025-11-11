using FFMpegCore;
using RightClicks.Models;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Features.Video
{
    /// <summary>
    /// Feature to capture the last frame of a video as a JPG image.
    /// Output file: {original_name}_Last.jpg (next to source file)
    /// </summary>
    public class LastFrameToJpgFeature : IFileFeature
    {
        public string Id => "LastFrameToJpg";

        public string DisplayName => "Last Frame to JPG";

        public string Description => "Capture the last frame of the video as a JPG image";

        public string[] SupportedExtensions => new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };

        public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            Log.Information("LastFrameToJpgFeature: Starting execution for file: {FilePath}", filePath);

            try
            {
                // Resolve full path
                var fullPath = Path.GetFullPath(filePath);
                Log.Debug("Full path resolved: {FullPath}", fullPath);

                if (!File.Exists(fullPath))
                {
                    Log.Error("File not found: {FullPath}", fullPath);
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return FeatureResult.CreateFailure($"File not found: {fullPath}", null, duration);
                }

                // Get video info to determine duration
                Log.Information("Analyzing video file...");
                var mediaInfo = await FFProbe.AnalyseAsync(fullPath, null, cancellationToken);
                var videoDuration = mediaInfo.Duration;
                Log.Information("Video duration: {Duration:F2} seconds", videoDuration.TotalSeconds);

                // Calculate output path: {original_name}_Last.jpg
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                var directory = Path.GetDirectoryName(fullPath);
                var outputPath = Path.Combine(directory!, $"{fileNameWithoutExt}_Last.jpg");
                Log.Information("Output path: {OutputPath}", outputPath);

                // Capture last frame (0.1 seconds before end to avoid black frames)
                var captureTime = videoDuration - TimeSpan.FromSeconds(0.1);
                if (captureTime < TimeSpan.Zero)
                {
                    captureTime = TimeSpan.Zero;
                }

                Log.Information("Capturing frame at {Time:F2} seconds...", captureTime.TotalSeconds);

                var success = await FFMpeg.SnapshotAsync(
                    fullPath,
                    outputPath,
                    captureTime: captureTime,
                    cancellationToken: cancellationToken
                );

                if (!success)
                {
                    Log.Error("FFmpeg snapshot failed");
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return FeatureResult.CreateFailure("FFmpeg snapshot failed", null, duration);
                }

                // Verify output file was created
                if (!File.Exists(outputPath))
                {
                    Log.Error("Output file was not created: {OutputPath}", outputPath);
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return FeatureResult.CreateFailure("Output file was not created", null, duration);
                }

                var outputFileInfo = new FileInfo(outputPath);
                Log.Information("Output file created: {OutputPath} ({Size} bytes)", outputPath, outputFileInfo.Length);

                var finalDuration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                Log.Information("LastFrameToJpgFeature: Completed successfully in {Duration}ms", finalDuration);

                return FeatureResult.CreateSuccess(
                    $"Last frame captured successfully",
                    outputPath,
                    finalDuration
                );
            }
            catch (OperationCanceledException)
            {
                Log.Warning("LastFrameToJpgFeature: Operation cancelled");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure("Operation cancelled by user", null, duration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "LastFrameToJpgFeature: Execution failed");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure($"Failed to capture last frame: {ex.Message}", ex, duration);
            }
        }
    }
}

