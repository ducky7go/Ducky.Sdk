#if DUCKY_SDK_HARMONY
using System;
using Ducky.Sdk.Combat;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Threading;
using Ducky.Sdk.Utils;
using HarmonyLib;

namespace Ducky.Sdk.GameEvents;

/// <summary>What is known about a spawned enemy once it is ready.</summary>
public readonly struct EnemyReadyInfo
{
    /// <summary>Creates the info.</summary>
    public EnemyReadyInfo(CharacterMainControl character, bool hasGun)
    {
        Character = character;
        HasGun = hasGun;
    }

    /// <summary>The enemy.</summary>
    public CharacterMainControl Character { get; }

    /// <summary>True when the main weapon slot holds a gun. The gun in the hand is not used.</summary>
    public bool HasGun { get; }
}

/// <summary>
/// Calls a handler once for each spawned enemy whose AI controller and main weapon-slot item exist. The game's spawn hook
/// (<c>CharacterMainControl.SetTeam</c>) runs before those exist, so the check waits a few frames on the main thread, at most
/// <see cref="DefaultMaxFrames"/> of them. The player's team is skipped. Handlers run on the main thread.
/// </summary>
/// <remarks>
/// Requires Harmony: the file is compiled only when the mod includes it (<c>IncludeHarmony</c>). Each compiled copy patches
/// under its own identifier and removes the patch with the last subscription.
/// </remarks>
public static class EnemyReady
{
    /// <summary>How many frames an enemy may take to become ready before the wait gives up.</summary>
    public const int DefaultMaxFrames = 10;

    // One per loaded copy: a patch id that another mod's copy cannot share.
    private static readonly Guid InstanceId = Guid.NewGuid();

    private static readonly string HarmonyId = PatchIds.For(
        typeof(EnemyReady).Assembly.GetName().Name ?? "Ducky.Sdk", "EnemyReady", InstanceId);

    // Installed with the first subscription, removed with the last one.
    private static readonly HandlerSet<EnemyReadyInfo> Handlers = new(
        onFirst: Install,
        onLast: Uninstall,
        onFailure: ex => Log.Error(ex, "[EnemyReady] A handler failed; the other handlers still run"));

    // Bumped on every install and uninstall, so a wait started earlier stops once the subscription has changed.
    private static int _generation;

    /// <summary>
    /// Runs <paramref name="handler"/> once for each enemy that becomes ready after this call. Dispose the handle to stop;
    /// disposing it again does nothing.
    /// </summary>
    public static IDisposable Subscribe(Action<EnemyReadyInfo> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return Handlers.Add(handler);
    }

    private static void Install()
    {
        // installed on the main thread, so the scheduler's holder object can be created here
        MainThread.EnsureStarted();
        if (HarmonyPatches.Install(HarmonyId, harmony => harmony.Patch(
                AccessTools.Method(typeof(CharacterMainControl), nameof(CharacterMainControl.SetTeam)),
                postfix: new HarmonyMethod(typeof(EnemyReady), nameof(SetTeamPostfix))),
            "[EnemyReady] could not install its patch"))
        {
            _generation++;
        }
    }

    private static void Uninstall()
    {
        HarmonyPatches.Uninstall(HarmonyId);
        _generation++;
    }

    // Harmony binds __instance by name: the character whose team was just set.
    private static void SetTeamPostfix(CharacterMainControl __instance)
    {
        if (__instance == null)
        {
            return;
        }

        var generation = _generation;
        MainThread.Post(() => Check(__instance, generation, 0));
    }

    private static void Check(CharacterMainControl enemy, int generation, int framesWaited)
    {
        if (generation != _generation || Handlers.Count == 0)
        {
            return; // unsubscribed since this wait began
        }

        if (enemy == null)
        {
            Log.Info("[EnemyReady] An enemy was destroyed before it was ready; no handler is called.");
            return;
        }

        var slotItem = enemy.PrimWeaponSlot()?.Content;
        var step = EnemyReadiness.Decide(
            enemy.IsMainCharacter,
            enemy.Health != null && !enemy.Health.IsDead,
            enemy.aiCharacterController != null,
            slotItem != null,
            framesWaited,
            DefaultMaxFrames);

        switch (step)
        {
            case ReadyStep.Ready:
                var hasGun = EnemyReadiness.HasGun(slotItem!.GetComponent<ItemSetting_Gun>() != null, holdingGun: false);
                Handlers.Raise(new EnemyReadyInfo(enemy, hasGun));
                break;
            case ReadyStep.Wait:
                MainThread.Post(() => Check(enemy, generation, framesWaited + 1));
                break;
            case ReadyStep.GiveUp:
                Log.Info($"[EnemyReady] {enemy.name} was not ready after {framesWaited} frames; no handler is called.");
                break;
            case ReadyStep.Skip:
                break;
        }
    }
}
#endif
