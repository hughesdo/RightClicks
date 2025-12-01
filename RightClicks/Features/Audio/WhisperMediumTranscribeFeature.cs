using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Whisper Medium model transcription feature.
/// High accuracy model with higher VRAM requirement (~5 GB).
/// Slower but produces more accurate transcriptions.
/// </summary>
public class WhisperMediumTranscribeFeature : WhisperTranscribeFeatureBase
{
    public override string Id => "WhisperMediumTranscribe";

    public override string DisplayName => "Transcribe > Medium (accurate, ~5 GB VRAM)";

    public override string Description => "Transcribe audio/video using Whisper Medium model (slower, high accuracy)";

    protected override GgmlType WhisperModelType => GgmlType.MediumEn;
}

