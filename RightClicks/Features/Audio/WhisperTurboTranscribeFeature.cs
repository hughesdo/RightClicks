using Whisper.net;
using Whisper.net.Ggml;
using GgmlType = Whisper.net.Ggml.GgmlType;

namespace RightClicks.Features.Audio;

/// <summary>
/// Whisper Turbo model transcription feature.
/// Near-large accuracy with significantly improved speed (~6 GB VRAM).
/// Best balance of speed and quality for production use.
/// </summary>
public class WhisperTurboTranscribeFeature : WhisperTranscribeFeatureBase
{
    public override string Id => "WhisperTurboTranscribe";

    public override string DisplayName => "Transcribe > Turbo (fast + accurate, ~6 GB VRAM)";

    public override string Description => "Transcribe audio/video using Whisper Turbo model (fast with near-large accuracy)";

    protected override GgmlType WhisperModelType => GgmlType.LargeV3Turbo;
}

