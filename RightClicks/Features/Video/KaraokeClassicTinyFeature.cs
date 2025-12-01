using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with Classic style and Whisper Tiny model.
/// </summary>
public class KaraokeClassicTinyFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeClassicTiny";
    public override string DisplayName => "Karaoke > Classic > Tiny (fastest, ~1 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Classic style using Whisper Tiny model";
    protected override string StyleName => "Classic";
    protected override GgmlType WhisperModelType => GgmlType.TinyEn;
}

