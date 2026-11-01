using System;

namespace Ducky.Sdk.GameEvents;

/// <summary>Level events of the game, as disposable subscriptions.</summary>
public static class LevelEvents
{
    /// <summary>
    /// Runs <paramref name="handler"/> each time the game raises <c>LevelManager.OnLevelInitialized</c>. Dispose the
    /// returned handle to unsubscribe, or add it to <c>Subscriptions</c> in a <c>ModBehaviourBase</c>.
    /// </summary>
    /// <remarks>
    /// O2 (resolved): the event is raised in <c>LevelManager.InitLevel</c> (a <c>UniTaskVoid</c>, decompiled at
    /// Newbe.Dod/decompiles/TeamSoda.Duckov.Core/LevelManager.cs) without any thread switch, so it runs on the main
    /// thread. The wrapper therefore does not marshal to the main thread.
    /// </remarks>
    public static IDisposable OnInitialized(Action handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return EventSubscription.Create<Action>(
            EventSubscription.Isolate(handler),
            add: h => LevelManager.OnLevelInitialized += h,
            remove: h => LevelManager.OnLevelInitialized -= h);
    }
}
