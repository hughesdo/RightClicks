namespace RightClicksClipEditor.Models;

/// <summary>
/// User preferences for clip editor
/// </summary>
public class ClipEditorSettings
{
    // Output settings
    public string VideoOutputFormat { get; set; } = "mp4";
    public string AudioOutputFormat { get; set; } = "mp3";
    public string VideoCodec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "libmp3lame";
    public int VideoQuality { get; set; } = 18; // CRF
    public int AudioBitrate { get; set; } = 192;
    
    // Naming
    public string NamingPattern { get; set; } = "{filename}_clip_{index}";
    
    // Output location
    public bool UseSameFolder { get; set; } = true;
    public string CustomOutputFolder { get; set; } = "";
    
    // Behavior
    public bool UseStreamCopy { get; set; } = false;
    public bool LoopSelection { get; set; } = false;
    public double DefaultZoomLevel { get; set; } = 1.0;
    
    // Window state
    public double WindowWidth { get; set; } = 1000;
    public double WindowHeight { get; set; } = 700;
}

