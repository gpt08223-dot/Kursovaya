using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace AvaloniaApplication1.AudioEngine;

public sealed class SynthEngine : ISampleProvider, IDisposable
{
    public const int SampleRate = 44100;
    public const int MaxVoices = 32;

    private const int ScopeSize = 8192;
    private const int ScopeMask = ScopeSize - 1;
    private const int MaxRecordSamples = SampleRate * 60 * 5;

    private readonly WaveOutEvent _waveOut = new() { DesiredLatency = 60 };
    private readonly SynthVoice[] _voices = new SynthVoice[MaxVoices];
    private readonly Dictionary<int, int> _heldNotes = new();
    private readonly SynthParameters _parameters = new();
    private readonly DelayEffect _delay = new();
    private readonly ReverbEffect _reverb = new();
    private readonly object _voiceLock = new();
    private readonly object _scopeLock = new();
    private readonly object _recordLock = new();
    private readonly float[] _scopeRing = new float[ScopeSize];
    private readonly List<float> _recorded = new();
    private readonly WaveFormat _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1);

    private float[] _mixBuffer = Array.Empty<float>();
    private long _voiceOrder;
    private int _scopeWrite;
    private volatile float _peak;
    private volatile bool _recording;
    private bool _initialized;
    private bool _disposed;

    public SynthEngine()
    {
        for (int i = 0; i < _voices.Length; i++)
        {
            _voices[i] = new SynthVoice();
        }
    }

    public event Action<int>? NotePressed;

    public event Action<int>? NoteReleased;

    public WaveFormat WaveFormat => _waveFormat;

    public SynthParameters Parameters => _parameters;

    public bool IsRunning => _waveOut.PlaybackState == PlaybackState.Playing;

    public bool IsRecording => _recording;

    public float OutputPeak => _peak;

    public int ActiveVoiceCount
    {
        get
        {
            int count = 0;

            foreach (SynthVoice voice in _voices)
            {
                if (voice.IsActive)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public void Start()
    {
        if (_disposed)
        {
            return;
        }

        if (!_initialized)
        {
            _waveOut.Init(this);
            _initialized = true;
        }

        if (_waveOut.PlaybackState != PlaybackState.Playing)
        {
            _waveOut.Play();
        }
    }

    public void Stop()
    {
        try
        {
            if (_initialized && _waveOut.PlaybackState != PlaybackState.Stopped)
            {
                _waveOut.Stop();
            }
        }
        catch
        {
        }

        _recording = false;
        SilenceAll();
        _delay.Reset();
        _reverb.Reset();

        lock (_scopeLock)
        {
            Array.Clear(_scopeRing);
            _scopeWrite = 0;
        }

        _peak = 0f;
    }

    public void NoteOn(int midiNote)
    {
        if (midiNote < MusicTheory.LowestNote || midiNote > MusicTheory.HighestNote)
        {
            return;
        }

        bool started = false;

        lock (_voiceLock)
        {
            _heldNotes.TryGetValue(midiNote, out int held);
            _heldNotes[midiNote] = held + 1;

            if (held > 0)
            {
                return;
            }

            SynthVoice? target = FindVoice();
            if (target == null)
            {
                return;
            }

            target.Start(midiNote, _parameters, ++_voiceOrder);
            started = true;
        }

        if (started)
        {
            NotePressed?.Invoke(midiNote);
        }
    }

    public void NoteOff(int midiNote)
    {
        bool stopped = false;

        lock (_voiceLock)
        {
            if (!_heldNotes.TryGetValue(midiNote, out int held))
            {
                return;
            }

            if (held > 1)
            {
                _heldNotes[midiNote] = held - 1;
                return;
            }

            _heldNotes.Remove(midiNote);

            foreach (SynthVoice voice in _voices)
            {
                if (voice.IsActive && voice.MidiNote == midiNote && !voice.IsReleasing)
                {
                    voice.Release(_parameters.ReleaseMs);
                }
            }

            stopped = true;
        }

        if (stopped)
        {
            NoteReleased?.Invoke(midiNote);
        }
    }

    public void AllNotesOff()
    {
        List<int> released;

        lock (_voiceLock)
        {
            released = new List<int>(_heldNotes.Keys);
            _heldNotes.Clear();

            foreach (SynthVoice voice in _voices)
            {
                if (voice.IsActive)
                {
                    voice.Release(_parameters.ReleaseMs);
                }
            }
        }

        foreach (int note in released)
        {
            NoteReleased?.Invoke(note);
        }
    }

    public void StartRecording()
    {
        lock (_recordLock)
        {
            _recorded.Clear();
            _recording = true;
        }
    }

    public float[] StopRecording()
    {
        lock (_recordLock)
        {
            _recording = false;
            float[] result = _recorded.ToArray();
            _recorded.Clear();
            return result;
        }
    }

    public void CopyScope(float[] destination)
    {
        if (destination.Length == 0)
        {
            return;
        }

        lock (_scopeLock)
        {
            int count = Math.Min(destination.Length, ScopeSize);
            int start = (_scopeWrite - count + ScopeSize) & ScopeMask;

            for (int i = 0; i < count; i++)
            {
                destination[i] = _scopeRing[(start + i) & ScopeMask];
            }

            for (int i = count; i < destination.Length; i++)
            {
                destination[i] = 0f;
            }
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_mixBuffer.Length < count)
        {
            _mixBuffer = new float[count];
        }

        float[] mix = _mixBuffer;
        Array.Clear(mix, 0, count);

        lock (_voiceLock)
        {
            foreach (SynthVoice voice in _voices)
            {
                if (voice.IsActive)
                {
                    voice.Process(mix, count, _parameters);
                }
            }
        }

        ApplyDistortion(mix, count);
        _delay.Process(mix, count, _parameters);
        _reverb.Process(mix, count, _parameters);

        float volume = Math.Clamp(_parameters.MasterVolume, 0f, 1f);
        float blockPeak = 0f;

        for (int i = 0; i < count; i++)
        {
            float sample = (float)Math.Tanh(mix[i] * 1.2f) * 0.9f * volume;
            mix[i] = sample;

            float magnitude = MathF.Abs(sample);
            if (magnitude > blockPeak)
            {
                blockPeak = magnitude;
            }
        }

        _peak = MathF.Max(blockPeak, _peak * 0.90f);

        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] = mix[i];
        }

        lock (_scopeLock)
        {
            int writeCount = Math.Min(count, ScopeSize);
            int start = count - writeCount;

            for (int i = 0; i < writeCount; i++)
            {
                _scopeRing[_scopeWrite] = buffer[offset + start + i];
                _scopeWrite = (_scopeWrite + 1) & ScopeMask;
            }
        }

        if (_recording)
        {
            lock (_recordLock)
            {
                if (_recording)
                {
                    int room = MaxRecordSamples - _recorded.Count;
                    int copy = Math.Min(room, count);

                    for (int i = 0; i < copy; i++)
                    {
                        _recorded.Add(buffer[offset + i]);
                    }

                    if (copy < count)
                    {
                        _recording = false;
                    }
                }
            }
        }

        return count;
    }

    private void SilenceAll()
    {
        List<int> released;

        lock (_voiceLock)
        {
            released = new List<int>(_heldNotes.Keys);
            _heldNotes.Clear();

            foreach (SynthVoice voice in _voices)
            {
                voice.Silence();
            }
        }

        foreach (int note in released)
        {
            NoteReleased?.Invoke(note);
        }
    }

    private SynthVoice? FindVoice()
    {
        foreach (SynthVoice voice in _voices)
        {
            if (!voice.IsActive)
            {
                return voice;
            }
        }

        SynthVoice? releasing = null;
        SynthVoice? oldest = null;

        foreach (SynthVoice voice in _voices)
        {
            if (voice.IsReleasing && (releasing == null || voice.Order < releasing.Order))
            {
                releasing = voice;
            }

            if (oldest == null || voice.Order < oldest.Order)
            {
                oldest = voice;
            }
        }

        return releasing ?? oldest;
    }

    private void ApplyDistortion(float[] buffer, int count)
    {
        float mixAmount = Math.Clamp(_parameters.DistortionMix, 0f, 1f);
        if (mixAmount <= 0.0001f)
        {
            return;
        }

        float drive = 1f + Math.Clamp(_parameters.DistortionDrive, 0f, 1f) * 11f;
        float dryGain = 1f - mixAmount;

        for (int i = 0; i < count; i++)
        {
            float dry = buffer[i];
            float saturated = (float)Math.Tanh(dry * drive) / drive;
            buffer[i] = dry * dryGain + saturated * mixAmount * drive * 0.9f;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _recording = false;

        try
        {
            _waveOut.Stop();
        }
        catch
        {
        }

        _waveOut.Dispose();
        SilenceAll();
    }
}
