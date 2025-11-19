using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Whisper Tiny model transcription feature.
/// Fastest model with lowest VRAM requirement (~1 GB).
/// Best for quick transcriptions where accuracy is less critical.
/// </summary>
public class WhisperTinyTranscribeFeature : WhisperTranscribeFeatureBase
{
    public override string Id => "WhisperTinyTranscribe";

    public override string DisplayName => "Transcribe > Tiny (fastest, ~1 GB VRAM)";

    public override string Description => "Transcribe audio/video using Whisper Tiny model (fastest, basic accuracy)";

    protected override GgmlType WhisperModelType => GgmlType.TinyEn;
}

