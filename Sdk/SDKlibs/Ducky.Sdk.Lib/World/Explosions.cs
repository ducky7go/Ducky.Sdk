using System;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace Ducky.Sdk.World;

/// <summary>
/// Creates explosions through the game's <c>ExplosionManager</c>, with control over the radius, damage, source, self-damage,
/// visual effect and true damage.
/// </summary>
public static class Explosions
{
    /// <summary>
    /// Creates an explosion at <paramref name="center"/>. Without a <paramref name="source"/> the damage has no source, so the
    /// explosion still harms everyone. With a source and <paramref name="canHurtSelf"/> false, the source's team is not hurt.
    /// </summary>
    /// <remarks>
    /// Targeting is the game's rule and is not changed here. One explosion affects at most eight colliders; a character behind a
    /// wall is not hurt; a character is hurt once; a character that is rolling is not hurt. The effect prefab exists only for
    /// <see cref="ExplosionFxTypes.normal"/> and <see cref="ExplosionFxTypes.flash"/>: the fire, ice and custom types show nothing,
    /// so the caller must show its own effect. Damage is normal (reduced by armor and difficulty) unless
    /// <paramref name="isTrueDamage"/> is set, which makes it real damage that ignores both. Camera shake is off. Main thread only.
    /// </remarks>
    /// <param name="center">The centre of the explosion.</param>
    /// <param name="radius">The radius; must be finite and positive.</param>
    /// <param name="damage">The damage; must be finite and zero or more. Zero is accepted for the flash effect.</param>
    /// <param name="source">The character the damage is attributed to, or null for no source.</param>
    /// <param name="canHurtSelf">With a source, whether the source's team can be hurt.</param>
    /// <param name="effect">The visual effect type.</param>
    /// <param name="isTrueDamage">Whether the damage is real damage.</param>
    /// <exception cref="ArgumentException">The radius or damage is not a number.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The radius or damage is not finite or is out of range.</exception>
    public static void Create(Vector3 center, float radius, float damage,
        CharacterMainControl? source = null, bool canHurtSelf = false,
        ExplosionFxTypes effect = ExplosionFxTypes.normal, bool isTrueDamage = false)
    {
        ExplosionRules.Validate(radius, damage);

        var info = new DamageInfo(source)
        {
            damageValue = damage,
            damageType = isTrueDamage ? DamageTypes.realDamage : DamageTypes.normal,
        };

        if (LevelManager.Instance == null || LevelManager.Instance.ExplosionManager == null)
        {
            Log.Warn("[Explosions] no explosion manager is loaded; the explosion was not created");
            return;
        }

        LevelManager.Instance.ExplosionManager.CreateExplosion(center, radius, info, effect, shakeStrength: 0f, canHurtSelf: canHurtSelf);
    }
}
