using AvaloniaApplication1.AudioEngine;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApplication1.ViewModels;

public partial class SequencerStepViewModel : ObservableObject
{
    public int Index { get; }

    public string IndexText { get; }

    [ObservableProperty]
    public partial int Note { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    public string NoteName => Note <= 0 ? "—" : MusicTheory.GetNoteName(Note);

    public SequencerStepViewModel(int index)
    {
        Index = index;
        IndexText = (index + 1).ToString("00");
    }

    partial void OnNoteChanged(int value)
    {
        OnPropertyChanged(nameof(NoteName));
    }
}
