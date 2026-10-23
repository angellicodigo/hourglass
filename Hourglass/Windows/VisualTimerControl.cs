// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VisualTimerControl.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Windows;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Extensions;
using Timing;

/// <summary>
/// A pure circular analog visual timer dial (0-60 minutes) featuring 60 tick marks,
/// 5-minute counter-clockwise numerals (0, 5, 10, ... 55), a colored disc wedge,
/// and interactive draggable knob with continuous second precision.
/// </summary>
public sealed class VisualTimerControl : FrameworkElement
{
    private static readonly Typeface DialTypeface = new(
        new FontFamily("Segoe UI"),
        FontStyles.Normal,
        FontWeights.SemiBold,
        FontStretches.Normal);

    private Theme? _theme;
    private TimeSpan _timeLeft = TimeSpan.Zero;
    private bool _isLocked;
    private bool _isDragging;
    private int _draggedSeconds;
    private bool _isMouseOverKnob;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualTimerControl"/> class.
    /// </summary>
    public VisualTimerControl()
    {
        ClipToBounds = true;
        Focusable = true;
        FocusVisualStyle = null;
    }

    /// <summary>
    /// Occurs when the user releases the knob after dragging to set a timer duration.
    /// </summary>
    public event EventHandler<TimeSpan>? DurationCommitted;

    /// <summary>
    /// Occurs continuously while the user drags the knob to choose a timer duration.
    /// </summary>
    public event EventHandler<TimeSpan>? DraggedDurationChanged;

