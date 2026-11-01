#if DUCKY_SDK_HARMONY
using System;
using Ducky.Sdk.Combat;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using HarmonyLib;

namespace Ducky.Sdk.GameEvents;

/// <summary>
/// The first time each enemy notices the player, as a one-time callback. The game calls
/// <c>AICharacterController.SetNoticedToTarget</c> about 20 times a second per enemy while it sees the player, so a postfix
/// records each enemy instance once. The record is cleared on every level change.
/// </summary>
/// <remarks>
/// Discovery through sound or through being hurt does not go through this hook and does not raise the callback. Requires
/// Harmony: the file is compiled only when the mod includes it (<c>IncludeHarmony</c>).
/// </remarks>
public static class EnemyNoticed
{
    // One per loaded copy: a patch id that another mod's copy cannot share.
    private static readonly Guid InstanceId = Guid.NewGuid();

    private static readonly string HarmonyId = PatchIds.For(
        typeof(EnemyNoticed).Assembly.GetName().Name ?? "Ducky.Sdk", "EnemyNoticed", InstanceId);

    private static readonly FirstSeenSet<int> Seen = new();

    private static readonly HandlerSet<EnemyNoticedInfo> Handlers = new(
        onFirst: Attach,
        onLast: Detach,
        onFailure: ex => Log.Error(ex, "[EnemyNoticed] A handler failed; other subscribers still run"));

    private static Harmony? _harmony;
    private static IDisposable? _levelReset;

    /// <summary>
    /// Runs <paramref name="handler"/> once per enemy, the first time that enemy notices the player. Dispose the returned handle
    /// to unsubscribe; disposing it more than once has no further effect.
    /// </summary>
    public static IDisposable OnFirstNoticed(Action<EnemyNoticedInfo> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return Handlers.Add(handler);
    }

    private static void Attach()
    {
        _levelReset = LevelEvents.OnInitialized(Seen.Clear);

        var harmony = new Harmony(HarmonyId);
        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(AICharacterController), nameof(AICharacterController.SetNoticedToTarget),
                    new[] { typeof(DamageReceiver) }),
                postfix: new HarmonyMethod(typeof(EnemyNoticed), nameof(NoticedPostfix)));
            _harmony = harmony;
        }
        catch (Exception ex)
        {
            harmony.UnpatchAll(HarmonyId); // never leave half of it in place
            Log.Error(ex, "[EnemyNoticed] could not install its postfix");
        }
    }

    private static void Detach()
    {
        _harmony?.UnpatchAll(HarmonyId);
        _harmony = null;
        _levelReset?.Dispose();
        _levelReset = null;
        Seen.Clear();
    }

    // Harmony binds these by name: __instance is the AI controller, target is the parameter of SetNoticedToTarget.
    private static void NoticedPostfix(AICharacterController __instance, DamageReceiver target)
    {
        var player = CharacterMainControl.Main;
        if (target == null || player == null || target != player.mainDamageReceiver)
        {
            return; // only the player counts, and a call for another target does not mark the enemy
        }

        var enemy = __instance.CharacterMainControl;
        if (enemy == null || !Seen.TryMarkFirst(enemy.GetInstanceID()))
        {
            return;
        }

        Handlers.Raise(new EnemyNoticedInfo(enemy, target));
    }
}
#endif
