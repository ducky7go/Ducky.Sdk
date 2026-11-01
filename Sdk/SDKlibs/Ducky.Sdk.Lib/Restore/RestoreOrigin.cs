namespace Ducky.Sdk.Restore;

/// <summary>Where a restoration came from.</summary>
public enum RestoreOrigin
{
    /// <summary>The game, for example because a medical or food item was used or another game effect healed.</summary>
    Game,

    /// <summary>A mod, through one of the SDK's <see cref="CharacterRestore"/> helpers, including nested calls.</summary>
    Mod,
}
