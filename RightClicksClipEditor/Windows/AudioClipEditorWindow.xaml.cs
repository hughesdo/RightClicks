using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using FFMpegCore;
using NAudio.Wave;
using RightClicksClipEditor.Models;
using RightClicksClipEditor.Services;
using Serilog;

namespace RightClicksClipEditor.Windows;

public partial class AudioClipEditorWindow : Window
{
    private readonly string _filePath;
    private MediaInfo? _mediaInfo;
    private ClipEditorSettings _settings;
    private DispatcherTimer? _positionTimer;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFileReader;
    private bool _isPlaying = false;
    private bool _isUpdatingSlider = false;
    private bool _isLooping = false;

    private TimeSpan _inPoint = TimeSpan.Zero;
    private TimeSpan _outPoint = TimeSpan.Zero;

    private ObservableCollection<ClipSegment> _clips = new();

    public AudioClipEditorWindow(string filePath)
    {
        InitializeComponent();

        _filePath = filePath;
        _settings = SettingsService.Load();

        ClipListBox.ItemsSource = _clips;

        // Set window size from settings
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;

        Loaded += AudioClipEditorWindow_Loaded;
        Closing += AudioClipEditorWindow_Closing;

        // Keyboard shortcuts
        KeyDown += AudioClipEditorWindow_KeyDown;

        Log.Information("Audio clip editor opened for: {FilePath}", _filePath);
    }

    private async void AudioClipEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Log.Information("Loading audio file: {FilePath}", _filePath);

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

            Log.Information("Media analyzed: Duration: {Duration}, Codec: {Codec}",
                _mediaInfo.Duration, _mediaInfo.AudioCodec);

            // Update UI
            FileNameText.Text = _mediaInfo.FileName;
            FileInfoText.Text = $"{_mediaInfo.AudioCodec} • {_mediaInfo.Duration:hh\\:mm\\:ss\\.fff}";

