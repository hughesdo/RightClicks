using RightClicks.Features.FalType;

namespace RightClicks.Features.Text;

/// <summary>
/// Text-to-Video via base LTX 2.3. Right-click a .txt; its contents pre-fill the prompt.
/// </summary>
public class TextToVideoLtxFeature : FalTypeFeatureBase
{
    public override string Id => "TextToVideoLtx";

    public override string DisplayName => "Text to Video > ☁️ LTX 2.3 ~$0.06/s";

    public override string Description => "Text-to-video from a prompt file (base LTX 2.3). Native audio, 1080p+.";

    public override string[] SupportedExtensions => new[] { ".txt" };

    protected override string CategoryFolder => "TextToVideo";
    protected override string CategoryTitle => "Text to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3/text-to-video";
    protected override string OutputSuffix => "t2v";
}
