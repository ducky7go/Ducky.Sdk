namespace Ducky.Sdk.Restore;

/// <summary>The three restorations of a character that rules can rewrite.</summary>
public enum RestoreKind
{
    /// <summary>Health, through <c>CharacterMainControl.AddHealth</c>. The gain is <c>HealGain</c>.</summary>
    Health,

    /// <summary>Energy (food), through <c>CharacterMainControl.AddEnergy</c>. The gain is <c>FoodGain</c>.</summary>
    Energy,

    /// <summary>Water, through <c>CharacterMainControl.AddWater</c>. The gain is <c>FoodGain</c>.</summary>
    Water,
}
