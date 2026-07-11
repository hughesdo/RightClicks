namespace RightClicks.Features.Image;

/// <summary>
/// Generate video from an image using Alibaba Wan 2.7 via fal.ai.
/// The user can switch to any other model in the configuration window - this feature
/// only decides which model the window opens on.
/// </summary>
public class ImageToVideoWan27Feature : ImageToVideoFeatureBase
{
    public override string Id => "ImageToVideoWan27";

    public override string DisplayName => "Image to Video > ☁️ Wan 2.7 ~$0.10/s";

    public override string Description => "Generate video from image using Wan 2.7 (fast, affordable, strong physical motion)";

    protected override string DefaultModelId => "fal-ai/wan/v2.7/image-to-video";
}
