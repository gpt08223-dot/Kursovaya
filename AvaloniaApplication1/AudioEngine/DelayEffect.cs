using System;

namespace AvaloniaApplication1.AudioEngine;

public sealed class DelayEffect
{
    private const int MaxDelaySamples = 48000;

    private readonly float[] _buffer = new float[MaxDelaySamples];
    private int _writeIndex;
    private float _delaySamples;
    private bool _initialized;

    public void Reset()
    {
        Array.Clear(_buffer);
        _writeIndex = 0;
        _initialized = false;
    }

    public void Process(float[] buffer, int count, SynthParameters parameters)
    {
        int sampleRate = SynthEngine.SampleRate;
        float targetSamples = Math.Clamp(parameters.DelayTimeMs, 1f, 800f) * sampleRate / 1000f;

        if (!_initialized)
        {
            _delaySamples = targetSamples;
            _initialized = true;
        }
        else
        {
            float smoothing = 1f - MathF.Exp(-count / (0.06f * sampleRate));
            _delaySamples += (targetSamples - _delaySamples) * smoothing;
        }

        float feedback = Math.Clamp(parameters.DelayFeedback, 0f, 0.85f);
        float mix = Math.Clamp(parameters.DelayMix, 0f, 1f);
        float dryGain = 1f - mix;
        float delaySamples = Math.Clamp(_delaySamples, 1f, MaxDelaySamples - 2f);

        for (int i = 0; i < count; i++)
        {
            float readPosition = _writeIndex - delaySamples;
            if (readPosition < 0f)
            {
                readPosition += MaxDelaySamples;
            }

            int indexA = (int)readPosition;
            int indexB = indexA + 1;
            if (indexB >= MaxDelaySamples)
            {
                indexB = 0;
            }

            float fraction = readPosition - indexA;
            float wet = _buffer[indexA] + (_buffer[indexB] - _buffer[indexA]) * fraction;
            float dry = buffer[i];

            _buffer[_writeIndex] = dry + wet * feedback;
            _writeIndex++;
            if (_writeIndex >= MaxDelaySamples)
            {
                _writeIndex = 0;
            }

            buffer[i] = dry * dryGain + wet * mix;
        }
    }
}
