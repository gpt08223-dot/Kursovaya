using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaApplication1.AudioEngine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApplication1.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SynthEngine _engine = new();
    private readonly Dictionary<NoteKeyViewModel, SynthVoice> _voices = new();
    private SynthVoice? _toneVoice;

    public WaveType[] WaveTypes { get; } = Enum.GetValues<WaveType>();

    public List<NoteKeyViewModel> Notes { get; }

    [ObservableProperty]
    public partial WaveType SelectedWaveType { get; set; } = WaveType.Sine;

    [ObservableProperty]
    public partial double Volume { get; set; } = 0.25;

    [ObservableProperty]
    public partial double Frequency { get; set; } = 440;

    [ObservableProperty]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial bool IsToneOn { get; set; }

    public string VolumeText => $"{Volume * 100:0} %";

    public string FrequencyText => $"{Frequency:0} Гц";

    public string ActiveNotesText
    {
        get
        {
            List<string> active = Notes.Where(note => note.IsActive).Select(note => note.Name).ToList();
            return active.Count == 0
                ? "Активные клавиши: нет"
                : "Активные клавиши: " + string.Join(", ", active);
        }
    }

    public MainWindowViewModel()
    {
        Notes = new List<NoteKeyViewModel>
        {
            new NoteKeyViewModel("До", 261.63, OnNoteChanged),
            new NoteKeyViewModel("Ре", 293.66, OnNoteChanged),
            new NoteKeyViewModel("Ми", 329.63, OnNoteChanged),
            new NoteKeyViewModel("Фа", 349.23, OnNoteChanged),
            new NoteKeyViewModel("Соль", 392.00, OnNoteChanged),
            new NoteKeyViewModel("Ля", 440.00, OnNoteChanged),
            new NoteKeyViewModel("Си", 493.88, OnNoteChanged)
        };

        _engine.MasterVolume = (float)Volume;
    }

    partial void OnSelectedWaveTypeChanged(WaveType value)
    {
        _engine.WaveType = value;
    }

    partial void OnVolumeChanged(double value)
    {
        _engine.MasterVolume = (float)value;
        OnPropertyChanged(nameof(VolumeText));
    }

    partial void OnFrequencyChanged(double value)
    {
        if (_toneVoice != null)
        {
            _toneVoice.Frequency = value;
        }

        OnPropertyChanged(nameof(FrequencyText));
    }

    partial void OnIsToneOnChanged(bool value)
    {
        if (value)
        {
            _toneVoice = _engine.NoteOn(Frequency);
        }
        else if (_toneVoice != null)
        {
            _engine.NoteOff(_toneVoice);
            _toneVoice = null;
        }
    }

    [RelayCommand]
    private void StartAudio()
    {
        if (IsRunning)
        {
            return;
        }

        _engine.Start();
        IsRunning = true;
        IsToneOn = true;
    }

    [RelayCommand]
    private void StopAudio()
    {
        if (!IsRunning)
        {
            return;
        }

        IsToneOn = false;
        _engine.Stop();

        foreach (NoteKeyViewModel note in Notes)
        {
            note.IsActive = false;
        }

        _voices.Clear();
        IsRunning = false;
        OnPropertyChanged(nameof(ActiveNotesText));
    }

    private void OnNoteChanged(NoteKeyViewModel note)
    {
        if (note.IsActive)
        {
            _voices[note] = _engine.NoteOn(note.Frequency);
        }
        else if (_voices.Remove(note, out SynthVoice? voice))
        {
            _engine.NoteOff(voice);
        }

        OnPropertyChanged(nameof(ActiveNotesText));
    }
}
