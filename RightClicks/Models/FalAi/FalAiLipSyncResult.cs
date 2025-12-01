using System.Text.Json.Serialization;

namespace RightClicks.Models.FalAi;

/// <summary>
/// Result from a completed fal.ai Pixverse Lipsync operation.
/// </summary>
public class FalAiLipSyncResult
{
    /// <summary>
    /// Output video information.
    /// </summary>
    [JsonPropertyName("video")]
    public FalAiVideoFile? Video { get; set; }
}

/// <summary>
/// Information about a video file returned by fal.ai.
/// </summary>
public class FalAiVideoFile
{
    /// <summary>
    /// URL to download the output video.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Content type of the video file.
    /// </summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File name of the output video.
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Size of the output video file in bytes.
    /// </summary>
    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }
}

