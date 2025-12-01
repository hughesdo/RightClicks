using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with ModernGlow style and Whisper Tiny model.
/// </summary>
public class KaraokeModernGlowTinyFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeModernGlowTiny";
    public override string DisplayName => "Karaoke > Modern Glow > Tiny (fastest, ~1 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Modern Glow style using Whisper Tiny model";
    protected override string StyleName => "ModernGlow";
    protected override GgmlType WhisperModelType => GgmlType.TinyEn;
}

