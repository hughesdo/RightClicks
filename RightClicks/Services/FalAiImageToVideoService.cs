using RightClicks.Models.FalAi;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Services;

/// <summary>
/// Service for calling fal.ai Image-to-Video API endpoints.
/// Supports multiple models (Veo 3.1, Kling, Wan, Vidu, Seedance) via configurable endpoint.
/// Uses synchronous endpoints (https://fal.run/{model}) for simplicity.
/// </summary>
public class FalAiImageToVideoService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 2000; // 2 seconds

    /// <summary>
    /// Initializes a new instance of the FalAiImageToVideoService.
    /// </summary>
    /// <param name="apiKey">fal.ai API key.</param>
    /// <param name="endpoint">The fal.ai model endpoint (e.g., "fal-ai/veo3.1/fast/image-to-video").</param>
    public FalAiImageToVideoService(string apiKey, string endpoint)
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
            Timeout = TimeSpan.FromMinutes(10) // 10 minute timeout for video generation
        };

        // CRITICAL: fal.ai uses "Key" prefix, not "Bearer"
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {_apiKey}");

        Log.Information("FalAiImageToVideoService initialized (endpoint: {Endpoint}, API key: {KeyPrefix}...)",
            endpoint, _apiKey.Substring(0, Math.Min(8, _apiKey.Length)));
    }

    /// <summary>
    /// Generates a video from an image using the fal.ai synchronous endpoint.
    /// This method blocks until the video is processed and returns the result directly.
    /// </summary>
    /// <param name="payload">
    /// The request body, keyed by the parameter names the target model actually declares.
    /// Every fal model has a different input schema (Kling takes start_image_url, Wan takes an
    /// integer duration, Veo takes "8s"), so the caller builds this from the model's config
    /// rather than a shared DTO.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Image-to-video result with output video URL.</returns>
    public async Task<FalAiImageToVideoResult> GenerateVideoAsync(
        Dictionary<string, object> payload,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Submitting image-to-video request to fal.ai (synchronous endpoint)...");

        return await ExecuteWithRetryAsync(async () =>
        {
            // Response uses snake_case; the payload keys are already exact model parameter names.
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(payload, options);

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

            var result = JsonSerializer.Deserialize<FalAiImageToVideoResult>(responseBody, options);

            if (result?.Video == null)
            {
                Log.Error("Invalid response from fal.ai: {ResponseBody}", responseBody);
                throw new InvalidOperationException("Invalid response from fal.ai API - no video in result");
            }

            Log.Information("Image-to-video generation completed successfully!");
            Log.Information("Output video URL: {VideoUrl}", result.Video.Url);
            Log.Information("Output video size: {SizeKB:F2} KB", result.Video.FileSize / 1024.0);

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// Execute an operation with retry logic for transient failures.
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt < MaxRetries; attempt++)
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

