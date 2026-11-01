using System;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Combat;

/// <summary>
/// The one place where this copy of the SDK listens to <c>Health.OnHurt</c> and <c>Health.OnDead</c> for player hits and
/// kills. The game-side event is subscribed while at least one handler is registered and released with the last one.
/// Each event is filtered once with <see cref="DamageFromPlayerToEnemy"/>. A handler that throws is logged and does not
/// stop the other handlers or the game's damage code.
/// </summary>
/// <remarks>
/// Each compiled copy of the SDK has its own subscription, so one mod's handlers never remove another mod's.
/// Call from the main thread only.
/// </remarks>
public static class PlayerCombat
{
    private static readonly HandlerSet<(Health Health, DamageInfo Info)> Hits = new(
        onFirst: () => Health.OnHurt += RaiseHit,
        onLast: () => Health.OnHurt -= RaiseHit,
        onFailure: ex => Log.Error(ex, "[PlayerCombat] A hit handler failed; the other handlers still run"));

    private static readonly HandlerSet<(Health Health, DamageInfo Info)> Kills = new(
        onFirst: () => Health.OnDead += RaiseKill,
        onLast: () => Health.OnDead -= RaiseKill,
        onFailure: ex => Log.Error(ex, "[PlayerCombat] A kill handler failed; the other handlers still run"));

    /// <summary>
    /// Runs <paramref name="handler"/> each time the player hurts an enemy. The hit that kills the enemy also counts,
    /// because the game fires hurt after it decides on death.
    /// </summary>
    /// <returns>A handle that removes this handler when disposed; disposing it again does nothing.</returns>
    public static IDisposable OnHit(Action<Health, DamageInfo> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return Hits.Add(args => handler(args.Health, args.Info));
    }

    /// <summary>Runs <paramref name="handler"/> each time the player kills an enemy.</summary>
    /// <returns>A handle that removes this handler when disposed; disposing it again does nothing.</returns>
    public static IDisposable OnKill(Action<Health, DamageInfo> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return Kills.Add(args => handler(args.Health, args.Info));
    }

    /// <summary>
    /// True when the hit is the player hurting a character that is not the player. Damage with no attacker (an explosion,
    /// a trap) and damage to something that is not a character do not count.
    /// </summary>
    public static bool DamageFromPlayerToEnemy(Health h, DamageInfo info)
    {
        if (!h)
        {
            return false;
        }

        var victim = h.TryGetCharacter();
        var attacker = info.fromCharacter;
        return HitDirection.PlayerToEnemy(
            victim != null,
            attacker != null,
            victim != null && victim.IsMainCharacter,
            attacker != null && attacker.IsMainCharacter);
    }

    private static void RaiseHit(Health h, DamageInfo info) => Raise(Hits, h, info, "hit");

    private static void RaiseKill(Health h, DamageInfo info) => Raise(Kills, h, info, "kill");

    private static void Raise(HandlerSet<(Health Health, DamageInfo Info)> handlers, Health h, DamageInfo info, string what)
    {
        if (handlers.Count == 0)
        {
            return;
        }

        try
        {
            if (!DamageFromPlayerToEnemy(h, info))
            {
                return;
            }
        }
        catch (Exception e)
        {
            Log.Warn($"[PlayerCombat] could not read the {what}: {e.Message}");
            return;
        }

        handlers.Raise((h, info));
    }
}
