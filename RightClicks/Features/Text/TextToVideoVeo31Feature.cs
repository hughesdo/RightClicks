using RightClicks.Features.FalType;

namespace RightClicks.Features.Text;

/// <summary>
/// Text-to-Video via Google Veo 3.1 (premium, strong audio/lip-sync). Right-click a .txt; its
/// contents pre-fill the prompt. NOTE: endpoint id is unverified — confirm against the fal playground.
/// </summary>
public class TextToVideoVeo31Feature : FalTypeFeatureBase
{
    public override string Id => "TextToVideoVeo31";

    public override string DisplayName => "Text to Video > ☁️ Veo 3.1 ~$0.20/s";

    public override string Description => "Premium text-to-video from a prompt file (Google Veo 3.1). Strong audio and lip-sync.";

    public override string[] SupportedExtensions => new[] { ".txt" };

    protected override string CategoryFolder => "TextToVideo";
    protected override string CategoryTitle => "Text to Video";
    protected override string DefaultModelId => "fal-ai/veo3.1/text-to-video";
    protected override string OutputSuffix => "t2v";
}
