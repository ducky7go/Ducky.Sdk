using System;

namespace Ducky.Sdk.Utils;

/// <summary>
/// A seeded random source for a deterministic draw. It uses SplitMix64 written here, so the same seed gives the same
/// numbers on every runtime. While the dice are forced it returns the forced value instead and does not advance.
/// Touches no Unity object.
/// </summary>
public sealed class DrawStream
{
    private readonly Func<float?> _forced;
    private ulong _state;

    /// <param name="seed">The seed; the same seed gives the same values.</param>
    /// <param name="forced">Returns the forced value while the dice are forced, otherwise null.</param>
    public DrawStream(int seed, Func<float?> forced)
    {
        _state = unchecked((ulong)(uint)seed);
        _forced = forced;
    }

    /// <summary>A value in 0..1 (the forced value when the dice are forced).</summary>
    public float Value()
    {
        var forced = _forced();
        if (forced.HasValue)
        {
            return forced.Value;
        }

        // the top 24 bits, so the result is exact in a float and always below 1
        return (float)(NextUInt64() >> 40) * (1f / 16777216f);
    }

    private ulong NextUInt64()
    {
        unchecked
        {
            _state += 0x9E3779B97F4A7C15UL;
            var z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
