using System;

namespace AvaloniaApplication1.AudioEngine;

public sealed class BiquadFilter
{
    private float _b0 = 1f;
    private float _b1;
    private float _b2;
    private float _a1;
    private float _a2;

    private float _x1;
    private float _x2;
    private float _y1;
    private float _y2;

    private float _smoothedCutoff;
    private bool _initialized;

    public void Reset()
    {
        _x1 = 0f;
        _x2 = 0f;
        _y1 = 0f;
        _y2 = 0f;
        _initialized = false;
    }

    public void Update(float cutoffHz, float q, FilterType type, int sampleRate, int blockSize)
    {
        float target = Math.Clamp(cutoffHz, 20f, 20000f);

        if (!_initialized)
        {
            _smoothedCutoff = target;
            _initialized = true;
        }
        else
        {
            float smoothing = 1f - MathF.Exp(-blockSize / (0.025f * sampleRate));
            _smoothedCutoff += (target - _smoothedCutoff) * smoothing;
        }

        float frequency = Math.Clamp(_smoothedCutoff, 20f, (float)(sampleRate * 0.45));
        double w0 = 2.0 * Math.PI * frequency / sampleRate;
        double cos = Math.Cos(w0);
        double sin = Math.Sin(w0);
        double alpha = sin / (2.0 * Math.Max(0.1f, q));

        double b0;
        double b1;
        double b2;
        double a0;
        double a1;
        double a2;

        if (type == FilterType.LowPass)
        {
            b0 = (1.0 - cos) * 0.5;
            b1 = 1.0 - cos;
            b2 = (1.0 - cos) * 0.5;
        }
        else
        {
            b0 = (1.0 + cos) * 0.5;
            b1 = -(1.0 + cos);
            b2 = (1.0 + cos) * 0.5;
        }

        a0 = 1.0 + alpha;
        a1 = -2.0 * cos;
        a2 = 1.0 - alpha;

        _b0 = (float)(b0 / a0);
        _b1 = (float)(b1 / a0);
        _b2 = (float)(b2 / a0);
        _a1 = (float)(a1 / a0);
        _a2 = (float)(a2 / a0);
    }

    public float Process(float input)
    {
        float output = _b0 * input + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;

        _x2 = _x1;
        _x1 = input;
        _y2 = _y1;
        _y1 = output;

        return output;
    }
}
