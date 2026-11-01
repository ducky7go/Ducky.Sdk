using System;

namespace Ducky.Sdk.Utils;

/// <summary>
/// A clock that takes the real time as a function and can be moved forward, so a test does not have to wait out a
/// ten second timer. Its elapsed time never goes backwards. Touches no Unity object.
/// </summary>
public sealed class ClockCore
{
    private readonly Func<float> _source;

    /// <param name="source">The real time, in seconds.</param>
    public ClockCore(Func<float> source) => _source = source;

    /// <summary>Seconds the clock has been moved forward in total.</summary>
    public float Skipped { get; private set; }

    /// <summary>The real time plus the skipped seconds.</summary>
    public float Now => _source() + Skipped;

    /// <summary>Moves the clock forward by <paramref name="seconds"/>. A negative amount is rejected and changes nothing.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
    public void Skip(float seconds)
    {
        if (seconds < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "The clock cannot go backwards.");
        }

        Skipped += seconds;
    }

    /// <summary>Forgets every skipped second.</summary>
    public void Reset() => Skipped = 0f;
}

/// <summary>
/// The time mod logic measures its own timers with: <c>Time.time</c> plus what a test has skipped. It does not change
/// the game's own buff lifetimes.
/// </summary>
public static class Clock
{
    private static readonly ClockCore Core = new(() => UnityEngine.Time.time);

    /// <summary>The current time, including skipped seconds.</summary>
    public static float Now => Core.Now;

    /// <summary>Seconds the clock has been moved forward in total.</summary>
    public static float Skipped => Core.Skipped;

    /// <summary>Moves the clock forward. A negative amount is rejected and changes nothing.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
    public static void Skip(float seconds) => Core.Skip(seconds);

    /// <summary>Forgets every skipped second.</summary>
    public static void Reset() => Core.Reset();
}
