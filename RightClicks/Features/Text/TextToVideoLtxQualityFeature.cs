using RightClicks.Features.FalType;

namespace RightClicks.Features.Text;

/// <summary>
/// Text-to-Video via LTX 2.3 Quality (distilled single-stage). Right-click a .txt; its contents pre-fill the prompt.
/// </summary>
public class TextToVideoLtxQualityFeature : FalTypeFeatureBase
{
    public override string Id => "TextToVideoLtxQuality";

    public override string DisplayName => "Text to Video > ☁️ LTX 2.3 Quality ~$0.06/s";

    public override string Description => "Distilled single-stage text-to-video from a prompt file (LTX 2.3 Quality).";

    public override string[] SupportedExtensions => new[] { ".txt" };

    protected override string CategoryFolder => "TextToVideo";
    protected override string CategoryTitle => "Text to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3-quality/text-to-video";
    protected override string OutputSuffix => "t2v";
}
