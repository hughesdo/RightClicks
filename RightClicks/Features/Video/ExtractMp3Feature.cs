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
    /// Feature to extract MP3 audio from video files.
    /// Output file: {original_name}.mp3 (next to source file)
    /// </summary>
    public class ExtractMp3Feature : IFileFeature
    {
        public string Id => "ExtractMp3";

        public string DisplayName => "Extract MP3";

        public string Description => "Extract audio from video file and save as MP3";

        public string[] SupportedExtensions => new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };

        public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            Log.Information("ExtractMp3Feature: Starting execution for file: {FilePath}", filePath);

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

                // Calculate output path: {original_name}.mp3
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
                var directory = Path.GetDirectoryName(fullPath);
                var outputPath = Path.Combine(directory!, $"{fileNameWithoutExt}.mp3");
                Log.Information("Output path: {OutputPath}", outputPath);

                // Check if output file already exists
                if (File.Exists(outputPath))
                {
                    Log.Warning("Output file already exists, will be overwritten: {OutputPath}", outputPath);
                }

                // Extract audio to MP3 using FFmpeg
                Log.Information("Extracting audio to MP3...");

                var success = await FFMpegArguments
                    .FromFileInput(fullPath)
                    .OutputToFile(outputPath, overwrite: true, options => options
                        .WithAudioCodec("libmp3lame")
                        .WithAudioBitrate(192)
                        .WithoutMetadata()
                        .ForceFormat("mp3"))
                    .CancellableThrough(cancellationToken)
                    .ProcessAsynchronously();

                if (!success)
                {
                    Log.Error("FFmpeg audio extraction failed");
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return FeatureResult.CreateFailure("FFmpeg audio extraction failed", null, duration);
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
                Log.Information("ExtractMp3Feature: Completed successfully in {Duration}ms", finalDuration);

                return FeatureResult.CreateSuccess(
                    $"MP3 audio extracted successfully",
                    outputPath,
                    finalDuration
                );
            }
            catch (OperationCanceledException)
            {
                Log.Warning("ExtractMp3Feature: Operation cancelled");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure("Operation cancelled by user", null, duration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ExtractMp3Feature: Execution failed");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure($"Failed to extract MP3: {ex.Message}", ex, duration);
            }
        }
    }
}

