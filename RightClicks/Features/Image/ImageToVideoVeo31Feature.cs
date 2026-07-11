namespace RightClicks.Features.Image;

/// <summary>
/// Generate video from an image using Google Veo 3.1 (fast tier) via fal.ai.
/// The user can switch to any other model in the configuration window - this feature
/// only decides which model the window opens on.
/// </summary>
public class ImageToVideoVeo31Feature : ImageToVideoFeatureBase
{
    public override string Id => "ImageToVideoVeo31";

    public override string DisplayName => "Image to Video > ☁️ Veo 3.1 Fast ~$0.15/s";

    public override string Description => "Generate video from image using Veo 3.1 Fast (best prompt understanding, dialogue-quality audio)";

    protected override string DefaultModelId => "fal-ai/veo3.1/fast/image-to-video";
}
