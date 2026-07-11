namespace RightClicksClipEditor.Models;

/// <summary>
/// Represents a clip segment with IN and OUT points
/// </summary>
public class ClipSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool IsEnabled { get; set; } = true;
    
    public string DisplayName => $"{FormatTime(StartTime)} → {FormatTime(EndTime)} ({Duration.TotalSeconds:F2}s)";
    
    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return time.ToString(@"h\:mm\:ss\.fff");
        }
        else
        {
            return time.ToString(@"mm\:ss\.fff");
        }
    }
    
    public override string ToString() => DisplayName;
}

