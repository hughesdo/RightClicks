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
    /// Feature to convert various image formats to WebP.
    /// Supports: JPG, PNG, AVIF, GIF, BMP, TIFF
    /// Output file: {original_name}.webp (next to source file)
    /// </summary>
    public class ConvertToWebpFeature : IFileFeature
    {
        public string Id => "ConvertToWebp";

        public string DisplayName => "Image > Convert to WebP";

        public string Description => "Convert image to WebP format (modern, smaller)";

        public string[] SupportedExtensions => new[] 
        { 
            ".jpg", ".jpeg", ".png", ".avif", ".gif", ".bmp", ".tiff", ".tif"
        };

        public bool IsCloudBased => false;

        public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            Log.Information("ConvertToWebpFeature: Starting execution for file: {FilePath}", filePath);

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

                // Calculate output path: {original_name}.webp
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                var directory = Path.GetDirectoryName(fullPath);
                var outputPath = Path.Combine(directory!, $"{fileNameWithoutExt}.webp");
                Log.Information("Output path: {OutputPath}", outputPath);

                // Check if output file already exists
                if (File.Exists(outputPath))
                {
                    Log.Warning("Output file already exists, will overwrite: {OutputPath}", outputPath);
                }

                // Convert to WebP using FFmpeg
                Log.Information("Converting {InputFormat} to WebP...", inputExt);

                var success = await FFMpegArguments
                    .FromFileInput(fullPath)
                    .OutputToFile(outputPath, overwrite: true, options => options
                        .WithVideoCodec("libwebp")
                        .ForceFormat("webp"))
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
                Log.Information("ConvertToWebpFeature: Completed successfully in {Duration}ms", finalDuration);

                return FeatureResult.CreateSuccess(
                    $"Image converted to WebP successfully",
                    outputPath,
                    finalDuration
                );
            }
            catch (OperationCanceledException)
            {
                Log.Warning("ConvertToWebpFeature: Operation cancelled");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure("Operation cancelled by user", null, duration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ConvertToWebpFeature: Execution failed");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure($"Failed to convert to WebP: {ex.Message}", ex, duration);
            }
        }
    }
}

