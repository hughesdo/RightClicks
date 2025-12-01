using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with Classic style and Whisper Turbo model (High tier).
/// </summary>
public class KaraokeClassicHighFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeClassicHigh";
    public override string DisplayName => "Karaoke > Classic > High (best quality, ~6 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Classic style using Whisper Turbo model";
    protected override string StyleName => "Classic";
    protected override GgmlType WhisperModelType => GgmlType.LargeV3Turbo;
}

