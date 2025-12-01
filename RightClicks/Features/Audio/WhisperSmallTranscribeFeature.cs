using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Whisper Small model transcription feature.
/// Balanced model with moderate VRAM requirement (~2 GB).
/// Better accuracy than Tiny/Base with reasonable speed.
/// </summary>
public class WhisperSmallTranscribeFeature : WhisperTranscribeFeatureBase
{
    public override string Id => "WhisperSmallTranscribe";

    public override string DisplayName => "Transcribe > Small (balanced, ~2 GB VRAM)";

    public override string Description => "Transcribe audio/video using Whisper Small model (balanced speed and accuracy)";

    protected override GgmlType WhisperModelType => GgmlType.SmallEn;
}

