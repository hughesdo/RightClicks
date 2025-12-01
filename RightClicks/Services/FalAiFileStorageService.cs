using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Service for uploading files to fal.ai's file storage.
/// This is the recommended way to provide files to fal.ai APIs.
/// Files are stored temporarily and automatically cleaned up.
/// 
/// API Documentation: https://fal.ai/docs/model-endpoints/file-uploads
/// </summary>
public class FalAiFileStorageService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    // Note: The document shows storage.fal.ai but DNS fails. Trying fal.run domain.
    // Based on fal.ai documentation, storage endpoint should be under fal.run
    private const string UploadUrl = "https://fal.run/storage/upload";
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 2000; // 2 seconds

    /// <summary>
    /// Response from fal.ai file upload API.
    /// Returns a JSON object with "url" field containing the uploaded file URL.
    /// Example: { "url": "https://v3.fal.media/files/..." }
    /// </summary>
    private class UploadResponse
    {
        public string? url { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the FalAiFileStorageService.
    /// </summary>
    /// <param name="apiKey">fal.ai API key for authentication.</param>
    public FalAiFileStorageService(string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10) // 10 minute timeout for large uploads
        };
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "RightClicks/1.0 (https://github.com/hughesdo/RightClicks)");
        
        Log.Information("FalAiFileStorageService initialized (API key: {ApiKeyPrefix}...)", 
            _apiKey.Substring(0, Math.Min(8, _apiKey.Length)));
    }

    /// <summary>
    /// Uploads a file to fal.ai storage and returns the URL.
    /// </summary>
    /// <param name="filePath">Path to the file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URL where the file can be accessed.</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="HttpRequestException">If the upload fails.</exception>
    public async Task<string> UploadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        Log.Information("Uploading file to fal.ai storage: {FilePath} ({Size:F2} MB)", 
            filePath, fileInfo.Length / 1024.0 / 1024.0);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);
                using var content = new MultipartFormDataContent();
                
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(filePath));
                content.Add(fileContent, "file", Path.GetFileName(filePath));

                Log.Debug("Sending upload request to fal.ai storage (attempt {Attempt}/{MaxRetries})...", 
                    attempt, MaxRetries);
                
                var response = await _httpClient.PostAsync(UploadUrl, content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    Log.Warning("Upload failed (attempt {Attempt}/{MaxRetries}): {StatusCode} - {Error}", 
                        attempt, MaxRetries, response.StatusCode, errorBody);
                    
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(RetryDelayMs, cancellationToken);
                        continue;
                    }
                    
                    throw new HttpRequestException(
                        $"Failed to upload file to fal.ai storage: {response.StatusCode} - {errorBody}");
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                Log.Debug("Upload response: {Response}", responseBody);

                var uploadResponse = JsonSerializer.Deserialize<UploadResponse>(responseBody);

                if (string.IsNullOrEmpty(uploadResponse?.url))
                {
                    throw new HttpRequestException("Upload response did not contain url field");
                }

                Log.Information("File uploaded successfully to fal.ai storage: {Url}", uploadResponse.url);
                return uploadResponse.url;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < MaxRetries)
            {
                Log.Warning(ex, "Upload attempt {Attempt}/{MaxRetries} failed, retrying...", 
                    attempt, MaxRetries);
                await Task.Delay(RetryDelayMs, cancellationToken);
            }
        }

        throw new HttpRequestException($"Failed to upload file after {MaxRetries} attempts");
    }

    /// <summary>
    /// Gets the MIME content type for a file based on its extension.
    /// </summary>
    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            _ => "application/octet-stream"
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

