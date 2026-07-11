# Lightweight Clip Editing - Technical Specification

**Status:** 🟢 Ready for Implementation  
**Created:** 2026-01-14  
**Target:** Standalone WPF application integrated with RightClicks

---

## Executive Summary

This specification defines a **standalone clip editing application** that provides frame-accurate video clipping and sample-accurate audio clipping through an intuitive timeline-based interface. The application is accessible both via Windows Explorer context menus (right-click integration) and as a standalone executable.

### Key Design Principles

1. **Standalone Architecture** - Self-contained WPF application, not a feature within RightClicks
2. **Timeline-Centric UI** - Timeline dominates the interface (70% of screen space)
3. **Non-Destructive** - Original files are never modified
4. **Multiple Clips Per Session** - Save multiple segments before closing
5. **Frame/Sample Precision** - Frame-accurate for video, sample-accurate for audio
6. **Minimal Dependencies** - Reuse existing RightClicks infrastructure (FFmpeg, NAudio, Serilog)

---

## Architecture Overview

### Project Structure

```
RightClicks.sln
├── RightClicks/                          # Main application (existing)
├── RightClicksShellExtension/            # Shell extension (existing)
├── RightClicksShellInstaller/            # Installer (existing)
├── RightClicksShellManager/              # Manager (existing)
└── RightClicksClipEditor/                # NEW - Standalone clip editor
    ├── RightClicksClipEditor.csproj      # .NET 8 WPF project
    ├── App.xaml                          # Application entry point
    ├── App.xaml.cs
    ├── Windows/
    │   ├── VideoClipEditorWindow.xaml    # Video editor UI
    │   ├── VideoClipEditorWindow.xaml.cs
    │   ├── AudioClipEditorWindow.xaml    # Audio editor UI
    │   └── AudioClipEditorWindow.xaml.cs
    ├── Controls/
    │   ├── TimelineControl.xaml          # Shared timeline component
    │   ├── TimelineControl.xaml.cs
    │   ├── WaveformControl.xaml          # Audio waveform visualization
    │   └── WaveformControl.xaml.cs
    ├── Services/
    │   ├── MediaPlayerService.cs         # Video/audio playback
    │   ├── WaveformGeneratorService.cs   # Waveform rendering
    │   ├── ClipExportService.cs          # FFmpeg clip extraction
    │   └── SettingsService.cs            # User preferences
    ├── Models/
    │   ├── ClipSegment.cs                # Represents a clip (IN/OUT points)
    │   ├── MediaInfo.cs                  # Media file metadata
    │   └── ExportSettings.cs             # Export configuration
    └── Resources/
        └── Icons/                        # Application icons
```

### Integration with RightClicks

**Context Menu Integration:**
```
Right-click video.mp4
├── RightClicks ▶
│   ├── Video Clip Editor...        ⭐ NEW - Launches RightClicksClipEditor.exe
│   ├── ─────────────────
│   ├── Extract MP3
│   └── ...

Right-click audio.mp3
├── RightClicks ▶
│   ├── Audio Clip Editor...        ⭐ NEW - Launches RightClicksClipEditor.exe
│   ├── ─────────────────
│   ├── RVC ▶
│   └── ...
```

**Launch Mechanism:**
```csharp
// In RightClicks/Features/Clipping/VideoClipFeature.cs
public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken ct)
{
    // Launch standalone clip editor
    var clipEditorPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RightClicksClipEditor.exe");
    
    var startInfo = new ProcessStartInfo
    {
        FileName = clipEditorPath,
        Arguments = $"--video \"{filePath}\"",
        UseShellExecute = true
    };
    
    Process.Start(startInfo);
    
    return FeatureResult.CreateInformational("Clip editor launched");
}
```

---

## Component Design

### 1. VideoClipEditorWindow

**Purpose:** Frame-accurate video clipping with visual preview

