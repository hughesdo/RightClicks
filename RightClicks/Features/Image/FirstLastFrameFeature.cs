using RightClicks.Models;
using RightClicks.Services;
using RightClicks.Windows;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RightClicks.Features.Image;

/// <summary>
/// Feature to generate videos from first and last frame images using AI models.
/// Uses a two-click pattern: user selects first image, then last image within 20 seconds.
/// Opens a configuration window on second click to select model and parameters.
/// Supports two models: wan-flf2v (Wan-2.1) and veo3.1 (Google Veo 3.1).
/// Requires fal.ai API key configured in environment variable FAL_KEY.
/// </summary>
public class FirstLastFrameFeature : IFileFeature
{
    public string Id => "FirstLastFrame";

    public string DisplayName => "First + Last Frames";

    public string Description => "Generate video from first and last frame images using AI (two-click: select first image, then last image)";

    public string[] SupportedExtensions => new[]
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif"
    };

    public bool IsCloudBased => true;

    public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.StartNew();

        try
        {
            Log.Information("FirstLastFrame: Starting execution for file: {FilePath}", filePath);

            // Validate file exists
            if (!File.Exists(filePath))
            {
                return FeatureResult.CreateFailure(
                    $"File not found: {filePath}",
                    durationMs: startTime.ElapsedMilliseconds);
            }

            // Try to pair this image with a pending one
            var pairResult = FirstLastFrameStateService.TryPairImage(filePath);

            // If not ready to process, return informational result (no job created, no notification)
            if (!pairResult.IsReadyToProcess)
            {
                var firstClickDuration = startTime.ElapsedMilliseconds;
                Log.Information("FirstLastFrame: Waiting for second image. {Message}", pairResult.Message);

                return FeatureResult.CreateInformational(
                    pairResult.Message,
                    firstClickDuration
                );
            }

            // Second click - we have both images, open configuration window
            Log.Information("FirstLastFrame: Both images selected. Opening configuration window...");
            Log.Information("  First Image: {FirstImage}", pairResult.FirstImagePath);
            Log.Information("  Last Image: {LastImage}", pairResult.LastImagePath);

            // Open configuration window on UI thread
            bool? dialogResult = null;
            FirstLastFrameConfigWindow? configWindow = null;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                configWindow = new FirstLastFrameConfigWindow(
                    pairResult.FirstImagePath!,
                    pairResult.LastImagePath!);

                dialogResult = configWindow.ShowDialog();
            });

            if (dialogResult != true || configWindow == null)
            {
                // User cancelled
                Log.Information("FirstLastFrame: User cancelled configuration");

                return FeatureResult.CreateInformational(
                    "Configuration cancelled by user.",
                    startTime.ElapsedMilliseconds
                );
            }

            // User clicked Submit - call API
            Log.Information("FirstLastFrame: User submitted configuration");
            Log.Information("Model: {ModelId}", configWindow.SelectedModelId);

            // Get API key from environment
            var apiKey = Environment.GetEnvironmentVariable("FAL_KEY", EnvironmentVariableTarget.User);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Log.Error("FAL_KEY environment variable not set");
                return FeatureResult.CreateFailure(
                    "FAL_KEY environment variable not set. Please configure your fal.ai API key.",
                    durationMs: startTime.ElapsedMilliseconds);
            }

            // Get Cloudinary credentials from config.json (same pattern as lip sync features)
            var cloudinaryConfig = GetCloudinaryConfig();
            if (!cloudinaryConfig.HasValue)
            {
                Log.Error("Cloudinary not configured");
                return FeatureResult.CreateFailure(
                    "Cloudinary not configured. Please check config.json and environment variables.",
                    durationMs: startTime.ElapsedMilliseconds);
            }

            // Call fal.ai API
            Log.Information("Calling fal.ai API to generate video...");
            Log.Information("Image param names: start={StartParam}, end={EndParam}",
                configWindow.StartImageParamName, configWindow.EndImageParamName);

            using var apiService = new FirstLastFrameApiService(apiKey, configWindow.SelectedModelId!);

            FirstLastFrameResult result;
            try
            {
                result = await apiService.GenerateVideoAsync(
                    configWindow.FirstImagePath,
                    configWindow.LastImagePath,
                    configWindow.Parameters,
                    configWindow.StartImageParamName,
                    configWindow.EndImageParamName,
                    cloudinaryConfig.Value.CloudName,
                    cloudinaryConfig.Value.ApiKey,
                    cloudinaryConfig.Value.ApiSecret,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate video");
                return FeatureResult.CreateFailure(
                    $"Failed to generate video: {ex.Message}",
                    ex,
                    startTime.ElapsedMilliseconds);
            }

            if (result.Video == null || string.IsNullOrEmpty(result.Video.Url))
            {
                Log.Error("Result does not contain video URL");
                return FeatureResult.CreateFailure(
                    "Result does not contain video URL",
                    durationMs: startTime.ElapsedMilliseconds);
            }

            // Download output video
            var outputPath = GenerateOutputPath(pairResult.FirstImagePath!, pairResult.LastImagePath!);
            Log.Information("Downloading video to: {OutputPath}", outputPath);

            try
            {
                await apiService.DownloadVideoAsync(result.Video.Url, outputPath, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download video");
                return FeatureResult.CreateFailure(
                    $"Failed to download video: {ex.Message}",
                    ex,
                    startTime.ElapsedMilliseconds);
            }

            Log.Information("FirstLastFrame: Video generated successfully!");
            Log.Information("Output: {OutputPath}", outputPath);

            return FeatureResult.CreateSuccess(
                $"Video generated successfully: {Path.GetFileName(outputPath)}",
                outputPath,
                startTime.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "FirstLastFrame: Unexpected error during execution");
            return FeatureResult.CreateFailure(
                $"Unexpected error: {ex.Message}",
                ex,
                startTime.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Generates output path for the generated video.
    /// Format: {FirstImageName}_to_{LastImageName}_flf2v.mp4
    /// </summary>
    private string GenerateOutputPath(string firstImagePath, string lastImagePath)
    {
        var directory = Path.GetDirectoryName(firstImagePath) ?? Environment.CurrentDirectory;
        var firstName = Path.GetFileNameWithoutExtension(firstImagePath);
        var lastName = Path.GetFileNameWithoutExtension(lastImagePath);

        var outputFileName = $"{firstName}_to_{lastName}_flf2v.mp4";
        var outputPath = Path.Combine(directory, outputFileName);

        // Handle duplicate filenames
        int counter = 1;
        while (File.Exists(outputPath))
        {
            outputFileName = $"{firstName}_to_{lastName}_flf2v_{counter}.mp4";
            outputPath = Path.Combine(directory, outputFileName);
            counter++;
        }

        return outputPath;
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
            return "****";

        return $"{apiKey.Substring(0, 4)}...{apiKey.Substring(apiKey.Length - 4)}";
    }
}

