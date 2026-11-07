using Avalonia.Media;

namespace AvaloniaApplication1.Controls;

public static class StudioPalette
{
    public static readonly Color BackgroundColor = Color.Parse("#121316");
    public static readonly Color CardColor = Color.Parse("#1A1C20");
    public static readonly Color BorderColor = Color.Parse("#282B30");

    public static readonly IBrush Text = new SolidColorBrush(Color.Parse("#F0F2F5"));
    public static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#8A9099"));
    public static readonly IBrush Dim = new SolidColorBrush(Color.Parse("#5D636D"));

    public static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#4F8EF7"));
    public static readonly IBrush AccentSoft = new SolidColorBrush(Color.Parse("#3E6FB8"));
    public static readonly IBrush Amber = new SolidColorBrush(Color.Parse("#E58E5A"));

    public static readonly IBrush Track = new SolidColorBrush(Color.Parse("#262A30"));
    public static readonly IBrush TrackFill = new SolidColorBrush(Color.Parse("#3A3F47"));
    public static readonly IBrush GridLine = new SolidColorBrush(Color.Parse("#1E2126"));
    public static readonly IBrush GridLineStrong = new SolidColorBrush(Color.Parse("#24282E"));

    public static readonly IBrush WhiteKey = new SolidColorBrush(Color.Parse("#E8EAED"));
    public static readonly IBrush WhiteKeyHover = new SolidColorBrush(Color.Parse("#F4F6F9"));
    public static readonly IBrush WhiteKeyBorder = new SolidColorBrush(Color.Parse("#C9CDD3"));
    public static readonly IBrush BlackKey = new SolidColorBrush(Color.Parse("#22252A"));
    public static readonly IBrush BlackKeyHover = new SolidColorBrush(Color.Parse("#2C3037"));
    public static readonly IBrush BlackKeyBorder = new SolidColorBrush(Color.Parse("#0E1013"));
    public static readonly IBrush KeyLabel = new SolidColorBrush(Color.Parse("#70757D"));
    public static readonly IBrush KeyLabelPressed = new SolidColorBrush(Color.Parse("#F5F8FF"));
    public static readonly IBrush KeyShadow = new SolidColorBrush(Color.Parse("#55000000"));

    public static readonly IBrush Glow = new SolidColorBrush(Color.Parse("#334F8EF7"));
    public static readonly IBrush SpectrumGlow = new SolidColorBrush(Color.Parse("#22E58E5A"));
}
