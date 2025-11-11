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
    /// Feature to capture the first frame of a video as a JPG image.
    /// Output file: {original_name}_First.jpg (next to source file)
    /// </summary>
    public class FirstFrameToJpgFeature : IFileFeature
    {
        public string Id => "FirstFrameToJpg";

        public string DisplayName => "First Frame to JPG";

        public string Description => "Capture the first frame of the video as a JPG image";

        public string[] SupportedExtensions => new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };

        public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            Log.Information("FirstFrameToJpgFeature: Starting execution for file: {FilePath}", filePath);

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

                // Calculate output path: {original_name}_First.jpg
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                var directory = Path.GetDirectoryName(fullPath);
                var outputPath = Path.Combine(directory!, $"{fileNameWithoutExt}_First.jpg");
                Log.Information("Output path: {OutputPath}", outputPath);

                // Capture first frame at 0.1 seconds (to avoid potential black frame at 0.0)
                var captureTime = TimeSpan.FromSeconds(0.1);
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
                Log.Information("FirstFrameToJpgFeature: Completed successfully in {Duration}ms", finalDuration);

                return FeatureResult.CreateSuccess(
                    $"First frame captured successfully",
                    outputPath,
                    finalDuration
                );
            }
            catch (OperationCanceledException)
            {
                Log.Warning("FirstFrameToJpgFeature: Operation cancelled");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure("Operation cancelled by user", null, duration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FirstFrameToJpgFeature: Execution failed");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure($"Failed to capture first frame: {ex.Message}", ex, duration);
            }
        }
    }
}

