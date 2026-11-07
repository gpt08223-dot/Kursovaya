using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace AvaloniaApplication1.Controls;

public sealed class ParamSlider : Grid
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ParamSlider, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> ValueTextProperty =
        AvaloniaProperty.Register<ParamSlider, string>(nameof(ValueText), string.Empty);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ParamSlider, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ParamSlider, double>(nameof(Maximum), 1.0);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<ParamSlider, double>(nameof(Step), 0.0);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ParamSlider, double>(
            nameof(Value),
            0.0,
            defaultBindingMode: BindingMode.TwoWay);

    private readonly TextBlock _label;
    private readonly TextBlock _value;
    private readonly SynthSlider _slider;
    private bool _syncing;

    public ParamSlider()
    {
        RowDefinitions = new RowDefinitions("Auto,Auto");
        ColumnDefinitions = new ColumnDefinitions("*,Auto");

        _label = new TextBlock
        {
            FontSize = 10,
            LetterSpacing = 0.8,
            Foreground = StudioPalette.Muted,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        _value = new TextBlock
        {
            FontSize = 10,
            FontFamily = new FontFamily("Cascadia Mono,Consolas"),
            Foreground = StudioPalette.Text,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        _slider = new SynthSlider
        {
            Height = 16,
            MinHeight = 16,
            Margin = new Thickness(0, 3, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        SetRow(_value, 0);
        SetColumn(_value, 1);
        SetRow(_slider, 1);
        SetColumnSpan(_slider, 2);

        Children.Add(_label);
        Children.Add(_value);
        Children.Add(_slider);

        _slider.PropertyChanged += (_, args) =>
        {
            if (args.Property == SynthSlider.ValueProperty && !_syncing)
            {
                _syncing = true;
                SetCurrentValue(ValueProperty, _slider.Value);
                _syncing = false;
            }
        };

        this.PropertyChanged += (_, args) =>
        {
            if (args.Property == ValueProperty && !_syncing)
            {
                _syncing = true;
                _slider.Value = Value;
                _syncing = false;
            }
        };

        ApplyLabel();
        ApplyValueText();
        ApplyMinimum();
        ApplyMaximum();
        ApplyStep();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string ValueText
    {
        get => GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
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

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LabelProperty)
        {
            ApplyLabel();
        }
        else if (change.Property == ValueTextProperty)
        {
            ApplyValueText();
        }
        else if (change.Property == MinimumProperty)
        {
            ApplyMinimum();
        }
        else if (change.Property == MaximumProperty)
        {
            ApplyMaximum();
        }
        else if (change.Property == StepProperty)
        {
            ApplyStep();
        }
    }

    private void ApplyLabel()
    {
        _label.Text = Label;
    }

    private void ApplyValueText()
    {
        _value.Text = ValueText;
    }

    private void ApplyMinimum()
    {
        _slider.Minimum = Minimum;
    }

    private void ApplyMaximum()
    {
        _slider.Maximum = Maximum;
    }

    private void ApplyStep()
    {
        _slider.Step = Step;
    }
}
