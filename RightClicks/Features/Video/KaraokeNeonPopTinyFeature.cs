using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with NeonPop style and Whisper Tiny model.
/// </summary>
public class KaraokeNeonPopTinyFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeNeonPopTiny";
    public override string DisplayName => "Karaoke > Neon Pop > Tiny (fastest, ~1 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Neon Pop style using Whisper Tiny model";
    protected override string StyleName => "NeonPop";
    protected override GgmlType WhisperModelType => GgmlType.TinyEn;
}

