using RightClicks.Models.FalAi;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RightClicks.Services;

/// <summary>
/// Calls fal.ai endpoints via the QUEUE API (submit → poll status → fetch result), unlike the
/// synchronous <see cref="FalAiImageToVideoService"/>. The queue pattern is required for the longer
/// LTX jobs the new category types use, and it returns EITHER a video or an image so face-swaps work.
///
/// Endpoint strings MUST be full fal paths (e.g. "fal-ai/ltx-2.3-quality/audio-to-video/lora").
/// A short alias yields fal "NotFound: Application ..." — surfaced here with the offending id.
/// </summary>
public class FalAiQueueService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private const string QueueBase = "https://queue.fal.run";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(15);

    private class SubmitResponse
    {
        public string? request_id { get; set; }
        public string? status_url { get; set; }
        public string? response_url { get; set; }
    }

    private class StatusResponse
    {
        public string? status { get; set; } // IN_QUEUE | IN_PROGRESS | COMPLETED
        public string? response_url { get; set; }
    }

    public FalAiQueueService(string apiKey, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be null or empty.", nameof(endpoint));

        // Full-path guard: fal endpoints look like "vendor/model/.../variant" with at least one slash.
        if (!endpoint.Contains('/'))
            throw new ArgumentException(
                $"fal endpoint '{endpoint}' is not a full path. Use the complete id " +
                "(e.g. 'fal-ai/ltx-2.3/audio-to-video'), never a short alias.", nameof(endpoint));

        _endpoint = endpoint.Trim('/');
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {apiKey}");

        Log.Information("FalAiQueueService initialized (endpoint: {Endpoint})", _endpoint);
    }

    /// <summary>
    /// Submit the job, poll until it completes, and return the parsed result (video or image).
    /// </summary>
    public async Task<FalTypeResult> RunAsync(Dictionary<string, object> payload, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // 1. Submit
        var submitUrl = $"{QueueBase}/{_endpoint}";
        var json = JsonSerializer.Serialize(payload, options);
        Log.Debug("Queue submit payload: {Json}", json);

        using var submitContent = new StringContent(json, Encoding.UTF8, "application/json");
        var submitResp = await _httpClient.PostAsync(submitUrl, submitContent, cancellationToken);
        var submitBody = await submitResp.Content.ReadAsStringAsync(cancellationToken);

        if (submitResp.StatusCode == HttpStatusCode.NotFound)
        {
            Log.Error("fal returned NotFound for endpoint '{Endpoint}': {Body}", _endpoint, submitBody);
            throw new InvalidOperationException(
                $"fal NotFound: '{_endpoint}' is not a valid endpoint id. Check the model page for the exact full path.");
        }

        if (!submitResp.IsSuccessStatusCode)
        {
            Log.Error("fal queue submit failed: {Status} - {Body}", submitResp.StatusCode, submitBody);
            throw new HttpRequestException($"fal queue submit returned {submitResp.StatusCode}: {submitBody}");
        }

        var submit = JsonSerializer.Deserialize<SubmitResponse>(submitBody, options);
        if (string.IsNullOrEmpty(submit?.request_id))
            throw new InvalidOperationException($"fal queue submit returned no request_id: {submitBody}");

        var requestId = submit.request_id;
        var statusUrl = submit.status_url ?? $"{QueueBase}/{_endpoint}/requests/{requestId}/status";
        var responseUrl = submit.response_url ?? $"{QueueBase}/{_endpoint}/requests/{requestId}";

        Log.Information("fal job queued: request_id={RequestId}", requestId);

        // 2. Poll status
        var started = DateTime.UtcNow;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DateTime.UtcNow - started > MaxWait)
                throw new TimeoutException($"fal job {requestId} did not complete within {MaxWait.TotalMinutes} minutes.");

            await Task.Delay(PollInterval, cancellationToken);

            var statusResp = await _httpClient.GetAsync(statusUrl, cancellationToken);
            var statusBody = await statusResp.Content.ReadAsStringAsync(cancellationToken);

            if (!statusResp.IsSuccessStatusCode)
            {
                Log.Warning("fal status poll returned {Status}: {Body}", statusResp.StatusCode, statusBody);
                continue; // transient; keep polling until MaxWait
            }

            var status = JsonSerializer.Deserialize<StatusResponse>(statusBody, options);
            Log.Debug("fal job {RequestId} status: {Status}", requestId, status?.status);

            if (string.Equals(status?.status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(status?.response_url))
                    responseUrl = status.response_url;
                break;
            }
        }

        // 3. Fetch result
        var resultResp = await _httpClient.GetAsync(responseUrl, cancellationToken);
        var resultBody = await resultResp.Content.ReadAsStringAsync(cancellationToken);

        if (!resultResp.IsSuccessStatusCode)
        {
            Log.Error("fal result fetch failed: {Status} - {Body}", resultResp.StatusCode, resultBody);
            throw new HttpRequestException($"fal result fetch returned {resultResp.StatusCode}: {resultBody}");
        }

        Log.Debug("fal result body: {Body}", resultBody);

        var result = JsonSerializer.Deserialize<FalTypeResult>(resultBody, options);
        if (result?.PrimaryOutput == null || string.IsNullOrEmpty(result.PrimaryOutput.Url))
        {
            Log.Error("fal result had no video/image output: {Body}", resultBody);
            throw new InvalidOperationException("fal result contained no video or image output.");
        }

        Log.Information("fal job {RequestId} completed. Output URL: {Url}", requestId, result.PrimaryOutput.Url);
        return result;
    }

    public void Dispose() => _httpClient?.Dispose();
}
