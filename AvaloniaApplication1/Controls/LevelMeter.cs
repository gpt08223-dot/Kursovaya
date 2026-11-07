using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaApplication1.Controls;

public sealed class LevelMeter : Control
{
    public static readonly StyledProperty<double> LevelProperty =
        AvaloniaProperty.Register<LevelMeter, double>(nameof(Level), 0.0);

    private const int SegmentCount = 22;

    static LevelMeter()
    {
        AffectsRender<LevelMeter>(LevelProperty);
    }

    public double Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        double gap = 1.5;
        double segmentWidth = Math.Max(1.0, (width - gap * (SegmentCount - 1)) / SegmentCount);
        double y = Math.Max(0, (height - 4) * 0.5);
        double normalized = Math.Clamp(Level, 0.0, 1.0);

        for (int i = 0; i < SegmentCount; i++)
        {
            double threshold = (i + 1.0) / SegmentCount;
            bool lit = normalized >= threshold;
            IBrush brush;

            if (lit)
            {
                brush = threshold > 0.86 ? StudioPalette.Amber : StudioPalette.Accent;
            }
            else
            {
                brush = StudioPalette.Track;
            }

            var rect = new Rect(i * (segmentWidth + gap), y, segmentWidth, 4);
            context.DrawRectangle(brush, null, rect, 2);
        }
    }
}
