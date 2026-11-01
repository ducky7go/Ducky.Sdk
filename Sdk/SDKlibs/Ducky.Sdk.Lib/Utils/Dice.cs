using System;

namespace Ducky.Sdk.Utils;

/// <summary>
/// Source of chance. It takes the real source as a function, so a test can force every roll to a fixed value: 0 makes
/// every chance succeed, 1 makes every chance fail. Touches no Unity object.
/// </summary>
public sealed class DiceCore
{
    private readonly Func<float> _source;

    /// <param name="source">The real source of rolls, each in 0..1.</param>
    public DiceCore(Func<float> source) => _source = source;

    /// <summary>The forced value, or null when rolls are real.</summary>
    public float? Forced { get; private set; }

    /// <summary>Forces every roll to <paramref name="value"/> (clamped to 0..1); null gives the real source back.</summary>
    public void Force(float? value) => Forced = value.HasValue ? Math.Min(1f, Math.Max(0f, value.Value)) : null;

    /// <summary>A roll in 0..1.</summary>
    public float Value() => Forced ?? _source();

    /// <summary>A seeded stream of rolls for one draw; it follows <see cref="Force"/> like every other roll.</summary>
    public DrawStream Stream(int seed) => new(seed, () => Forced);

    /// <summary>True with the given probability.</summary>
    public bool Chance(float probability) => Value() < probability;

    /// <summary>An integer in <paramref name="minInclusive"/> to <paramref name="maxExclusive"/> - 1.</summary>
    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        var index = minInclusive + (int)(Value() * (maxExclusive - minInclusive));
        return Math.Min(index, maxExclusive - 1);
    }
}

/// <summary>
/// The chance source for mod logic. Every roll goes through here rather than through <c>Random</c> directly, so a test
/// or a developer tool can force the dice. Each compiled copy of the SDK has its own state, so forcing it in one mod
/// does not reach another mod.
/// </summary>
public static class Dice
{
    private static readonly DiceCore Core = new(() => UnityEngine.Random.value);

    /// <summary>The forced value, or null when rolls are real.</summary>
    public static float? Forced => Core.Forced;

    /// <summary>Forces every roll to <paramref name="value"/> (clamped to 0..1); null gives the real source back.</summary>
    public static void Force(float? value) => Core.Force(value);

    /// <summary>A roll in 0..1.</summary>
    public static float Value() => Core.Value();

    /// <summary>True with the given probability.</summary>
    public static bool Chance(float probability) => Core.Chance(probability);

    /// <summary>An integer in <paramref name="minInclusive"/> to <paramref name="maxExclusive"/> - 1.</summary>
    public static int Range(int minInclusive, int maxExclusive) => Core.Range(minInclusive, maxExclusive);

    /// <summary>A seeded stream for a deterministic draw; forced dice still apply to it.</summary>
    public static DrawStream Stream(int seed) => Core.Stream(seed);
}
