using System;

namespace Ducky.Sdk.Restore;

/// <summary>
/// Restores a character's health, energy or water through the game's own methods, so rules registered with
/// <see cref="RestoreRules"/> and the game's gain scaling both apply. Each call is marked as a mod restoration
/// (<see cref="RestoreOrigin.Mod"/>) for the duration of the game call, including nested calls.
/// </summary>
/// <remarks>
/// Main thread only, as the game's character methods are.
/// </remarks>
public static class CharacterRestore
{
    /// <summary>
    /// Heals <paramref name="character"/> by <paramref name="amount"/> through <c>CharacterMainControl.AddHealth</c>, so the
    /// health gain (<c>HealGain</c>) applies as it does to an item.
    /// </summary>
    public static void Heal(CharacterMainControl character, float amount)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        using (RestoreOriginScope.Enter())
        {
            character.AddHealth(amount);
        }
    }

    /// <summary>
    /// Heals <paramref name="character"/> to its maximum, asking for the amount that fills it once the health gain is applied.
    /// Returns false, and requests nothing, when the character is already at maximum or the gain leaves nothing to fill.
    /// </summary>
    public static bool HealToFull(CharacterMainControl character)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        var health = character.Health;
        if (health == null)
        {
            return false;
        }

        var amount = RestoreMath.AmountToFill(health.CurrentHealth, health.MaxHealth, character.HealGain);
        if (amount <= 0f)
        {
            return false;
        }

        Heal(character, amount);
        return true;
    }

    /// <summary>Adds <paramref name="amount"/> of energy through <c>CharacterMainControl.AddEnergy</c>, with the food gain applied.</summary>
    public static void AddEnergy(CharacterMainControl character, float amount)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        using (RestoreOriginScope.Enter())
        {
            character.AddEnergy(amount);
        }
    }

    /// <summary>Adds <paramref name="amount"/> of water through <c>CharacterMainControl.AddWater</c>, with the food gain applied.</summary>
    public static void AddWater(CharacterMainControl character, float amount)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        using (RestoreOriginScope.Enter())
        {
            character.AddWater(amount);
        }
    }
}
