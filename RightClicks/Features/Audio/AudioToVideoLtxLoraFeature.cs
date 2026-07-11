using RightClicks.Features.FalType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Audio-to-Video via LTX 2.3 Quality (LoRA) — the audio-reactive workflow Don uses most.
/// Exposed on BOTH audio and image right-clicks; whichever was clicked pre-fills its slot.
/// The user can switch models in the window; their choice wins.
/// </summary>
public class AudioToVideoLtxLoraFeature : FalTypeFeatureBase
{
    public override string Id => "AudioToVideoLtxLora";

    public override string DisplayName => "Audio to Video > ☁️ LTX 2.3 Quality (LoRA) ~$0.06/s";

    public override string Description => "Audio-reactive video from an audio clip + first-frame image, with a selectable LoRA.";

    public override string[] SupportedExtensions => new[]
    {
        ".wav", ".mp3", ".m4a", ".aac", ".ogg", ".flac",
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".avif"
    };

    protected override string CategoryFolder => "AudioToVideo";
    protected override string CategoryTitle => "Audio to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3-quality/audio-to-video/lora";
    protected override string OutputSuffix => "a2v";
}
