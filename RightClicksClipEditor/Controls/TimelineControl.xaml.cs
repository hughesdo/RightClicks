using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Serilog;

namespace RightClicksClipEditor.Controls;

public partial class TimelineControl : UserControl
{
    // Dependency Properties
    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(nameof(Duration), typeof(TimeSpan), typeof(TimelineControl),
            new PropertyMetadata(TimeSpan.Zero, OnDurationChanged));

    public static readonly DependencyProperty CurrentPositionProperty =
        DependencyProperty.Register(nameof(CurrentPosition), typeof(TimeSpan), typeof(TimelineControl),
            new PropertyMetadata(TimeSpan.Zero, OnCurrentPositionChanged));

    public static readonly DependencyProperty InPointProperty =
        DependencyProperty.Register(nameof(InPoint), typeof(TimeSpan?), typeof(TimelineControl),
            new PropertyMetadata(null, OnInPointChanged));

    public static readonly DependencyProperty OutPointProperty =
        DependencyProperty.Register(nameof(OutPoint), typeof(TimeSpan?), typeof(TimelineControl),
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
    private double _scrollOffset = 0.0;
    private bool _isDraggingPlayhead = false;
    private bool _isDraggingInMarker = false;
    private bool _isDraggingOutMarker = false;
    private Point _dragStartPoint;

    public TimelineControl()
    {
        InitializeComponent();
        Loaded += TimelineControl_Loaded;
        SizeChanged += TimelineControl_SizeChanged;
    }

    private void TimelineControl_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateTimeline();
    }

