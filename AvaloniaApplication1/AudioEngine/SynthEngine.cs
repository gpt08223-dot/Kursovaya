using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace AvaloniaApplication1.AudioEngine;

public class SynthEngine : ISampleProvider
{
    private readonly WaveOutEvent _waveOut = new() { DesiredLatency = 100 };
    private readonly List<SynthVoice> _voices = new();
    private readonly object _sync = new();
    private float[] _mixBuffer = new float[4096];
    private WaveType _waveType = WaveType.Sine;

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SynthVoice.SampleRate, 1);

    public float MasterVolume { get; set; } = 0.25f;

    public bool IsRunning => _waveOut.PlaybackState == PlaybackState.Playing;

    public WaveType WaveType
    {
        get => _waveType;
        set
        {
            _waveType = value;

            lock (_sync)
            {
                foreach (SynthVoice voice in _voices)
                {
                    voice.WaveType = value;
                }
            }
        }
    }

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _waveOut.Init(this);
        _waveOut.Play();
    }

    public void Stop()
    {
        _waveOut.Stop();
        AllNotesOff();
    }

    public SynthVoice NoteOn(double frequency)
    {
        SynthVoice voice = new SynthVoice
        {
            Frequency = frequency,
            WaveType = _waveType,
            Gain = 1f
        };

        lock (_sync)
        {
            _voices.Add(voice);
        }

        return voice;
    }

    public void NoteOff(SynthVoice voice)
    {
        lock (_sync)
        {
            _voices.Remove(voice);
        }
    }

    public void AllNotesOff()
    {
        lock (_sync)
        {
            _voices.Clear();
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);

        lock (_sync)
        {
            if (_mixBuffer.Length < count)
            {
                _mixBuffer = new float[count];
            }

            foreach (SynthVoice voice in _voices)
            {
                voice.Read(_mixBuffer, 0, count);

                for (int i = 0; i < count; i++)
                {
                    buffer[offset + i] += _mixBuffer[i];
                }
            }
        }

        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] = Math.Clamp(buffer[offset + i] * MasterVolume, -1f, 1f);
        }

        return count;
    }
}