**Key Features:**
- Embedded video player (LibVLCSharp or WPF MediaElement)
- Zoomable timeline with draggable IN/OUT markers
- Frame-by-frame stepping (detects source FPS)
- Multiple clip segments per session
- Export with re-encoding or stream copy

**UI Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│ Video Clip Editor - filename.mp4                      [_][□][X] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│              ┌─────────────────────────┐                    │
│              │   Video Preview         │                    │
│              │   (MediaElement)        │                    │
│              │   1920x1080 @ 00:01:23  │                    │
│              └─────────────────────────┘                    │
│                                                             │
│  Transport Controls:                                       │
│  [◄◄] [◄] [▶] [▶▶]  [====|████████████|=======]  [🔊] [⚙️] │
│   -1s  -1f Play +1f +1s                                    │
│                                                             │
│  Timeline (Zoomable):                                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ ▼IN                                            OUT▼   │  │
│  │ ├─────────────────────────────────────────────────┤  │  │
│  │ 0:00        0:30        1:00        1:30      2:00  │  │
│  │ │░░░░░░░░░░░█████████████████████░░░░░░░░░░░░░░░│  │  │
│  │ │           ▲ Playhead (00:45.250)              │  │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  Clips to Save:                                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ ☑ Clip 1: 00:15.500 → 00:42.750 (27.25s)  [Remove]  │  │
│  │ ☑ Clip 2: 01:05.000 → 01:18.333 (13.33s)  [Remove]  │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  [+ Add Current Selection]  [Save All Clips]  [Close]      │
└─────────────────────────────────────────────────────────────┘
```

**XAML Structure:**
```xml
<Window x:Class="RightClicksClipEditor.Windows.VideoClipEditorWindow"
        Title="Video Clip Editor"
        Width="1000" Height="700"
        MinWidth="800" MinHeight="600">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>      <!-- Title bar -->
            <RowDefinition Height="300"/>       <!-- Video preview -->
            <RowDefinition Height="Auto"/>      <!-- Transport controls -->
            <RowDefinition Height="150"/>       <!-- Timeline -->
            <RowDefinition Height="*"/>         <!-- Clip list -->
            <RowDefinition Height="Auto"/>      <!-- Action buttons -->
        </Grid.RowDefinitions>
        
        <!-- Video preview -->
        <MediaElement x:Name="VideoPlayer" Grid.Row="1"/>
        
        <!-- Timeline control -->
        <local:TimelineControl x:Name="Timeline" Grid.Row="3"/>
        
        <!-- Clip list -->
        <ListBox x:Name="ClipList" Grid.Row="4"/>
    </Grid>
</Window>
```

---

### 2. AudioClipEditorWindow

**Purpose:** Sample-accurate audio clipping with waveform visualization

**Key Features:**
- NAudio-based waveform rendering
- Zoomable waveform timeline
- Loop playback of selection
- Sub-second stepping (10ms, 100ms, 1s)
- Multiple clip segments per session

**UI Layout:**
```
┌─────────────────────────────────────────────────────────────┐
│ Audio Clip Editor - song.mp3                          [_][□][X] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Transport Controls:                                       │
│  [◄◄] [◄] [▶] [▶▶]  [====|████████████|=======]  [🔊] [⚙️] │
│   -1s -10ms Play +10ms +1s                                 │
│                                                             │
│  Waveform (Zoomable):                                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ ▼IN                                            OUT▼   │  │
│  │ ├─────────────────────────────────────────────────┤  │  │
│  │ │     ╱╲    ╱╲╱╲  ╱╲      ╱╲    ╱╲╱╲  ╱╲        │  │  │
│  │ │    ╱  ╲  ╱    ╲╱  ╲    ╱  ╲  ╱    ╲╱  ╲       │  │  │
│  │ │───╱────╲╱──────────╲──╱────╲╱──────────╲──────│  │  │
│  │ │                     ▲ Playhead (00:45.250)     │  │  │
│  │ 0:00        0:30        1:00        1:30      2:00  │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  Selection: 00:15.500 → 00:42.750 (27.25s)                 │
│  [🔁 Loop Selection]  [🔇 Mute Outside Selection]          │
│                                                             │
│  Clips to Save:                                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ ☑ Clip 1: 00:15.500 → 00:42.750 (27.25s)  [Remove]  │  │
│  │ ☑ Clip 2: 01:05.000 → 01:18.333 (13.33s)  [Remove]  │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  [+ Add Current Selection]  [Save All Clips]  [Close]      │
└─────────────────────────────────────────────────────────────┘
```

---

### 3. TimelineControl (Shared Component)

**Purpose:** Reusable timeline with draggable markers and zoom

**Public API:**
```csharp
public class TimelineControl : UserControl
{
    // Properties
    public TimeSpan Duration { get; set; }
    public TimeSpan CurrentPosition { get; set; }
    public TimeSpan InPoint { get; set; }
    public TimeSpan OutPoint { get; set; }
    public double ZoomLevel { get; set; } // 1.0 = fit to width

