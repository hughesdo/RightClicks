using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Video;

/// <summary>
/// Karaoke subtitle rendering with Classic style and Whisper Small model (Medium tier).
/// </summary>
public class KaraokeClassicMediumFeature : KaraokeFeatureBase
{
    public override string Id => "KaraokeClassicMedium";
    public override string DisplayName => "Karaoke > Classic > Medium (balanced, ~2 GB VRAM)";
    public override string Description => "Generate karaoke-style subtitled video with Classic style using Whisper Small model";
    protected override string StyleName => "Classic";
    protected override GgmlType WhisperModelType => GgmlType.SmallEn;
}

