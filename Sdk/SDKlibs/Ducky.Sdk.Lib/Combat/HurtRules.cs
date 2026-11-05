#if DUCKY_SDK_HARMONY
using System;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using HarmonyLib;

namespace Ducky.Sdk.Combat;

/// <summary>
/// The state of a character at the moment a hit reaches <c>Health.Hurt</c>, read-only for the rules.
/// </summary>
/// <remarks>
/// The prefix runs even when the game then ignores the hit: <see cref="Invincible"/> and <see cref="IsDead"/> say so.
/// </remarks>
public readonly struct HurtContext
{
    /// <summary>Creates a context.</summary>
    public HurtContext(float currentHealth, float maxHealth, bool invincible, bool isDead, bool isExplosion,
        float bodyArmor, float headArmor)
    {
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        Invincible = invincible;
        IsDead = isDead;
        IsExplosion = isExplosion;
        BodyArmor = bodyArmor;
        HeadArmor = headArmor;
    }

    /// <summary>The victim's health before this hit.</summary>
    public float CurrentHealth { get; }

    /// <summary>The victim's maximum health.</summary>
    public float MaxHealth { get; }

    /// <summary>True when the victim is invincible; the game then ignores the hit.</summary>
    public bool Invincible { get; }

    /// <summary>True when the victim is already dead; the game then ignores the hit.</summary>
    public bool IsDead { get; }

    /// <summary>True when the hit is an explosion.</summary>
    public bool IsExplosion { get; }

    /// <summary>The victim's body armor.</summary>
    public float BodyArmor { get; }

    /// <summary>The victim's head armor.</summary>
    public float HeadArmor { get; }
}

/// <summary>
/// A damage-rewrite rule. It receives the damage by reference: a change to <c>damage.damageValue</c> continues through
/// the rest of the game's damage code (armor, crit and health loss).
/// </summary>
public delegate void HurtRule(ref DamageInfo damage, HurtContext context);

/// <summary>
/// The shared <c>Health.Hurt</c> prefix of this copy of the SDK, where mods register damage-rewrite rules. Rules run by
/// ascending priority, then registration order, each seeing the value the previous one left. A rule that throws is
/// logged and skipped. One patch is installed for all rules of this copy and removed with the last rule, so one mod's
/// rules never replace or remove another mod's.
/// </summary>
/// <remarks>
/// Requires Harmony: the file is compiled only when the mod includes it (<c>IncludeHarmony</c>).
/// </remarks>
public static class HurtRules
{
    // One per loaded copy: a patch id that another mod's copy cannot share.
    private static readonly Guid InstanceId = Guid.NewGuid();

    private static readonly string HarmonyId = PatchIds.For(
        typeof(HurtRules).Assembly.GetName().Name ?? "Ducky.Sdk", "HurtRules", InstanceId);

    // Installed with the first rule, removed with the last one.
    private static readonly OrderedRules<HurtRule> Rules = new(onFirst: Install, onLast: Uninstall);

    /// <summary>
    /// Registers a rule at <paramref name="priority"/> (lower runs first). Dispose the returned handle to remove this rule
    /// only; disposing it again does nothing.
    /// </summary>
    public static IDisposable Register(HurtRule rule, int priority = 0)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        return Rules.Add(rule, priority);
    }

    private static void Install() =>
        HarmonyPatches.Install(HarmonyId, harmony => harmony.Patch(
                AccessTools.Method(typeof(Health), nameof(Health.Hurt), new[] { typeof(DamageInfo) }),
                prefix: new HarmonyMethod(typeof(HurtRules), nameof(HurtPrefix))),
            "[HurtRules] could not install its prefix");

    private static void Uninstall() => HarmonyPatches.Uninstall(HarmonyId);

    // Harmony binds these by name: __instance is the Health, damageInfo is the parameter of Health.Hurt.
    private static void HurtPrefix(Health __instance, ref DamageInfo damageInfo)
    {
        var context = new HurtContext(
            __instance.CurrentHealth,
            __instance.MaxHealth,
            __instance.Invincible,
            __instance.IsDead,
            damageInfo.isExplosion,
            __instance.BodyArmor,
            __instance.HeadArmor);

        // a ref parameter cannot be captured by a lambda, so the rules run in a loop
        foreach (var rule in Rules.InRunOrder())
        {
            try
            {
                rule(ref damageInfo, context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[HurtRules] A rule failed and was skipped; the game is not affected");
            }
        }
    }
}
#endif
