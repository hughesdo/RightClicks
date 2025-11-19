using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Whisper Base model transcription feature.
/// Fast model with low VRAM requirement (~1 GB).
/// Good balance of speed and accuracy for most use cases.
/// </summary>
public class WhisperBaseTranscribeFeature : WhisperTranscribeFeatureBase
{
    public override string Id => "WhisperBaseTranscribe";

    public override string DisplayName => "Transcribe > Base (fast, ~1 GB VRAM)";

    public override string Description => "Transcribe audio/video using Whisper Base model (fast, good accuracy)";

    protected override GgmlType WhisperModelType => GgmlType.BaseEn;
}

