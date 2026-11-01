using System;
using ItemStatsSystem;
using UnityEngine;

namespace Ducky.Sdk.World;

/// <summary>
/// Spawns the game's grenades and landmines. The grenade is instantiated from the prefab of its item's <c>Skill_Grenade</c>,
/// configured, launched with its owner and marked with its item type, as the game does when a player throws one.
/// </summary>
public static class Grenades
{
    /// <summary>
    /// Spawns a grenade of <paramref name="itemType"/> at <paramref name="position"/>, owned by <paramref name="owner"/>. The owner
    /// decides the grenade's team. Returns the spawned grenade.
    /// </summary>
    /// <remarks>
    /// A landmine the player must trigger (the default, see <see cref="GrenadeOptions.RequirePlayerTrigger"/>) needs an owner on a
    /// team hostile to the player; otherwise an argument error is thrown and nothing is spawned. A grenade without an owner has no
    /// owner character. The grenade ignores collisions with its owner for a short time after landing. Main thread only.
    /// </remarks>
    /// <exception cref="ArgumentException">The item type is not a grenade, an option is not a number, or the landmine owner rule is broken.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is negative or infinite.</exception>
    public static Grenade Spawn(int itemType, Vector3 position, CharacterMainControl? owner, GrenadeOptions? options = null)
    {
        options ??= new GrenadeOptions();
        GrenadeRules.ValidateOptions(options.DelaySeconds, options.Damage, options.BlastRadius, options.LandmineTriggerRange);
        if (!IsFinite(options.Velocity))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Velocity, "The velocity must be finite.");
        }

        var prefab = FindGrenadePrefab(itemType)
                     ?? throw new ArgumentException($"Item type {itemType} is not a grenade.", nameof(itemType));

        var isLandmine = prefab.isLandmine || options.IsLandmine;
        var ownerError = GrenadeRules.ValidateLandmineOwner(
            isLandmine,
            options.RequirePlayerTrigger,
            hasOwner: owner != null,
            ownerHostileToPlayer: owner != null && Team.IsEnemy(owner.Team, Teams.player));
        if (ownerError != null)
        {
            throw new ArgumentException(ownerError, nameof(owner));
        }

        var grenade = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);

        // the item's own values stay unless the option is set
        if (options.Damage.HasValue)
        {
            var info = grenade.damageInfo;
            info.damageValue = options.Damage.Value;
            grenade.damageInfo = info;
        }

        if (options.BlastRadius.HasValue)
        {
            grenade.damageRange = options.BlastRadius.Value;
        }

        if (options.DelaySeconds.HasValue)
        {
            grenade.delayTime = options.DelaySeconds.Value;
        }

        grenade.isLandmine = isLandmine;
        if (options.LandmineTriggerRange.HasValue)
        {
            grenade.landmineTriggerRange = options.LandmineTriggerRange.Value;
        }

        grenade.Launch(position, options.Velocity, owner, options.CanHurtSelf);
        grenade.SetWeaponIdInfo(itemType);
        return grenade;
    }

    private static Grenade? FindGrenadePrefab(int itemType)
    {
        var item = ItemAssetsCollection.GetPrefab(itemType);
        var skill = item != null ? item.GetComponent<ItemSetting_Skill>() : null;
        var grenadeSkill = skill != null ? skill.Skill as Skill_Grenade : null;
        return grenadeSkill != null ? grenadeSkill.grenadePfb : null;
    }

    private static bool IsFinite(Vector3 value) =>
        IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
