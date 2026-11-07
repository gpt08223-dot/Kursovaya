using System.Collections.Generic;

namespace AvaloniaApplication1.Models;

public static class PatternFactory
{
    public static IReadOnlyList<SequencerPattern> CreatePatterns()
    {
        return new List<SequencerPattern>
        {
            new("Crystal Arp",
            new[] { 60, 0, 67, 72, 75, 72, 67, 0, 63, 0, 70, 74, 79, 74, 70, 0 }),
            new("Night Drive",
            new[] { 36, 0, 36, 48, 39, 0, 39, 51, 41, 0, 41, 53, 43, 0, 46, 55 }),
            new("Glass Bells",
            new[] { 72, 76, 79, 84, 79, 76, 72, 0, 74, 77, 81, 86, 81, 77, 74, 0 }),
            new("Soft Motion",
            new[] { 57, 60, 64, 67, 60, 64, 67, 72, 55, 59, 62, 67, 59, 62, 67, 71 })
        };
    }
}
