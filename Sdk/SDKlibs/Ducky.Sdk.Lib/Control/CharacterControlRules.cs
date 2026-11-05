#if DUCKY_SDK_HARMONY
using System;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Rules;
using Ducky.Sdk.Utils;
using HarmonyLib;
using UnityEngine;

namespace Ducky.Sdk.Control;

/// <summary>
/// The shared move and run input rules of this copy of the SDK. The rules replace what the game receives in
/// <c>CharacterMainControl.SetMoveInput</c> and <c>SetRunInput</c>, so they win over the input script that writes the input
/// every frame. Rules run by ascending priority, then registration order. A throwing rule is logged and its change undone.
/// Each of the two kinds has one prefix, installed with its first rule and removed with its last.
/// </summary>
/// <remarks>
/// The prefixes run for every character every frame; they exist only while a rule is registered. Not covered and not
/// affected: forced velocity (rolling), knockback, aim, shooting and reloading. Requires Harmony: the file is compiled only
/// when the mod includes it (<c>IncludeHarmony</c>).
/// </remarks>
public static class CharacterControlRules
{
    // One per loaded copy: a patch id that another mod's copy cannot share.
    private static readonly Guid InstanceId = Guid.NewGuid();

    private static readonly string AssemblyName = typeof(CharacterControlRules).Assembly.GetName().Name ?? "Ducky.Sdk";

    private static readonly RuleRegistry<Vector3, ControlContext> MoveRules = new(
        onFirst: InstallMove, onLast: UninstallMove);

    private static readonly RuleRegistry<bool, ControlContext> RunRules = new(
        onFirst: InstallRun, onLast: UninstallRun);

    /// <summary>
    /// Registers a move-input rule at <paramref name="priority"/> (lower runs first). Dispose the returned handle to remove this
    /// rule only.
    /// </summary>
    public static IDisposable RegisterMove(MoveInputRule rule, int priority = 0)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        return MoveRules.Add((ref Vector3 input, ControlContext context) => rule(ref input, context), priority);
    }

    /// <summary>
    /// Registers a run-input rule at <paramref name="priority"/> (lower runs first). Dispose the returned handle to remove this
    /// rule only.
    /// </summary>
    public static IDisposable RegisterRun(RunInputRule rule, int priority = 0)
    {
        if (rule == null)
        {
            throw new ArgumentNullException(nameof(rule));
        }

        return RunRules.Add((ref bool run, ControlContext context) => rule(ref run, context), priority);
    }

    private static void InstallMove() => Install(nameof(CharacterMainControl.SetMoveInput), "move",
        new[] { typeof(Vector3) }, nameof(MovePrefix));

    private static void InstallRun() => Install(nameof(CharacterMainControl.SetRunInput), "run",
        new[] { typeof(bool) }, nameof(RunPrefix));

    private static void UninstallMove() => Uninstall("move");

    private static void UninstallRun() => Uninstall("run");

    private static string IdFor(string feature) =>
        PatchIds.For(AssemblyName, "CharacterControlRules." + feature, InstanceId);

    private static void Install(string method, string feature, Type[] parameters, string prefix) =>
        HarmonyPatches.Install(IdFor(feature), harmony => harmony.Patch(
                AccessTools.Method(typeof(CharacterMainControl), method, parameters),
                prefix: new HarmonyMethod(typeof(CharacterControlRules), prefix)),
            $"[CharacterControlRules] could not install the {feature} prefix");

    private static void Uninstall(string feature) => HarmonyPatches.Uninstall(IdFor(feature));

    // Harmony binds these by name: the parameter names are those of the game's methods.
    private static void MovePrefix(CharacterMainControl __instance, ref Vector3 moveInput)
    {
        if (MoveRules.Count == 0)
        {
            return;
        }

        MoveRules.Apply(ref moveInput, new ControlContext(__instance, __instance == CharacterMainControl.Main),
            ex => Log.Error(ex, "[CharacterControlRules] A move rule failed and was skipped; the game is not affected"));
    }

    private static void RunPrefix(CharacterMainControl __instance, ref bool _runInput)
    {
        if (RunRules.Count == 0)
        {
            return;
        }

        RunRules.Apply(ref _runInput, new ControlContext(__instance, __instance == CharacterMainControl.Main),
            ex => Log.Error(ex, "[CharacterControlRules] A run rule failed and was skipped; the game is not affected"));
    }
}
#endif