    // Events
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<TimeSpan>? InPointChanged;
    public event EventHandler<TimeSpan>? OutPointChanged;
    public event EventHandler<double>? ZoomChanged;

    // Methods
    public void SetDuration(TimeSpan duration);
    public void SeekTo(TimeSpan position);
    public void ZoomIn();
    public void ZoomOut();
    public void FitToWidth();
}
```

**Interaction Model:**
- **Click timeline** → Seek playhead to position
- **Drag IN/OUT markers** → Adjust selection
- **Mouse wheel** → Zoom in/out (centered on mouse)
- **Shift + Mouse wheel** → Horizontal scroll (when zoomed)
- **Double-click marker** → Jump playhead to marker

---

### 4. WaveformControl (Audio-Specific)

**Purpose:** Render audio waveform using NAudio

**Implementation:**
```csharp
public class WaveformControl : UserControl
{
    private WaveformRenderer _renderer;
    private WriteableBitmap _waveformBitmap;

    public void LoadAudio(string filePath)
    {
        using var reader = new AudioFileReader(filePath);

        // Generate waveform data
        var waveformData = GenerateWaveformData(reader);

        // Render to bitmap
        _waveformBitmap = RenderWaveform(waveformData);

        // Display in Image control
        WaveformImage.Source = _waveformBitmap;
    }

    private float[] GenerateWaveformData(AudioFileReader reader)
    {
        // Sample audio at regular intervals
        // Return array of peak values for visualization
    }

    private WriteableBitmap RenderWaveform(float[] data)
    {
        // Draw waveform as vertical bars
        // Use gradient colors for visual appeal
    }
}
```

**NAudio Integration:**
```csharp
// Use NAudio.WaveFormRenderer (if available) or custom implementation
var renderer = new WaveFormRenderer();
var image = renderer.Render(audioFilePath, new WaveFormRendererSettings
{
    Width = 1000,
    TopHeight = 100,
    BottomHeight = 100,
    TopPeakPen = new Pen(Color.Blue),
    BottomPeakPen = new Pen(Color.Blue)
});
```

---

## Service Layer Design

### 1. MediaPlayerService

**Purpose:** Unified playback for video and audio

**API:**
```csharp
public class MediaPlayerService : IDisposable
{
    private MediaElement? _videoPlayer;
    private WaveOutEvent? _audioPlayer;
    private AudioFileReader? _audioReader;

    public TimeSpan Duration { get; private set; }
    public TimeSpan Position { get; private set; }
    public bool IsPlaying { get; private set; }

    public event EventHandler<TimeSpan>? PositionChanged;

    public void LoadVideo(string filePath, MediaElement player);
    public void LoadAudio(string filePath);

    public void Play();
    public void Pause();
    public void Stop();
    public void Seek(TimeSpan position);

    public void StepForward(TimeSpan step);
    public void StepBackward(TimeSpan step);

