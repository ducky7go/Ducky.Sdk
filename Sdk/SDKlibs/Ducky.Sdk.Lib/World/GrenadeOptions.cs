using UnityEngine;

namespace Ducky.Sdk.World;

/// <summary>
/// Options of <see cref="Grenades.Spawn"/>. A value that is not set keeps the item's own value. Numeric values must be finite,
/// and time, damage, radius and trigger range must be zero or more.
/// </summary>
public sealed class GrenadeOptions
{
    /// <summary>The initial velocity. Zero (the default) makes the grenade fall in place.</summary>
    public Vector3 Velocity { get; set; }

    /// <summary>Seconds until the grenade detonates. Null keeps the item's delay.</summary>
    public float? DelaySeconds { get; set; }

    /// <summary>The damage of the explosion. Null keeps the item's damage.</summary>
    public float? Damage { get; set; }

    /// <summary>The blast radius. Null keeps the item's radius.</summary>
    public float? BlastRadius { get; set; }

    /// <summary>Whether the explosion can hurt the owner's team. Defaults to false.</summary>
    public bool CanHurtSelf { get; set; }

    /// <summary>
    /// Whether the grenade is a landmine. A landmine arms after landing and explodes when a character of a team other than its
    /// owner's enters its trigger range.
    /// </summary>
    public bool IsLandmine { get; set; }

    /// <summary>The landmine trigger range. Null keeps the game's default (0.5) or the item's value.</summary>
    public float? LandmineTriggerRange { get; set; }

    /// <summary>
    /// Only checked for a landmine: when true (the default), the owner must be a character on a team hostile to the player, so the
    /// player can trigger the mine. Set it to false for a mine meant for others.
    /// </summary>
    public bool RequirePlayerTrigger { get; set; } = true;
}
