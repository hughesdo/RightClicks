using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using FFMpegCore;
using RightClicksClipEditor.Models;
using RightClicksClipEditor.Services;
using Serilog;

namespace RightClicksClipEditor.Windows;

public partial class VideoClipEditorWindow : Window
{
    private readonly string _filePath;
    private MediaInfo? _mediaInfo;
    private ClipEditorSettings _settings;
    private DispatcherTimer? _positionTimer;
    private bool _isPlaying = false;
    private bool _isUpdatingSlider = false;
    private bool _isLooping = false;

    private TimeSpan _inPoint = TimeSpan.Zero;
    private TimeSpan _outPoint = TimeSpan.Zero;
    private double _frameDuration = 0;

    private ObservableCollection<ClipSegment> _clips = new();
    
    public VideoClipEditorWindow(string filePath)
    {
        InitializeComponent();
        
        _filePath = filePath;
        _settings = SettingsService.Load();
        
        ClipListBox.ItemsSource = _clips;
        
        // Set window size from settings
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;
        
        Loaded += VideoClipEditorWindow_Loaded;
        Closing += VideoClipEditorWindow_Closing;
        
        // Keyboard shortcuts
        KeyDown += VideoClipEditorWindow_KeyDown;
    }
    
    private async void VideoClipEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Information("Loading video file: {FilePath}", _filePath);
            
            // Configure FFmpeg
            var ffmpegPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RightClicks", "bin");
            
            if (Directory.Exists(ffmpegPath))
            {
                GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
                Log.Information("FFmpeg path configured: {Path}", ffmpegPath);
            }
            
            // Analyze media
            _mediaInfo = await MediaInfo.AnalyzeAsync(_filePath);
            
            Log.Information("Media analyzed: {Width}x{Height} @ {FrameRate}fps, Duration: {Duration}",
                _mediaInfo.Width, _mediaInfo.Height, _mediaInfo.FrameRate, _mediaInfo.Duration);
            
            // Update UI
            FileNameText.Text = _mediaInfo.FileName;
            FileInfoText.Text = $"{_mediaInfo.Width}x{_mediaInfo.Height} @ {_mediaInfo.FrameRate:F2}fps • {_mediaInfo.Duration:hh\\:mm\\:ss}";
            
            // Calculate frame duration
            if (_mediaInfo.FrameRate > 0)
            {
                _frameDuration = 1.0 / _mediaInfo.FrameRate;
            }
            
            // Load video
            VideoPlayer.Source = new Uri(_filePath);
            VideoPlayer.Volume = VolumeSlider.Value;

            // Initialize Timeline
            Timeline.Duration = _mediaInfo.Duration;
            Timeline.PositionChanged += Timeline_PositionChanged;
            Timeline.InPointChanged += Timeline_InPointChanged;
            Timeline.OutPointChanged += Timeline_OutPointChanged;

            // Set OUT point to end of video
            _outPoint = _mediaInfo.Duration;
            Timeline.OutPoint = _outPoint;
            UpdateSelectionText();

            // Start position timer
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _positionTimer.Tick += PositionTimer_Tick;
            _positionTimer.Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load video");
            MessageBox.Show($"Failed to load video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }
    
    private void VideoClipEditorWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save window size
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        SettingsService.Save(_settings);
        
        // Stop playback
        _positionTimer?.Stop();
        VideoPlayer.Stop();
        VideoPlayer.Close();
        
        Log.Information("Video clip editor window closed");
    }
    
    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (_mediaInfo == null || VideoPlayer.Source == null)
            return;

        var position = VideoPlayer.Position;

        // Loop playback if enabled and we've passed the OUT point
        if (_isLooping && _isPlaying && _outPoint > _inPoint)
        {
            if (position >= _outPoint)
            {
                VideoPlayer.Position = _inPoint;
                position = _inPoint;
                Log.Debug("Looped playback back to IN point");
            }
        }

        // Update position text
        PositionText.Text = $"{FormatTime(position)} / {FormatTime(_mediaInfo.Duration)}";

        // Update Timeline
        Timeline.CurrentPosition = position;