    public void Dispose();
}
```

**Video Playback:**
```csharp
public void LoadVideo(string filePath, MediaElement player)
{
    _videoPlayer = player;
    _videoPlayer.Source = new Uri(filePath);
    _videoPlayer.LoadedBehavior = MediaState.Manual;
    _videoPlayer.MediaOpened += (s, e) =>
    {
        Duration = _videoPlayer.NaturalDuration.TimeSpan;
    };
}
```

**Audio Playback:**
```csharp
public void LoadAudio(string filePath)
{
    _audioReader = new AudioFileReader(filePath);
    _audioPlayer = new WaveOutEvent();
    _audioPlayer.Init(_audioReader);

    Duration = _audioReader.TotalTime;

    // Position update timer
    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
    timer.Tick += (s, e) =>
    {
        Position = _audioReader.CurrentTime;
        PositionChanged?.Invoke(this, Position);
    };
    timer.Start();
}
```

---

### 2. WaveformGeneratorService

**Purpose:** Generate waveform visualization from audio files

**API:**
```csharp
public static class WaveformGeneratorService
{
    public static WriteableBitmap GenerateWaveform(
        string audioFilePath,
        int width,
        int height,
        Color waveColor)
    {
        using var reader = new AudioFileReader(audioFilePath);

        // Calculate samples per pixel
        var totalSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8);
        var samplesPerPixel = (int)(totalSamples / width);

        // Generate peak data
        var peaks = new float[width];
        for (int i = 0; i < width; i++)
        {
            peaks[i] = GetPeakForRange(reader, i * samplesPerPixel, samplesPerPixel);
        }

        // Render to bitmap
        return RenderWaveformBitmap(peaks, width, height, waveColor);
    }

    private static float GetPeakForRange(AudioFileReader reader, int startSample, int count)
    {
        // Read audio samples and find peak value
    }

    private static WriteableBitmap RenderWaveformBitmap(float[] peaks, int width, int height, Color color)
    {
        // Draw waveform as vertical bars centered on midpoint
    }
}
```

---

### 3. ClipExportService

**Purpose:** Export clips using FFmpeg with frame/sample accuracy

**API:**
```csharp
public static class ClipExportService
{
    public static async Task<bool> ExportVideoClip(
        string sourceFile,
        string outputFile,
        TimeSpan startTime,
        TimeSpan duration,
        ExportSettings settings,
        CancellationToken ct)
    {
        if (settings.UseStreamCopy)
        {
            // Fast mode - stream copy (keyframe-accurate only)
            return await ExportVideoStreamCopy(sourceFile, outputFile, startTime, duration, ct);
        }
        else
        {
            // Accurate mode - re-encode (frame-accurate)
            return await ExportVideoReencode(sourceFile, outputFile, startTime, duration, settings, ct);
        }
    }

    private static async Task<bool> ExportVideoReencode(
        string sourceFile,
        string outputFile,
        TimeSpan startTime,
        TimeSpan duration,
        ExportSettings settings,
        CancellationToken ct)
    {
        var success = await FFMpegArguments
            .FromFileInput(sourceFile, verifyExists: true, options => options
                .Seek(startTime))
            .OutputToFile(outputFile, overwrite: true, options => options
                .WithDuration(duration)
                .WithVideoCodec(settings.VideoCodec ?? "libx264")
                .WithConstantRateFactor(settings.Quality)
                .WithAudioCodec(settings.AudioCodec ?? "aac")
                .WithAudioBitrate(192)
                .ForceFormat(settings.OutputFormat ?? "mp4"))
            .CancellableThrough(ct)
            .ProcessAsynchronously();

        return success;
    }

