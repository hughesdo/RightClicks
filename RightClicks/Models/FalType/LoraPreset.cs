using System.Collections.Generic;

namespace RightClicks.Models.FalType;

/// <summary>Root of loras.json — the LoRA preset registry (data, so Don can add more without code edits).</summary>
public class LoraRegistry
{
    public List<LoraPreset> loras { get; set; } = new();
}

/// <summary>
/// One ready-made HuggingFace LoRA. Gated to the endpoints it's valid for via
/// <see cref="compatible_endpoints"/> (audio-reactive → audio-to-video; 3DREAL → render-to-real /
/// reference-video-to-video).
/// </summary>
public class LoraPreset
{
    public string name { get; set; } = string.Empty;

    /// <summary>Direct .safetensors URL passed to fal as loras[].path.</summary>
    public string url { get; set; } = string.Empty;

    /// <summary>"good for..." text shown as tooltip + caption under the dropdown.</summary>
    public string use_hint { get; set; } = string.Empty;

    public double default_scale { get; set; } = 1.2;

    /// <summary>"both" | "high" | "low" — which transformer(s) the LoRA applies to.</summary>
    public string default_transformer { get; set; } = "both";

    /// <summary>Full fal endpoint paths this LoRA is valid for. Empty = show for any LoRA-capable model.</summary>
    public List<string> compatible_endpoints { get; set; } = new();
}
