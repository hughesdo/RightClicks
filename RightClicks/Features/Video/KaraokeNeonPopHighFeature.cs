using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with NeonPop style and Whisper Turbo model (High tier).
/// </summary>
public class KaraokeNeonPopHighFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeNeonPopHigh";
    public override string DisplayName => "Karaoke > Neon Pop > High (best quality, ~6 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Neon Pop style using Whisper Turbo model";
    protected override string StyleName => "NeonPop";
    protected override GgmlType WhisperModelType => GgmlType.LargeV3Turbo;
}

