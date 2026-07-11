using RightClicks.Features.FalType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Audio-to-Video via base LTX 2.3 (no custom LoRA). Exposed on both audio and image right-clicks.
/// </summary>
public class AudioToVideoLtxFeature : FalTypeFeatureBase
{
    public override string Id => "AudioToVideoLtx";

    public override string DisplayName => "Audio to Video > ☁️ LTX 2.3 ~$0.08/s";

    public override string Description => "Audio-reactive video from an audio clip + first-frame image (base LTX 2.3).";

    public override string[] SupportedExtensions => new[]
    {
        ".wav", ".mp3", ".m4a", ".aac", ".ogg", ".flac",
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".avif"
    };

    protected override string CategoryFolder => "AudioToVideo";
    protected override string CategoryTitle => "Audio to Video";
    protected override string DefaultModelId => "fal-ai/ltx-2.3/audio-to-video";
    protected override string OutputSuffix => "a2v";
}
