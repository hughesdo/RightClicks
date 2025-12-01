using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with ModernGlow style and Whisper Small model (Medium tier).
/// </summary>
public class KaraokeModernGlowMediumFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeModernGlowMedium";
    public override string DisplayName => "Karaoke > Modern Glow > Medium (balanced, ~2 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Modern Glow style using Whisper Small model";
    protected override string StyleName => "ModernGlow";
    protected override GgmlType WhisperModelType => GgmlType.SmallEn;
}

