using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Service for calling fal.ai First+Last Frame to Video APIs.
/// Supports both wan-flf2v and veo3.1 models.
/// </summary>
public class FirstLastFrameApiService : IDisposable
{
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the FirstLastFrameApiService.
    /// </summary>
    /// <param name="apiKey">fal.ai API key.</param>
    /// <param name="endpoint">The fal.ai model endpoint (e.g., "fal-ai/wan-flf2v" or "fal-ai/veo3.1/first-last-frame-to-video").</param>
    public FirstLastFrameApiService(string apiKey, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));
        }

        _apiKey = apiKey;
        _endpoint = endpoint;
        _baseUrl = $"https://fal.run/{endpoint}";
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10) // 10 minute timeout for video generation
        };

        // CRITICAL: fal.ai uses "Key" prefix, not "Bearer"
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {_apiKey}");

        Log.Information("FirstLastFrameApiService initialized (endpoint: {Endpoint})", endpoint);
    }

    /// <summary>
    /// Converts an image file to base64 Data URI format.
    /// </summary>
    public string ConvertImageToBase64DataUri(string imagePath)
    {
        var bytes = File.ReadAllBytes(imagePath);
        var base64 = Convert.ToBase64String(bytes);
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        
        var mimeType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".avif" => "image/avif",
            _ => "image/jpeg"
        };

        return $"data:{mimeType};base64,{base64}";
    }

    /// <summary>
    /// Generates a video from first and last frame images using fal.ai API.
    /// Uses Cloudinary for image hosting (same pattern as lip sync features).
    /// </summary>
    public async Task<FirstLastFrameResult> GenerateVideoAsync(
        string firstImagePath,
        string lastImagePath,
        Dictionary<string, object> parameters,
        string? cloudinaryCloudName,
        string? cloudinaryApiKey,
        string? cloudinaryApiSecret,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Generating video from first+last frames using {Endpoint}...", _endpoint);
        Log.Information("First image: {FirstImage}", Path.GetFileName(firstImagePath));
        Log.Information("Last image: {LastImage}", Path.GetFileName(lastImagePath));

        // Upload images to Cloudinary
        string? firstImageUrl = null;
        string? lastImageUrl = null;
        string? firstImagePublicId = null;
        string? lastImagePublicId = null;
        string? firstImageResourceType = null;
        string? lastImageResourceType = null;
        CloudinaryStorageService? cloudinaryStorage = null;

        try
        {
            if (string.IsNullOrWhiteSpace(cloudinaryCloudName) ||
                string.IsNullOrWhiteSpace(cloudinaryApiKey) ||
                string.IsNullOrWhiteSpace(cloudinaryApiSecret))
            {
                throw new InvalidOperationException("Cloudinary configuration is required for First+Last Frame feature");
            }

            Log.Information("Uploading images to Cloudinary...");
            cloudinaryStorage = new CloudinaryStorageService(
                cloudinaryCloudName,
                cloudinaryApiKey,
                cloudinaryApiSecret);

            var firstImageResult = await cloudinaryStorage.UploadFileAsync(firstImagePath, cancellationToken);
            firstImageUrl = firstImageResult.SecureUrl;
            firstImagePublicId = firstImageResult.PublicId;
            firstImageResourceType = firstImageResult.ResourceType;
            Log.Information("First image uploaded to Cloudinary: {Url} (public_id: {PublicId})", firstImageUrl, firstImagePublicId);

            var lastImageResult = await cloudinaryStorage.UploadFileAsync(lastImagePath, cancellationToken);
            lastImageUrl = lastImageResult.SecureUrl;
            lastImagePublicId = lastImageResult.PublicId;
            lastImageResourceType = lastImageResult.ResourceType;
            Log.Information("Last image uploaded to Cloudinary: {Url} (public_id: {PublicId})", lastImageUrl, lastImagePublicId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to upload images to Cloudinary");
            throw new InvalidOperationException($"Failed to upload images to Cloudinary: {ex.Message}", ex);
        }

        // Build request payload
        // Note: veo3.1 uses "first_frame_url" and "last_frame_url"
        // wan-flf2v uses "start_image_url" and "end_image_url"
        // We'll detect based on endpoint
        var requestPayload = new Dictionary<string, object>(parameters);

        if (_endpoint.Contains("veo3.1"))
        {
            requestPayload["first_frame_url"] = firstImageUrl!;
            requestPayload["last_frame_url"] = lastImageUrl!;
        }
        else
        {
            requestPayload["start_image_url"] = firstImageUrl!;
            requestPayload["end_image_url"] = lastImageUrl!;
        }

        var json = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        });

        Log.Debug("Request JSON: {Json}", json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Log.Information("Sending POST request to {BaseUrl}...", _baseUrl);

        FirstLastFrameResult? result = null;
        try
        {
            var response = await _httpClient.PostAsync(_baseUrl, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            Log.Debug("Response status: {StatusCode}", response.StatusCode);
            Log.Debug("Response body: {ResponseBody}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error("fal.ai API error: {StatusCode} - {ResponseBody}",
                    response.StatusCode, responseBody);
                throw new HttpRequestException(
                    $"fal.ai API returned {response.StatusCode}: {responseBody}");
            }

            result = JsonSerializer.Deserialize<FirstLastFrameResult>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (result?.Video == null)
            {
                Log.Error("Invalid response from fal.ai: {ResponseBody}", responseBody);
                throw new InvalidOperationException("Invalid response from fal.ai API - no video in result");
            }

            Log.Information("Video generation completed successfully!");
            Log.Information("Output video URL: {VideoUrl}", result.Video.Url);
        }
        finally
        {
            // Cleanup uploaded images from Cloudinary
            if (cloudinaryStorage != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(firstImagePublicId))
                    {
                        Log.Information("Deleting first image from Cloudinary: {PublicId}", firstImagePublicId);
                        await cloudinaryStorage.DeleteFileAsync(firstImagePublicId, firstImageResourceType!, cancellationToken);
                    }

                    if (!string.IsNullOrEmpty(lastImagePublicId))
                    {
                        Log.Information("Deleting last image from Cloudinary: {PublicId}", lastImagePublicId);
                        await cloudinaryStorage.DeleteFileAsync(lastImagePublicId, lastImageResourceType!, cancellationToken);
                    }

                    Log.Information("Cloudinary cleanup completed");
                }
                catch (Exception cleanupEx)
                {
                    Log.Warning(cleanupEx, "Failed to cleanup Cloudinary files (non-critical)");
                }
            }
        }

        return result!;
    }

    /// <summary>
    /// Downloads a video from a URL to a local file path.
    /// </summary>
    public async Task DownloadVideoAsync(string videoUrl, string outputPath, CancellationToken cancellationToken = default)
    {
        Log.Information("Downloading video from {VideoUrl} to {OutputPath}...", videoUrl, outputPath);

        using var response = await _httpClient.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        Log.Information("Video size: {SizeKB:F2} KB", totalBytes / 1024.0);

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalRead += bytesRead;

            if (totalBytes > 0)
            {
                var progress = (int)((totalRead * 100) / totalBytes);
                if (progress % 10 == 0) // Log every 10%
                {
                    Log.Debug("Download progress: {Progress}%", progress);
                }
            }
        }

        Log.Information("Video downloaded successfully: {OutputPath} ({SizeKB:F2} KB)", outputPath, totalRead / 1024.0);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Result from fal.ai First+Last Frame API.
/// </summary>
public class FirstLastFrameResult
{
    public VideoInfo? Video { get; set; }
}

/// <summary>
/// Video information from fal.ai response.
/// </summary>
public class VideoInfo
{
    public string Url { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public string FileName { get; set; } = "";
}


