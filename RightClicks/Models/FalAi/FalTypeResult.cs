using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RightClicks.Models.FalAi;

/// <summary>
/// Result from a completed fal.ai call for the NEW category types. Unlike the Image-to-Video result
/// (video-only), this supports BOTH video-output models and image-output models (face swaps), plus
/// the <c>images[]</c> array shape some image endpoints use.
/// </summary>
public class FalTypeResult
{
    /// <summary>Set by video-output models (result.video.url).</summary>
    [JsonPropertyName("video")]
    public FalAiVideoFile? Video { get; set; }

    /// <summary>Set by single-image-output models (result.image.url).</summary>
    [JsonPropertyName("image")]
    public FalAiVideoFile? Image { get; set; }

    /// <summary>Set by image-output models that return a list (result.images[0].url).</summary>
    [JsonPropertyName("images")]
    public List<FalAiVideoFile>? Images { get; set; }

    [JsonPropertyName("request_id")]
    public string? RequestId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }

    /// <summary>
    /// The single output URL regardless of shape, or null if the response carried none.
    /// Prefers video, then a single image, then the first of an images[] array.
    /// </summary>
    [JsonIgnore]
    public FalAiVideoFile? PrimaryOutput =>
        Video ?? Image ?? (Images is { Count: > 0 } ? Images[0] : null);
}
