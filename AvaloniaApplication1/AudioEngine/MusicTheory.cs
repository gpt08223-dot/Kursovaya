using System;

namespace AvaloniaApplication1.AudioEngine;

public static class MusicTheory
{
    public const int LowestNote = 21;
    public const int HighestNote = 108;
    public const int KeyCount = HighestNote - LowestNote + 1;

    private static readonly string[] NoteNames =
    {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    };

    public static double MidiToFrequency(int midiNote)
    {
        return 440.0 * Math.Pow(2.0, (midiNote - 69.0) / 12.0);
    }

    public static bool IsBlackKey(int midiNote)
    {
        return midiNote % 12 is 1 or 3 or 6 or 8 or 10;
    }

    public static string GetNoteName(int midiNote)
    {
        if (midiNote < 0)
        {
            return string.Empty;
        }

        int octave = midiNote / 12 - 1;
        return NoteNames[midiNote % 12] + octave;
    }

    public static int CountWhiteKeys(int startNote, int endNote)
    {
        int count = 0;

        for (int note = startNote; note <= endNote; note++)
        {
            if (!IsBlackKey(note))
            {
                count++;
            }
        }

        return count;
    }
}
