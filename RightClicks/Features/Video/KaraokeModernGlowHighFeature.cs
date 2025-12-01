using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with ModernGlow style and Whisper Turbo model (High tier).
/// </summary>
public class KaraokeModernGlowHighFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeModernGlowHigh";
    public override string DisplayName => "Karaoke > Modern Glow > High (best quality, ~6 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Modern Glow style using Whisper Turbo model";
    protected override string StyleName => "ModernGlow";
    protected override GgmlType WhisperModelType => GgmlType.LargeV3Turbo;
}