    /// <summary>
    /// Gets or sets the active theme.
    /// </summary>
    public Theme? Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Gets or sets the remaining countdown time.
    /// </summary>
    public TimeSpan TimeLeft
    {
        get => _timeLeft;
        set
        {
            if (_timeLeft != value)
            {
                _timeLeft = value;
                if (!_isDragging)
                {
                    InvalidateVisual();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dial is locked against dragging.
    /// </summary>
    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                if (_isLocked && _isDragging)
                {
                    _isDragging = false;
                    _draggedSeconds = 0;
                    ReleaseMouseCapture();
                    DraggedDurationChanged?.Invoke(this, TimeSpan.Zero);
                }

                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the knob is actively being dragged.
    /// </summary>
    public bool IsDragging => _isDragging;

    private const double BaseDimension = 260.0;

    /// <inheritdoc />
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Right && _isDragging)
        {
            _isDragging = false;
            _draggedSeconds = 0;
            ReleaseMouseCapture();
            InvalidateVisual();
            DraggedDurationChanged?.Invoke(this, TimeSpan.Zero);
            e.Handled = true;
            return;
        }

        if (e.ChangedButton != MouseButton.Left || _isLocked)
        {
            return;
        }

        Point p = e.GetPosition(this);
        double minDim = Math.Min(ActualWidth, ActualHeight);
        if (minDim <= 20)
        {
            return;
        }

        double cx = ActualWidth / 2.0;
        double cy = ActualHeight / 2.0;
        double scale = minDim / BaseDimension;
        double outerRadius = Math.Max(10.0, (minDim / 2.0) - (8.0 * scale));
        double rNumerals = outerRadius - (12.0 * scale);
        double rTicksOuter = rNumerals - (12.0 * scale);

        double dx = p.X - cx;
        double dy = p.Y - cy;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        // Check if click is on or near the dial face / perimeter
        if (dist <= rTicksOuter + (14.0 * scale))
        {
            _isDragging = true;
            CaptureMouse();
            _draggedSeconds = (int)Math.Round(MathExtensions.LimitToRange(TimeLeft.TotalSeconds, 0, 3600));
            UpdateDragFromPoint(p, cx, cy);
            DraggedDurationChanged?.Invoke(this, TimeSpan.FromSeconds(_draggedSeconds));
            e.Handled = true;
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        Point p = e.GetPosition(this);
        double minDim = Math.Min(ActualWidth, ActualHeight);
        if (minDim <= 20)
        {
            return;
        }

        double cx = ActualWidth / 2.0;
        double cy = ActualHeight / 2.0;

        if (_isDragging)
        {
            UpdateDragFromPoint(p, cx, cy);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (!_isLocked)
        {
            double scale = minDim / BaseDimension;
            double outerRadius = Math.Max(10.0, (minDim / 2.0) - (8.0 * scale));
            double rNumerals = outerRadius - (12.0 * scale);
            double rTicksOuter = rNumerals - (12.0 * scale);
            double rTicksMajorInner = rTicksOuter - (8.0 * scale);
            double rDisc = Math.Max(5.0, rTicksMajorInner - (2.0 * scale));

            double currentSec = MathExtensions.LimitToRange(TimeLeft.TotalSeconds, 0, 3600);
            double currentAngleDeg = (currentSec / 3600.0) * 360.0;
            double currentAngleRad = currentAngleDeg * Math.PI / 180.0;
            double kx = cx - rDisc * Math.Sin(currentAngleRad);
            double ky = cy - rDisc * Math.Cos(currentAngleRad);

            double distToKnob = Math.Sqrt((p.X - kx) * (p.X - kx) + (p.Y - ky) * (p.Y - ky));
            double distToCenter = Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy));

            double hitKnobRadius = Math.Max(18.0, 18.0 * scale);
            double hitBandWidth = Math.Max(15.0, 15.0 * scale);
            bool overKnob = distToKnob <= hitKnobRadius || Math.Abs(distToCenter - rDisc) <= hitBandWidth;
            if (overKnob != _isMouseOverKnob)
            {
                _isMouseOverKnob = overKnob;
                InvalidateVisual();
            }

            Cursor = overKnob ? Cursors.Hand : Cursors.Arrow;
        }
        else
        {
            if (_isMouseOverKnob)
            {
                _isMouseOverKnob = false;
                InvalidateVisual();
            }

            Cursor = Cursors.Arrow;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (_isMouseOverKnob && !_isDragging)
        {
            _isMouseOverKnob = false;
            InvalidateVisual();
        }

        if (!_isDragging)
        {
            Cursor = Cursors.Arrow;
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (_isDragging && e.Key == Key.Escape)
        {
            _isDragging = false;
            _draggedSeconds = 0;
            ReleaseMouseCapture();
            InvalidateVisual();
            DraggedDurationChanged?.Invoke(this, TimeSpan.Zero);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == MouseButton.Left && _isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
            InvalidateVisual();

            int committed = _draggedSeconds;
            _draggedSeconds = 0;

            if (committed > 0)
            {
                DurationCommitted?.Invoke(this, TimeSpan.FromSeconds(committed));
            }
            else
            {
                DraggedDurationChanged?.Invoke(this, TimeSpan.Zero);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);

        if (_isDragging)
        {
            _isDragging = false;
            _draggedSeconds = 0;
            InvalidateVisual();
            DraggedDurationChanged?.Invoke(this, TimeSpan.Zero);
        }
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth;
        double h = ActualHeight;
        double minDim = Math.Min(w, h);
        if (minDim <= 20)
        {
            return;
        }

        double cx = w / 2.0;
        double cy = h / 2.0;
        double scale = minDim / BaseDimension;

        double outerRadius = Math.Max(10.0, (minDim / 2.0) - (8.0 * scale));
        double rNumerals = outerRadius - (12.0 * scale);
        double rTicksOuter = rNumerals - (12.0 * scale);
        double rTicksMajorInner = rTicksOuter - (8.0 * scale);
        double rTicksMinorInner = rTicksOuter - (4.0 * scale);
        double rDisc = Math.Max(5.0, rTicksMajorInner - (2.0 * scale));
        double rKnob = Math.Max(4.0, 7.5 * scale);

        // Theme brushes
        Brush discBrush = _theme?.ProgressBarBrush ?? Brushes.Crimson;
        Brush trackBrush = _theme?.ProgressBackgroundBrush ?? new SolidColorBrush(Color.FromArgb(40, 128, 128, 128));
        Brush primaryTextBrush = _theme?.PrimaryTextBrush ?? Brushes.Black;
        Brush secondaryTextBrush = _theme?.SecondaryTextBrush ?? primaryTextBrush;
        Brush bgBrush = _theme?.BackgroundBrush ?? Brushes.White;
        Brush knobAccentBrush = (_isDragging || _isMouseOverKnob)
            ? (_theme?.ButtonHoverBrush ?? primaryTextBrush)
            : primaryTextBrush;

        // 1. Background circular track
        Pen trackPen = new(new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)), 1.0);
        trackPen.Freeze();
        dc.DrawEllipse(trackBrush, trackPen, new Point(cx, cy), rDisc, rDisc);

        // 2. Colored disc wedge (no inner hub circle)
        double displaySeconds = _isDragging ? _draggedSeconds : MathExtensions.LimitToRange(TimeLeft.TotalSeconds, 0, 3600);
        if (displaySeconds >= 3600)
        {
            dc.DrawEllipse(discBrush, null, new Point(cx, cy), rDisc, rDisc);
        }
        else if (displaySeconds > 0)
        {
            double wedgeAngleDeg = (displaySeconds / 3600.0) * 360.0;
            double wedgeAngleRad = wedgeAngleDeg * Math.PI / 180.0;
            Point arcEndPoint = new(cx - rDisc * Math.Sin(wedgeAngleRad), cy - rDisc * Math.Cos(wedgeAngleRad));

            StreamGeometry wedgeGeom = new();
            using (StreamGeometryContext ctx = wedgeGeom.Open())
            {
                ctx.BeginFigure(new Point(cx, cy), isFilled: true, isClosed: true);
                ctx.LineTo(new Point(cx, cy - rDisc), isStroked: false, isSmoothJoin: false);
                ctx.ArcTo(
                    arcEndPoint,
                    new Size(rDisc, rDisc),
                    0.0,
                    isLargeArc: wedgeAngleDeg > 180.0,
                    SweepDirection.Counterclockwise,
                    isStroked: false,
                    isSmoothJoin: false);
            }

            wedgeGeom.Freeze();
            dc.DrawGeometry(discBrush, null, wedgeGeom);
        }

        // 3. 60 Tick Marks
        Pen majorTickPen = new(primaryTextBrush, Math.Max(1.0, 1.75 * scale));
        majorTickPen.Freeze();

        Brush minorTickColorBrush = secondaryTextBrush.CloneCurrentValue();
        minorTickColorBrush.Opacity = 0.65;
        minorTickColorBrush.Freeze();
        Pen minorTickPen = new(minorTickColorBrush, Math.Max(0.5, 0.85 * scale));
        minorTickPen.Freeze();

        for (int i = 0; i < 60; i++)
        {
            double tickAngleDeg = i * 6.0;
            double tickAngleRad = tickAngleDeg * Math.PI / 180.0;
            double dx = -Math.Sin(tickAngleRad);
            double dy = -Math.Cos(tickAngleRad);

            if (i % 5 == 0)
            {
                Point p1 = new(cx + dx * rTicksMajorInner, cy + dy * rTicksMajorInner);
                Point p2 = new(cx + dx * rTicksOuter, cy + dy * rTicksOuter);
                dc.DrawLine(majorTickPen, p1, p2);
            }
            else
            {
                Point p1 = new(cx + dx * rTicksMinorInner, cy + dy * rTicksMinorInner);
                Point p2 = new(cx + dx * rTicksOuter, cy + dy * rTicksOuter);
                dc.DrawLine(minorTickPen, p1, p2);
            }
        }

        // 4. 5-Minute Numerals (counter-clockwise: 0, 5, 10, ... 55)
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        double numeralFontSize = Math.Max(7.0, 11.5 * scale);

        for (int m = 0; m < 60; m += 5)
        {
            double numAngleDeg = m * 6.0;
            double numAngleRad = numAngleDeg * Math.PI / 180.0;
            double dx = -Math.Sin(numAngleRad);
            double dy = -Math.Cos(numAngleRad);

            double tx = cx + dx * rNumerals;
            double ty = cy + dy * rNumerals;

            FormattedText ft = new(
                m.ToString(CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                DialTypeface,
                numeralFontSize,
                primaryTextBrush,
                pixelsPerDip);

            dc.DrawText(ft, new Point(tx - ft.Width / 2.0, ty - ft.Height / 2.0));
        }

        // 5. Knob Handle at the perimeter of the colored disc
        double knobAngleDeg = (displaySeconds / 3600.0) * 360.0;
        double knobAngleRad = knobAngleDeg * Math.PI / 180.0;
        double kx = cx - rDisc * Math.Sin(knobAngleRad);
        double ky = cy - rDisc * Math.Cos(knobAngleRad);

        Pen knobPen = new(knobAccentBrush, Math.Max(1.0, 2.0 * scale));
        knobPen.Freeze();
        dc.DrawEllipse(bgBrush, knobPen, new Point(kx, ky), rKnob, rKnob);
        dc.DrawEllipse(knobAccentBrush, null, new Point(kx, ky), Math.Max(1.5, 2.2 * scale), Math.Max(1.5, 2.2 * scale));
    }

    private void UpdateDragFromPoint(Point p, double cx, double cy)
    {
        int oldSec = _draggedSeconds;
        int candidateSeconds = MathExtensions.AngleToSeconds(p.X - cx, p.Y - cy);
        _draggedSeconds = MathExtensions.ComputeDraggedSeconds(_draggedSeconds, candidateSeconds);
        if (_draggedSeconds != oldSec)
        {
            DraggedDurationChanged?.Invoke(this, TimeSpan.FromSeconds(_draggedSeconds));
        }
    }
}
