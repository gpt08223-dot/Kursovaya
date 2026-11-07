using System;

namespace AvaloniaApplication1.AudioEngine;

public sealed class FastFourierTransform
{
    private readonly int[] _reversal;
    private readonly float[] _cosines;
    private readonly float[] _sines;

    public FastFourierTransform(int size)
    {
        if (size < 2 || (size & (size - 1)) != 0)
        {
            throw new ArgumentException("FFT size must be a power of two.", nameof(size));
        }

        Size = size;
        _reversal = new int[size];
        _cosines = new float[size / 2];
        _sines = new float[size / 2];

        int bits = 0;
        while ((1 << bits) < size)
        {
            bits++;
        }

        for (int i = 0; i < size; i++)
        {
            int value = i;
            int reversed = 0;

            for (int bit = 0; bit < bits; bit++)
            {
                reversed = (reversed << 1) | (value & 1);
                value >>= 1;
            }

            _reversal[i] = reversed;
        }

        for (int i = 0; i < size / 2; i++)
        {
            double angle = -2.0 * Math.PI * i / size;
            _cosines[i] = (float)Math.Cos(angle);
            _sines[i] = (float)Math.Sin(angle);
        }
    }

    public int Size { get; }

    public void Transform(float[] real, float[] imaginary)
    {
        if (real.Length < Size || imaginary.Length < Size)
        {
            throw new ArgumentException("Buffers are smaller than the FFT size.");
        }

        for (int i = 0; i < Size; i++)
        {
            int j = _reversal[i];
            if (j > i)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
        }

        for (int length = 2; length <= Size; length <<= 1)
        {
            int half = length >> 1;
            int step = Size / length;

            for (int start = 0; start < Size; start += length)
            {
                for (int j = 0; j < half; j++)
                {
                    int twiddle = j * step;
                    float wr = _cosines[twiddle];
                    float wi = _sines[twiddle];

                    int a = start + j;
                    int b = a + half;

                    float tr = real[b] * wr - imaginary[b] * wi;
                    float ti = real[b] * wi + imaginary[b] * wr;

                    real[b] = real[a] - tr;
                    imaginary[b] = imaginary[a] - ti;
                    real[a] += tr;
                    imaginary[a] += ti;
                }
            }
        }
    }
}
