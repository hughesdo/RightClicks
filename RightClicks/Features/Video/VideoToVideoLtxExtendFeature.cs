using RightClicks.Features.FalType;

namespace RightClicks.Features.Video;

/// <summary>
/// Video-to-Video via LTX 2.3 Extend Video — adds generated frames onto a source clip,
/// optionally toward an End Image to make a transition.
/// </summary>
public class VideoToVideoLtxExtendFeature : FalTypeFeatureBase
{
    public override string Id => "VideoToVideoLtxExtend";

    public override string DisplayName => "Video to Video > ☁️ LTX 2.3 Extend Video ~$0.06/s";

    public override string Description => "Adds frames onto an existing clip; add an End Image to make a transition.";

    public override string[] SupportedExtensions => new[] { ".mp4", ".mov", ".webm", ".m4v" };

    protected override string CategoryFolder => "VideoToVideo";
    protected override string CategoryTitle => "Video to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3/extend-video";
    protected override string OutputSuffix => "v2v";
}
