using RightClicks.Features.FalType;

namespace RightClicks.Features.Video;

/// <summary>
/// Video-to-Video via LTX 2.3 Retake Video — regenerates a fresh variation ("take") of a clip.
/// </summary>
public class VideoToVideoLtxRetakeFeature : FalTypeFeatureBase
{
    public override string Id => "VideoToVideoLtxRetake";

    public override string DisplayName => "Video to Video > ☁️ LTX 2.3 Retake Video ~$0.06/s";

    public override string Description => "Regenerate / vary an existing clip — a fresh take of the same shot.";

    public override string[] SupportedExtensions => new[] { ".mp4", ".mov", ".webm", ".m4v" };

    protected override string CategoryFolder => "VideoToVideo";
    protected override string CategoryTitle => "Video to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3/retake-video";
    protected override string OutputSuffix => "v2v";
}
