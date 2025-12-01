using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RightClicks.Models.FalAi;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Service for interacting with the fal.ai API.
/// Supports multiple fal.ai models by accepting the endpoint as a parameter.
///
/// SYNCHRONOUS vs QUEUE-BASED ENDPOINTS:
/// This service currently uses synchronous endpoints (https://fal.run/{model}).
/// For longer videos or slower models, queue-based endpoints may be needed.
/// See FalAiLipSyncFeatureBase.cs for more details.
/// </summary>
public class FalAiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 2000; // 2 seconds

    /// <summary>
    /// Initializes a new instance of the FalAiService.
    /// </summary>
    /// <param name="apiKey">fal.ai API key.</param>
    /// <param name="endpoint">The fal.ai model endpoint (e.g., "fal-ai/pixverse/lipsync" or "veed/lipsync").</param>
    public FalAiService(string apiKey, string endpoint)
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
        _baseUrl = $"https://fal.run/{endpoint}";
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5) // 5 minute timeout for synchronous processing
        };

        // CRITICAL: fal.ai uses "Key" prefix, not "Bearer"
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {_apiKey}");

        Log.Information("FalAiService initialized (endpoint: {Endpoint}, API key: {KeyPrefix}...)",
            endpoint, _apiKey.Substring(0, Math.Min(8, _apiKey.Length)));
    }

    /// <summary>
    /// Generates a lip-synced video using the fal.ai synchronous endpoint.
    /// This method blocks until the video is processed and returns the result directly.
    /// </summary>
    /// <param name="request">Lipsync request with video and audio URLs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lipsync result with output video URL.</returns>
    public async Task<FalAiLipSyncResult> GenerateLipsyncAsync(
        FalAiLipSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Submitting lipsync request to fal.ai (synchronous endpoint)...");
        Log.Debug("Video URL: {VideoUrl}", request.Input.VideoUrl);
        Log.Debug("Audio URL: {AudioUrl}", request.Input.AudioUrl ?? "(none)");

        return await ExecuteWithRetryAsync(async () =>
        {
            // Serialize just the Input object (not the wrapper)
            var json = JsonSerializer.Serialize(request.Input, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            Log.Debug("Request JSON: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Log.Information("Sending POST request to {BaseUrl}...", _baseUrl);
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

            var result = JsonSerializer.Deserialize<FalAiLipSyncResult>(responseBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (result?.Video == null)
            {
                Log.Error("Invalid response from fal.ai: {ResponseBody}", responseBody);
                throw new InvalidOperationException("Invalid response from fal.ai API - no video in result");
            }

            Log.Information("Lipsync completed successfully!");
            Log.Information("Output video URL: {VideoUrl}", result.Video.Url);
            Log.Information("Output video size: {SizeKB:F2} KB", result.Video.FileSize / 1024.0);

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// Downloads the output video from the result URL.
    /// </summary>
    /// <param name="videoUrl">URL of the output video.</param>
    /// <param name="outputPath">Path to save the downloaded video.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DownloadVideoAsync(
        string videoUrl,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Downloading video from {VideoUrl} to {OutputPath}...", videoUrl, outputPath);

        await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.GetAsync(videoUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Video download failed: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Video download failed: {response.StatusCode}");
            }

            var videoBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(outputPath, videoBytes, cancellationToken);

            Log.Information("Video downloaded successfully: {OutputPath} ({SizeKB:F2} KB)",
                outputPath, videoBytes.Length / 1024.0);

            return Task.CompletedTask;
        }, cancellationToken);
    }

    /// <summary>
    /// Executes an async operation with retry logic for network errors.
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                Log.Warning("HTTP request failed (attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {DelayMs}ms...",
                    attempt, MaxRetries, ex.Message, RetryDelayMs);

                await Task.Delay(RetryDelayMs, cancellationToken);
            }
            catch (TaskCanceledException ex) when (attempt < MaxRetries && !cancellationToken.IsCancellationRequested)
            {
                // Timeout, not user cancellation
                Log.Warning("Request timed out (attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {DelayMs}ms...",
                    attempt, MaxRetries, ex.Message, RetryDelayMs);

                await Task.Delay(RetryDelayMs, cancellationToken);
            }
        }

        // Final attempt without catching
        return await operation();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}