            // Initialize audio playback
            _audioFileReader = new AudioFileReader(_filePath);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioFileReader);
            _waveOut.Volume = (float)VolumeSlider.Value;
            _waveOut.PlaybackStopped += WaveOut_PlaybackStopped;

            // Initialize Timeline
            Timeline.Duration = _mediaInfo.Duration;
            Timeline.PositionChanged += Timeline_PositionChanged;
            Timeline.InPointChanged += Timeline_InPointChanged;
            Timeline.OutPointChanged += Timeline_OutPointChanged;

            // Initialize Waveform
            Waveform.Duration = _mediaInfo.Duration;
            Waveform.PositionChanged += Waveform_PositionChanged;
            Waveform.InPointChanged += Waveform_InPointChanged;
            Waveform.OutPointChanged += Waveform_OutPointChanged;
            await Waveform.GenerateWaveformAsync(_filePath);

            // Set OUT point to end of audio
            _outPoint = _mediaInfo.Duration;
            Timeline.OutPoint = _outPoint;
            Waveform.OutPoint = _outPoint;
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
            Log.Error(ex, "Failed to load audio");
            MessageBox.Show($"Failed to load audio: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void AudioClipEditorWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save window size
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        SettingsService.Save(_settings);

        // Stop playback
        _positionTimer?.Stop();
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _audioFileReader?.Dispose();

        Log.Information("Audio clip editor window closed");
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        if (_mediaInfo == null || _audioFileReader == null)
            return;

        var position = _audioFileReader.CurrentTime;

        // Check for loop
        if (_isLooping && _isPlaying && position >= _outPoint)
        {
            _audioFileReader.CurrentTime = _inPoint;
            position = _inPoint;
        }

        // Update position text
        PositionText.Text = $"{FormatTime(position)} / {FormatTime(_mediaInfo.Duration)}";

        // Update Timeline and Waveform
        Timeline.CurrentPosition = position;
        Waveform.CurrentPosition = position;

        // Update slider
        if (!_isUpdatingSlider && _mediaInfo.Duration.TotalSeconds > 0)
        {
            _isUpdatingSlider = true;
            PositionSlider.Value = (position.TotalSeconds / _mediaInfo.Duration.TotalSeconds) * 100;
            _isUpdatingSlider = false;
        }
    }
    private void WaveOut_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _isPlaying = false;
            PlayPauseButton.Content = "▶";
        });
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_waveOut == null || _audioFileReader == null)
            return;

        if (_isPlaying)
        {
            _waveOut.Pause();
            _isPlaying = false;
            PlayPauseButton.Content = "▶";
            Log.Information("Audio paused");
        }
        else
        {
            _waveOut.Play();
            _isPlaying = true;
            PlayPauseButton.Content = "⏸";
            Log.Information("Audio playing");
        }
    }

    private void StepBackward10ms_Click(object sender, RoutedEventArgs e)
    {
        if (_audioFileReader == null)
            return;

        var newPosition = _audioFileReader.CurrentTime - TimeSpan.FromMilliseconds(10);
        _audioFileReader.CurrentTime = newPosition < TimeSpan.Zero ? TimeSpan.Zero : newPosition;
    }

    private void StepForward10ms_Click(object sender, RoutedEventArgs e)
    {
        if (_audioFileReader == null || _mediaInfo == null)
            return;

        var newPosition = _audioFileReader.CurrentTime + TimeSpan.FromMilliseconds(10);
        _audioFileReader.CurrentTime = newPosition > _mediaInfo.Duration ? _mediaInfo.Duration : newPosition;
    }

    private void StepBackward1s_Click(object sender, RoutedEventArgs e)
    {
        if (_audioFileReader == null)
            return;

        var newPosition = _audioFileReader.CurrentTime - TimeSpan.FromSeconds(1);
        _audioFileReader.CurrentTime = newPosition < TimeSpan.Zero ? TimeSpan.Zero : newPosition;
    }

    private void StepForward1s_Click(object sender, RoutedEventArgs e)
    {
        if (_audioFileReader == null || _mediaInfo == null)
            return;

        var newPosition = _audioFileReader.CurrentTime + TimeSpan.FromSeconds(1);
        _audioFileReader.CurrentTime = newPosition > _mediaInfo.Duration ? _mediaInfo.Duration : newPosition;
    }

    private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingSlider || _audioFileReader == null || _mediaInfo == null)
            return;

        var newPosition = TimeSpan.FromSeconds((e.NewValue / 100.0) * _mediaInfo.Duration.TotalSeconds);
        _audioFileReader.CurrentTime = newPosition;
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_waveOut != null)
        {
            _waveOut.Volume = (float)e.NewValue;
        }
    }

    private void LoopCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _isLooping = LoopCheckBox.IsChecked == true;
        Log.Information("Loop selection: {IsLooping}", _isLooping);
    }

    private void SetIn_Click(object sender, RoutedEventArgs e)
    {
        if (_audioFileReader == null)
            return;

        _inPoint = _audioFileReader.CurrentTime;
        Timeline.InPoint = _inPoint;
        Waveform.InPoint = _inPoint;
        UpdateSelectionText();
        Log.Information("IN point set to {InPoint}", _inPoint);
    }

    private void SetOut_Click(object sender, RoutedEventArgs e)
    {
        if (_audioFileReader == null)
            return;

        _outPoint = _audioFileReader.CurrentTime;
        Timeline.OutPoint = _outPoint;
        Waveform.OutPoint = _outPoint;
        UpdateSelectionText();
        Log.Information("OUT point set to {OutPoint}", _outPoint);
    }

    private void Timeline_PositionChanged(object? sender, TimeSpan position)
    {
        if (_audioFileReader != null)
        {
            _audioFileReader.CurrentTime = position;
        }
    }

    private void Timeline_InPointChanged(object? sender, TimeSpan inPoint)
    {
        _inPoint = inPoint;
        Waveform.InPoint = inPoint;
        UpdateSelectionText();
        Log.Information("IN point changed via timeline to {InPoint}", _inPoint);
    }

    private void Timeline_OutPointChanged(object? sender, TimeSpan outPoint)
    {
        _outPoint = outPoint;
        Waveform.OutPoint = outPoint;
        UpdateSelectionText();
        Log.Information("OUT point changed via timeline to {OutPoint}", _outPoint);
    }

    private void Waveform_PositionChanged(object? sender, TimeSpan position)
    {
        if (_audioFileReader != null)
        {
            _audioFileReader.CurrentTime = position;
            Timeline.CurrentPosition = position;
        }
    }

    private void Waveform_InPointChanged(object? sender, TimeSpan inPoint)
    {
        _inPoint = inPoint;
        Timeline.InPoint = inPoint;
        UpdateSelectionText();
        Log.Information("IN point changed via waveform to {InPoint}", _inPoint);
    }

    private void Waveform_OutPointChanged(object? sender, TimeSpan outPoint)
    {
        _outPoint = outPoint;
        Timeline.OutPoint = outPoint;
        UpdateSelectionText();
        Log.Information("OUT point changed via waveform to {OutPoint}", _outPoint);
    }

    private void UpdateSelectionText()
    {
        if (_outPoint > _inPoint)
        {
            var duration = _outPoint - _inPoint;
            SelectionText.Text = $"Selection: {FormatTime(_inPoint)} → {FormatTime(_outPoint)} ({duration.TotalSeconds:F1}s)";
        }
        else
        {
            SelectionText.Text = "Selection: Not set";
        }
    }

    private void AddSelection_Click(object sender, RoutedEventArgs e)
    {
        if (_outPoint <= _inPoint)
        {
            MessageBox.Show("Please set valid IN and OUT points first.", "Invalid Selection",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var clip = new ClipSegment
        {
            StartTime = _inPoint,
            EndTime = _outPoint,
            IsEnabled = true
        };

        _clips.Add(clip);
        Log.Information("Clip added: {StartTime} to {EndTime}", _inPoint, _outPoint);
    }

    private void RemoveClip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ClipSegment clip)
        {
            _clips.Remove(clip);
            Log.Information("Clip removed: {StartTime} to {EndTime}", clip.StartTime, clip.EndTime);
        }
    }

    private async void SaveAllClips_Click(object sender, RoutedEventArgs e)
    {
        var enabledClips = _clips.Where(c => c.IsEnabled).ToList();

        if (enabledClips.Count == 0)
        {
            MessageBox.Show("No clips to export. Add clips first.", "No Clips",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Log.Information("Exporting {Count} clips", enabledClips.Count);

            var outputDir = Path.GetDirectoryName(_filePath) ?? "";
            var baseName = Path.GetFileNameWithoutExtension(_filePath);

            for (int i = 0; i < enabledClips.Count; i++)
            {
                var clip = enabledClips[i];
                var outputPath = Path.Combine(outputDir, $"{baseName}_clip_{i + 1:D3}.mp3");

                Log.Information("Exporting clip {Index}/{Total}: {OutputPath}", i + 1, enabledClips.Count, outputPath);

                var settings = new ExportSettings
                {
                    OutputFormat = "mp3",
                    AudioCodec = "libmp3lame",
                    AudioBitrate = 192
                };

                await ClipExportService.ExportAudioClipAsync(_filePath, outputPath, clip.StartTime, clip.Duration, settings);
            }

            MessageBox.Show($"Successfully exported {enabledClips.Count} clip(s)!", "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);

            Log.Information("All clips exported successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export clips");
            MessageBox.Show($"Failed to export clips: {ex.Message}", "Export Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
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
                Log.Warning("Help file not found, showing inline help");
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
        var helpText = @"Audio Clip Editor - Quick Help

KEYBOARD SHORTCUTS:
  Spacebar       - Play/Pause
  I              - Set IN point
  O              - Set OUT point
  L              - Toggle loop selection
  Left/Right     - Step 10ms
  Shift+Left/Right - Step 1 second
  Ctrl+A         - Add current selection to clip list
  Ctrl+S         - Save all clips
  Ctrl+W         - Close window
  F1             - Show this help

WORKFLOW:
  1. Play audio and find start of clip
  2. Press I to set IN point
  3. Find end of clip
  4. Press O to set OUT point
  5. Press Ctrl+A to add clip to list
  6. Repeat for more clips
  7. Press Ctrl+S to export all clips

WAVEFORM:
  - Visual representation of audio amplitude
  - Green IN marker, Red OUT marker
  - Blue playhead shows current position
  - Ctrl+Mouse Wheel to zoom
  - Shift+Mouse Wheel to scroll

OUTPUT:
  Clips are saved as MP3 files next to the source
  with sequential numbering (e.g., audio_clip_001.mp3)";

        MessageBox.Show(helpText, "Audio Clip Editor Help",
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

    private void AudioClipEditorWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle keyboard shortcuts
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
                    StepBackward10ms_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;

            case Key.Right:
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    StepForward1s_Click(this, new RoutedEventArgs());
                else
                    StepForward10ms_Click(this, new RoutedEventArgs());
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
        return time.ToString(@"mm\:ss\.fff");
    }
}
