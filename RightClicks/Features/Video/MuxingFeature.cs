using FFMpegCore;
using RightClicks.Models;
using RightClicks.Services;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Features.Video
{
    /// <summary>
    /// Feature to mux (combine) audio and video files using FFmpeg.
    /// Requires two clicks: one on audio file, one on video file (order doesn't matter).
    /// 30-second pairing window enforced.
    /// Output file: {video_name}_Muxed.mp4 (in directory of second click)
    /// </summary>
    public class MuxingFeature : IFileFeature
    {
        public string Id => "Muxing";

        public string DisplayName => "Muxing (audio + video)";

        public string Description => "Combine audio and video files (requires two clicks: audio + video within 30 seconds)";

        // Support both audio and video files
        public string[] SupportedExtensions => new[]
        {
            // Audio formats
            ".mp3", ".wav", ".aac", ".flac", ".m4a", ".ogg", ".opus", ".wma",
            // Video formats
            ".mp4", ".mov", ".mkv", ".avi", ".wmv", ".flv", ".webm", ".mpeg", ".mpg", ".m4v"
        };

        public bool IsCloudBased => false;

        public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            Log.Information("=== Muxing Feature Started ===");
            Log.Information("File clicked: {FilePath}", filePath);

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

                // Attempt to pair this file with a pending muxing operation
                var pairResult = MuxingStateService.TryPairFile(fullPath);

                Log.Information("Pairing result: {Message}", pairResult.Message);

                // If not ready to mux, return informational result (no job created, no notification)
                if (!pairResult.IsReadyToMux)
                {
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    Log.Information("Muxing: Waiting for second file. {Message}", pairResult.Message);

                    return FeatureResult.CreateInformational(
                        pairResult.Message,
                        duration
                    );
                }

                // We have a valid audio + video pair - proceed with muxing
                Log.Information("=== Starting Muxing Operation ===");
                Log.Information("Audio file: {AudioFile}", pairResult.AudioFilePath);
                Log.Information("Video file: {VideoFile}", pairResult.VideoFilePath);

                // Validate both files exist
                if (!File.Exists(pairResult.AudioFilePath))
                {
                    Log.Error("Audio file not found: {AudioFile}", pairResult.AudioFilePath);
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return FeatureResult.CreateFailure($"Audio file not found: {pairResult.AudioFilePath}", null, duration);
                }

                if (!File.Exists(pairResult.VideoFilePath))
                {
                    Log.Error("Video file not found: {VideoFile}", pairResult.VideoFilePath);
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return FeatureResult.CreateFailure($"Video file not found: {pairResult.VideoFilePath}", null, duration);
                }

                // Calculate output path: {video_name}_Muxed.mp4 in directory of second click (current file)
                var videoFileName = Path.GetFileNameWithoutExtension(pairResult.VideoFilePath);
                var outputDirectory = Path.GetDirectoryName(fullPath); // Directory of second click
                var outputPath = Path.Combine(outputDirectory!, $"{videoFileName}_Muxed.mp4");
                
                Log.Information("Output path: {OutputPath}", outputPath);

                // Check if output file already exists
                if (File.Exists(outputPath))
                {
                    Log.Warning("Output file already exists, will be overwritten: {OutputPath}", outputPath);
                }

                // Mux audio and video using FFmpeg
                // Command: ffmpeg -i <video> -i <audio> -map 0:v:0 -map 1:a:0 -c:v copy -c:a copy -shortest <output>
                // -map 0:v:0 = Take video stream from first input (video file)
                // -map 1:a:0 = Take audio stream from second input (audio file) - REPLACES any existing audio
                Log.Information("Muxing audio and video with FFmpeg...");
                Log.Information("FFmpeg command: -i \"{VideoFile}\" -i \"{AudioFile}\" -map 0:v:0 -map 1:a:0 -c:v copy -c:a copy -shortest \"{OutputFile}\"",
                    pairResult.VideoFilePath, pairResult.AudioFilePath, outputPath);

                var success = await FFMpegArguments
                    .FromFileInput(pairResult.VideoFilePath!)
                    .AddFileInput(pairResult.AudioFilePath!)
                    .OutputToFile(outputPath, overwrite: true, options => options
                        .WithCustomArgument("-map 0:v:0") // Map video from first input
                        .WithCustomArgument("-map 1:a:0") // Map audio from second input (replaces existing audio)
                        .CopyChannel() // Copy video stream without re-encoding
                        .WithAudioCodec("copy") // Copy audio stream without re-encoding
                        .WithCustomArgument("-shortest") // Use shortest stream duration
                        .ForceFormat("mp4"))
                    .CancellableThrough(cancellationToken)
                    .ProcessAsynchronously();

                if (!success)
                {
                    Log.Error("FFmpeg muxing failed");
                    var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                    return FeatureResult.CreateFailure("FFmpeg muxing failed", null, duration);
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
                Log.Information("=== Muxing Complete ===");
                Log.Information("Duration: {Duration}ms", finalDuration);

                return FeatureResult.CreateSuccess(
                    $"Audio and video muxed successfully",
                    outputPath,
                    finalDuration
                );
            }
            catch (OperationCanceledException)
            {
                Log.Warning("MuxingFeature: Operation cancelled");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure("Operation cancelled by user", null, duration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MuxingFeature: Execution failed");
                var duration = (long)(DateTime.Now - startTime).TotalMilliseconds;
                return FeatureResult.CreateFailure($"Failed to mux audio and video: {ex.Message}", ex, duration);
            }
        }
    }
}