    private void TimelineControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateTimeline();
    }

    private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineControl control)
        {
            control.UpdateTimeline();
        }
    }

    private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineControl control)
        {
            control.UpdatePlayheadPosition();
        }
    }

    private static void OnInPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineControl control)
        {
            control.UpdateInMarkerPosition();
            control.UpdateSelectionHighlight();
        }
    }

    private static void OnOutPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TimelineControl control)
        {
            control.UpdateOutMarkerPosition();
            control.UpdateSelectionHighlight();
        }
    }

    private void UpdateTimeline()
    {
        if (Duration == TimeSpan.Zero || TimelineCanvas.ActualWidth == 0)
            return;

        DrawTimecodeLabels();
        UpdatePlayheadPosition();
        UpdateInMarkerPosition();
        UpdateOutMarkerPosition();
        UpdateSelectionHighlight();
    }

    private void DrawTimecodeLabels()
    {
        TimecodeCanvas.Children.Clear();

        if (Duration == TimeSpan.Zero || TimecodeCanvas.ActualWidth == 0)
            return;

        double width = TimecodeCanvas.ActualWidth * _zoomLevel;
        double pixelsPerSecond = width / Duration.TotalSeconds;

        // Determine interval based on zoom level
        double interval = DetermineTimecodeInterval(pixelsPerSecond);

        for (double seconds = 0; seconds <= Duration.TotalSeconds; seconds += interval)
        {
            double x = (seconds / Duration.TotalSeconds) * width - _scrollOffset;

            if (x < -50 || x > TimecodeCanvas.ActualWidth + 50)
                continue;

            // Draw tick mark
            var tick = new Line
            {
                X1 = x, Y1 = 20, X2 = x, Y2 = 30,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };
            TimecodeCanvas.Children.Add(tick);

            // Draw timecode label
            var timecode = TimeSpan.FromSeconds(seconds);
            var label = new TextBlock
            {
                Text = FormatTimecode(timecode),
                Foreground = Brushes.LightGray,
                FontSize = 10
            };
            Canvas.SetLeft(label, x - 25);
            Canvas.SetTop(label, 2);
            TimecodeCanvas.Children.Add(label);
        }
    }

    private double DetermineTimecodeInterval(double pixelsPerSecond)
    {
        // Choose interval so labels don't overlap (minimum 60 pixels apart)
        double[] intervals = { 0.1, 0.5, 1, 5, 10, 30, 60, 300, 600 }; // seconds

        foreach (var interval in intervals)
        {
            if (interval * pixelsPerSecond >= 60)
                return interval;
        }

        return 600; // 10 minutes
    }

    private string FormatTimecode(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return time.ToString(@"h\:mm\:ss\.fff");
        else if (time.TotalMinutes >= 1)
            return time.ToString(@"m\:ss\.fff");
        else
            return time.ToString(@"s\.fff");
    }

    private void UpdatePlayheadPosition()
    {
        if (Duration == TimeSpan.Zero || TimelineCanvas.ActualWidth == 0)
            return;

        double width = TimelineCanvas.ActualWidth * _zoomLevel;
        double x = (CurrentPosition.TotalSeconds / Duration.TotalSeconds) * width - _scrollOffset;

        Canvas.SetLeft(Playhead, x);
        PlayheadLabel.Text = CurrentPosition.ToString(@"hh\:mm\:ss\.fff");
    }

    private void UpdateInMarkerPosition()
    {
        if (InPoint.HasValue)
        {
            InMarker.Visibility = Visibility.Visible;

            if (Duration != TimeSpan.Zero && TimelineCanvas.ActualWidth > 0)
            {
                double width = TimelineCanvas.ActualWidth * _zoomLevel;
                double x = (InPoint.Value.TotalSeconds / Duration.TotalSeconds) * width - _scrollOffset;
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

            if (Duration != TimeSpan.Zero && TimelineCanvas.ActualWidth > 0)
            {
                double width = TimelineCanvas.ActualWidth * _zoomLevel;
                double x = (OutPoint.Value.TotalSeconds / Duration.TotalSeconds) * width - _scrollOffset;
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
        if (InPoint.HasValue && OutPoint.HasValue && Duration != TimeSpan.Zero && TimelineCanvas.ActualWidth > 0)
        {
            SelectionHighlight.Visibility = Visibility.Visible;

            double width = TimelineCanvas.ActualWidth * _zoomLevel;
            double inX = (InPoint.Value.TotalSeconds / Duration.TotalSeconds) * width - _scrollOffset;
            double outX = (OutPoint.Value.TotalSeconds / Duration.TotalSeconds) * width - _scrollOffset;

            Canvas.SetLeft(SelectionHighlight, inX);
            SelectionHighlight.Width = outX - inX;
            SelectionHighlight.Height = TimelineCanvas.ActualHeight;
        }
        else
        {
            SelectionHighlight.Visibility = Visibility.Collapsed;
        }
    }

    private void TimelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(TimelineCanvas);
        _dragStartPoint = pos;

        // Check if clicking on playhead
        double playheadX = Canvas.GetLeft(Playhead);
        if (Math.Abs(pos.X - playheadX) < 10)
        {
            _isDraggingPlayhead = true;
            TimelineCanvas.CaptureMouse();
            return;
        }

        // Check if clicking on IN marker
        if (InPoint.HasValue)
        {
            double inX = Canvas.GetLeft(InMarker);
            if (Math.Abs(pos.X - inX) < 10)
            {
                _isDraggingInMarker = true;
                TimelineCanvas.CaptureMouse();
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
                TimelineCanvas.CaptureMouse();
                return;
            }
        }

        // Otherwise, seek to clicked position
        SeekToPosition(pos.X);
    }

    private void TimelineCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingPlayhead && !_isDraggingInMarker && !_isDraggingOutMarker)
            return;

        var pos = e.GetPosition(TimelineCanvas);

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

    private void TimelineCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingPlayhead = false;
        _isDraggingInMarker = false;
        _isDraggingOutMarker = false;
        TimelineCanvas.ReleaseMouseCapture();
    }

    private void TimelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Zoom
            if (e.Delta > 0)
                ZoomIn();
            else
                ZoomOut();
        }
        else if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            // Horizontal scroll
            _scrollOffset -= e.Delta;
            _scrollOffset = Math.Max(0, _scrollOffset);
            UpdateTimeline();
        }
    }

    private void SeekToPosition(double x)
    {
        if (Duration == TimeSpan.Zero || TimelineCanvas.ActualWidth == 0)
            return;

        double width = TimelineCanvas.ActualWidth * _zoomLevel;
        double normalizedX = (x + _scrollOffset) / width;
        normalizedX = Math.Clamp(normalizedX, 0, 1);

        var newPosition = TimeSpan.FromSeconds(normalizedX * Duration.TotalSeconds);
        CurrentPosition = newPosition;
        PositionChanged?.Invoke(this, newPosition);
    }

    private void SetInPointFromPosition(double x)
    {
        if (Duration == TimeSpan.Zero || TimelineCanvas.ActualWidth == 0)
            return;

        double width = TimelineCanvas.ActualWidth * _zoomLevel;
        double normalizedX = (x + _scrollOffset) / width;
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
        if (Duration == TimeSpan.Zero || TimelineCanvas.ActualWidth == 0)
            return;

        double width = TimelineCanvas.ActualWidth * _zoomLevel;
        double normalizedX = (x + _scrollOffset) / width;
        normalizedX = Math.Clamp(normalizedX, 0, 1);

        var newOutPoint = TimeSpan.FromSeconds(normalizedX * Duration.TotalSeconds);

        // Don't allow OUT point before IN point
        if (InPoint.HasValue && newOutPoint <= InPoint.Value)
            return;

        OutPoint = newOutPoint;
        OutPointChanged?.Invoke(this, newOutPoint);
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
        _scrollOffset = 0;
        UpdateZoomLevel();
        UpdateTimeline();
    }

    private void ZoomIn()
    {
        _zoomLevel = Math.Min(_zoomLevel * 1.5, 20.0);
        UpdateZoomLevel();
        UpdateTimeline();
    }

    private void ZoomOut()
    {
        _zoomLevel = Math.Max(_zoomLevel / 1.5, 1.0);
        if (_zoomLevel == 1.0)
            _scrollOffset = 0;
        UpdateZoomLevel();
        UpdateTimeline();
    }

    private void UpdateZoomLevel()
    {
        ZoomLevelText.Text = $"{_zoomLevel * 100:F0}%";
    }
}
