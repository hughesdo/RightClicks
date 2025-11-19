using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using RightClicks.Models;
using RightClicks.Models.FalAi;
using RightClicks.Services;
using Serilog;

namespace RightClicks.Features.Video;

/// <summary>
/// Base class for all fal.ai lip sync features.
/// Provides shared logic for file hosting, audio extraction, cleanup, and error handling.
///
/// ARCHITECTURE NOTE: This base class exists to avoid code duplication across multiple
/// fal.ai lip sync models (Pixverse, VEED, Sync, Kling, Creatify, etc.). Each model has the same workflow:
/// 1. Upload video/audio to fal.ai file storage
/// 2. Call fal.ai API with hosted URLs
/// 3. Download result
/// 4. Files are automatically cleaned up by fal.ai
///
/// The only differences between models are:
/// - API endpoint URL
/// - Display name / pricing
/// - Optional model-specific parameters
///
/// FILE HOSTING: Using fal.ai's own file storage (https://fal.ai/storage/upload) instead of
/// third-party services like 0x0.st. This is more reliable and recommended by fal.ai.
/// Files are automatically cleaned up after processing.
///
/// SYNCHRONOUS vs QUEUE-BASED ENDPOINTS:
/// Currently using synchronous endpoints (https://fal.run/{model}) for simplicity.
/// For longer videos (>5 min?), queue-based endpoints (https://queue.fal.run/{model})
/// may be needed to avoid timeouts. This is a TODO for future investigation.
/// See README.md for more details.
/// </summary>
public abstract class FalAiLipSyncFeatureBase : IFileFeature
{
    // Abstract properties - must be implemented by derived classes
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    
    /// <summary>
    /// The fal.ai API endpoint for this specific model.
    /// Example: "fal-ai/pixverse/lipsync" or "veed/lipsync"
    /// </summary>
    protected abstract string FalAiEndpoint { get; }

    // Common properties for all lip sync features
    public string[] SupportedExtensions => new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
    public bool IsCloudBased => true;

    /// <summary>
    /// Audio file extensions in priority order.
    /// Always use MP3 for API submission to minimize payload size.
    /// </summary>
    private static readonly string[] AudioExtensions = { ".mp3" };

    public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;
        Log.Information("{FeatureName}: Starting execution for file: {FilePath}", GetType().Name, filePath);

        // Variables for file storage and cleanup
        string? videoUrl = null;
        string? audioUrl = null;
        string? videoToken = null;
        string? audioToken = null;
        string? videoPublicId = null;
        string? audioPublicId = null;
        string? videoResourceType = null;
        string? audioResourceType = null;
        CloudinaryStorageService? cloudinaryStorage = null;
        FileHostingService? fileHosting = null;
        string storageMethod = "unknown";

