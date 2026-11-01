using System;

namespace Ducky.Sdk.World;

/// <summary>
/// The owner check and the option checks of <see cref="Grenades.Spawn"/>, with no game object.
/// </summary>
internal static class GrenadeRules
{
    /// <summary>
    /// For a landmine that the player must trigger, returns the error text when the owner is missing or is not on a team hostile to
    /// the player. Returns null when the spawn may continue, including every case that is not such a landmine.
    /// </summary>
    public static string? ValidateLandmineOwner(bool isLandmine, bool requirePlayerTrigger, bool hasOwner, bool ownerHostileToPlayer)
    {
        if (!isLandmine || !requirePlayerTrigger)
        {
            return null;
        }

        if (!hasOwner)
        {
            return "A landmine the player must trigger needs an owner from a team hostile to the player. Pass an owner, or set RequirePlayerTrigger to false for a mine meant for others.";
        }

        if (!ownerHostileToPlayer)
        {
            return "The owner of a landmine the player must trigger must be on a team hostile to the player; an owner on the player team cannot make it explode for the player.";
        }

        return null;
    }

    /// <summary>
    /// Checks each option that is set: it must be a number, finite, and zero or more. Throws an argument error otherwise.
    /// </summary>
    public static void ValidateOptions(float? delaySeconds, float? damage, float? blastRadius, float? landmineTriggerRange)
    {
        Check(delaySeconds, nameof(delaySeconds));
        Check(damage, nameof(damage));
        Check(blastRadius, nameof(blastRadius));
        Check(landmineTriggerRange, nameof(landmineTriggerRange));
    }

    private static void Check(float? value, string name)
    {
        if (!value.HasValue)
        {
            return;
        }

        var number = value.Value;
        if (float.IsNaN(number))
        {
            throw new ArgumentException($"{name} must be a number.", name);
        }

        if (float.IsInfinity(number) || number < 0f)
        {
            throw new ArgumentOutOfRangeException(name, number, $"{name} must be finite and zero or more.");
        }
    }
}
