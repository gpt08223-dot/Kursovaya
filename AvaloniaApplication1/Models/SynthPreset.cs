using AvaloniaApplication1.AudioEngine;

namespace AvaloniaApplication1.Models;

public sealed class SynthPreset
{
    public string Name { get; set; } = "Init";

    public WaveType Waveform { get; set; } = WaveType.Triangle;

    public int Unison { get; set; } = 3;

    public double DetuneCents { get; set; } = 9.0;

    public double AttackMs { get; set; } = 120.0;

    public double DecayMs { get; set; } = 900.0;

    public double Sustain { get; set; } = 0.75;

    public double ReleaseMs { get; set; } = 1400.0;

    public FilterType Filter { get; set; } = FilterType.LowPass;

    public double CutoffHz { get; set; } = 5200.0;

    public double Resonance { get; set; } = 0.9;

    public double DistortionDrive { get; set; }

    public double DistortionMix { get; set; }

    public double DelayTimeMs { get; set; } = 380.0;

    public double DelayFeedback { get; set; } = 0.25;

    public double DelayMix { get; set; } = 0.1;

    public double ReverbRoom { get; set; } = 0.55;

    public double ReverbMix { get; set; } = 0.28;

    public double MasterVolume { get; set; } = 0.75;
}
