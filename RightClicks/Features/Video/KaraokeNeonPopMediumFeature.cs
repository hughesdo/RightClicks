using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with NeonPop style and Whisper Small model (Medium tier).
/// </summary>
public class KaraokeNeonPopMediumFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeNeonPopMedium";
    public override string DisplayName => "Karaoke > Neon Pop > Medium (balanced, ~2 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Neon Pop style using Whisper Small model";
    protected override string StyleName => "NeonPop";
    protected override GgmlType WhisperModelType => GgmlType.SmallEn;
}

