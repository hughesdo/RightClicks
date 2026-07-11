using RightClicks.Features.FalType;

namespace RightClicks.Features.Image;

/// <summary>
/// Face Swap — swaps the face from one image onto the right-clicked IMAGE. Image-output; the result
/// is saved directly (no re-encode/mux).
/// </summary>
public class SwapFaceFeature : FalTypeFeatureBase
{
    public override string Id => "SwapFace";

    public override string DisplayName => "Swaps > ☁️ Face Swap ~$0.01";

    public override string Description => "Swap the face from one image onto another. Image-output.";

    public override string[] SupportedExtensions => new[]
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    protected override string CategoryFolder => "Swaps";
    protected override string CategoryTitle => "Swaps";
    protected override string DefaultModelId => "fal-ai/face-swap";
    protected override string OutputSuffix => "swap";
}
