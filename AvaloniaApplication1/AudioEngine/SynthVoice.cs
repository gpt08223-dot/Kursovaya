using System;
using NAudio.Wave;

namespace AvaloniaApplication1.AudioEngine;

public class SynthVoice : ISampleProvider
{
    public const int SampleRate = 44100;

    private static readonly WaveFormat Format = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1);
    private readonly Random _random = new();
    private double _phase;

    public WaveType WaveType { get; set; }

    public double Frequency { get; set; }

    public float Gain { get; set; } = 1f;

    public WaveFormat WaveFormat => Format;

    public int Read(float[] buffer, int offset, int count)
    {
        double phaseStep = Frequency / SampleRate;

        for (int i = 0; i < count; i++)
        {
            float sample;

            switch (WaveType)
            {
                case WaveType.Sine:
                    sample = (float)Math.Sin(2 * Math.PI * _phase);
                    break;
                case WaveType.Square:
                    sample = _phase < 0.5 ? 1f : -1f;
                    break;
                case WaveType.Saw:
                    sample = (float)(2 * _phase - 1);
                    break;
                default:
                    sample = (float)(_random.NextDouble() * 2 - 1);
                    break;
            }

            buffer[offset + i] = sample * Gain;

            _phase += phaseStep;
            if (_phase >= 1)
            {
                _phase -= 1;
            }
        }

        return count;
    }
}