        try
        {
            // 1. Validate file exists
            var fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                Log.Error("File not found: {FullPath}", fullPath);
                return FeatureResult.CreateFailure($"File not found: {fullPath}", null,
                    (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            // 2. Get fal.ai API key
            var apiKey = GetFalAiApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Log.Error("fal.ai API key not configured");
                return FeatureResult.CreateFailure(
                    "fal.ai API key not configured. Please add it in the API Config tab.", null,
                    (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            // 3. Find or create audio file
            var audioPath = await FindOrCreateAudioFileAsync(fullPath, cancellationToken);
            if (string.IsNullOrEmpty(audioPath))
            {
                Log.Error("Failed to find or create audio file");
                return FeatureResult.CreateFailure("Failed to find or create audio file", null,
                    (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            // 4. Upload files to file storage
            // Strategy: Try Cloudinary first (enterprise-grade, most reliable)
            // Fallback to 0x0.st if Cloudinary fails
            // Note: 0x0.st has known server-side bugs causing segmentation faults
            Log.Information("Uploading video and audio to file storage...");

            try
            {
                // Try Cloudinary first (primary method - enterprise-grade, most reliable)
                Log.Information("Attempting upload to Cloudinary (primary method)...");

                var cloudinaryConfig = GetCloudinaryConfig();
                if (cloudinaryConfig.HasValue)
                {
                    try
                    {
                        cloudinaryStorage = new CloudinaryStorageService(
                            cloudinaryConfig.Value.CloudName,
                            cloudinaryConfig.Value.ApiKey,
                            cloudinaryConfig.Value.ApiSecret);

                        var videoUploadResult = await cloudinaryStorage.UploadFileAsync(fullPath, cancellationToken);
                        videoUrl = videoUploadResult.SecureUrl;
                        videoPublicId = videoUploadResult.PublicId;
                        videoResourceType = videoUploadResult.ResourceType;
                        Log.Information("Video uploaded to Cloudinary: {VideoUrl} (public_id: {PublicId})", videoUrl, videoPublicId);

                        var audioUploadResult = await cloudinaryStorage.UploadFileAsync(audioPath, cancellationToken);
                        audioUrl = audioUploadResult.SecureUrl;
                        audioPublicId = audioUploadResult.PublicId;
                        audioResourceType = audioUploadResult.ResourceType;
                        Log.Information("Audio uploaded to Cloudinary: {AudioUrl} (public_id: {PublicId})", audioUrl, audioPublicId);

                        storageMethod = "Cloudinary";
                        Log.Information("Successfully uploaded files to Cloudinary");
                    }
                    catch (Exception cloudinaryEx)
                    {
                        Log.Warning(cloudinaryEx, "Cloudinary upload failed, falling back to 0x0.st");

                        // Fallback to 0x0.st
                        fileHosting = new FileHostingService();

                        (videoUrl, videoToken) = await fileHosting.UploadFileAsync(fullPath, cancellationToken);
                        Log.Information("Video uploaded to 0x0.st (fallback): {VideoUrl}", videoUrl);

                        (audioUrl, audioToken) = await fileHosting.UploadFileAsync(audioPath, cancellationToken);
                        Log.Information("Audio uploaded to 0x0.st (fallback): {AudioUrl}", audioUrl);

                        storageMethod = "0x0.st (fallback)";
                        Log.Information("Successfully uploaded files to 0x0.st (fallback)");
                    }
                }
                else
                {
                    Log.Warning("Cloudinary not configured, using 0x0.st as primary method");

                    // Use 0x0.st as primary if Cloudinary not configured
                    fileHosting = new FileHostingService();

                    (videoUrl, videoToken) = await fileHosting.UploadFileAsync(fullPath, cancellationToken);
                    Log.Information("Video uploaded to 0x0.st: {VideoUrl}", videoUrl);

                    (audioUrl, audioToken) = await fileHosting.UploadFileAsync(audioPath, cancellationToken);
                    Log.Information("Audio uploaded to 0x0.st: {AudioUrl}", audioUrl);

                    storageMethod = "0x0.st";
                    Log.Information("Successfully uploaded files to 0x0.st");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to upload files to any storage service");
                return FeatureResult.CreateFailure($"Failed to upload files: {ex.Message}", ex,
                    (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            // 5. Submit to fal.ai API (synchronous - waits for result)
            Log.Information("Submitting lip sync request to fal.ai (this may take 1-3 minutes)...");

            using var falService = new FalAiService(apiKey, FalAiEndpoint);

            var request = new FalAiLipSyncRequest
            {
                Input = new FalAiLipSyncInput
                {
                    VideoUrl = videoUrl,
                    AudioUrl = audioUrl
                }
            };

            FalAiLipSyncResult result;
            try
            {
                result = await falService.GenerateLipsyncAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate lip sync video");
                return FeatureResult.CreateFailure($"Failed to generate lip sync: {ex.Message}", ex,
                    (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            if (result.Video == null || string.IsNullOrEmpty(result.Video.Url))
            {
                Log.Error("Result does not contain video URL");
                return FeatureResult.CreateFailure("Result does not contain video URL", null,
                    (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            // 6. Download output video
            var outputPath = GenerateOutputPath(fullPath);

            try
            {
                await falService.DownloadVideoAsync(result.Video.Url, outputPath, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download output video");
                return FeatureResult.CreateFailure($"Failed to download video: {ex.Message}", ex,
                    (long)(DateTime.Now - startTime).TotalMilliseconds);
            }

            var finalDuration = (long)(DateTime.Now - startTime).TotalMilliseconds;
            Log.Information("{FeatureName}: Completed successfully in {Duration}ms", GetType().Name, finalDuration);

            return FeatureResult.CreateSuccess(
                $"Lip sync completed successfully",
                outputPath,
                finalDuration
            );
        }
        catch (OperationCanceledException)
        {
            Log.Warning("{FeatureName}: Operation cancelled", GetType().Name);
            return FeatureResult.CreateFailure("Operation cancelled by user", null,
                (long)(DateTime.Now - startTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{FeatureName}: Execution failed", GetType().Name);
            return FeatureResult.CreateFailure($"Lip sync failed: {ex.Message}", ex,
                (long)(DateTime.Now - startTime).TotalMilliseconds);
        }
        finally
        {
            // Clean up uploaded files
            if (storageMethod == "Cloudinary")
            {
                // Delete Cloudinary files after processing (success or failure)
                if (cloudinaryStorage != null && !string.IsNullOrEmpty(videoPublicId))
                {
                    try
                    {
                        Log.Information("Deleting Cloudinary files...");

                        var videoDeleted = await cloudinaryStorage.DeleteFileAsync(
                            videoPublicId,
                            videoResourceType ?? "video",
                            CancellationToken.None);

                        if (videoDeleted)
                        {
                            Log.Information("Video file deleted from Cloudinary: {PublicId}", videoPublicId);
                        }

                        if (!string.IsNullOrEmpty(audioPublicId))
                        {
                            var audioDeleted = await cloudinaryStorage.DeleteFileAsync(
                                audioPublicId,
                                audioResourceType ?? "video",
                                CancellationToken.None);

                            if (audioDeleted)
                            {
                                Log.Information("Audio file deleted from Cloudinary: {PublicId}", audioPublicId);
                            }
                        }

                        Log.Information("Cloudinary files cleaned up successfully");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to delete Cloudinary files (they will remain in storage)");
                    }
                }
            }
            else if (storageMethod == "0x0.st" || storageMethod == "0x0.st (fallback)")
            {
                // Clean up 0x0.st files (expire in 1 hour anyway)
                if (fileHosting != null && !string.IsNullOrEmpty(videoUrl) && !string.IsNullOrEmpty(audioUrl))
                {
                    try
                    {
                        Log.Information("Cleaning up 0x0.st temporary files...");

                        if (!string.IsNullOrEmpty(videoToken))
                        {
                            await fileHosting.DeleteFileAsync(videoUrl, videoToken, CancellationToken.None);
                        }

                        if (!string.IsNullOrEmpty(audioToken))
                        {
                            await fileHosting.DeleteFileAsync(audioUrl, audioToken, CancellationToken.None);
                        }

                        Log.Information("0x0.st temporary files cleaned up successfully");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to clean up 0x0.st files (they will expire in 1 hour)");
                    }
                }
            }

            fileHosting?.Dispose();
            cloudinaryStorage?.Dispose();
        }
    }

    /// <summary>
    /// Gets the fal.ai API key from environment variables.
    /// </summary>
    protected string? GetFalAiApiKey()
    {
        // Load config to get the environment variable name
        var config = ConfigurationService.LoadConfig();

        // Look for "fal.ai" entry (case-insensitive)
        var falAiEntry = config.ApiKeys.FirstOrDefault(
            kvp => kvp.Key.Equals("fal.ai", StringComparison.OrdinalIgnoreCase));

        if (falAiEntry.Key == null)
        {
            Log.Warning("fal.ai not found in API configuration");
            return null;
        }

        var envVarName = falAiEntry.Value;
        var apiKey = ConfigurationService.ResolveApiKey(envVarName);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Log.Warning("fal.ai API key environment variable '{EnvVarName}' is not set or empty", envVarName);
            return null;
        }

        Log.Debug("fal.ai API key found (env var: {EnvVarName})", envVarName);
        return apiKey;
    }

    /// <summary>
    /// Gets Cloudinary configuration from config.json and resolves API credentials from environment variables.
    /// Returns null if Cloudinary is not configured.
    /// </summary>
    protected (string CloudName, string ApiKey, string ApiSecret)? GetCloudinaryConfig()
    {
        var config = ConfigurationService.LoadConfig();
        if (config?.Cloudinary == null)
        {
            Log.Debug("Cloudinary configuration not found in config.json");
            return null;
        }

        var cloudName = config.Cloudinary.CloudName;
        if (string.IsNullOrWhiteSpace(cloudName))
        {
            Log.Warning("Cloudinary cloud name is not set in config.json");
            return null;
        }

        var apiKey = ConfigurationService.ResolveApiKey(config.Cloudinary.ApiKeyEnvVar);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Log.Warning("Cloudinary API key environment variable '{EnvVarName}' is not set or empty",
                config.Cloudinary.ApiKeyEnvVar);
            return null;
        }

        var apiSecret = ConfigurationService.ResolveApiKey(config.Cloudinary.ApiSecretEnvVar);
        if (string.IsNullOrWhiteSpace(apiSecret))
        {
            Log.Warning("Cloudinary API secret environment variable '{EnvVarName}' is not set or empty",
                config.Cloudinary.ApiSecretEnvVar);
            return null;
        }

        Log.Debug("Cloudinary configuration loaded (cloud: {CloudName})", cloudName);
        return (cloudName, apiKey, apiSecret);
    }

    /// <summary>
    /// Finds an existing MP3 audio file or creates one by extracting from the video.
    /// Always uses MP3 for efficient file size and upload speed.
    /// </summary>
    protected async Task<string?> FindOrCreateAudioFileAsync(string videoPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(videoPath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoPath);

        if (string.IsNullOrEmpty(directory))
        {
            Log.Error("Could not determine directory for video file");
            return null;
        }

        // Check if MP3 already exists
        var mp3Path = Path.Combine(directory, fileNameWithoutExt + ".mp3");
        if (File.Exists(mp3Path))
        {
            Log.Information("Found existing MP3 audio file: {Mp3Path}", mp3Path);
            return mp3Path;
        }

        // No MP3 found, extract from video
        Log.Information("No MP3 audio file found. Extracting MP3 from video...");

        try
        {
            var success = await FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(mp3Path, overwrite: true, options => options
                    .WithAudioCodec("libmp3lame")
                    .WithAudioBitrate(128)
                    .ForceFormat("mp3"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            if (!success || !File.Exists(mp3Path))
            {
                Log.Error("Failed to extract MP3 audio from video");
                return null;
            }

            var mp3FileInfo = new FileInfo(mp3Path);
            Log.Information("MP3 audio extracted successfully: {Mp3Path} ({SizeKB:F2} KB)",
                mp3Path, mp3FileInfo.Length / 1024.0);
            return mp3Path;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error extracting MP3 audio from video");
            return null;
        }
    }

    /// <summary>
    /// Generates the output file path with _LIPSYNC suffix.
    /// If file exists, appends _2, _3, etc.
    /// </summary>
    protected string GenerateOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);

        var basePath = Path.Combine(directory, $"{fileNameWithoutExt}_LIPSYNC{extension}");

        if (!File.Exists(basePath))
        {
            return basePath;
        }

        // File exists, append number
        int counter = 2;
        string outputPath;
        do
        {
            outputPath = Path.Combine(directory, $"{fileNameWithoutExt}_LIPSYNC_{counter}{extension}");
            counter++;
        } while (File.Exists(outputPath));

        return outputPath;
    }
}

