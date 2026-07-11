using System.IO;
using FFMpegCore;

namespace RightClicksClipEditor.Models;

/// <summary>
/// Media file metadata
/// </summary>
public class MediaInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public MediaType Type { get; set; }
    
    // Video-specific
    public int Width { get; set; }
    public int Height { get; set; }
    public double FrameRate { get; set; }
    public string VideoCodec { get; set; } = "";
    
    // Audio-specific
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public string AudioCodec { get; set; } = "";
    public int BitRate { get; set; }
    
    public static async Task<MediaInfo> AnalyzeAsync(string filePath)
    {
        var mediaInfo = await FFProbe.AnalyseAsync(filePath);
        
        return new MediaInfo
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Duration = mediaInfo.Duration,
            Type = mediaInfo.PrimaryVideoStream != null ? MediaType.Video : MediaType.Audio,
            Width = mediaInfo.PrimaryVideoStream?.Width ?? 0,
            Height = mediaInfo.PrimaryVideoStream?.Height ?? 0,
            FrameRate = mediaInfo.PrimaryVideoStream?.FrameRate ?? 0,
            VideoCodec = mediaInfo.PrimaryVideoStream?.CodecName ?? "",
            SampleRate = mediaInfo.PrimaryAudioStream?.SampleRateHz ?? 0,
            Channels = mediaInfo.PrimaryAudioStream?.Channels ?? 0,
            AudioCodec = mediaInfo.PrimaryAudioStream?.CodecName ?? "",
            BitRate = (int)(mediaInfo.PrimaryAudioStream?.BitRate ?? 0)
        };
    }
}

