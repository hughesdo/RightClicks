using RightClicks.Features.FalType;

namespace RightClicks.Features.Video;

/// <summary>
/// Pixverse Swap — person/object/background swap inside the right-clicked VIDEO using a reference
/// image. Video-output; the config window adds the Reference Image slot. Original audio is kept by
/// the model itself (original_sound_switch), so no reattach is configured.
/// </summary>
public class SwapPixverseFeature : FalTypeFeatureBase
{
    public override string Id => "SwapPixverse";

    public override string DisplayName => "Swaps > ☁️ Pixverse Swap ~$0.15/5s";

    public override string Description => "Swap a person, object, or background inside a video using a reference image. Keeps the original audio.";

    public override string[] SupportedExtensions => new[]
    {
        ".mp4", ".mov", ".webm", ".m4v", ".gif"
    };

    protected override string CategoryFolder => "Swaps";
    protected override string CategoryTitle => "Swaps";
    protected override string DefaultModelId => "fal-ai/pixverse/swap";
    protected override string OutputSuffix => "swap";
}
