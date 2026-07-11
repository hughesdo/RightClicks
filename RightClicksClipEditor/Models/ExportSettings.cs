namespace RightClicksClipEditor.Models;

/// <summary>
/// Export configuration for clip export
/// </summary>
public class ExportSettings
{
    public string? OutputFormat { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int Quality { get; set; } = 18; // CRF for video
    public int AudioBitrate { get; set; } = 192;
    public bool UseStreamCopy { get; set; } = false;
}