    public static async Task<bool> ExportAudioClip(
        string sourceFile,
        string outputFile,
        TimeSpan startTime,
        TimeSpan duration,
        ExportSettings settings,
        CancellationToken ct)
    {
        var success = await FFMpegArguments
            .FromFileInput(sourceFile, verifyExists: true, options => options
                .Seek(startTime))
            .OutputToFile(outputFile, overwrite: true, options => options
                .WithDuration(duration)
                .WithAudioCodec(settings.AudioCodec ?? "libmp3lame")
                .WithAudioBitrate(settings.AudioBitrate ?? 192)
                .ForceFormat(settings.OutputFormat ?? "mp3"))
            .CancellableThrough(ct)
            .ProcessAsynchronously();

        return success;
    }
}
```

---

### 4. SettingsService

**Purpose:** Persist user preferences

**Settings Model:**
```csharp
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
```

**Persistence:**
```csharp
public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightClicks",
        "ClipEditorSettings.json");

    public static ClipEditorSettings Load()
    {
        if (File.Exists(SettingsPath))
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<ClipEditorSettings>(json) ?? new ClipEditorSettings();
        }
        return new ClipEditorSettings();
    }

    public static void Save(ClipEditorSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, json);
    }
}
```

---

## Data Models

### ClipSegment

```csharp
public class ClipSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool IsEnabled { get; set; } = true;

    public string DisplayName => $"{StartTime:mm\\:ss\\.fff} → {EndTime:mm\\:ss\\.fff} ({Duration.TotalSeconds:F2}s)";
}
```

### MediaInfo

```csharp
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

    public static async Task<MediaInfo> Analyze(string filePath)
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
            AudioCodec = mediaInfo.PrimaryAudioStream?.CodecName ?? ""
        };
    }
}

public enum MediaType
{
    Video,
    Audio
}
```

### ExportSettings

```csharp
public class ExportSettings
{
    public string? OutputFormat { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int Quality { get; set; } = 18; // CRF for video
    public int AudioBitrate { get; set; } = 192;
    public bool UseStreamCopy { get; set; } = false;
}
```

---

## Keyboard Shortcuts

### Universal (Both Editors)

| Key | Action |
|-----|--------|
| **Spacebar** | Play/Pause |
| **I** | Set IN point |
| **O** | Set OUT point |
| **Home** | Jump to start |
| **End** | Jump to end |
| **Ctrl+S** | Save all clips |
| **Ctrl+W** | Close window |
| **Ctrl+Z** | Remove last clip from list |
| **Ctrl+A** | Add current selection to list |
| **Delete** | Remove selected clip from list |

### Video-Specific

| Key | Action |
|-----|--------|
| **Left/Right** | Previous/Next frame |
| **Shift+Left/Right** | -1s / +1s |
| **Ctrl+Wheel** | Zoom timeline |
| **Shift+Wheel** | Scroll timeline (when zoomed) |

### Audio-Specific

| Key | Action |
|-----|--------|
| **Left/Right** | -1s / +1s |
| **Shift+Left/Right** | -10ms / +10ms |
| **Ctrl+Shift+Left/Right** | -1ms / +1ms |
| **L** | Toggle loop selection |
| **M** | Toggle mute outside selection |
| **Ctrl+Wheel** | Zoom waveform |

---

## File Naming Strategy

### Default Pattern: `{filename}_clip_{index}.{ext}`

**Examples:**
```
video.mp4 → video_clip_01.mp4
          → video_clip_02.mp4

song.mp3  → song_clip_01.mp3
          → song_clip_02.mp3
```

### Alternative Pattern: `{filename}_clip_{index}_{start}-{end}.{ext}`

**Examples:**
```
video.mp4 → video_clip_01_0015-0042.mp4
song.mp3  → song_clip_01_0015-0042.mp3
```

### Configurable via Settings Panel

---

## Command-Line Interface

### Launch Arguments

```bash
# Video mode
RightClicksClipEditor.exe --video "C:\path\to\video.mp4"

# Audio mode
RightClicksClipEditor.exe --audio "C:\path\to\audio.mp3"

# Auto-detect mode (based on file extension)
RightClicksClipEditor.exe "C:\path\to\file.mp4"
```

### Argument Parsing

```csharp
// In App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    if (e.Args.Length == 0)
    {
        MessageBox.Show("Usage: RightClicksClipEditor.exe [--video|--audio] <filepath>",
            "Clip Editor", MessageBoxButton.OK, MessageBoxImage.Information);
        Shutdown();
        return;
    }

