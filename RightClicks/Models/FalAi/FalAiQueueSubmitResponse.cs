using System.Text.Json.Serialization;

namespace RightClicks.Models.FalAi;

/// <summary>
/// Response from submitting a job to the fal.ai queue.
/// </summary>
public class FalAiQueueSubmitResponse
{
    /// <summary>
    /// Unique identifier for the queued request.
    /// </summary>
    [JsonPropertyName("request_id")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// URL to check the status of the request.
    /// </summary>
    [JsonPropertyName("status_url")]
    public string StatusUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL to get the response/result when completed.
    /// </summary>
    [JsonPropertyName("response_url")]
    public string ResponseUrl { get; set; } = string.Empty;
}

