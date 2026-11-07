using System;

namespace AvaloniaApplication1.AudioEngine;

public sealed class ReverbEffect
{
    private sealed class CombFilter
    {
        private readonly float[] _buffer;
        private int _index;
        private float _damp;

        public CombFilter(int length)
        {
            _buffer = new float[length];
        }

        public void Reset()
        {
            Array.Clear(_buffer);
            _index = 0;
            _damp = 0f;
        }

        public float Process(float input, float feedback, float damping)
        {
            float output = _buffer[_index];
            _damp = output * (1f - damping) + _damp * damping;
            _buffer[_index] = input + _damp * feedback;

            _index++;
            if (_index >= _buffer.Length)
            {
                _index = 0;
            }

            return output;
        }
    }

    private sealed class AllPassFilter
    {
        private readonly float[] _buffer;
        private int _index;

        public AllPassFilter(int length)
        {
            _buffer = new float[length];
        }

        public void Reset()
        {
            Array.Clear(_buffer);
            _index = 0;
        }

        public float Process(float input)
        {
            const float gain = 0.5f;
            float delayed = _buffer[_index];
            float output = delayed - input * gain;
            _buffer[_index] = input + delayed * gain;

            _index++;
            if (_index >= _buffer.Length)
            {
                _index = 0;
            }

            return output;
        }
    }

    private readonly CombFilter[] _combs =
    {
        new(1116),
        new(1188),
        new(1277),
        new(1356)
    };

    private readonly AllPassFilter[] _allPasses =
    {
        new(556),
        new(441)
    };

    public void Reset()
    {
        foreach (CombFilter comb in _combs)
        {
            comb.Reset();
        }

        foreach (AllPassFilter allPass in _allPasses)
        {
            allPass.Reset();
        }
    }

    public void Process(float[] buffer, int count, SynthParameters parameters)
    {
        float room = Math.Clamp(parameters.ReverbRoom, 0f, 1f);
        float mix = Math.Clamp(parameters.ReverbMix, 0f, 1f);
        float feedback = 0.70f + room * 0.18f;
        float damping = 0.16f + (1f - room) * 0.28f;
        float dryGain = 1f - mix;

        for (int i = 0; i < count; i++)
        {
            float dry = buffer[i];
            float input = dry * 0.22f;
            float wet = 0f;

            foreach (CombFilter comb in _combs)
            {
                wet += comb.Process(input, feedback, damping);
            }

            wet *= 0.25f;

            foreach (AllPassFilter allPass in _allPasses)
            {
                wet = allPass.Process(wet);
            }

            buffer[i] = dry * dryGain + wet * 0.7f * mix;
        }
    }
}
