namespace AvaloniaApplication1.AudioEngine;

public sealed class SynthParameters
{
    public volatile int Waveform = (int)WaveType.Triangle;
    public volatile int Unison = 3;
    public volatile float DetuneCents = 9f;

    public volatile float AttackMs = 120f;
    public volatile float DecayMs = 900f;
    public volatile float Sustain = 0.75f;
    public volatile float ReleaseMs = 1400f;

    public volatile int FilterMode = (int)FilterType.LowPass;
    public volatile float CutoffHz = 5200f;
    public volatile float Resonance = 0.9f;

    public volatile float DistortionDrive = 0f;
    public volatile float DistortionMix = 0f;

    public volatile float DelayTimeMs = 380f;
    public volatile float DelayFeedback = 0.25f;
    public volatile float DelayMix = 0.1f;

    public volatile float ReverbRoom = 0.55f;
    public volatile float ReverbMix = 0.28f;

    public volatile float MasterVolume = 0.75f;
}
