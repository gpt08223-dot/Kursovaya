using System.Collections.Generic;
using AvaloniaApplication1.AudioEngine;

namespace AvaloniaApplication1.Models;

public static class PresetFactory
{
    public static IReadOnlyList<SynthPreset> CreateFactoryPresets()
    {
        return new List<SynthPreset>
        {
            new()
            {
                Name = "Clean Piano Pad",
                Waveform = WaveType.Triangle,
                Unison = 3,
                DetuneCents = 9.0,
                AttackMs = 120.0,
                DecayMs = 900.0,
                Sustain = 0.75,
                ReleaseMs = 1400.0,
                Filter = FilterType.LowPass,
                CutoffHz = 5200.0,
                Resonance = 0.9,
                DistortionDrive = 0.0,
                DistortionMix = 0.0,
                DelayTimeMs = 380.0,
                DelayFeedback = 0.25,
                DelayMix = 0.10,
                ReverbRoom = 0.55,
                ReverbMix = 0.28,
                MasterVolume = 0.75
            },
            new()
            {
                Name = "Warm Analog Bass",
                Waveform = WaveType.Sawtooth,
                Unison = 2,
                DetuneCents = 7.0,
                AttackMs = 5.0,
                DecayMs = 240.0,
                Sustain = 0.50,
                ReleaseMs = 320.0,
                Filter = FilterType.LowPass,
                CutoffHz = 700.0,
                Resonance = 1.4,
                DistortionDrive = 0.28,
                DistortionMix = 0.35,
                DelayTimeMs = 300.0,
                DelayFeedback = 0.20,
                DelayMix = 0.06,
                ReverbRoom = 0.40,
                ReverbMix = 0.08,
                MasterVolume = 0.75
            },
            new()
            {
                Name = "Soft Lead",
                Waveform = WaveType.Square,
                Unison = 2,
                DetuneCents = 5.0,
                AttackMs = 15.0,
                DecayMs = 320.0,
                Sustain = 0.68,
                ReleaseMs = 450.0,
                Filter = FilterType.LowPass,
                CutoffHz = 3600.0,
                Resonance = 1.2,
                DistortionDrive = 0.12,
                DistortionMix = 0.15,
                DelayTimeMs = 300.0,
                DelayFeedback = 0.33,
                DelayMix = 0.22,
                ReverbRoom = 0.50,
                ReverbMix = 0.12,
                MasterVolume = 0.72
            },
            new()
            {
                Name = "Pluck",
                Waveform = WaveType.Triangle,
                Unison = 2,
                DetuneCents = 4.0,
                AttackMs = 2.0,
                DecayMs = 190.0,
                Sustain = 0.02,
                ReleaseMs = 220.0,
                Filter = FilterType.LowPass,
                CutoffHz = 9000.0,
                Resonance = 2.0,
                DistortionDrive = 0.0,
                DistortionMix = 0.0,
                DelayTimeMs = 260.0,
                DelayFeedback = 0.34,
                DelayMix = 0.28,
                ReverbRoom = 0.45,
                ReverbMix = 0.16,
                MasterVolume = 0.75
            },
            new()
            {
                Name = "Strings",
                Waveform = WaveType.Sawtooth,
                Unison = 4,
                DetuneCents = 12.0,
                AttackMs = 450.0,
                DecayMs = 1200.0,
                Sustain = 0.80,
                ReleaseMs = 1800.0,
                Filter = FilterType.LowPass,
                CutoffHz = 6400.0,
                Resonance = 0.8,
                DistortionDrive = 0.0,
                DistortionMix = 0.0,
                DelayTimeMs = 420.0,
                DelayFeedback = 0.30,
                DelayMix = 0.14,
                ReverbRoom = 0.78,
                ReverbMix = 0.40,
                MasterVolume = 0.70
            }
        };
    }
}
