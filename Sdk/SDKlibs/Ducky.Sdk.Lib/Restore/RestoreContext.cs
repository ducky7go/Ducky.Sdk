namespace Ducky.Sdk.Restore;

/// <summary>
/// What a restore rule knows about one restoration. It is read-only. <see cref="Overflow"/> is recalculated from the amount
/// each rule sees, so it always matches the value the rule is about to change.
/// </summary>
public readonly struct RestoreContext
{
    /// <summary>Creates the context of a restoration of <paramref name="kind"/> for <paramref name="character"/>.</summary>
    public RestoreContext(RestoreKind kind, CharacterMainControl character, RestoreOrigin origin,
        float current, float max, float gain, float overflow)
    {
        Kind = kind;
        Character = character;
        Origin = origin;
        Current = current;
        Max = max;
        Gain = gain;
        Overflow = overflow;
    }

    /// <summary>Which value is restored.</summary>
    public RestoreKind Kind { get; }

    /// <summary>The character that is restored.</summary>
    public CharacterMainControl Character { get; }

    /// <summary>Whether the restoration came from a mod helper or from the game.</summary>
    public RestoreOrigin Origin { get; }

    /// <summary>The current value before the restoration: current health, energy or water.</summary>
    public float Current { get; }

    /// <summary>The maximum of the same value.</summary>
    public float Max { get; }

    /// <summary>The gain stat: <c>HealGain</c> for health, <c>FoodGain</c> for energy and water.</summary>
    public float Gain { get; }

    /// <summary>
    /// The amount the restoration would waste above the maximum: <c>max(0, Current + amount × (1 + Gain) − Max)</c>, where
    /// <c>amount</c> is the value the rule sees.
    /// </summary>
    public float Overflow { get; }

    /// <summary>The same context, with <see cref="Overflow"/> recalculated for <paramref name="amount"/>.</summary>
    public RestoreContext ForAmount(float amount) =>
        new(Kind, Character, Origin, Current, Max, Gain, RestoreMath.Overflow(Current, amount, Gain, Max));
}
