using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.Wave;
using Serilog;

namespace RightClicksClipEditor.Controls;

public partial class WaveformControl : UserControl
{
    // Dependency Properties
    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(nameof(Duration), typeof(TimeSpan), typeof(WaveformControl),
            new PropertyMetadata(TimeSpan.Zero));

    public static readonly DependencyProperty CurrentPositionProperty =
        DependencyProperty.Register(nameof(CurrentPosition), typeof(TimeSpan), typeof(WaveformControl),
            new PropertyMetadata(TimeSpan.Zero, OnCurrentPositionChanged));

    public static readonly DependencyProperty InPointProperty =
        DependencyProperty.Register(nameof(InPoint), typeof(TimeSpan?), typeof(WaveformControl),
            new PropertyMetadata(null, OnInPointChanged));

    public static readonly DependencyProperty OutPointProperty =
        DependencyProperty.Register(nameof(OutPoint), typeof(TimeSpan?), typeof(WaveformControl),
            new PropertyMetadata(null, OnOutPointChanged));

    // Properties
    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    public TimeSpan CurrentPosition
    {
        get => (TimeSpan)GetValue(CurrentPositionProperty);
        set => SetValue(CurrentPositionProperty, value);
    }

    public TimeSpan? InPoint
    {
        get => (TimeSpan?)GetValue(InPointProperty);
        set => SetValue(InPointProperty, value);
    }

    public TimeSpan? OutPoint
    {
        get => (TimeSpan?)GetValue(OutPointProperty);
        set => SetValue(OutPointProperty, value);
    }

    // Events
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<TimeSpan>? InPointChanged;
    public event EventHandler<TimeSpan>? OutPointChanged;

    // Private fields
    private double _zoomLevel = 1.0;
    private WriteableBitmap? _waveformBitmap;
    private float[]? _samples;
    private int _sampleRate;
    private double _baseWidth;
    private bool _isDraggingPlayhead = false;
    private bool _isDraggingInMarker = false;
    private bool _isDraggingOutMarker = false;

    public WaveformControl()
    {
        InitializeComponent();
        SizeChanged += WaveformControl_SizeChanged;
        Loaded += WaveformControl_Loaded;
    }

    private void WaveformControl_Loaded(object sender, RoutedEventArgs e)
    {
        _baseWidth = WaveformScrollViewer.ActualWidth > 0 ? WaveformScrollViewer.ActualWidth : 800;
    }

