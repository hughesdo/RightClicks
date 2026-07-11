namespace RightClicks.Features.Image;

/// <summary>
/// Generate video from an image using Vidu Q3 via fal.ai.
/// The user can switch to any other model in the configuration window - this feature
/// only decides which model the window opens on.
/// </summary>
public class ImageToVideoViduQ3Feature : ImageToVideoFeatureBase
{
    public override string Id => "ImageToVideoViduQ3";

    public override string DisplayName => "Image to Video > ☁️ Vidu Q3 ~$0.07/s";

    public override string Description => "Generate video from image using Vidu Q3 (cheapest at low resolution, good stylised motion)";

    protected override string DefaultModelId => "fal-ai/vidu/q3/image-to-video";
}
