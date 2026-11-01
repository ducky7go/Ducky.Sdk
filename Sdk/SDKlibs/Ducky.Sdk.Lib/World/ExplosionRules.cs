using System;

namespace Ducky.Sdk.World;

/// <summary>
/// The rules of <see cref="Explosions.Create"/> that need no game object: argument checks and which team is excluded.
/// </summary>
internal static class ExplosionRules
{
    /// <summary>
    /// Checks the radius and the damage. The radius must be a finite positive number. The damage must be a finite number that is
    /// zero or more; zero is accepted because the flash effect uses it.
    /// </summary>
    public static void Validate(float radius, float damage)
    {
        if (float.IsNaN(radius))
        {
            throw new ArgumentException("The explosion radius must be a number.", nameof(radius));
        }

        if (float.IsInfinity(radius) || radius <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "The explosion radius must be finite and positive.");
        }

        if (float.IsNaN(damage))
        {
            throw new ArgumentException("The explosion damage must be a number.", nameof(damage));
        }

        if (float.IsInfinity(damage) || damage < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), damage, "The explosion damage must be finite and zero or more.");
        }
    }

    /// <summary>
    /// True when the game's targeting excludes the source's team: a source is given and self-damage is off. Without a source, or with
    /// self-damage on, everyone is eligible.
    /// </summary>
    public static bool ExcludesSourceTeam(bool hasSource, bool canHurtSelf) => hasSource && !canHurtSelf;
}
