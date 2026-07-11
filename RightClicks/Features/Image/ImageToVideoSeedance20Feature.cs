namespace RightClicks.Features.Image;

/// <summary>
/// Generate video from an image using ByteDance Seedance 2.0 via fal.ai.
/// The user can switch to any other model in the configuration window - this feature
/// only decides which model the window opens on.
/// </summary>
public class ImageToVideoSeedance20Feature : ImageToVideoFeatureBase
{
    public override string Id => "ImageToVideoSeedance20";

    public override string DisplayName => "Image to Video > ☁️ Seedance 2.0 ~$0.30/s";

    public override string Description => "Generate video from image using Seedance 2.0 (best realism, premium pricing)";

    protected override string DefaultModelId => "bytedance/seedance-2.0/image-to-video";
}
