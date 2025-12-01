using System.Text.Json.Serialization;

namespace RightClicks.Models.FalAi;

/// <summary>
/// Response from checking the status of a queued fal.ai request.
/// </summary>
public class FalAiStatusResponse
{
    /// <summary>
    /// Current status of the request.
    /// Possible values: "IN_QUEUE", "IN_PROGRESS", "COMPLETED", "FAILED"
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Optional error message if status is FAILED.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// URL to fetch the response when status is COMPLETED.
    /// </summary>
    [JsonPropertyName("response_url")]
    public string? ResponseUrl { get; set; }
}

