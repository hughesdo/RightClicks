using System.Text.Json.Serialization;

namespace RightClicks.Models.FalAi;

/// <summary>
/// Request model for fal.ai Pixverse Lipsync API.
/// </summary>
public class FalAiLipSyncRequest
{
    /// <summary>
    /// Input parameters for the lipsync operation.
    /// </summary>
    [JsonPropertyName("input")]
    public FalAiLipSyncInput Input { get; set; } = new();
}

/// <summary>
/// Input parameters for fal.ai Pixverse Lipsync.
/// </summary>
public class FalAiLipSyncInput
{
    /// <summary>
    /// URL of the input video (can be a Base64 data URI).
    /// </summary>
    [JsonPropertyName("video_url")]
    public string VideoUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL of the input audio (can be a Base64 data URI).
    /// Optional - if not provided, TTS will be used.
    /// </summary>
    [JsonPropertyName("audio_url")]
    public string? AudioUrl { get; set; }
}

