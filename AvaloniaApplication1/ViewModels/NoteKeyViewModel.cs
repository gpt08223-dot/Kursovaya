using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApplication1.ViewModels;

public partial class NoteKeyViewModel : ObservableObject
{
    private readonly Action<NoteKeyViewModel> _changed;

    public string Name { get; }

    public double Frequency { get; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    public NoteKeyViewModel(string name, double frequency, Action<NoteKeyViewModel> changed)
    {
        Name = name;
        Frequency = frequency;
        _changed = changed;
    }

    partial void OnIsActiveChanged(bool value)
    {
        _changed(this);
    }
}