    string? filePath = null;
    MediaType? mediaType = null;

    if (e.Args.Length == 2 && (e.Args[0] == "--video" || e.Args[0] == "--audio"))
    {
        mediaType = e.Args[0] == "--video" ? MediaType.Video : MediaType.Audio;
        filePath = e.Args[1];
    }
    else if (e.Args.Length == 1)
    {
        filePath = e.Args[0];
        mediaType = DetectMediaType(filePath);
    }

    if (filePath == null || !File.Exists(filePath))
    {
        MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown();
        return;
    }

    // Launch appropriate editor
    if (mediaType == MediaType.Video)
    {
        var window = new VideoClipEditorWindow(filePath);
        window.Show();
    }
    else
    {
        var window = new AudioClipEditorWindow(filePath);
        window.Show();
    }
}
```

---

## Integration with RightClicks

### New Features (Launcher Features)

**VideoClipFeature.cs:**
```csharp
namespace RightClicks.Features.Clipping
{
    public class VideoClipFeature : IFileFeature
    {
        public string Id => "VideoClipEditor";
        public string DisplayName => "Video Clip Editor...";
        public string Description => "Open video in clip editor for frame-accurate clipping";
        public string[] SupportedExtensions => new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
        public bool IsCloudBased => false;

        public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken ct)
        {
            try
            {
                var clipEditorPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "RightClicksClipEditor.exe");

                if (!File.Exists(clipEditorPath))
                {
                    return FeatureResult.CreateFailure(
                        "Clip editor not found. Please reinstall RightClicks.");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = clipEditorPath,
                    Arguments = $"--video \"{filePath}\"",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(clipEditorPath)
                };

                Process.Start(startInfo);

                return FeatureResult.CreateInformational("Video clip editor launched");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to launch video clip editor");
                return FeatureResult.CreateFailure($"Failed to launch clip editor: {ex.Message}", ex);
            }
        }
    }
}
```

**AudioClipFeature.cs:**
```csharp
namespace RightClicks.Features.Clipping
{
    public class AudioClipFeature : IFileFeature
    {
        public string Id => "AudioClipEditor";
        public string DisplayName => "Audio Clip Editor...";
        public string Description => "Open audio in clip editor for sample-accurate clipping";
        public string[] SupportedExtensions => new[] { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".ogg", ".wma" };
        public bool IsCloudBased => false;

        public async Task<FeatureResult> ExecuteAsync(string filePath, CancellationToken ct)
        {
            try
            {
                var clipEditorPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "RightClicksClipEditor.exe");

                if (!File.Exists(clipEditorPath))
                {
                    return FeatureResult.CreateFailure(
                        "Clip editor not found. Please reinstall RightClicks.");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = clipEditorPath,
                    Arguments = $"--audio \"{filePath}\"",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(clipEditorPath)
                };

                Process.Start(startInfo);

                return FeatureResult.CreateInformational("Audio clip editor launched");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to launch audio clip editor");
                return FeatureResult.CreateFailure($"Failed to launch clip editor: {ex.Message}", ex);
            }
        }
    }
}
```

---

## Dependencies

### NuGet Packages (RightClicksClipEditor.csproj)

```xml
<ItemGroup>
  <!-- FFmpeg Integration (already in RightClicks) -->
  <PackageReference Include="FFMpegCore" Version="5.4.0" />

  <!-- Audio Processing (already in RightClicks) -->
  <PackageReference Include="NAudio" Version="2.2.1" />

  <!-- Logging (already in RightClicks) -->
  <PackageReference Include="Serilog" Version="4.3.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />

  <!-- JSON Serialization -->
  <PackageReference Include="System.Text.Json" Version="8.0.0" />

  <!-- Optional: LibVLCSharp for better video playback -->
  <!-- <PackageReference Include="LibVLCSharp.WPF" Version="3.8.5" /> -->
  <!-- <PackageReference Include="VideoLAN.LibVLC.Windows" Version="3.0.20" /> -->
