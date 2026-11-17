namespace Ducky.Sdk.Facade;

/// <summary>How a stat modifier changes its stat. Maps the game's <c>ModifierType</c> (only the two kinds mods use so far).</summary>
public enum StatModifierKind
{
    /// <summary>Adds the value. Maps <c>ModifierType.Add</c>.</summary>
    Add,

    /// <summary>Adds the value as a fraction of the base (0.5 = +50%). Maps <c>ModifierType.PercentageAdd</c>.</summary>
    PercentageAdd,
}
