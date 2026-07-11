using System.Text.Json.Serialization;

namespace RightClicks.Models.FalAi;

/// <summary>
/// Result from a completed fal.ai Image-to-Video operation.
/// </summary>
public class FalAiImageToVideoResult
{
    /// <summary>
    /// Output video information.
    /// </summary>
    [JsonPropertyName("video")]
    public FalAiVideoFile? Video { get; set; }

    /// <summary>
    /// Request ID (for queue-based endpoints).
    /// </summary>
    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Additional metadata that may be model-specific.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

