#if DUCKY_SDK_HARMONY
using System;
using System.Collections.Generic;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using HarmonyLib;

namespace Ducky.Sdk.Combat;

/// <summary>
/// Lets a mod save a character from a hit that would kill it.
/// </summary>
/// <remarks>
/// The game decides death first (it sets <c>isDead</c>, fires <c>OnDead</c>, deactivates the character) and fires
/// <c>OnHurt</c> afterwards, so a handler on <c>OnHurt</c> is too late. This guard works at the moment the health would
/// be written as zero or less: a prefix on <c>Health.Hurt</c> remembers the hit, and a prefix on the
/// <c>Health.CurrentHealth</c> setter asks the registered rescues what health to keep instead. A rescue that keeps a
/// positive value saves the character. Damage the game ignores (invincible or dead targets) is never offered.
/// Each compiled copy of the SDK patches under its own identifier, so two mods never remove each other's patch.
/// Requires Harmony: the file is compiled only when the mod includes it (<c>IncludeHarmony</c>).
/// </remarks>
public static class LethalDamageGuard
{
    /// <summary>
    /// Called when <paramref name="health"/> is about to drop to 0 or below because of <paramref name="info"/>. Return the
    /// health to keep (above 0) to save the character, or null to decline. It must not throw and must change nothing when
    /// it declines.
    /// </summary>
    public delegate float? Rescue(Health health, DamageInfo info);

    private sealed class GuardedHit
    {
        public Health Health = null!;
        public DamageInfo Info;
        public bool Consulted;
    }

    // One per loaded copy: a patch id that another mod's copy cannot share.
    private static readonly Guid InstanceId = Guid.NewGuid();

    private static readonly string HarmonyId = PatchIds.For(
        typeof(LethalDamageGuard).Assembly.GetName().Name ?? "Ducky.Sdk", "LethalDamageGuard", InstanceId);

    // Installed on the first rescue, removed with the last one.
    private static readonly OrderedRules<Rescue> Rescues = new(onFirst: Install, onLast: Uninstall);

    // Hurt can nest (an effect may hurt someone from inside an event), so keep a stack.
    private static readonly List<GuardedHit> Active = new();

    /// <summary>Registers a rescue. Dispose the returned handle to remove it; disposing it again does nothing.</summary>
    public static IDisposable Register(Rescue rescue)
    {
        if (rescue == null)
        {
            throw new ArgumentNullException(nameof(rescue));
        }

        return Rescues.Add(rescue, 0);
    }

    private static void Install() =>
        HarmonyPatches.Install(HarmonyId, harmony =>
        {
            harmony.Patch(
                AccessTools.Method(typeof(Health), nameof(Health.Hurt), new[] { typeof(DamageInfo) }),
                prefix: new HarmonyMethod(typeof(LethalDamageGuard), nameof(HurtPrefix)),
                finalizer: new HarmonyMethod(typeof(LethalDamageGuard), nameof(HurtFinalizer)));
            harmony.Patch(
                AccessTools.PropertySetter(typeof(Health), nameof(Health.CurrentHealth)),
                prefix: new HarmonyMethod(typeof(LethalDamageGuard), nameof(SetCurrentHealthPrefix)));
        }, "[LethalDamageGuard] could not install its patches");

    private static void Uninstall()
    {
        HarmonyPatches.Uninstall(HarmonyId);
        Active.Clear();
    }

    // Harmony binds these by name: __instance is the Health, damageInfo is the parameter of Health.Hurt.
    private static void HurtPrefix(Health __instance, DamageInfo damageInfo)
    {
        Active.Add(new GuardedHit { Health = __instance, Info = damageInfo });
    }

    // A finalizer runs on every exit of Hurt, including early returns and exceptions, so the stack never leaks.
    private static void HurtFinalizer()
    {
        if (Active.Count > 0)
        {
            Active.RemoveAt(Active.Count - 1);
        }
    }

    private static void SetCurrentHealthPrefix(Health __instance, ref float value)
    {
        if (Active.Count == 0)
        {
            return;
        }

        var hit = Active[Active.Count - 1];
        // only the hit that is being processed right now, and only once per hit
        // (Hurt writes 0 again after the first lethal write when nobody saved the character)
        if (!ReferenceEquals(hit.Health, __instance)
            || !LethalDamageRules.ShouldAsk(value, hit.Consulted, __instance.Invincible, __instance.IsDead))
        {
            return;
        }

        hit.Consulted = true;
        foreach (var rescue in Rescues.InRunOrder())
        {
            try
            {
                var keep = rescue(__instance, hit.Info);
                if (LethalDamageRules.IsSaving(keep))
                {
                    value = keep!.Value;
                    return;
                }
            }
            catch (Exception ex)
            {
                // a broken rescue must never break the game's damage handling
                Log.Error(ex, "[LethalDamageGuard] A rescue failed; the game is not affected");
            }
        }
    }
}
#endif
