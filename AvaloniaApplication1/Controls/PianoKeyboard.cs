using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaApplication1.AudioEngine;

namespace AvaloniaApplication1.Controls;

public sealed class PianoKeyboard : Control
{
    public static readonly StyledProperty<SynthEngine?> EngineProperty =
        AvaloniaProperty.Register<PianoKeyboard, SynthEngine?>(nameof(Engine));

    public static readonly StyledProperty<double> WhiteKeyWidthProperty =
        AvaloniaProperty.Register<PianoKeyboard, double>(nameof(WhiteKeyWidth), 0.0);

    public static readonly StyledProperty<int> StartNoteProperty =
        AvaloniaProperty.Register<PianoKeyboard, int>(nameof(StartNote), MusicTheory.LowestNote);

    public static readonly StyledProperty<int> EndNoteProperty =
        AvaloniaProperty.Register<PianoKeyboard, int>(nameof(EndNote), MusicTheory.HighestNote);

    private static readonly Typeface LabelTypeface =
        new(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);

    private static readonly IBrush WhiteGradient = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#F3F5F8"), 0.0),
            new GradientStop(Color.Parse("#DDE0E5"), 1.0)
        }
    };

    private static readonly IBrush BlackGradient = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#2B2F36"), 0.0),
            new GradientStop(Color.Parse("#1B1E23"), 1.0)
        }
    };

    private readonly HashSet<int> _pressed = new();
    private SynthEngine? _subscribedEngine;
    private int _mouseNote = -1;
    private int _hoverNote = -1;
    private bool _mouseDown;
    private bool _attached;

    static PianoKeyboard()
    {
        AffectsMeasure<PianoKeyboard>(WhiteKeyWidthProperty, StartNoteProperty, EndNoteProperty);
        AffectsRender<PianoKeyboard>(EngineProperty);
    }

    public PianoKeyboard()
    {
        ClipToBounds = true;
        Focusable = false;
    }

    public SynthEngine? Engine
    {
        get => GetValue(EngineProperty);
        set => SetValue(EngineProperty, value);
    }

    public double WhiteKeyWidth
    {
        get => GetValue(WhiteKeyWidthProperty);
        set => SetValue(WhiteKeyWidthProperty, value);
    }

    public int StartNote
    {
        get => GetValue(StartNoteProperty);
        set => SetValue(StartNoteProperty, value);
    }

    public int EndNote
    {
        get => GetValue(EndNoteProperty);
        set => SetValue(EndNoteProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int whiteCount = Math.Max(1, MusicTheory.CountWhiteKeys(StartNote, EndNote));
        double height = double.IsInfinity(availableSize.Height) || double.IsNaN(availableSize.Height)
            ? 150.0
            : availableSize.Height;

        double width;
        if (WhiteKeyWidth > 0.0)
        {
            width = whiteCount * WhiteKeyWidth;
        }
        else if (double.IsInfinity(availableSize.Width) || double.IsNaN(availableSize.Width))
        {
            width = whiteCount * 24.0;
        }
        else
        {
            width = availableSize.Width;
        }

        return new Size(width, height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width <= 1.0 || height <= 1.0)
        {
            return;
        }

        int whiteCount = MusicTheory.CountWhiteKeys(StartNote, EndNote);
        double whiteWidth = width / whiteCount;
        double blackWidth = Math.Clamp(whiteWidth * 0.62, 4.0, 24.0);
        double blackHeight = height * 0.62;

        using (context.PushClip(new Rect(0, 0, width, height)))
        {
            int whiteIndex = 0;

            for (int note = StartNote; note <= EndNote; note++)
            {
                if (MusicTheory.IsBlackKey(note))
                {
                    continue;
                }

                bool pressed = _pressed.Contains(note);
                bool hovered = note == _hoverNote;

                var rect = new Rect(whiteIndex * whiteWidth + 0.6, 0, Math.Max(0.5, whiteWidth - 1.2), height);
                IBrush fill = pressed
                    ? StudioPalette.Accent
                    : hovered
                        ? StudioPalette.WhiteKeyHover
                        : WhiteGradient;

                context.DrawRectangle(fill, null, rect, 2, 2);

                if (note % 12 == 0)
                {
                    DrawOctaveLabel(context, note, rect, height, pressed);
                }

                whiteIndex++;
            }

            int whitesBefore = 0;

            for (int note = StartNote; note <= EndNote; note++)
            {
                if (!MusicTheory.IsBlackKey(note))
                {
                    whitesBefore++;
                    continue;
                }

                double x = whitesBefore * whiteWidth - blackWidth * 0.5;
                bool pressed = _pressed.Contains(note);
                bool hovered = note == _hoverNote;

                var shadow = new Rect(x + 1.0, 2.5, blackWidth, blackHeight);
                context.DrawRectangle(StudioPalette.KeyShadow, null, shadow, 2, 2);

                var rect = new Rect(x, 0, blackWidth, blackHeight);
                IBrush fill = pressed
                    ? StudioPalette.Accent
                    : hovered
                        ? StudioPalette.BlackKeyHover
                        : BlackGradient;

                context.DrawRectangle(fill, new Pen(StudioPalette.BlackKeyBorder, 1), rect, 2, 2);
            }
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        Subscribe();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        Unsubscribe();
        ReleaseMouseNote();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == EngineProperty)
        {
            Unsubscribe();
            _pressed.Clear();

            if (_attached)
            {
                Subscribe();
            }

            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _mouseDown = true;
        e.Pointer.Capture(this);
        _mouseNote = HitTest(e.GetPosition(this));

        if (_mouseNote >= 0)
        {
            PressNote(_mouseNote);
        }

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        Point point = e.GetPosition(this);
        int hover = HitTest(point);
        if (hover != _hoverNote)
        {
            _hoverNote = hover;
            InvalidateVisual();
        }

        if (!_mouseDown)
        {
            return;
        }

        int note = HitTest(point);
        if (note == _mouseNote)
        {
            return;
        }

        ReleaseMouseNote();

        if (note >= 0)
        {
            _mouseNote = note;
            PressNote(note);
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_mouseDown)
        {
            return;
        }

        _mouseDown = false;
        ReleaseMouseNote();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _mouseDown = false;
        ReleaseMouseNote();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (_hoverNote != -1)
        {
            _hoverNote = -1;
            InvalidateVisual();
        }
    }

    private void PressNote(int note)
    {
        if (_engineRef == null)
        {
            _pressed.Add(note);
            InvalidateVisual();
            return;
        }

        _engineRef.NoteOn(note);
    }

    private SynthEngine? _engineRef => Engine;

    private void ReleaseMouseNote()
    {
        if (_mouseNote < 0)
        {
            return;
        }

        int note = _mouseNote;
        _mouseNote = -1;

        if (_engineRef == null)
        {
            _pressed.Remove(note);
            InvalidateVisual();
            return;
        }

        _engineRef.NoteOff(note);
    }

    private int HitTest(Point point)
    {
        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width <= 1.0 || height <= 1.0 || point.X < 0 || point.X >= width || point.Y < 0 || point.Y >= height)
        {
            return -1;
        }

        int whiteCount = MusicTheory.CountWhiteKeys(StartNote, EndNote);
        double whiteWidth = width / whiteCount;
        double blackWidth = Math.Clamp(whiteWidth * 0.62, 4.0, 24.0);
        double blackHeight = height * 0.62;

        if (point.Y <= blackHeight)
        {
            int whitesBefore = 0;

            for (int note = StartNote; note <= EndNote; note++)
            {
                if (!MusicTheory.IsBlackKey(note))
                {
                    whitesBefore++;
                    continue;
                }

                double x = whitesBefore * whiteWidth - blackWidth * 0.5;
                if (point.X >= x && point.X <= x + blackWidth)
                {
                    return note;
                }
            }
        }

        int index = (int)Math.Floor(point.X / whiteWidth);
        if (index < 0 || index >= whiteCount)
        {
            return -1;
        }

        int count = 0;

        for (int note = StartNote; note <= EndNote; note++)
        {
            if (MusicTheory.IsBlackKey(note))
            {
                continue;
            }

            if (count == index)
            {
                return note;
            }

            count++;
        }

        return -1;
    }

    private static void DrawOctaveLabel(DrawingContext context, int note, Rect rect, double height, bool pressed)
    {
        string text = MusicTheory.GetNoteName(note);
        var brush = pressed ? StudioPalette.KeyLabelPressed : StudioPalette.KeyLabel;
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            8.5,
            brush);

        double x = rect.X + (rect.Width - formatted.Width) * 0.5;
        double y = height - formatted.Height - 6;
        context.DrawText(formatted, new Point(x, y));
    }

    private void Subscribe()
    {
        if (_subscribedEngine != null || Engine == null)
        {
            return;
        }

        _subscribedEngine = Engine;
        _subscribedEngine.NotePressed += OnEngineNotePressed;
        _subscribedEngine.NoteReleased += OnEngineNoteReleased;
        _pressed.Clear();
        InvalidateVisual();
    }

    private void Unsubscribe()
    {
        if (_subscribedEngine == null)
        {
            return;
        }

        _subscribedEngine.NotePressed -= OnEngineNotePressed;
        _subscribedEngine.NoteReleased -= OnEngineNoteReleased;
        _subscribedEngine = null;
        _pressed.Clear();
    }

    private void OnEngineNotePressed(int note)
    {
        _pressed.Add(note);
        InvalidateVisual();
    }

    private void OnEngineNoteReleased(int note)
    {
        _pressed.Remove(note);
        InvalidateVisual();
    }
}
