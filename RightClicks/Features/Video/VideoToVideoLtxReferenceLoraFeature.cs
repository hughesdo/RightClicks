using RightClicks.Features.FalType;

namespace RightClicks.Features.Video;

/// <summary>
/// Video-to-Video via LTX 2.3 Quality Reference Video-to-Video (LoRA) — restyle a source clip
/// guided by a selectable LoRA (e.g. 3DREAL for render-to-real).
/// </summary>
public class VideoToVideoLtxReferenceLoraFeature : FalTypeFeatureBase
{
    public override string Id => "VideoToVideoLtxReferenceLora";

    public override string DisplayName => "Video to Video > ☁️ LTX 2.3 Quality Reference (LoRA) ~$0.06/s";

    public override string Description => "Transform a source video guided by a selectable LoRA.";

    public override string[] SupportedExtensions => new[] { ".mp4", ".mov", ".webm", ".m4v" };

    protected override string CategoryFolder => "VideoToVideo";
    protected override string CategoryTitle => "Video to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3-quality/reference-video-to-video/lora";
    protected override string OutputSuffix => "v2v";
}
