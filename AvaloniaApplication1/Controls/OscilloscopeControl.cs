using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaApplication1.AudioEngine;

namespace AvaloniaApplication1.Controls;

public sealed class OscilloscopeControl : Control
{
    public static readonly StyledProperty<SynthEngine?> EngineProperty =
        AvaloniaProperty.Register<OscilloscopeControl, SynthEngine?>(nameof(Engine));

    private static readonly Pen GridPen = new(StudioPalette.GridLine, 1);
    private static readonly Pen CenterPen = new(StudioPalette.GridLineStrong, 1);
    private static readonly Pen GlowPen = new(StudioPalette.Glow, 4, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly Pen WavePen = new(StudioPalette.Accent, 1.6, null, PenLineCap.Round, PenLineJoin.Round);

    private readonly float[] _raw = new float[2048];
    private readonly float[] _display = new float[1024];
    private readonly DispatcherTimer _timer;

    public OscilloscopeControl()
    {
        ClipToBounds = true;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, OnTick);
    }

    public SynthEngine? Engine
    {
        get => GetValue(EngineProperty);
        set => SetValue(EngineProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        SynthEngine? engine = Engine;

        if (engine != null)
        {
            engine.CopyScope(_raw);
        }
        else
        {
            Array.Clear(_raw);
        }

        int trigger = 0;

        for (int i = 1; i < _display.Length; i++)
        {
            if (_raw[i - 1] < 0f && _raw[i] >= 0f)
            {
                trigger = i;
                break;
            }
        }

        int available = Math.Min(_display.Length, _raw.Length - trigger);
        Array.Copy(_raw, trigger, _display, 0, available);

        if (available < _display.Length)
        {
            Array.Clear(_display, available, _display.Length - available);
        }

        InvalidateVisual();
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

        using (context.PushClip(new Rect(0, 0, width, height)))
        {
            for (int i = 1; i < 8; i++)
            {
                double x = width * i / 8.0;
                context.DrawLine(GridPen, new Point(x, 0), new Point(x, height));
            }

            for (int i = 1; i < 4; i++)
            {
                double y = height * i / 4.0;
                context.DrawLine(GridPen, new Point(0, y), new Point(width, y));
            }

            context.DrawLine(CenterPen, new Point(0, height * 0.5), new Point(width, height * 0.5));

            double center = height * 0.5;
            double amplitude = Math.Max(1.0, height * 0.5 - 7.0);
            int pointCount = Math.Max(2, Math.Min((int)width, 512));

            var geometry = new StreamGeometry();

            using (StreamGeometryContext geometryContext = geometry.Open())
            {
                for (int i = 0; i < pointCount; i++)
                {
                    double t = (double)i / (pointCount - 1);
                    double sampleIndex = t * (_display.Length - 1);
                    int indexA = (int)sampleIndex;
                    int indexB = Math.Min(indexA + 1, _display.Length - 1);
                    float fraction = (float)(sampleIndex - indexA);
                    float sample = _display[indexA] + (_display[indexB] - _display[indexA]) * fraction;
                    double x = t * width;
                    double y = center - sample * amplitude;

                    if (i == 0)
                    {
                        geometryContext.BeginFigure(new Point(x, y), false);
                    }
                    else
                    {
                        geometryContext.LineTo(new Point(x, y));
                    }
                }

                geometryContext.EndFigure(false);
            }

            context.DrawGeometry(null, GlowPen, geometry);
            context.DrawGeometry(null, WavePen, geometry);
        }
    }
}
