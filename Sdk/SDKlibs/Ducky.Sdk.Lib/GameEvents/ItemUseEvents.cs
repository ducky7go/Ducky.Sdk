using System;
using Ducky.Sdk.Combat;
using Ducky.Sdk.Logging;
using ItemStatsSystem;

namespace Ducky.Sdk.GameEvents;

/// <summary>
/// Item-use events of the game as disposable subscriptions. Any number of handlers in one copy of the SDK share one
/// game-side subscription per event: it is made with the first handler and released with the last.
/// </summary>
public static class ItemUseEvents
{
    private static readonly HandlerSet<Item> StartUses = new(
        onFirst: () => CharacterMainControl.OnMainCharacterStartUseItem += RaiseStartUse,
        onLast: () => CharacterMainControl.OnMainCharacterStartUseItem -= RaiseStartUse,
        onFailure: ex => Log.Error(ex, "[ItemUseEvents] A start-use handler failed; other subscribers still run"));

    private static readonly HandlerSet<Item> Uses = new(
        onFirst: () => UsageUtilities.OnItemUsedStaticEvent += RaiseUsed,
        onLast: () => UsageUtilities.OnItemUsedStaticEvent -= RaiseUsed,
        onFailure: ex => Log.Error(ex, "[ItemUseEvents] A use-completed handler failed; other subscribers still run"));

    /// <summary>
    /// Runs <paramref name="handler"/> with the item when the main character starts using an item. Dispose the returned
    /// handle to unsubscribe; disposing it more than once has no further effect.
    /// </summary>
    public static IDisposable OnStartUse(Action<Item> handler) => StartUses.Add(handler);

    /// <summary>
    /// Runs <paramref name="handler"/> with the item when an item use finishes (the game's <c>UsageUtilities</c> event).
    /// Dispose the returned handle to unsubscribe; disposing it more than once has no further effect.
    /// </summary>
    public static IDisposable OnUsed(Action<Item> handler) => Uses.Add(handler);

    private static void RaiseStartUse(Item item) => StartUses.Raise(item);

    private static void RaiseUsed(Item item) => Uses.Raise(item);
}
