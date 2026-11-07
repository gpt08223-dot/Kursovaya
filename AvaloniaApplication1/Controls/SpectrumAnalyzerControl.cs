using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaApplication1.AudioEngine;

namespace AvaloniaApplication1.Controls;

public sealed class SpectrumAnalyzerControl : Control
{
    public static readonly StyledProperty<SynthEngine?> EngineProperty =
        AvaloniaProperty.Register<SpectrumAnalyzerControl, SynthEngine?>(nameof(Engine));

    private const int FftSize = 2048;
    private const int BinCount = FftSize / 2;
    private const double MinFrequency = 20.0;
    private const double MaxFrequency = 20000.0;
    private const double MinDecibels = -84.0;

    private static readonly Typeface LabelTypeface =
        new(FontFamily.Default, FontStyle.Normal, FontWeight.Medium);

    private static readonly Pen GridPen = new(StudioPalette.GridLine, 1);
    private static readonly Pen LabelGridPen = new(StudioPalette.GridLineStrong, 1);
    private static readonly Pen SpectrumPen = new(StudioPalette.Amber, 1.4, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly Pen SpectrumGlowPen = new(StudioPalette.SpectrumGlow, 3.5, null, PenLineCap.Round, PenLineJoin.Round);

    private static readonly IBrush FillBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#3DE58E5A"), 0.0),
            new GradientStop(Color.Parse("#08E58E5A"), 1.0)
        }
    };

    private readonly float[] _samples = new float[FftSize];
    private readonly float[] _real = new float[FftSize];
    private readonly float[] _imaginary = new float[FftSize];
    private readonly float[] _window = new float[FftSize];
    private readonly float[] _smoothed = new float[BinCount];
    private readonly FastFourierTransform _fft = new(FftSize);
    private readonly DispatcherTimer _timer;

    public SpectrumAnalyzerControl()
    {
        ClipToBounds = true;

        for (int i = 0; i < FftSize; i++)
        {
            _window[i] = (float)(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (FftSize - 1))));
        }

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
            engine.CopyScope(_samples);
        }
        else
        {
            Array.Clear(_samples);
        }

        for (int i = 0; i < FftSize; i++)
        {
            _real[i] = _samples[i] * _window[i];
            _imaginary[i] = 0f;
        }

        _fft.Transform(_real, _imaginary);

        float normalization = 4f / FftSize;

        for (int bin = 1; bin < BinCount; bin++)
        {
            float real = _real[bin];
            float imaginary = _imaginary[bin];
            float magnitude = MathF.Sqrt(real * real + imaginary * imaginary) * normalization;
            float decibels = 20f * MathF.Log10(magnitude + 1e-7f);
            float normalized = Math.Clamp((decibels - (float)MinDecibels) / (float)-MinDecibels, 0f, 1f);

            float previous = _smoothed[bin];
            _smoothed[bin] = normalized >= previous ? normalized : Math.Max(0f, previous - 0.015f);
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

        double top = 4.0;
        double bottom = Math.Max(top + 10.0, height - 15.0);

        using (context.PushClip(new Rect(0, 0, width, height)))
        {
            for (int i = 1; i < 4; i++)
            {
                double y = top + (bottom - top) * i / 4.0;
                context.DrawLine(GridPen, new Point(0, y), new Point(width, y));
            }

            DrawFrequencyLine(context, 100.0, width, height, top, bottom, "100 Hz");
            DrawFrequencyLine(context, 1000.0, width, height, top, bottom, "1 kHz");
            DrawFrequencyLine(context, 10000.0, width, height, top, bottom, "10 kHz");

            const int pointCount = 256;
            var fillGeometry = new StreamGeometry();
            var lineGeometry = new StreamGeometry();

            using (StreamGeometryContext geometryContext = fillGeometry.Open())
            {
                geometryContext.BeginFigure(new Point(0, bottom), true);

                for (int i = 0; i <= pointCount; i++)
                {
                    double t = (double)i / pointCount;
                    double x = t * width;
                    double frequency = MinFrequency * Math.Pow(MaxFrequency / MinFrequency, t);
                    double binPosition = frequency * FftSize / SynthEngine.SampleRate;
                    int binA = Math.Clamp((int)binPosition, 1, BinCount - 1);
                    int binB = Math.Min(binA + 1, BinCount - 1);
                    float fraction = (float)(binPosition - binA);
                    float value = _smoothed[binA] + (_smoothed[binB] - _smoothed[binA]) * fraction;
                    double y = bottom - value * (bottom - top);
                    geometryContext.LineTo(new Point(x, y));
                }

                geometryContext.LineTo(new Point(width, bottom));
                geometryContext.EndFigure(true);
            }

            using (StreamGeometryContext geometryContext = lineGeometry.Open())
            {
                for (int i = 0; i <= pointCount; i++)
                {
                    double t = (double)i / pointCount;
                    double x = t * width;
                    double frequency = MinFrequency * Math.Pow(MaxFrequency / MinFrequency, t);
                    double binPosition = frequency * FftSize / SynthEngine.SampleRate;
                    int binA = Math.Clamp((int)binPosition, 1, BinCount - 1);
                    int binB = Math.Min(binA + 1, BinCount - 1);
                    float fraction = (float)(binPosition - binA);
                    float value = _smoothed[binA] + (_smoothed[binB] - _smoothed[binA]) * fraction;
                    double y = bottom - value * (bottom - top);

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

            context.DrawGeometry(FillBrush, null, fillGeometry);
            context.DrawGeometry(null, SpectrumGlowPen, lineGeometry);
            context.DrawGeometry(null, SpectrumPen, lineGeometry);
        }
    }

    private static void DrawFrequencyLine(
        DrawingContext context,
        double frequency,
        double width,
        double height,
        double top,
        double bottom,
        string label)
    {
        double t = Math.Log(frequency / MinFrequency) / Math.Log(MaxFrequency / MinFrequency);
        double x = t * width;

        context.DrawLine(LabelGridPen, new Point(x, top), new Point(x, bottom));

        var formatted = new FormattedText(
            label,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            8.5,
            StudioPalette.Dim);

        double labelX = Math.Clamp(x - formatted.Width * 0.5, 0, width - formatted.Width);
        context.DrawText(formatted, new Point(labelX, height - formatted.Height - 1));
    }
}