    private void WaveformControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WaveformScrollViewer.ActualWidth > 0)
        {
            _baseWidth = WaveformScrollViewer.ActualWidth;
            RegenerateWaveformAtZoom();
        }
    }

    public async Task GenerateWaveformAsync(string audioFilePath)
    {
        try
        {
            LoadingPanel.Visibility = Visibility.Visible;
            Log.Information("Generating waveform for {FilePath}", audioFilePath);

            await Task.Run(() =>
            {
                using var reader = new AudioFileReader(audioFilePath);
                var samples = new List<float>();
                var buffer = new float[reader.WaveFormat.SampleRate];
                int samplesRead;

                while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    samples.AddRange(buffer.Take(samplesRead));
                }

                // Store samples for zoom regeneration
                Dispatcher.Invoke(() =>
                {
                    _samples = samples.ToArray();
                    _sampleRate = reader.WaveFormat.SampleRate;
                    _baseWidth = WaveformScrollViewer.ActualWidth > 0 ? WaveformScrollViewer.ActualWidth : 800;
                    RegenerateWaveformAtZoom();
                });
            });

            LoadingPanel.Visibility = Visibility.Collapsed;
            Log.Information("Waveform generated successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate waveform");
            LoadingPanel.Visibility = Visibility.Collapsed;
            throw;
        }
    }

    private void RegenerateWaveformAtZoom()
    {
        if (_samples == null || _samples.Length == 0)
            return;

        int width = (int)(_baseWidth * _zoomLevel);
        int height = (int)(WaveformScrollViewer.ActualHeight > 30 ? WaveformScrollViewer.ActualHeight - 10 : 150);

        if (width <= 0 || height <= 0)
            return;

        CreateWaveformBitmap(_samples, width, height);
        UpdateCanvasSize(width, height);
        UpdateWaveformDisplay();
    }

    private void CreateWaveformBitmap(float[] samples, int width, int height)
    {
        _waveformBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

        // Calculate samples per pixel
        int samplesPerPixel = Math.Max(1, samples.Length / width);

        _waveformBitmap.Lock();

        try
        {
            unsafe
            {
                int* pixels = (int*)_waveformBitmap.BackBuffer;
                int stride = (int)_waveformBitmap.BackBufferStride / 4;

                // Clear to background color
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pixels[y * stride + x] = unchecked((int)0xFF2D2D2D); // Dark gray background
                    }
                }

                // Draw waveform
                int centerY = height / 2;
                Color waveColor = Color.FromArgb(255, 33, 150, 243); // Blue

                for (int x = 0; x < width; x++)
                {
                    int sampleIndex = x * samplesPerPixel;
                    if (sampleIndex >= samples.Length)
                        break;

                    // Find min/max in this pixel's sample range
                    float min = 0, max = 0;
                    for (int i = 0; i < samplesPerPixel && sampleIndex + i < samples.Length; i++)
                    {
                        float sample = samples[sampleIndex + i];
                        min = Math.Min(min, sample);
                        max = Math.Max(max, sample);
                    }

                    // Draw vertical line for this pixel
                    int yMin = centerY - (int)(max * centerY);
                    int yMax = centerY - (int)(min * centerY);

                    for (int y = yMin; y <= yMax && y >= 0 && y < height; y++)
                    {
                        pixels[y * stride + x] = (waveColor.A << 24) | (waveColor.R << 16) |
                                                  (waveColor.G << 8) | waveColor.B;
                    }
                }
            }
        }
        finally
        {
            _waveformBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _waveformBitmap.Unlock();
        }

        WaveformImage.Source = _waveformBitmap;
        WaveformImage.Width = width;
        WaveformImage.Height = height;
    }

    private void UpdateCanvasSize(int width, int height)
    {
        WaveformCanvas.Width = width;
        WaveformCanvas.Height = height;
    }

    private void UpdateWaveformDisplay()
    {
        UpdatePlayheadPosition();
        UpdateInMarkerPosition();
        UpdateOutMarkerPosition();
        UpdateSelectionHighlight();
    }

    private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaveformControl control)
        {
            control.UpdatePlayheadPosition();
        }
    }

    private static void OnInPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaveformControl control)
        {
            control.UpdateInMarkerPosition();
            control.UpdateSelectionHighlight();
        }
    }

    private static void OnOutPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaveformControl control)
        {
            control.UpdateOutMarkerPosition();
            control.UpdateSelectionHighlight();
        }
    }

    private void UpdatePlayheadPosition()
    {
        if (Duration == TimeSpan.Zero || WaveformCanvas.Width == 0)
            return;

        double width = WaveformCanvas.Width;
        double x = (CurrentPosition.TotalSeconds / Duration.TotalSeconds) * width;

        Canvas.SetLeft(Playhead, x);
        PlayheadLabel.Text = CurrentPosition.ToString(@"mm\:ss\.fff");
    }

    private void UpdateInMarkerPosition()
    {
        if (InPoint.HasValue)
        {
            InMarker.Visibility = Visibility.Visible;

            if (Duration != TimeSpan.Zero && WaveformCanvas.Width > 0)
            {
                double width = WaveformCanvas.Width;
                double x = (InPoint.Value.TotalSeconds / Duration.TotalSeconds) * width;
                Canvas.SetLeft(InMarker, x);
            }
        }
        else
        {
            InMarker.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateOutMarkerPosition()
    {
        if (OutPoint.HasValue)
        {
            OutMarker.Visibility = Visibility.Visible;

            if (Duration != TimeSpan.Zero && WaveformCanvas.Width > 0)
            {
                double width = WaveformCanvas.Width;
                double x = (OutPoint.Value.TotalSeconds / Duration.TotalSeconds) * width;
                Canvas.SetLeft(OutMarker, x);
            }
        }
        else
        {
            OutMarker.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateSelectionHighlight()
    {
        if (InPoint.HasValue && OutPoint.HasValue && Duration != TimeSpan.Zero && WaveformCanvas.Width > 0)
        {
            SelectionHighlight.Visibility = Visibility.Visible;

            double width = WaveformCanvas.Width;
            double inX = (InPoint.Value.TotalSeconds / Duration.TotalSeconds) * width;
            double outX = (OutPoint.Value.TotalSeconds / Duration.TotalSeconds) * width;

            Canvas.SetLeft(SelectionHighlight, inX);
            SelectionHighlight.Width = Math.Max(0, outX - inX);
            SelectionHighlight.Height = WaveformCanvas.Height;
        }
        else
        {
            SelectionHighlight.Visibility = Visibility.Collapsed;
        }
    }

    // Mouse event handlers for dragging markers
    private void WaveformCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(WaveformCanvas);

        // Check if clicking on IN marker
        if (InPoint.HasValue)
        {
            double inX = Canvas.GetLeft(InMarker);
            if (Math.Abs(pos.X - inX) < 10)
            {
                _isDraggingInMarker = true;
                WaveformCanvas.CaptureMouse();
                return;
            }
        }

        // Check if clicking on OUT marker
        if (OutPoint.HasValue)
        {
            double outX = Canvas.GetLeft(OutMarker);
            if (Math.Abs(pos.X - outX) < 10)
            {
                _isDraggingOutMarker = true;
                WaveformCanvas.CaptureMouse();
                return;
            }
        }

        // Check if clicking on playhead
        double playheadX = Canvas.GetLeft(Playhead);
        if (Math.Abs(pos.X - playheadX) < 10)
        {
            _isDraggingPlayhead = true;
            WaveformCanvas.CaptureMouse();
            return;
        }

        // Otherwise, seek to clicked position
        SeekToPosition(pos.X);
    }

    private void WaveformCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingPlayhead && !_isDraggingInMarker && !_isDraggingOutMarker)
            return;

        var pos = e.GetPosition(WaveformCanvas);

        if (_isDraggingPlayhead)
        {
            SeekToPosition(pos.X);
        }
        else if (_isDraggingInMarker)
        {
            SetInPointFromPosition(pos.X);
        }
        else if (_isDraggingOutMarker)
        {
            SetOutPointFromPosition(pos.X);
        }
    }

    private void WaveformCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingPlayhead = false;
        _isDraggingInMarker = false;
        _isDraggingOutMarker = false;
        WaveformCanvas.ReleaseMouseCapture();
    }

    private void SeekToPosition(double x)
    {
        if (Duration == TimeSpan.Zero || WaveformCanvas.Width == 0)
            return;

        double normalizedX = x / WaveformCanvas.Width;
        normalizedX = Math.Clamp(normalizedX, 0, 1);

        var newPosition = TimeSpan.FromSeconds(normalizedX * Duration.TotalSeconds);
        CurrentPosition = newPosition;
        PositionChanged?.Invoke(this, newPosition);
    }

    private void SetInPointFromPosition(double x)
    {
        if (Duration == TimeSpan.Zero || WaveformCanvas.Width == 0)
            return;

        double normalizedX = x / WaveformCanvas.Width;
        normalizedX = Math.Clamp(normalizedX, 0, 1);

        var newInPoint = TimeSpan.FromSeconds(normalizedX * Duration.TotalSeconds);

        // Don't allow IN point after OUT point
        if (OutPoint.HasValue && newInPoint >= OutPoint.Value)
            return;

        InPoint = newInPoint;
        InPointChanged?.Invoke(this, newInPoint);
    }

    private void SetOutPointFromPosition(double x)
    {
        if (Duration == TimeSpan.Zero || WaveformCanvas.Width == 0)
            return;

        double normalizedX = x / WaveformCanvas.Width;
        normalizedX = Math.Clamp(normalizedX, 0, 1);

        var newOutPoint = TimeSpan.FromSeconds(normalizedX * Duration.TotalSeconds);

        // Don't allow OUT point before IN point
        if (InPoint.HasValue && newOutPoint <= InPoint.Value)
            return;

        OutPoint = newOutPoint;
        OutPointChanged?.Invoke(this, newOutPoint);
    }

    private void WaveformScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Zoom
            if (e.Delta > 0)
                ZoomIn();
            else
                ZoomOut();
            e.Handled = true;
        }
        // Otherwise let ScrollViewer handle horizontal scrolling naturally
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomIn();
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomOut();
    }

    private void ZoomFit_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = 1.0;
        UpdateZoomLevel();
        RegenerateWaveformAtZoom();
    }

    private void ZoomIn()
    {
        _zoomLevel = Math.Min(_zoomLevel * 1.5, 20.0);
        UpdateZoomLevel();
        RegenerateWaveformAtZoom();
    }

    private void ZoomOut()
    {
        _zoomLevel = Math.Max(_zoomLevel / 1.5, 1.0);
        UpdateZoomLevel();
        RegenerateWaveformAtZoom();
    }

    private void UpdateZoomLevel()
    {
        ZoomLevelText.Text = $"{_zoomLevel * 100:F0}%";
    }
}
