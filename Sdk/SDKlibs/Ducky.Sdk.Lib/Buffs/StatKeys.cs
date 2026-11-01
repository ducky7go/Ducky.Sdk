namespace Ducky.Sdk.Buffs;

/// <summary>
/// Stat keys of the player and other characters that the game's research validated. Each constant's value is the key the
/// game uses, so it can be passed to <see cref="StatBuff"/>'s modify operation in place of a string literal and a typo
/// cannot silently modify nothing. Units and side effects are given per constant; a side effect the research did not
/// verify in the real game is labeled <b>unverified</b>.
/// </summary>
public static class StatKeys
{
    /// <summary>Energy (food) consumption per minute, base 7.2. Hunger speed scales linearly with it (verified).</summary>
    public const string EnergyCost = "EnergyCost";

    /// <summary>Water consumption per minute, base 7.6.</summary>
    public const string WaterCost = "WaterCost";

    /// <summary>Fraction added to the energy from eating: food gain is multiplied by <c>1 + FoodGain</c> (verified). Base 0.</summary>
    public const string FoodGain = "FoodGain";

    /// <summary>Fraction added to healing: heal is multiplied by <c>1 + HealGain</c> (verified). Base 0.</summary>
    public const string HealGain = "HealGain";

    /// <summary>Dash (roll) speed, base 10. A resulting value below 0.1 prevents rolling (verified).</summary>
    public const string DashSpeed = "DashSpeed";

    /// <summary>Whether the character can use hands and aim while rolling, base 0. Firing during a roll is <b>unverified</b>.</summary>
    public const string DashCanControl = "DashCanControl";

    /// <summary>Walking noise radius, base 8. Setting it to 0 making walking fully silent is <b>unverified</b> in game.</summary>
    public const string WalkSoundRange = "WalkSoundRange";

    /// <summary>Running noise radius, base 16.</summary>
    public const string RunSoundRange = "RunSoundRange";

    /// <summary>Multiplier of melee damage, base 1.31. Effect size from the name; <b>unverified</b> per attack.</summary>
    public const string MeleeDamageMultiplier = "MeleeDamageMultiplier";

    /// <summary>Multiplier of gun scatter (spread), base 1.1. Use near 0 to remove scatter is <b>unverified</b>.</summary>
    public const string GunScatterMultiplier = "GunScatterMultiplier";

    /// <summary>Recoil control, base 1.1. Direction of its effect is from the name; <b>unverified</b>.</summary>
    public const string RecoilControl = "RecoilControl";

    /// <summary>Number of backpack slots, base 22. It is the number of slots, not a stack limit (verified).</summary>
    public const string InventoryCapacity = "InventoryCapacity";

    /// <summary>Maximum carry weight, base 52. Overweight behavior is documented in the mechanics reference; <b>unverified</b> here.</summary>
    public const string MaxWeight = "MaxWeight";

    /// <summary>Movement multiplier applied to walk, run and turn speed, base 1.12. A stun modifier lowers it to 0.25 (verified).</summary>
    public const string Moveability = "Moveability";

    /// <summary>View distance, base 50. Effect on fog of war through this key alone is <b>unverified</b>.</summary>
    public const string ViewDistance = "ViewDistance";

    /// <summary>Walking speed, base 4.6, before <see cref="Moveability"/> is applied (static code reading; the multiplication is <b>unverified</b> in game).</summary>
    public const string WalkSpeed = "WalkSpeed";

    /// <summary>Running speed, base 6.9, before <see cref="Moveability"/> is applied (static code reading; the multiplication is <b>unverified</b> in game).</summary>
    public const string RunSpeed = "RunSpeed";

    /// <summary>Hearing multiplier, base 1. An AI compares sound distance with the sound radius multiplied by this value (verified).</summary>
    public const string HearingAbility = "HearingAbility";
}
