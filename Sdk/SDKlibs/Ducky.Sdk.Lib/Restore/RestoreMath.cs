namespace Ducky.Sdk.Restore;

/// <summary>
/// The restoration arithmetic of the game, with no game object. The game heals or restores <c>amount × (1 + gain)</c> and
/// clamps the result at the maximum. These functions give the overflow that rules see and the amount that fills a
/// character to full. Verified against the notes: aspirin 70 + 10 − 77 = 3, first-aid kit 71 + 6 − 77 = 0.
/// </summary>
internal static class RestoreMath
{
    /// <summary>
    /// The part of a restoration that the maximum would cut: <c>current + amount × (1 + gain) − max</c>, and never below zero.
    /// </summary>
    /// <param name="current">The current value (health, energy or water) before the restoration.</param>
    /// <param name="amount">The amount the game is asked to restore.</param>
    /// <param name="gain">The gain stat: <c>HealGain</c> for health, <c>FoodGain</c> for energy and water.</param>
    /// <param name="max">The maximum of the same value.</param>
    public static float Overflow(float current, float amount, float gain, float max)
    {
        var overflow = current + amount * (1f + gain) - max;
        return overflow > 0f ? overflow : 0f;
    }

    /// <summary>
    /// The amount to ask for so that a restoration fills the value to <paramref name="max"/>: <c>(max − current) / (1 + gain)</c>.
    /// Zero when the value is already at or above the maximum, or when <c>1 + gain</c> is not positive (the game would not
    /// fill it), so a negative gain still reaches full.
    /// </summary>
    public static float AmountToFill(float current, float max, float gain)
    {
        var factor = 1f + gain;
        if (current >= max || factor <= 0f)
        {
            return 0f;
        }

        return (max - current) / factor;
    }
}
