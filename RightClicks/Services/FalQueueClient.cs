using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RightClicks.Services;

/// <summary>
/// Submits a job to the fal.ai queue API and polls it to completion.
///
/// WHY: the synchronous endpoint (fal.run) holds one HTTP request open for the whole generation.
/// If the client times out, fal.ai keeps running (and billing) the job, and a retry then submits
/// a SECOND billed job. The queue API returns a request_id immediately, so a slow job can never
/// cause a duplicate submission. The submit POST is NEVER retried.
/// </summary>
internal static class FalQueueClient
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxWait = TimeSpan.FromMinutes(30);
    private const int MaxConsecutivePollErrors = 5;

    /// <summary>Submits json to queue.fal.run/{endpoint}, waits for completion, returns the result body.</summary>
    public static async Task<string> RunAsync(
        HttpClient http, string endpoint, string json, CancellationToken ct)
    {
        var submitUrl = $"https://queue.fal.run/{endpoint}";
        Log.Information("Submitting to fal.ai queue: {Url} (submit is never retried)", submitUrl);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var submitResponse = await http.PostAsync(submitUrl, content, ct);
        var submitBody = await submitResponse.Content.ReadAsStringAsync(ct);
        Log.Debug("Queue submit response ({Status}): {Body}", submitResponse.StatusCode, submitBody);

        if (!submitResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"fal.ai queue submit returned {submitResponse.StatusCode}: {submitBody}");
        }

        using var submitDoc = JsonDocument.Parse(submitBody);
        var root = submitDoc.RootElement;
        var requestId = root.GetProperty("request_id").GetString();
        var statusUrl = root.GetProperty("status_url").GetString()!;
        var responseUrl = root.GetProperty("response_url").GetString()!;
        Log.Information("fal.ai request queued: {RequestId}", requestId);

        var started = DateTime.UtcNow;
        var pollErrors = 0;
        var lastStatus = "";

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            if (DateTime.UtcNow - started > MaxWait)
            {
                throw new TimeoutException(
                    $"fal.ai request {requestId} did not finish within {MaxWait.TotalMinutes:F0} minutes. " +
                    "It was NOT resubmitted - check fal.ai dashboard.");
            }

            await Task.Delay(PollInterval, ct);

            try
            {
                var statusResponse = await http.GetAsync(statusUrl, ct);
                var statusBody = await statusResponse.Content.ReadAsStringAsync(ct);
                if (!statusResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"status returned {statusResponse.StatusCode}: {statusBody}");
                }

                pollErrors = 0;
                using var statusDoc = JsonDocument.Parse(statusBody);
                var status = statusDoc.RootElement.GetProperty("status").GetString() ?? "";
                if (status != lastStatus)
                {
                    Log.Information("fal.ai request {RequestId} status: {Status}", requestId, status);
                    lastStatus = status;
                }

                if (status == "COMPLETED") break;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                // Polling is a read-only GET: safe to continue, never causes a new billed job.
                if (++pollErrors >= MaxConsecutivePollErrors) throw;
                Log.Warning("Status poll failed ({Count}/{Max}): {Message}", pollErrors, MaxConsecutivePollErrors, ex.Message);
            }
        }

        var resultResponse = await http.GetAsync(responseUrl, ct);
        var resultBody = await resultResponse.Content.ReadAsStringAsync(ct);
        Log.Debug("Queue result response ({Status}): {Body}", resultResponse.StatusCode, resultBody);

        if (!resultResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"fal.ai result fetch returned {resultResponse.StatusCode}: {resultBody}");
        }

        return resultBody;
    }
}