        // Update slider
        if (!_isUpdatingSlider && _mediaInfo.Duration.TotalSeconds > 0)
        {
            _isUpdatingSlider = true;
            PositionSlider.Value = (position.TotalSeconds / _mediaInfo.Duration.TotalSeconds) * 100;
            _isUpdatingSlider = false;
        }
    }
    
    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            VideoPlayer.Pause();
            PlayPauseButton.Content = "▶";
            _isPlaying = false;
            Log.Debug("Playback paused");
        }
        else
        {
            VideoPlayer.Play();
            PlayPauseButton.Content = "⏸";
            _isPlaying = true;
            Log.Debug("Playback started");
        }
    }

    private void StepForwardFrame_Click(object sender, RoutedEventArgs e)
    {
        if (_frameDuration > 0)
        {
            var newPosition = VideoPlayer.Position + TimeSpan.FromSeconds(_frameDuration);
            SeekTo(newPosition);
            Log.Debug("Stepped forward 1 frame to {Position}", newPosition);
        }
    }

    private void StepBackwardFrame_Click(object sender, RoutedEventArgs e)
    {
        if (_frameDuration > 0)
        {
            var newPosition = VideoPlayer.Position - TimeSpan.FromSeconds(_frameDuration);
            SeekTo(newPosition);
            Log.Debug("Stepped backward 1 frame to {Position}", newPosition);
        }
    }

    private void StepForward1s_Click(object sender, RoutedEventArgs e)
    {
        var newPosition = VideoPlayer.Position + TimeSpan.FromSeconds(1);
        SeekTo(newPosition);
        Log.Debug("Stepped forward 1 second to {Position}", newPosition);
    }

    private void StepBackward1s_Click(object sender, RoutedEventArgs e)
    {
        var newPosition = VideoPlayer.Position - TimeSpan.FromSeconds(1);
        SeekTo(newPosition);
        Log.Debug("Stepped backward 1 second to {Position}", newPosition);
    }

    private void SeekTo(TimeSpan position)
    {
        if (_mediaInfo == null)
            return;

        // Clamp position
        if (position < TimeSpan.Zero)
            position = TimeSpan.Zero;
        if (position > _mediaInfo.Duration)
            position = _mediaInfo.Duration;

        VideoPlayer.Position = position;
    }

    private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingSlider || _mediaInfo == null)
            return;

        var position = TimeSpan.FromSeconds((_mediaInfo.Duration.TotalSeconds * e.NewValue) / 100);
        VideoPlayer.Position = position;
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VideoPlayer != null)
        {
            VideoPlayer.Volume = e.NewValue;
        }
    }

    private void LoopCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _isLooping = LoopCheckBox.IsChecked == true;
        Log.Debug("Loop selection: {IsLooping}", _isLooping);
    }

    private void SetIn_Click(object sender, RoutedEventArgs e)
    {
        _inPoint = VideoPlayer.Position;
        Timeline.InPoint = _inPoint;
        UpdateSelectionText();
        Log.Information("IN point set to {InPoint}", _inPoint);
    }

    private void SetOut_Click(object sender, RoutedEventArgs e)
    {
        _outPoint = VideoPlayer.Position;
        Timeline.OutPoint = _outPoint;
        UpdateSelectionText();
        Log.Information("OUT point set to {OutPoint}", _outPoint);
    }

    private void Timeline_PositionChanged(object? sender, TimeSpan position)
    {
        // Seek video to timeline position
        VideoPlayer.Position = position;
    }

    private void Timeline_InPointChanged(object? sender, TimeSpan inPoint)
    {
        _inPoint = inPoint;
        UpdateSelectionText();
        Log.Information("IN point changed via timeline to {InPoint}", _inPoint);
    }

    private void Timeline_OutPointChanged(object? sender, TimeSpan outPoint)
    {
        _outPoint = outPoint;
        UpdateSelectionText();
        Log.Information("OUT point changed via timeline to {OutPoint}", _outPoint);
    }

    private void UpdateSelectionText()
    {
        if (_outPoint > _inPoint)
        {
            var duration = _outPoint - _inPoint;
            SelectionText.Text = $"Selection: {FormatTime(_inPoint)} → {FormatTime(_outPoint)} ({duration.TotalSeconds:F2}s)";
        }
        else
        {
            SelectionText.Text = "Selection: Not set (OUT must be after IN)";
        }
    }

    private void AddSelection_Click(object sender, RoutedEventArgs e)
    {
        if (_outPoint <= _inPoint)
        {
            MessageBox.Show("Please set valid IN and OUT points (OUT must be after IN).",
                "Invalid Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var clip = new ClipSegment
        {
            StartTime = _inPoint,
            EndTime = _outPoint
        };

        _clips.Add(clip);
        Log.Information("Added clip: {Clip}", clip.DisplayName);

        MessageBox.Show($"Clip added: {clip.DisplayName}", "Clip Added",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RemoveClip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ClipSegment clip)
        {
            _clips.Remove(clip);
            Log.Information("Removed clip: {Clip}", clip.DisplayName);
        }
    }

    private async void SaveAllClips_Click(object sender, RoutedEventArgs e)
    {
        var enabledClips = _clips.Where(c => c.IsEnabled).ToList();

        if (enabledClips.Count == 0)
        {
            MessageBox.Show("No clips to save. Please add at least one clip.",
                "No Clips", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Log.Information("Saving {Count} clips", enabledClips.Count);

        var exportSettings = new ExportSettings
        {
            OutputFormat = _settings.VideoOutputFormat,
            VideoCodec = _settings.VideoCodec,
            AudioCodec = _settings.AudioCodec,
            Quality = _settings.VideoQuality,
            AudioBitrate = _settings.AudioBitrate,
            UseStreamCopy = _settings.UseStreamCopy
        };

        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < enabledClips.Count; i++)
        {
            var clip = enabledClips[i];
            var outputPath = GenerateOutputPath(i + 1);

            Log.Information("Exporting clip {Index}/{Total}: {OutputPath}", i + 1, enabledClips.Count, outputPath);

            var success = await ClipExportService.ExportVideoClipAsync(
                _filePath,
                outputPath,
                clip.StartTime,
                clip.Duration,
                exportSettings);

            if (success)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        var message = $"Export complete!\n\nSuccessful: {successCount}\nFailed: {failCount}";
        MessageBox.Show(message, "Export Complete", MessageBoxButton.OK,
            failCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

        Log.Information("Export complete: {Success} succeeded, {Failed} failed", successCount, failCount);
    }

    private string GenerateOutputPath(int index)
    {
        var directory = _settings.UseSameFolder
            ? Path.GetDirectoryName(_filePath)!
            : _settings.CustomOutputFolder;

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(_filePath);
        var extension = _settings.VideoOutputFormat;

        var pattern = _settings.NamingPattern
            .Replace("{filename}", fileNameWithoutExt)
            .Replace("{index}", index.ToString("D2"));

        return Path.Combine(directory, $"{pattern}.{extension}");
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Try to find help file in multiple locations
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ClipEditorHelp.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClipEditorHelp.html"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RightClicks", "ClipEditorHelp.html")
            };

            string? helpPath = possiblePaths.FirstOrDefault(File.Exists);

            if (helpPath != null)
            {
                Log.Information("Opening help file: {HelpPath}", helpPath);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = helpPath,
                    UseShellExecute = true
                });
            }
            else
            {
                Log.Warning("Help file not found in any location, showing inline help");
                ShowInlineHelp();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open help");
            ShowInlineHelp();
        }
    }

    private void ShowInlineHelp()
    {
        var helpText = @"Video Clip Editor - Quick Help

KEYBOARD SHORTCUTS:
  Spacebar       - Play/Pause
  I              - Set IN point
  O              - Set OUT point
  L              - Toggle loop selection
  Left/Right     - Step 1 frame
  Shift+Left/Right - Step 1 second
  Ctrl+A         - Add current selection to clip list
  Ctrl+S         - Save all clips
  Ctrl+W         - Close window
  F1             - Show this help

WORKFLOW:
  1. Play video and find start of clip
  2. Press I to set IN point
  3. Find end of clip
  4. Press O to set OUT point
  5. Press L to loop and preview selection
  6. Press Ctrl+A to add clip to list
  7. Repeat for more clips
  8. Press Ctrl+S to export all clips

TIMELINE:
  - Drag playhead (blue) to seek
  - Drag IN marker (green) to adjust start
  - Drag OUT marker (red) to adjust end
  - Ctrl+Mouse Wheel to zoom
  - Shift+Mouse Wheel to scroll

OUTPUT:
  Clips are saved next to the source file with
  sequential numbering (e.g., video_clip_001.mp4)";

        MessageBox.Show(helpText, "Video Clip Editor Help",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Settings window not yet implemented.", "Coming Soon",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void VideoClipEditorWindow_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                PlayPause_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.I:
                SetIn_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.O:
                SetOut_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.L:
                LoopCheckBox.IsChecked = !LoopCheckBox.IsChecked;
                e.Handled = true;
                break;
            case Key.Left:
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    StepBackward1s_Click(this, new RoutedEventArgs());
                else
                    StepBackwardFrame_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.Right:
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    StepForward1s_Click(this, new RoutedEventArgs());
                else
                    StepForwardFrame_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.A:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    AddSelection_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                break;
            case Key.S:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    SaveAllClips_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                break;
            case Key.W:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    Close();
                    e.Handled = true;
                }
                break;
            case Key.F1:
                HelpButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

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
}

