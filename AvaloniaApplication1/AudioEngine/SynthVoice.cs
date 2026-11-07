using System;

namespace AvaloniaApplication1.AudioEngine;

public sealed class SynthVoice
{
    public const int SampleRate = 44100;
    public const int MaxUnison = 4;

    private const float VoiceGain = 0.16f;

    private readonly double[] _phases = new double[MaxUnison];
    private readonly float[] _increments = new float[MaxUnison];
    private readonly BiquadFilter _filter = new();
    private readonly AdsrEnvelope _envelope = new();

    private uint _noiseState;
    private long _order;
    private float _frequency;
    private bool _active;

    public int MidiNote { get; private set; }

    public long Order => _order;

    public bool IsActive => _active;

    public bool IsReleasing => _envelope.IsReleasing;

    public void Start(int midiNote, SynthParameters parameters, long order)
    {
        MidiNote = midiNote;
        _order = order;
        _frequency = (float)MusicTheory.MidiToFrequency(midiNote);
        _noiseState = (uint)(midiNote * 2654435761u + order * 97 + 1);

        if (_noiseState == 0)
        {
            _noiseState = 1;
        }

        for (int i = 0; i < MaxUnison; i++)
        {
            _phases[i] = NextRandom();
        }

        _filter.Reset();
        _envelope.Reset();
        _envelope.NoteOn(parameters.AttackMs);
        _active = true;
    }

    public void Release(float releaseMs)
    {
        _envelope.NoteOff(releaseMs);
    }

    public void Silence()
    {
        _active = false;
        _envelope.Reset();
        _filter.Reset();
    }

    public void Process(float[] mix, int count, SynthParameters parameters)
    {
        if (!_active)
        {
            return;
        }

        int sampleRate = SampleRate;
        WaveType waveform = (WaveType)parameters.Waveform;
        int unison = Math.Clamp(parameters.Unison, 1, MaxUnison);
        float detune = Math.Clamp(parameters.DetuneCents, 0f, 100f);
        float decayMs = Math.Max(1f, parameters.DecayMs);
        float sustain = Math.Clamp(parameters.Sustain, 0f, 1f);
        float filterCutoff = Math.Clamp(parameters.CutoffHz, 20f, 20000f);
        float resonance = Math.Clamp(parameters.Resonance, 0.5f, 12f);
        FilterType filterType = (FilterType)parameters.FilterMode;

        _filter.Update(filterCutoff, resonance, filterType, sampleRate, count);

        for (int i = 0; i < unison; i++)
        {
            float offset = unison == 1
                ? 0f
                : (i - (unison - 1) * 0.5f) / Math.Max(1f, (unison - 1) * 0.5f);
            float cents = detune * offset;
            double frequency = _frequency * Math.Pow(2.0, cents / 1200.0);
            _increments[i] = (float)(frequency / sampleRate);
        }

        float unisonGain = 1f / MathF.Sqrt(unison);

        for (int sample = 0; sample < count; sample++)
        {
            float oscillator = 0f;

            if (waveform == WaveType.WhiteNoise)
            {
                oscillator = NextNoise();
            }
            else
            {
                for (int i = 0; i < unison; i++)
                {
                    float phase = (float)_phases[i];
                    float increment = _increments[i];

                    oscillator += waveform switch
                    {
                        WaveType.Sine => MathF.Sin(phase * MathF.PI * 2f),
                        WaveType.Sawtooth => Sawtooth(phase, increment),
                        WaveType.Square => Square(phase, increment),
                        _ => Triangle(phase)
                    };

                    _phases[i] += increment;
                    if (_phases[i] >= 1.0)
                    {
                        _phases[i] -= 1.0;
                    }
                }

                oscillator *= unisonGain;
            }

            oscillator = _filter.Process(oscillator);
            float envelope = _envelope.Next(decayMs, sustain);
            mix[sample] += oscillator * envelope * VoiceGain;

            if (!_envelope.IsActive)
            {
                _active = false;
                break;
            }
        }
    }

    private static float Sawtooth(float phase, float increment)
    {
        return 2f * phase - 1f - PolyBlep(phase, increment);
    }

    private static float Square(float phase, float increment)
    {
        float value = phase < 0.5f ? 1f : -1f;
        value += PolyBlep(phase, increment);

        float shifted = phase + 0.5f;
        if (shifted >= 1f)
        {
            shifted -= 1f;
        }

        value -= PolyBlep(shifted, increment);
        return value;
    }

    private static float Triangle(float phase)
    {
        return 4f * MathF.Abs(phase - 0.5f) - 1f;
    }

    private static float PolyBlep(float phase, float increment)
    {
        if (increment <= 0f)
        {
            return 0f;
        }

        if (phase < increment)
        {
            float t = phase / increment;
            return t + t - t * t - 1f;
        }

        if (phase > 1f - increment)
        {
            float t = (phase - 1f) / increment;
            return t * t + t + t + 1f;
        }

        return 0f;
    }

    private float NextNoise()
    {
        uint state = _noiseState;
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        _noiseState = state;
        return (state & 0xFFFFFF) / 8388607.5f - 1f;
    }

    private float NextRandom()
    {
        uint state = _noiseState;
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        _noiseState = state;
        return (state & 0xFFFFFF) / 16777215f;
    }
}
