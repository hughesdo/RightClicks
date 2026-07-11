using RightClicks.Features.FalType;

namespace RightClicks.Features.Text;

/// <summary>
/// Text-to-Video via ByteDance Seedance 2.0 (native audio, multi-shot). Right-click a .txt; its
/// contents pre-fill the prompt. NOTE: endpoint id is unverified — confirm against the fal playground.
/// </summary>
public class TextToVideoSeedance20Feature : FalTypeFeatureBase
{
    public override string Id => "TextToVideoSeedance20";

    public override string DisplayName => "Text to Video > ☁️ Seedance 2.0 ~$0.30/s";

    public override string Description => "Text-to-video from a prompt file (ByteDance Seedance 2.0). Native audio, multi-shot.";

    public override string[] SupportedExtensions => new[] { ".txt" };

    protected override string CategoryFolder => "TextToVideo";
    protected override string CategoryTitle => "Text to Video";
    protected override string DefaultModelId => "bytedance/seedance-2.0/text-to-video";
    protected override string OutputSuffix => "t2v";
}
