using FFMpegCore;
using RightClicks.Models;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Features.Image
{
    /// <summary>
    /// Feature to convert various image formats to JPG.
    /// Supports: PNG, WebP, AVIF, GIF, BMP, TIFF
    /// Output file: {original_name}.jpg (next to source file)
    /// </summary>
    public class ConvertToJpgFeature : IFileFeature
    {
        public string Id => "ConvertToJpg";

        public string DisplayName => "Image > Convert to JPG";

        public string Description => "Convert image to JPG format";

        public string[] SupportedExtensions => new[] 
        { 
            ".png", ".webp", ".avif", ".gif", ".bmp", ".tiff", ".tif"
        };

        public bool IsCloudBased => false;

        public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            Log.Information("ConvertToJpgFeature: Starting execution for file: {FilePath}", filePath);

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

                // Get input format
                var inputExt = Path.GetExtension(fullPath).ToLowerInvariant();
                Log.Information("Input format: {InputFormat}", inputExt);

                // Calculate output path: {original_name}.jpg
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                var directory = Path.GetDirectoryName(fullPath);
                var outputPath = Path.Combine(directory!, $"{fileNameWithoutExt}.jpg");
                Log.Information("Output path: {OutputPath}", outputPath);

                // Check if output file already exists
                if (File.Exists(outputPath))
                {
                    Log.Warning("Output file already exists, will overwrite: {OutputPath}", outputPath);
                }

                // Convert to JPG using FFmpeg
                Log.Information("Converting {InputFormat} to JPG...", inputExt);

                var success = await FFMpegArguments
                    .FromFileInput(fullPath)
                    .OutputToFile(outputPath, overwrite: true, options => options
                        .WithVideoCodec("mjpeg")
                        .ForceFormat("image2"))
                    .CancellableThrough(cancellationToken)
                    .ProcessAsynchronously();

                if (!success)
                {
                    Log.Error("FFmpeg conversion failed");
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return FeatureResult.CreateFailure("FFmpeg conversion failed", null, duration);
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
                Log.Information("ConvertToJpgFeature: Completed successfully in {Duration}ms", finalDuration);

                return FeatureResult.CreateSuccess(
                    $"Image converted to JPG successfully",
                    outputPath,
                    finalDuration
                );
            }
            catch (OperationCanceledException)
            {
                Log.Warning("ConvertToJpgFeature: Operation cancelled");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure("Operation cancelled by user", null, duration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ConvertToJpgFeature: Execution failed");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure($"Failed to convert to JPG: {ex.Message}", ex, duration);
            }
        }
    }
}

