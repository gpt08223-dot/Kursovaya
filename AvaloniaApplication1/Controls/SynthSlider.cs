using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvaloniaApplication1.Controls;

public sealed class SynthSlider : Control
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<SynthSlider, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<SynthSlider, double>(nameof(Maximum), 1.0);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SynthSlider, double>(
            nameof(Value),
            0.0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<SynthSlider, double>(nameof(Step), 0.0);

    public static readonly StyledProperty<IBrush?> AccentProperty =
        AvaloniaProperty.Register<SynthSlider, IBrush?>(nameof(Accent));

    private const double HorizontalPadding = 7.0;
    private const double TrackThickness = 3.0;
    private const double ThumbRadius = 5.5;

    private bool _dragging;
    private bool _hovered;

    static SynthSlider()
    {
        AffectsRender<SynthSlider>(
            MinimumProperty,
            MaximumProperty,
            ValueProperty,
            StepProperty,
            AccentProperty);
    }

    public SynthSlider()
    {
        Height = 18;
        MinHeight = 18;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public IBrush? Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        if (width <= HorizontalPadding * 2)
        {
            return;
        }

        double range = Math.Max(0.000001, Maximum - Minimum);
        double normalized = Math.Clamp((Value - Minimum) / range, 0.0, 1.0);
        double trackY = Bounds.Height * 0.5;
        double left = HorizontalPadding;
        double right = width - HorizontalPadding;
        double thumbX = left + (right - left) * normalized;

        var backgroundRect = new Rect(left, trackY - TrackThickness * 0.5, right - left, TrackThickness);
        context.DrawRectangle(StudioPalette.Track, null, backgroundRect, TrackThickness * 0.5);

        IBrush accent = Accent ?? StudioPalette.Accent;

        if (thumbX > left)
        {
            var fillRect = new Rect(left, trackY - TrackThickness * 0.5, thumbX - left, TrackThickness);
            context.DrawRectangle(accent, null, fillRect, TrackThickness * 0.5);
        }

        if (_hovered || _dragging || IsFocused)
        {
            context.DrawEllipse(StudioPalette.Glow, null, new Point(thumbX, trackY), 10.0, 10.0);
        }

        var thumbCenter = new Point(thumbX, trackY);
        var thumbFill = _dragging ? accent : StudioPalette.Text;
        var thumbBorder = new Pen(_dragging ? StudioPalette.Text : accent, 1.5);
        context.DrawEllipse(thumbFill, thumbBorder, thumbCenter, ThumbRadius, ThumbRadius);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragging = true;
        e.Pointer.Capture(this);
        Focus();
        SetValueFromPoint(e.GetPosition(this));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        bool hovered = new Rect(Bounds.Size).Contains(e.GetPosition(this));
        if (hovered != _hovered)
        {
            _hovered = hovered;
            InvalidateVisual();
        }

        if (_dragging)
        {
            SetValueFromPoint(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_dragging)
        {
            _dragging = false;
            e.Pointer.Capture(null);
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragging = false;
        InvalidateVisual();
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _hovered = true;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = false;
        InvalidateVisual();
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        InvalidateVisual();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        double range = Maximum - Minimum;
        double increment = Step > 0.0 ? Step : Math.Max(0.01, range / 100.0);
        double next = Value;

        switch (e.Key)
        {
            case Key.Left:
            case Key.Down:
                next -= increment;
                break;
            case Key.Right:
            case Key.Up:
                next += increment;
                break;
            case Key.Home:
                next = Minimum;
                break;
            case Key.End:
                next = Maximum;
                break;
            default:
                return;
        }

        SetCurrentValue(ValueProperty, Snap(Math.Clamp(next, Minimum, Maximum)));
        e.Handled = true;
    }

    private void SetValueFromPoint(Point point)
    {
        double usableWidth = Math.Max(1.0, Bounds.Width - HorizontalPadding * 2);
        double normalized = Math.Clamp((point.X - HorizontalPadding) / usableWidth, 0.0, 1.0);
        double value = Minimum + normalized * (Maximum - Minimum);
        SetCurrentValue(ValueProperty, Snap(value));
    }

    private double Snap(double value)
    {
        if (Step <= 0.0)
        {
            return value;
        }

        return Minimum + Math.Round((value - Minimum) / Step) * Step;
    }
}
