using RightClicks.Features.FalType;

namespace RightClicks.Features.Video;

/// <summary>
/// Video-to-Video via Pixverse v3.5 Transition — morphs between a first-frame and last-frame
/// image to create a transition clip. Its inputs are two IMAGES (output is video), so it is
/// exposed on image right-clicks; the clicked image pre-fills the First Frame slot.
/// </summary>
public class VideoToVideoPixverseTransitionFeature : FalTypeFeatureBase
{
    public override string Id => "VideoToVideoPixverseTransition";

    public override string DisplayName => "Video to Video > ☁️ Pixverse v3.5 Transition ~$0.15-0.40/5s";

    public override string Description => "Morph between a first-frame and last-frame image into a transition clip.";

    public override string[] SupportedExtensions => new[] { ".jpg", ".jpeg", ".png", ".webp" };

    protected override string CategoryFolder => "VideoToVideo";
    protected override string CategoryTitle => "Video to Video";
    protected override string DefaultModelId => "fal-ai/pixverse/v3.5/transition";
    protected override string OutputSuffix => "v2v";
}
