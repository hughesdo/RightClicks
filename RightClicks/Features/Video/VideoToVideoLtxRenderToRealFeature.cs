using RightClicks.Features.FalType;

namespace RightClicks.Features.Video;

/// <summary>
/// Video-to-Video via LTX 2.3 Quality Render-to-Real — turns a grey 3D blockout / CG render
/// into a photoreal clip. Pairs with the 3DREAL LoRA and detail refine.
/// </summary>
public class VideoToVideoLtxRenderToRealFeature : FalTypeFeatureBase
{
    public override string Id => "VideoToVideoLtxRenderToReal";

    public override string DisplayName => "Video to Video > ☁️ LTX 2.3 Quality Render-to-Real ~$0.06/s";

    public override string Description => "Turns a grey 3D blockout / CG render into a photoreal clip.";

    public override string[] SupportedExtensions => new[] { ".mp4", ".mov", ".webm", ".m4v" };

    protected override string CategoryFolder => "VideoToVideo";
    protected override string CategoryTitle => "Video to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3-quality/render-to-real";
    protected override string OutputSuffix => "v2v";
}
