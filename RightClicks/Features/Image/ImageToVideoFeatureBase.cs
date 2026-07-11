using RightClicks.Models;
using RightClicks.Models.FalAi;
using RightClicks.Services;
using RightClicks.Windows;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Features.Image;

/// <summary>
/// The settings the user submitted in the config window, carried on the Job from the moment it is
/// queued until it runs.
/// </summary>
/// <param name="ModelId">The fal endpoint the user chose (not necessarily the feature's default).</param>
/// <param name="ImageParameter">The key that model wants the image URL under.</param>
/// <param name="Parameters">Values keyed by the model's own parameter names, already type-coerced.</param>
public record ImageToVideoConfiguration(
    string ModelId,
    string ImageParameter,
    Dictionary<string, object> Parameters);

/// <summary>
/// Base class for all fal.ai Image-to-Video features.
/// Provides shared logic for opening config window, uploading to Cloudinary, and calling fal.ai API.
/// 
/// ARCHITECTURE NOTE: This base class exists to avoid code duplication across multiple
/// fal.ai image-to-video models (Veo 3.1, Kling, Wan, Vidu, Seedance, etc.). Each model has the same workflow:
/// 1. Open configuration window to select model and parameters
/// 2. Upload image to Cloudinary
/// 3. Call fal.ai API with hosted URL
/// 4. Download result video
/// 5. Clean up Cloudinary file
/// 
/// The only differences between models are:
/// - API endpoint URL
/// - Display name / pricing
/// - Model-specific parameters (defined in JSON config files)
/// 
/// FILE HOSTING: Using Cloudinary for image hosting (required by fal.ai).
/// Images are uploaded, used for API call, then deleted after video is downloaded.
/// </summary>
public abstract class ImageToVideoFeatureBase : IConfigurableFeature
{
    // Abstract properties - must be implemented by derived classes
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }

    /// <summary>
    /// The model this feature preselects in the configuration window.
    /// This is NOT necessarily the model that gets called - the user is free to pick a different
    /// one from the dropdown, and their choice wins. Model definitions (endpoint, parameters,
    /// pricing) live in the ImageToVideo\*.json files, which are the single source of truth.
    /// </summary>
    protected abstract string DefaultModelId { get; }

    // Common properties for all image-to-video features
    public string[] SupportedExtensions => new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif" };
    public bool IsCloudBased => true;

    /// <summary>
    /// Show the settings dialog. Runs on the UI thread before the job is queued, so cancelling
    /// here means no job is ever created.
    /// </summary>
    public object? Configure(string fullPath)
    {
        Log.Information("=== {FeatureName}: collecting configuration ===", DisplayName);
        Log.Information("Input file: {FilePath}", fullPath);

        if (!File.Exists(fullPath))
        {
            Log.Error("File not found: {FilePath}", fullPath);
            return null;
        }

        var configWindow = new ImageToVideoConfigWindow(fullPath, DefaultModelId);
        var dialogResult = configWindow.ShowDialog();

        if (dialogResult != true)
        {
            Log.Information("User cancelled configuration window - no job will be queued");
            return null;
        }

        // The user's dropdown choice is authoritative, not this feature's DefaultModelId.
        var modelId = configWindow.SelectedModelId;
        var imageParameter = configWindow.SelectedImageParameter;

        if (string.IsNullOrWhiteSpace(modelId) || string.IsNullOrWhiteSpace(imageParameter))
        {
            Log.Error("No model selected in configuration window");
            return null;
        }

        if (modelId != DefaultModelId)
        {
            Log.Information("User switched model: {DefaultModelId} -> {SelectedModelId}", DefaultModelId, modelId);
        }

        Log.Information("Configuration collected: model={ModelId}, {ParamCount} parameters",
            modelId, configWindow.Parameters.Count);

        return new ImageToVideoConfiguration(modelId, imageParameter, configWindow.Parameters);
    }

    /// <summary>
    /// Not used - this feature always runs with settings gathered by <see cref="Configure"/>.
    /// </summary>
    public Task<FeatureResult> ExecuteAsync(string fullPath, CancellationToken cancellationToken)
        => ExecuteAsync(fullPath, null, cancellationToken);

    public async Task<FeatureResult> ExecuteAsync(string fullPath, object? configuration, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;

        Log.Information("=== {FeatureName} Feature Started ===", DisplayName);
        Log.Information("Input file: {FilePath}", fullPath);

        // 1. Validate file exists
        if (!File.Exists(fullPath))
        {
            Log.Error("File not found: {FilePath}", fullPath);
            return FeatureResult.CreateFailure("File not found", null,
                (long)(DateTime.Now - startTime).TotalMilliseconds);
        }

        // 2. Use the settings the user submitted before this job was queued
        if (configuration is not ImageToVideoConfiguration config)
        {
            Log.Error("Job was queued without configuration - Configure() should have run first");
            return FeatureResult.CreateFailure("Missing configuration", null,
                (long)(DateTime.Now - startTime).TotalMilliseconds);
        }

        var modelId = config.ModelId;
        var imageParameter = config.ImageParameter;

        Log.Information("Model: {ModelId}", modelId);
        Log.Information("Parameters: {ParamCount}", config.Parameters.Count);

        // 3. Get fal.ai API key
        var apiKey = Environment.GetEnvironmentVariable("FAL_KEY", EnvironmentVariableTarget.User);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Log.Error("FAL_KEY environment variable not set");
            return FeatureResult.CreateFailure(
                "FAL_KEY environment variable not set. Please configure your fal.ai API key.",
                null,
                (long)(DateTime.Now - startTime).TotalMilliseconds);
        }

        // 4. Get Cloudinary configuration
        var cloudinaryConfig = GetCloudinaryConfig();
        if (!cloudinaryConfig.HasValue)
        {
            Log.Error("Cloudinary not configured");
            return FeatureResult.CreateFailure(
                "Cloudinary not configured. Please check config.json and environment variables.",
                durationMs: (long)(DateTime.Now - startTime).TotalMilliseconds);
        }

        // 5. Upload image to Cloudinary
        Log.Information("Uploading image to Cloudinary...");

        string imageUrl;
        string imagePublicId;
        string imageResourceType;

        try
        {
            var cloudinaryStorage = new CloudinaryStorageService(
                cloudinaryConfig.Value.CloudName,
                cloudinaryConfig.Value.ApiKey,
                cloudinaryConfig.Value.ApiSecret);

            var uploadResult = await cloudinaryStorage.UploadFileAsync(fullPath, cancellationToken);
            imageUrl = uploadResult.SecureUrl;
            imagePublicId = uploadResult.PublicId;
            imageResourceType = uploadResult.ResourceType;

            Log.Information("Image uploaded to Cloudinary: {ImageUrl} (public_id: {PublicId})", imageUrl, imagePublicId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to upload image to Cloudinary");
            return FeatureResult.CreateFailure($"Failed to upload image: {ex.Message}", ex,
                durationMs: (long)(DateTime.Now - startTime).TotalMilliseconds);
        }

        // 6. Build fal.ai API request from configuration window parameters
        var payload = BuildPayload(imageUrl, imageParameter, config.Parameters);

        Log.Information("API request built successfully for {ModelId}", modelId);
        Log.Debug("Request payload: {@Payload}", payload);

        // 7. Call fal.ai API (synchronous - waits for result)
        Log.Information("Submitting image-to-video request to fal.ai (this may take 2-5 minutes)...");

        using var falService = new FalAiImageToVideoService(apiKey, modelId);

        FalAiImageToVideoResult result;
        try
        {
            result = await falService.GenerateVideoAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate video");

            // Clean up Cloudinary image
            try
            {
                var cloudinaryStorage = new CloudinaryStorageService(
                    cloudinaryConfig.Value.CloudName,
                    cloudinaryConfig.Value.ApiKey,
                    cloudinaryConfig.Value.ApiSecret);

                await cloudinaryStorage.DeleteFileAsync(imagePublicId, imageResourceType, cancellationToken);
                Log.Information("Cleaned up Cloudinary image: {PublicId}", imagePublicId);
            }
            catch (Exception cleanupEx)
            {
                Log.Warning(cleanupEx, "Failed to clean up Cloudinary image: {PublicId}", imagePublicId);
            }

            return FeatureResult.CreateFailure(
                $"Failed to generate video: {ex.Message}",
                ex,
                durationMs: (long)(DateTime.Now - startTime).TotalMilliseconds);
        }

        // 8. Download result video
        Log.Information("Downloading result video from {VideoUrl}...", result.Video!.Url);

        var outputPath = GenerateOutputPath(fullPath);

        try
        {
            using var httpClient = new HttpClient();
            var videoBytes = await httpClient.GetByteArrayAsync(result.Video.Url, cancellationToken);

            await File.WriteAllBytesAsync(outputPath, videoBytes, cancellationToken);

            Log.Information("Video saved to: {OutputPath} ({SizeKB:F2} KB)",
                outputPath, videoBytes.Length / 1024.0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download result video");

            // Clean up Cloudinary image
            try
            {
                var cloudinaryStorage = new CloudinaryStorageService(
                    cloudinaryConfig.Value.CloudName,
                    cloudinaryConfig.Value.ApiKey,
                    cloudinaryConfig.Value.ApiSecret);

                await cloudinaryStorage.DeleteFileAsync(imagePublicId, imageResourceType, cancellationToken);
                Log.Information("Cleaned up Cloudinary image: {PublicId}", imagePublicId);
            }
            catch (Exception cleanupEx)
            {
                Log.Warning(cleanupEx, "Failed to clean up Cloudinary image: {PublicId}", imagePublicId);
            }

            return FeatureResult.CreateFailure($"Failed to download video: {ex.Message}", ex,
                durationMs: (long)(DateTime.Now - startTime).TotalMilliseconds);
        }

        // 9. Clean up Cloudinary image
        try
        {
            var cloudinaryStorage = new CloudinaryStorageService(
                cloudinaryConfig.Value.CloudName,
                cloudinaryConfig.Value.ApiKey,
                cloudinaryConfig.Value.ApiSecret);

            await cloudinaryStorage.DeleteFileAsync(imagePublicId, imageResourceType, cancellationToken);
            Log.Information("Cleaned up Cloudinary image: {PublicId}", imagePublicId);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to clean up Cloudinary image: {PublicId}", imagePublicId);
            // Non-fatal - continue
        }

        var elapsedMs = (long)(DateTime.Now - startTime).TotalMilliseconds;

        Log.Information("=== {FeatureName} Feature Completed Successfully ===", DisplayName);
        Log.Information("Output: {OutputPath}", outputPath);
        Log.Information("Total time: {ElapsedMs}ms ({ElapsedSec:F1}s)", elapsedMs, elapsedMs / 1000.0);

        return FeatureResult.CreateSuccess(
            $"Video generated successfully: {Path.GetFileName(outputPath)}",
            outputFilePath: outputPath,
            durationMs: elapsedMs);
    }

    /// <summary>
    /// Build the fal.ai request body for the selected model.
    ///
    /// The keys are the model's own parameter names, taken straight from its JSON config, and the
    /// values keep the CLR type the config window coerced them to (int stays int, bool stays bool).
    /// fal rejects the request outright if a field is the wrong type or isn't in that model's
    /// schema - Kling wants "start_image_url" and a string duration, Wan wants "image_url" and an
    /// integer one - so nothing is added here beyond the image URL.
    /// </summary>
    private Dictionary<string, object> BuildPayload(
        string imageUrl,
        string imageParameter,
        Dictionary<string, object> parameters)
    {
        var payload = new Dictionary<string, object>
        {
            [imageParameter] = imageUrl
        };

        foreach (var param in parameters)
        {
            // Omit blanks rather than sending null/"" - fal treats an explicit null as a type error
            // on fields it declares as strings, and an omitted optional field just takes its default.
            if (param.Value == null)
                continue;

            if (param.Value is string s && string.IsNullOrWhiteSpace(s))
                continue;

            payload[param.Key] = param.Value;
        }

        return payload;
    }

    /// <summary>
    /// Generate output file path for the result video.
    /// </summary>
    private string GenerateOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);

        return Path.Combine(directory, $"{fileNameWithoutExt}_video.mp4");
    }

    /// <summary>
    /// Gets Cloudinary configuration from config.json and resolves API credentials from environment variables.
    /// Returns null if Cloudinary is not configured.
    /// </summary>
    private (string CloudName, string ApiKey, string ApiSecret)? GetCloudinaryConfig()
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

        Log.Debug("Cloudinary configuration loaded: CloudName={CloudName}, ApiKey={ApiKeyMasked}, ApiSecret={ApiSecretMasked}",
            cloudName, MaskApiKey(apiKey), MaskApiKey(apiSecret));

        return (cloudName, apiKey, apiSecret);
    }

    /// <summary>
    /// Masks an API key for logging (shows first 4 and last 4 characters).
    /// </summary>
    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Length <= 8)
        {
            return "****";
        }

        return $"{apiKey.Substring(0, 4)}...{apiKey.Substring(apiKey.Length - 4)}";
    }
}

