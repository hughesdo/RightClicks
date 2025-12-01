using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Whisper Large model transcription feature.
/// Highest accuracy model with highest VRAM requirement (~10 GB).
/// Slowest but produces the best quality transcriptions.
/// </summary>
public class WhisperLargeTranscribeFeature : WhisperTranscribeFeatureBase
{
    public override string Id => "WhisperLargeTranscribe";

    public override string DisplayName => "Transcribe > Large (best quality, ~10 GB VRAM)";

    public override string Description => "Transcribe audio/video using Whisper Large V3 model (slowest, best accuracy)";

    protected override GgmlType WhisperModelType => GgmlType.LargeV3;
}

