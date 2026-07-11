using RightClicks.Features.FalType;

namespace RightClicks.Features.Image;

/// <summary>
/// Easel Advanced Face Swap — higher-quality face swap onto the right-clicked IMAGE with gender +
/// workflow hints. Image-output; the result is saved directly (no re-encode/mux).
/// </summary>
public class SwapEaselFaceFeature : FalTypeFeatureBase
{
    public override string Id => "SwapEaselFace";

    public override string DisplayName => "Swaps > ☁️ Easel Advanced Face Swap ~$0.01";

    public override string Description => "Higher-quality face swap with gender and workflow control. Image-output.";

    public override string[] SupportedExtensions => new[]
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    protected override string CategoryFolder => "Swaps";
    protected override string CategoryTitle => "Swaps";
    protected override string DefaultModelId => "easel-ai/advanced-face-swap";
    protected override string OutputSuffix => "swap";
}
