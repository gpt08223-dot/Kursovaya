using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaApplication1.AudioEngine;
using AvaloniaApplication1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApplication1.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private static readonly Dictionary<Key, int> LowerKeyboard = new()
    {
        [Key.Z] = 0,
        [Key.S] = 1,
        [Key.X] = 2,
        [Key.D] = 3,
        [Key.C] = 4,
        [Key.V] = 5,
        [Key.G] = 6,
        [Key.B] = 7,
        [Key.H] = 8,
        [Key.N] = 9,
        [Key.J] = 10,
        [Key.M] = 11,
        [Key.OemComma] = 12,
        [Key.L] = 13,
        [Key.OemPeriod] = 14,
        [Key.OemSemicolon] = 15,
        [Key.OemQuestion] = 16
    };

    private static readonly Dictionary<Key, int> UpperKeyboard = new()
    {
        [Key.Q] = 12,
        [Key.D2] = 13,
        [Key.W] = 14,
        [Key.D3] = 15,
        [Key.E] = 16,
        [Key.R] = 17,
        [Key.D5] = 18,
        [Key.T] = 19,
        [Key.D6] = 20,
        [Key.Y] = 21,
        [Key.D7] = 22,
        [Key.U] = 23,
        [Key.I] = 24,
        [Key.D9] = 25,
        [Key.O] = 26,
        [Key.D0] = 27,
        [Key.P] = 28
    };

    private readonly SynthEngine _engine = new();
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _stepTimer;
    private readonly DispatcherTimer _gateTimer;
    private readonly Dictionary<Key, int> _heldComputerKeys = new();
    private readonly List<SynthPreset> _presets;
    private readonly List<SequencerPattern> _patterns;
    private readonly JsonSerializerOptions _jsonOptions;

    private DateTime _recordStarted;
    private int _stepIndex = -1;
    private int _sequencerNote;
    private bool _suppressAudioChange;
    private bool _suppressSequencerToggle;
    private bool _disposed;

    public MainWindowViewModel()
    {
        _presets = new List<SynthPreset>(PresetFactory.CreateFactoryPresets());
        _patterns = new List<SequencerPattern>(PatternFactory.CreatePatterns());

        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());

        Presets = _presets;
        Patterns = _patterns;

        Steps = new ObservableCollection<SequencerStepViewModel>(
            Enumerable.Range(0, 16).Select(index => new SequencerStepViewModel(index)));

        _uiTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, OnUiTick);

        _stepTimer = new DispatcherTimer(DispatcherPriority.Background);
        _stepTimer.Tick += OnStepTick;

        _gateTimer = new DispatcherTimer(DispatcherPriority.Background);
        _gateTimer.Tick += OnGateTick;

        SelectedPattern = _patterns[0];
        SelectedPreset = _presets[0];
        UpdateStepInterval();

        _uiTimer.Start();

        try
        {
            _engine.Start();
            _suppressAudioChange = true;
            IsAudioRunning = true;
            _suppressAudioChange = false;
            StatusText = "Audio engine online";
        }
        catch
        {
            StatusText = "Audio device unavailable";
        }
    }

    public SynthEngine Engine => _engine;

    public IReadOnlyList<SynthPreset> Presets { get; }

    public IReadOnlyList<SequencerPattern> Patterns { get; }

    public ObservableCollection<SequencerStepViewModel> Steps { get; }

    [ObservableProperty]
    public partial SynthPreset? SelectedPreset { get; set; }

    [ObservableProperty]
    public partial WaveType SelectedWaveType { get; set; } = WaveType.Triangle;

    [ObservableProperty]
    public partial double Unison { get; set; } = 3.0;

    [ObservableProperty]
    public partial double Detune { get; set; } = 9.0;

    [ObservableProperty]
    public partial double Volume { get; set; } = 0.75;

    [ObservableProperty]
    public partial double AttackMs { get; set; } = 120.0;

    [ObservableProperty]
    public partial double DecayMs { get; set; } = 900.0;

    [ObservableProperty]
    public partial double Sustain { get; set; } = 0.75;

    [ObservableProperty]
    public partial double ReleaseMs { get; set; } = 1400.0;

    [ObservableProperty]
    public partial FilterType SelectedFilterType { get; set; } = FilterType.LowPass;

    [ObservableProperty]
    public partial double CutoffPosition { get; set; } = FrequencyToPosition(5200.0);

    [ObservableProperty]
    public partial double Resonance { get; set; } = 0.9;

    [ObservableProperty]
    public partial EffectTab SelectedEffectTab { get; set; } = EffectTab.Saturation;

    [ObservableProperty]
    public partial double DistortionDrive { get; set; }

    [ObservableProperty]
    public partial double DistortionMix { get; set; }

    [ObservableProperty]
    public partial double DelayTimeMs { get; set; } = 380.0;

    [ObservableProperty]
    public partial double DelayFeedback { get; set; } = 0.25;

    [ObservableProperty]
    public partial double DelayMix { get; set; } = 0.10;

    [ObservableProperty]
    public partial double ReverbRoom { get; set; } = 0.55;

    [ObservableProperty]
    public partial double ReverbMix { get; set; } = 0.28;

    [ObservableProperty]
    public partial double Bpm { get; set; } = 120.0;

    [ObservableProperty]
    public partial SequencerPattern? SelectedPattern { get; set; }

    [ObservableProperty]
    public partial bool IsSequencerPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsAudioRunning { get; set; }

    [ObservableProperty]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial bool IsKeyboardZoomed { get; set; }

    [ObservableProperty]
    public partial int ActiveVoiceCount { get; set; }

    [ObservableProperty]
    public partial double OutputLevel { get; set; }

    [ObservableProperty]
    public partial int PcOctave { get; set; } = 3;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    public double CutoffHz => MinCutoff * Math.Pow(MaxCutoff / MinCutoff, Math.Clamp(CutoffPosition, 0.0, 1.0));

    private const double MinCutoff = 20.0;
    private const double MaxCutoff = 20000.0;

    public string VolumeText => $"{Volume * 100:0} %";

    public string UnisonText => $"{Math.Round(Unison):0}×";

    public string DetuneText => $"{Detune:0.0} ct";

    public string AttackText => $"{AttackMs:0} ms";

    public string DecayText => $"{DecayMs:0} ms";

    public string SustainText => $"{Sustain * 100:0} %";

    public string ReleaseText => $"{ReleaseMs:0} ms";

    public string CutoffText => CutoffHz >= 1000.0
        ? $"{CutoffHz / 1000.0:0.00} kHz"
        : $"{CutoffHz:0} Hz";

    public string ResonanceText => $"Q {Resonance:0.0}";

    public string DistortionDriveText => $"{DistortionDrive * 100:0} %";

    public string DistortionMixText => $"{DistortionMix * 100:0} %";

    public string DelayTimeText => $"{DelayTimeMs:0} ms";

    public string DelayFeedbackText => $"{DelayFeedback * 100:0} %";

    public string DelayMixText => $"{DelayMix * 100:0} %";

    public string ReverbRoomText => $"{ReverbRoom * 100:0} %";

    public string ReverbMixText => $"{ReverbMix * 100:0} %";

    public string BpmText => $"{Bpm:0} BPM";

    public string VoicesText => $"{ActiveVoiceCount:00} / {SynthEngine.MaxVoices}";

    public string PlayGlyph => IsSequencerPlaying ? "■" : "▶";

    public string RecordButtonText => IsRecording ? $"■ {RecordingTimeText}" : "● REC";

    public string RecordingTimeText => IsRecording
        ? (DateTime.UtcNow - _recordStarted).ToString(@"mm\:ss")
        : "00:00";

    public string OctaveText => $"C{PcOctave}";

    public double PianoWhiteKeyWidth => IsKeyboardZoomed ? 42.0 : 0.0;

    public ScrollBarVisibility PianoScrollVisibility => IsKeyboardZoomed
        ? ScrollBarVisibility.Visible
        : ScrollBarVisibility.Disabled;

    partial void OnSelectedPresetChanged(SynthPreset? value)
    {
        if (value != null)
        {
            ApplyPreset(value);
        }
    }

    partial void OnSelectedWaveTypeChanged(WaveType value)
    {
        _engine.Parameters.Waveform = (int)value;
    }

    partial void OnUnisonChanged(double value)
    {
        _engine.Parameters.Unison = Math.Clamp((int)Math.Round(value), 1, SynthVoice.MaxUnison);
        OnPropertyChanged(nameof(UnisonText));
    }

    partial void OnDetuneChanged(double value)
    {
        _engine.Parameters.DetuneCents = (float)value;
        OnPropertyChanged(nameof(DetuneText));
    }

    partial void OnVolumeChanged(double value)
    {
        _engine.Parameters.MasterVolume = (float)Math.Clamp(value, 0.0, 1.0);
        OnPropertyChanged(nameof(VolumeText));
    }

    partial void OnAttackMsChanged(double value)
    {
        _engine.Parameters.AttackMs = (float)Math.Max(1.0, value);
        OnPropertyChanged(nameof(AttackText));
    }

    partial void OnDecayMsChanged(double value)
    {
        _engine.Parameters.DecayMs = (float)Math.Max(1.0, value);
        OnPropertyChanged(nameof(DecayText));
    }

    partial void OnSustainChanged(double value)
    {
        _engine.Parameters.Sustain = (float)Math.Clamp(value, 0.0, 1.0);
        OnPropertyChanged(nameof(SustainText));
    }

    partial void OnReleaseMsChanged(double value)
    {
        _engine.Parameters.ReleaseMs = (float)Math.Max(1.0, value);
        OnPropertyChanged(nameof(ReleaseText));
    }

    partial void OnSelectedFilterTypeChanged(FilterType value)
    {
        _engine.Parameters.FilterMode = (int)value;
    }

    partial void OnCutoffPositionChanged(double value)
    {
        _engine.Parameters.CutoffHz = (float)CutoffHz;
        OnPropertyChanged(nameof(CutoffHz));
        OnPropertyChanged(nameof(CutoffText));
    }

    partial void OnResonanceChanged(double value)
    {
        _engine.Parameters.Resonance = (float)value;
        OnPropertyChanged(nameof(ResonanceText));
    }

    partial void OnDistortionDriveChanged(double value)
    {
        _engine.Parameters.DistortionDrive = (float)Math.Clamp(value, 0.0, 1.0);
        OnPropertyChanged(nameof(DistortionDriveText));
    }

    partial void OnDistortionMixChanged(double value)
    {
        _engine.Parameters.DistortionMix = (float)Math.Clamp(value, 0.0, 1.0);
        OnPropertyChanged(nameof(DistortionMixText));
    }

    partial void OnDelayTimeMsChanged(double value)
    {
        _engine.Parameters.DelayTimeMs = (float)value;
        OnPropertyChanged(nameof(DelayTimeText));
    }

    partial void OnDelayFeedbackChanged(double value)
    {
        _engine.Parameters.DelayFeedback = (float)Math.Clamp(value, 0.0, 0.85);
        OnPropertyChanged(nameof(DelayFeedbackText));
    }

    partial void OnDelayMixChanged(double value)
    {
        _engine.Parameters.DelayMix = (float)Math.Clamp(value, 0.0, 1.0);
        OnPropertyChanged(nameof(DelayMixText));
    }

    partial void OnReverbRoomChanged(double value)
    {
        _engine.Parameters.ReverbRoom = (float)Math.Clamp(value, 0.0, 1.0);
        OnPropertyChanged(nameof(ReverbRoomText));
    }

    partial void OnReverbMixChanged(double value)
    {
        _engine.Parameters.ReverbMix = (float)Math.Clamp(value, 0.0, 1.0);
        OnPropertyChanged(nameof(ReverbMixText));
    }

    partial void OnBpmChanged(double value)
    {
        OnPropertyChanged(nameof(BpmText));

        if (IsSequencerPlaying)
        {
            UpdateStepInterval();
        }
    }

    partial void OnSelectedPatternChanged(SequencerPattern? value)
    {
        if (value != null)
        {
            ApplyPattern(value);
            StatusText = $"Pattern · {value.Name}";
        }
    }

    partial void OnIsSequencerPlayingChanged(bool value)
    {
        if (_suppressSequencerToggle)
        {
            return;
        }

        if (value)
        {
            StartSequencer();
        }
        else
        {
            StopSequencer();
        }

        OnPropertyChanged(nameof(PlayGlyph));
    }

    partial void OnIsAudioRunningChanged(bool value)
    {
        if (_suppressAudioChange)
        {
            return;
        }

        if (value)
        {
            try
            {
                _engine.Start();
                StatusText = "Audio engine online";
            }
            catch
            {
                _suppressAudioChange = true;
                IsAudioRunning = false;
                _suppressAudioChange = false;
                StatusText = "Audio device unavailable";
            }
        }
        else
        {
            if (IsSequencerPlaying)
            {
                _suppressSequencerToggle = true;
                IsSequencerPlaying = false;
                _suppressSequencerToggle = false;
                StopSequencer();
                OnPropertyChanged(nameof(PlayGlyph));
            }

            _engine.Stop();
            StatusText = "Audio engine offline";
        }
    }

    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(RecordButtonText));
    }

    partial void OnIsKeyboardZoomedChanged(bool value)
    {
        OnPropertyChanged(nameof(PianoWhiteKeyWidth));
        OnPropertyChanged(nameof(PianoScrollVisibility));
    }

    partial void OnActiveVoiceCountChanged(int value)
    {
        OnPropertyChanged(nameof(VoicesText));
    }

    partial void OnPcOctaveChanged(int value)
    {
        OnPropertyChanged(nameof(OctaveText));
    }

    [RelayCommand]
    private void OctaveDown()
    {
        ChangeOctave(-1);
    }

    [RelayCommand]
    private void OctaveUp()
    {
        ChangeOctave(1);
    }

    public bool HandleKeyDown(Key key)
    {
        if (key is Key.OemMinus or Key.PageDown)
        {
            ChangeOctave(-1);
            return true;
        }

        if (key is Key.OemPlus or Key.PageUp)
        {
            ChangeOctave(1);
            return true;
        }

        if (_heldComputerKeys.ContainsKey(key))
        {
            return true;
        }

        int offset;

        if (!LowerKeyboard.TryGetValue(key, out offset) && !UpperKeyboard.TryGetValue(key, out offset))
        {
            return false;
        }

        int midiNote = (PcOctave + 1) * 12 + offset;
        if (midiNote < MusicTheory.LowestNote || midiNote > MusicTheory.HighestNote)
        {
            return true;
        }

        _heldComputerKeys[key] = midiNote;
        _engine.NoteOn(midiNote);
        return true;
    }

    public bool HandleKeyUp(Key key)
    {
        if (!_heldComputerKeys.Remove(key, out int midiNote))
        {
            return false;
        }

        _engine.NoteOff(midiNote);
        return true;
    }

    public void ReleaseComputerKeys()
    {
        foreach (int midiNote in _heldComputerKeys.Values)
        {
            _engine.NoteOff(midiNote);
        }

        _heldComputerKeys.Clear();
    }

    public string ExportPresetJson()
    {
        SynthPreset preset = BuildCurrentPreset(SelectedPreset?.Name ?? "User Preset");
        return JsonSerializer.Serialize(preset, _jsonOptions);
    }

    public void ImportPresetJson(string json)
    {
        try
        {
            SynthPreset? preset = JsonSerializer.Deserialize<SynthPreset>(json, _jsonOptions);

            if (preset == null)
            {
                StatusText = "Preset file is empty";
                return;
            }

            SelectedPreset = null;
            ApplyPreset(preset);
            StatusText = $"Preset loaded · {preset.Name}";
        }
        catch
        {
            StatusText = "Preset file is invalid";
        }
    }

    public void StartRecording()
    {
        if (!IsAudioRunning)
        {
            IsAudioRunning = true;

            if (!IsAudioRunning)
            {
                return;
            }
        }

        _recordStarted = DateTime.UtcNow;
        _engine.StartRecording();
        IsRecording = true;
        OnPropertyChanged(nameof(RecordingTimeText));
        StatusText = "Recording take";
    }

    public float[] StopRecording()
    {
        float[] samples = _engine.StopRecording();
        IsRecording = false;
        OnPropertyChanged(nameof(RecordingTimeText));
        StatusText = samples.Length > 0 ? "Take captured" : "Nothing recorded";
        return samples;
    }

    private void ApplyPreset(SynthPreset preset)
    {
        _engine.Parameters.Waveform = (int)preset.Waveform;
        _engine.Parameters.Unison = Math.Clamp(preset.Unison, 1, SynthVoice.MaxUnison);
        _engine.Parameters.DetuneCents = (float)preset.DetuneCents;
        _engine.Parameters.AttackMs = (float)preset.AttackMs;
        _engine.Parameters.DecayMs = (float)preset.DecayMs;
        _engine.Parameters.Sustain = (float)preset.Sustain;
        _engine.Parameters.ReleaseMs = (float)preset.ReleaseMs;
        _engine.Parameters.FilterMode = (int)preset.Filter;
        _engine.Parameters.CutoffHz = (float)preset.CutoffHz;
        _engine.Parameters.Resonance = (float)preset.Resonance;
        _engine.Parameters.DistortionDrive = (float)preset.DistortionDrive;
        _engine.Parameters.DistortionMix = (float)preset.DistortionMix;
        _engine.Parameters.DelayTimeMs = (float)preset.DelayTimeMs;
        _engine.Parameters.DelayFeedback = (float)preset.DelayFeedback;
        _engine.Parameters.DelayMix = (float)preset.DelayMix;
        _engine.Parameters.ReverbRoom = (float)preset.ReverbRoom;
        _engine.Parameters.ReverbMix = (float)preset.ReverbMix;
        _engine.Parameters.MasterVolume = (float)preset.MasterVolume;

        SelectedWaveType = preset.Waveform;
        Unison = preset.Unison;
        Detune = preset.DetuneCents;
        AttackMs = preset.AttackMs;
        DecayMs = preset.DecayMs;
        Sustain = preset.Sustain;
        ReleaseMs = preset.ReleaseMs;
        SelectedFilterType = preset.Filter;
        CutoffPosition = FrequencyToPosition(preset.CutoffHz);
        Resonance = preset.Resonance;
        DistortionDrive = preset.DistortionDrive;
        DistortionMix = preset.DistortionMix;
        DelayTimeMs = preset.DelayTimeMs;
        DelayFeedback = preset.DelayFeedback;
        DelayMix = preset.DelayMix;
        ReverbRoom = preset.ReverbRoom;
        ReverbMix = preset.ReverbMix;
        Volume = preset.MasterVolume;

        StatusText = $"Preset · {preset.Name}";
    }

    private SynthPreset BuildCurrentPreset(string name)
    {
        return new SynthPreset
        {
            Name = name,
            Waveform = SelectedWaveType,
            Unison = (int)Math.Round(Unison),
            DetuneCents = Detune,
            AttackMs = AttackMs,
            DecayMs = DecayMs,
            Sustain = Sustain,
            ReleaseMs = ReleaseMs,
            Filter = SelectedFilterType,
            CutoffHz = CutoffHz,
            Resonance = Resonance,
            DistortionDrive = DistortionDrive,
            DistortionMix = DistortionMix,
            DelayTimeMs = DelayTimeMs,
            DelayFeedback = DelayFeedback,
            DelayMix = DelayMix,
            ReverbRoom = ReverbRoom,
            ReverbMix = ReverbMix,
            MasterVolume = Volume
        };
    }

    private void ApplyPattern(SequencerPattern pattern)
    {
        for (int i = 0; i < Steps.Count; i++)
        {
            int note = i < pattern.Notes.Length ? pattern.Notes[i] : 0;
            Steps[i].Note = note;
            Steps[i].IsEnabled = note > 0;
        }
    }

    private void StartSequencer()
    {
        if (!IsAudioRunning)
        {
            IsAudioRunning = true;

            if (!IsAudioRunning)
            {
                _suppressSequencerToggle = true;
                IsSequencerPlaying = false;
                _suppressSequencerToggle = false;
                OnPropertyChanged(nameof(PlayGlyph));
                return;
            }
        }

        _stepIndex = -1;
        UpdateStepInterval();
        _stepTimer.Start();
        AdvanceStep();
        StatusText = "Sequencer running";
    }

    private void StopSequencer()
    {
        _stepTimer.Stop();
        _gateTimer.Stop();
        ReleaseSequencerNote();

        foreach (SequencerStepViewModel step in Steps)
        {
            step.IsCurrent = false;
        }

        _stepIndex = -1;
        StatusText = "Sequencer stopped";
    }

    private void AdvanceStep()
    {
        _gateTimer.Stop();
        ReleaseSequencerNote();

        if (_stepIndex >= 0 && _stepIndex < Steps.Count)
        {
            Steps[_stepIndex].IsCurrent = false;
        }

        _stepIndex = (_stepIndex + 1) % Steps.Count;
        SequencerStepViewModel step = Steps[_stepIndex];
        step.IsCurrent = true;

        if (!step.IsEnabled || step.Note <= 0)
        {
            return;
        }

        _sequencerNote = step.Note;
        _engine.NoteOn(step.Note);
        _gateTimer.Interval = TimeSpan.FromMilliseconds(StepDurationMs() * 0.65);
        _gateTimer.Start();
    }

    private void ReleaseSequencerNote()
    {
        if (_sequencerNote <= 0)
        {
            return;
        }

        _engine.NoteOff(_sequencerNote);
        _sequencerNote = 0;
    }

    private void UpdateStepInterval()
    {
        _stepTimer.Interval = TimeSpan.FromMilliseconds(StepDurationMs());
    }

    private double StepDurationMs()
    {
        return 60000.0 / Math.Clamp(Bpm, 60.0, 200.0) / 4.0;
    }

    private void OnStepTick(object? sender, EventArgs e)
    {
        AdvanceStep();
    }

    private void OnGateTick(object? sender, EventArgs e)
    {
        _gateTimer.Stop();
        ReleaseSequencerNote();
    }

    private void OnUiTick(object? sender, EventArgs e)
    {
        ActiveVoiceCount = _engine.ActiveVoiceCount;
        OutputLevel = Math.Clamp(_engine.OutputPeak / 0.78, 0.0, 1.0);

        if (IsRecording)
        {
            if (!_engine.IsRecording)
            {
                IsRecording = false;
            }
            else
            {
                OnPropertyChanged(nameof(RecordingTimeText));
                OnPropertyChanged(nameof(RecordButtonText));
            }
        }
    }

    private void ChangeOctave(int delta)
    {
        int next = Math.Clamp(PcOctave + delta, 1, 7);

        if (next == PcOctave)
        {
            return;
        }

        ReleaseComputerKeys();
        PcOctave = next;
        StatusText = $"Computer keyboard octave · C{next}";
    }

    private static double FrequencyToPosition(double hz)
    {
        double clamped = Math.Clamp(hz, MinCutoff, MaxCutoff);
        return Math.Log(clamped / MinCutoff) / Math.Log(MaxCutoff / MinCutoff);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _uiTimer.Stop();
        _stepTimer.Stop();
        _gateTimer.Stop();
        ReleaseComputerKeys();
        _engine.Dispose();
    }
}
