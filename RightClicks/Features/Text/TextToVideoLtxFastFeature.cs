using RightClicks.Features.FalType;

namespace RightClicks.Features.Text;

/// <summary>
/// Text-to-Video via LTX 2.3 Fast (speed-optimized). Right-click a .txt; its contents pre-fill the prompt.
/// </summary>
public class TextToVideoLtxFastFeature : FalTypeFeatureBase
{
    public override string Id => "TextToVideoLtxFast";

    public override string DisplayName => "Text to Video > ☁️ LTX 2.3 Fast ~$0.04/s";

    public override string Description => "Speed-optimized text-to-video from a prompt file (LTX 2.3 Fast).";

    public override string[] SupportedExtensions => new[] { ".txt" };

    protected override string CategoryFolder => "TextToVideo";
    protected override string CategoryTitle => "Text to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3/text-to-video/fast";
    protected override string OutputSuffix => "t2v";
}