</ItemGroup>
```

**Note:** We'll start with WPF's built-in `MediaElement` for video playback. LibVLCSharp can be added later if needed for better codec support.

---

## Build & Deployment

### Project Configuration

**RightClicksClipEditor.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <ApplicationIcon>Resources\ClipEditor.ico</ApplicationIcon>
    <AssemblyName>RightClicksClipEditor</AssemblyName>
  </PropertyGroup>

  <!-- Dependencies listed above -->

  <!-- Copy to RightClicks output directory -->
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <PropertyGroup>
      <RightClicksOutputDir>$(SolutionDir)RightClicks\bin\$(Configuration)\net8.0-windows\</RightClicksOutputDir>
    </PropertyGroup>

    <Message Text="Copying ClipEditor to RightClicks output: $(RightClicksOutputDir)" Importance="high" />

    <Copy SourceFiles="$(OutDir)RightClicksClipEditor.exe"
          DestinationFolder="$(RightClicksOutputDir)"
          SkipUnchangedFiles="true" />
    <Copy SourceFiles="$(OutDir)RightClicksClipEditor.dll"
          DestinationFolder="$(RightClicksOutputDir)"
          SkipUnchangedFiles="true" />
    <Copy SourceFiles="$(OutDir)RightClicksClipEditor.runtimeconfig.json"
          DestinationFolder="$(RightClicksOutputDir)"
          SkipUnchangedFiles="true" />
  </Target>
</Project>
```

### Deployment to %LOCALAPPDATA%\RightClicks

**Update RightClicks.csproj PostBuild:**
```xml
<!-- Add to existing PostBuild target -->
<Copy SourceFiles="$(OutDir)RightClicksClipEditor.exe"
      DestinationFolder="$(TestInstallDir)"
      SkipUnchangedFiles="true"
      ContinueOnError="true" />
<Copy SourceFiles="$(OutDir)RightClicksClipEditor.dll"
      DestinationFolder="$(TestInstallDir)"
      SkipUnchangedFiles="true"
      ContinueOnError="true" />
<Copy SourceFiles="$(OutDir)RightClicksClipEditor.runtimeconfig.json"
      DestinationFolder="$(TestInstallDir)"
      SkipUnchangedFiles="true"
      ContinueOnError="true" />
```

---

## Testing Strategy

### Phase 1: Standalone Testing
1. Build `RightClicksClipEditor.exe`
2. Test video mode: `RightClicksClipEditor.exe --video "testfiles\test.mp4"`
3. Test audio mode: `RightClicksClipEditor.exe --audio "testfiles\test.mp3"`
4. Verify UI loads, timeline works, playback functions

### Phase 2: Clip Export Testing
1. Set IN/OUT points
2. Add clip to list
3. Save clip
4. Verify output file created with correct name
5. Verify clip duration matches selection
6. Test multiple clips per session

### Phase 3: Integration Testing
1. Right-click video file in Explorer
2. Select "Video Clip Editor..." from RightClicks menu
3. Verify clip editor launches with file loaded
4. Repeat for audio files

### Phase 4: Edge Case Testing
1. Very short clips (< 1 second)
2. Very long files (> 1 hour)
3. High-resolution video (4K)
4. Large audio files (> 100MB)
5. Unsupported formats (should show error)
6. Missing FFmpeg (should show error)

---

## Error Handling

### File Not Found
```csharp
if (!File.Exists(filePath))
{
    MessageBox.Show($"File not found: {filePath}",
        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    Close();
    return;
}
```

### FFmpeg Not Available
```csharp
if (!FFMpegOptions.Options.BinaryFolder.Exists)
{
    MessageBox.Show("FFmpeg not found. Please reinstall RightClicks.",
        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    Close();
    return;
}
```

