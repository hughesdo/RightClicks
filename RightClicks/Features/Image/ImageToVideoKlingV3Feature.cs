namespace RightClicks.Features.Image;

/// <summary>
/// Generate video from an image using Kling v3 (standard tier) via fal.ai.
/// The user can switch to any other model in the configuration window - this feature
/// only decides which model the window opens on.
/// </summary>
public class ImageToVideoKlingV3Feature : ImageToVideoFeatureBase
{
    public override string Id => "ImageToVideoKlingV3";

    public override string DisplayName => "Image to Video > ☁️ Kling v3 ~$0.13/s";

    public override string Description => "Generate video from image using Kling v3 (strong motion, native audio, up to 15s)";

    protected override string DefaultModelId => "fal-ai/kling-video/v3/standard/image-to-video";
}
