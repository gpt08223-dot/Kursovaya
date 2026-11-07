using System;

namespace AvaloniaApplication1.AudioEngine;

public sealed class AdsrEnvelope
{
    private enum Stage
    {
        Idle,
        Attack,
        Decay,
        Sustain,
        Release
    }

    private const int SampleRate = 44100;
    private const float Silence = 0.00005f;

    private Stage _stage = Stage.Idle;
    private float _level;
    private float _attackDelta;
    private float _releaseDelta;
    private float _decayDelta;
    private float _lastDecayMs = -1f;
    private float _lastSustain = -1f;

    public bool IsActive => _stage != Stage.Idle;

    public bool IsReleasing => _stage == Stage.Release;

    public float Level => _level;

    public void Reset()
    {
        _stage = Stage.Idle;
        _level = 0f;
        _decayDelta = 0f;
        _releaseDelta = 0f;
        _lastDecayMs = -1f;
        _lastSustain = -1f;
    }

    public void NoteOn(float attackMs)
    {
        float attackSamples = MathF.Max(1f, attackMs * 0.001f * SampleRate);
        _attackDelta = (1f - _level) / attackSamples;
        _stage = Stage.Attack;
    }

    public void NoteOff(float releaseMs)
    {
        if (_stage == Stage.Idle)
        {
            return;
        }

        float releaseSamples = MathF.Max(1f, releaseMs * 0.001f * SampleRate);
        _releaseDelta = MathF.Max(_level, Silence) / releaseSamples;
        _stage = Stage.Release;
    }

    public float Next(float decayMs, float sustain)
    {
        switch (_stage)
        {
            case Stage.Attack:
                _level += _attackDelta;
                if (_level >= 1f)
                {
                    _level = 1f;
                    _stage = Stage.Decay;
                    _lastDecayMs = -1f;
                }

                break;

            case Stage.Decay:
                if (MathF.Abs(_lastDecayMs - decayMs) > 0.001f || MathF.Abs(_lastSustain - sustain) > 0.0001f)
                {
                    _lastDecayMs = decayMs;
                    _lastSustain = sustain;
                    float decaySamples = MathF.Max(1f, decayMs * 0.001f * SampleRate);
                    _decayDelta = (1f - sustain) / decaySamples;
                }

                _level -= _decayDelta;
                if (_level <= sustain || _decayDelta <= 0f)
                {
                    _level = sustain;
                    _stage = Stage.Sustain;
                }

                break;

            case Stage.Sustain:
                _level += (sustain - _level) * 0.0008f;
                break;

            case Stage.Release:
                _level -= _releaseDelta;
                if (_level <= Silence)
                {
                    _level = 0f;
                    _stage = Stage.Idle;
                }

                break;
        }

        return _level;
    }
}