### Unsupported Format
```csharp
try
{
    var mediaInfo = await MediaInfo.Analyze(filePath);
}
catch (Exception ex)
{
    MessageBox.Show($"Unsupported file format: {ex.Message}",
        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    Close();
    return;
}
```

### Export Failure
```csharp
try
{
    var success = await ClipExportService.ExportVideoClip(...);
    if (!success)
    {
        MessageBox.Show("Failed to export clip. Check logs for details.",
            "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Clip export failed");
    MessageBox.Show($"Export failed: {ex.Message}",
        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
}
```

---

## Logging

### Log File Location
```
%LOCALAPPDATA%\RightClicks\logs\ClipEditor-YYYYMMDD-HHMMSS.log
```

### Serilog Configuration
```csharp
// In App.xaml.cs
private void ConfigureLogging()
{
    var logDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RightClicks", "logs");

    Directory.CreateDirectory(logDir);

    var logFile = Path.Combine(logDir, $"ClipEditor-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.File(logFile,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    Log.Information("=== RightClicks Clip Editor Started ===");
    Log.Information("Version: {Version}", Assembly.GetExecutingAssembly().GetName().Version);
    Log.Information("OS: {OS}", Environment.OSVersion);
}
```

### Key Log Points
- Application startup
- File loaded
- Media info analyzed
- Clip added to list
- Clip export started
- Clip export completed
- Errors and exceptions

---

## Performance Considerations

### Waveform Generation
- **Problem:** Large audio files (> 100MB) can take time to analyze
- **Solution:** Generate waveform asynchronously with progress indicator
- **Optimization:** Cache waveform bitmap for zoom operations

### Video Scrubbing
- **Problem:** Seeking in video can be slow
- **Solution:** Use MediaElement's built-in buffering
- **Optimization:** Preload frames around current position

### Timeline Rendering
- **Problem:** Redrawing timeline on every mouse move
- **Solution:** Use WPF's built-in rendering optimization
- **Optimization:** Only redraw changed regions

---

## Future Enhancements (Post-MVP)

### Phase 2 Features
- [ ] Batch export all clips in one operation
- [ ] Clip preview before export
- [ ] Undo/Redo for clip list
- [ ] Keyboard shortcut customization
- [ ] Dark mode support

### Phase 3 Features
- [ ] Export presets (YouTube, Instagram, etc.)
- [ ] Fade in/out effects
- [ ] Volume adjustment per clip
- [ ] Clip trimming (adjust IN/OUT after adding to list)
- [ ] Drag-and-drop reordering of clips

### Phase 4 Features
- [ ] Multi-file batch clipping
- [ ] Clip merging (concatenate multiple clips)
- [ ] Audio normalization
- [ ] Video filters (brightness, contrast, etc.)

---

## Success Criteria

### MVP Complete When:
1. ✅ Video clip editor opens from context menu
2. ✅ Audio clip editor opens from context menu
3. ✅ Timeline shows duration and playhead
4. ✅ IN/OUT markers can be set and dragged
5. ✅ Play/Pause works
6. ✅ Frame-by-frame stepping works (video)
7. ✅ Waveform displays (audio)
8. ✅ Multiple clips can be added to list
9. ✅ Clips export with correct naming
10. ✅ Output files are frame/sample accurate
11. ✅ Settings persist between sessions
12. ✅ Keyboard shortcuts work
13. ✅ Errors are handled gracefully
14. ✅ Logs are written for debugging

---

## Implementation Roadmap

See **CLIP_EDITOR_TASKS.md** for detailed task breakdown.

**Estimated Timeline:**
- Phase 1 (Project Setup): 1 hour
- Phase 2 (Video Editor MVP): 4-6 hours
- Phase 3 (Audio Editor MVP): 3-4 hours
- Phase 4 (Integration): 1-2 hours
- Phase 5 (Testing & Polish): 2-3 hours

**Total: 11-16 hours**

---

**End of Technical Specification**


