using RightClicks.Models;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Features.Video
{
    /// <summary>
    /// Feature to capture the first frame of a video as a JPG image.
    /// Output file: {original_name}_first.jpg (next to source file)
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
                // TODO: Implement FFmpeg frame capture in Phase 2
                // For now, just return a placeholder result
                Log.Warning("FirstFrameToJpgFeature: Not yet implemented - placeholder execution");

                await Task.Delay(100, cancellationToken); // Simulate work

                var fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(filePath);
                var directory = System.IO.Path.GetDirectoryName(filePath);
                var outputPath = System.IO.Path.Combine(directory!, $"{fileNameWithoutExt}_first.jpg");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;

                Log.Information("FirstFrameToJpgFeature: Completed successfully (placeholder)");

                return FeatureResult.CreateSuccess(
                    "First frame captured (placeholder)",
                    outputPath,
                    duration
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

