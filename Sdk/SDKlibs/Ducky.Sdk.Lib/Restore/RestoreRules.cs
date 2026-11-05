#if DUCKY_SDK_HARMONY
using System;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using HarmonyLib;

namespace Ducky.Sdk.Restore;

/// <summary>
/// The shared restoration rules of this copy of the SDK. Rules rewrite the health, energy or water a character restores,
/// before the game scales it by its gain. Each kind has one prefix, installed with its first rule and removed with its last,
/// so one mod's rules never replace or remove another mod's. Rules are isolated: a throwing rule is logged and its change
/// is undone.
/// </summary>
/// <remarks>
/// Patches <c>CharacterMainControl.AddHealth</c>, <c>AddEnergy</c> and <c>AddWater</c>. Requires Harmony: the file is compiled
/// only when the mod includes it (<c>IncludeHarmony</c>).
/// </remarks>
public static class RestoreRules
{
    // One per loaded copy: a patch id that another mod's copy cannot share.
    private static readonly Guid InstanceId = Guid.NewGuid();

    private static readonly string AssemblyName = typeof(RestoreRules).Assembly.GetName().Name ?? "Ducky.Sdk";

    private static readonly RestoreRuleTable Table = new(
        onFirst: kind => () => Install(kind),
        onLast: kind => () => Uninstall(kind));

    /// <summary>
    /// Registers <paramref name="rule"/> for <paramref name="kind"/> at <paramref name="priority"/> (lower runs first, equal
    /// priority in registration order). Dispose the returned handle to remove this rule only.
    /// </summary>
    public static IDisposable Register(RestoreKind kind, RestoreRule rule, int priority = 0)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        // the overflow is recalculated for the value each rule sees, not the value the prefix started with
        return Table.For(kind).Add((ref float value, RestoreContext context) =>
            rule(ref value, context.ForAmount(value)), priority);
    }

    private static void Install(RestoreKind kind) =>
        HarmonyPatches.Install(RestoreRuleTable.PatchIdFor(AssemblyName, kind, InstanceId), harmony =>
        {
            var (method, prefix) = kind switch
            {
                RestoreKind.Health => (AccessTools.Method(typeof(CharacterMainControl), nameof(CharacterMainControl.AddHealth),
                    new[] { typeof(float) }), nameof(HealthPrefix)),
                RestoreKind.Energy => (AccessTools.Method(typeof(CharacterMainControl), nameof(CharacterMainControl.AddEnergy),
                    new[] { typeof(float) }), nameof(EnergyPrefix)),
                _ => (AccessTools.Method(typeof(CharacterMainControl), nameof(CharacterMainControl.AddWater),
                    new[] { typeof(float) }), nameof(WaterPrefix)),
            };
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(RestoreRules), prefix));
        }, $"[RestoreRules] could not install the prefix for {kind}");

    private static void Uninstall(RestoreKind kind) =>
        HarmonyPatches.Uninstall(RestoreRuleTable.PatchIdFor(AssemblyName, kind, InstanceId));

    // Harmony binds these by name: the parameter names are the parameter names of the game's methods.
    private static void HealthPrefix(CharacterMainControl __instance, ref float healthValue) =>
        ApplyRules(RestoreKind.Health, __instance, ref healthValue);

    private static void EnergyPrefix(CharacterMainControl __instance, ref float energyValue) =>
        ApplyRules(RestoreKind.Energy, __instance, ref energyValue);

    private static void WaterPrefix(CharacterMainControl __instance, ref float waterValue) =>
        ApplyRules(RestoreKind.Water, __instance, ref waterValue);

    private static void ApplyRules(RestoreKind kind, CharacterMainControl character, ref float value)
    {
        var registry = Table.For(kind);
        if (registry.Count == 0 || !TryBuildContext(kind, character, out var context))
        {
            return;
        }

        registry.Apply(ref value, context.ForAmount(value),
            ex => Log.Error(ex, "[RestoreRules] A rule failed and was skipped; the game is not affected"));
    }

    private static bool TryBuildContext(RestoreKind kind, CharacterMainControl character, out RestoreContext context)
    {
        var origin = RestoreOriginScope.IsActive ? RestoreOrigin.Mod : RestoreOrigin.Game;
        var health = character.Health;
        switch (kind)
        {
            case RestoreKind.Health when health != null:
                context = new RestoreContext(kind, character, origin, health.CurrentHealth, health.MaxHealth, character.HealGain, 0f);
                return true;
            case RestoreKind.Energy:
                context = new RestoreContext(kind, character, origin, character.CurrentEnergy, character.MaxEnergy, character.FoodGain, 0f);
                return true;
            case RestoreKind.Water:
                context = new RestoreContext(kind, character, origin, character.CurrentWater, character.MaxWater, character.FoodGain, 0f);
                return true;
            default:
                context = default;
                return false;
        }
    }
}
#endif
