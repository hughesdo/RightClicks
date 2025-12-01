namespace RightClicks.Models.Whisper;

/// <summary>
/// Result of a Whisper transcription operation.
/// </summary>
public class WhisperTranscriptionResult
{
    /// <summary>
    /// Full transcription text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Individual transcription segments with timestamps.
    /// </summary>
    public List<WhisperSegment> Segments { get; set; } = new();

    /// <summary>
    /// Total duration of the transcription process in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Whisper model type used (e.g., "Tiny", "Base", "Small").
    /// </summary>
    public string ModelType { get; set; } = string.Empty;
}

/// <summary>
/// Individual transcription segment with timestamp.
/// </summary>
public class WhisperSegment
{
    /// <summary>
    /// Start time of the segment.
    /// </summary>
    public TimeSpan Start { get; set; }

    /// <summary>
    /// End time of the segment.
    /// </summary>
    public TimeSpan End { get; set; }

    /// <summary>
    /// Transcribed text for this segment.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence probability (0.0 to 1.0).
    /// Optional - only available if WithProbabilities() is enabled.
    /// </summary>
    public float? Probability { get; set; }
}

