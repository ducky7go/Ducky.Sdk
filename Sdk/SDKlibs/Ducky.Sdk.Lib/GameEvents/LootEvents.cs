using System;
using Ducky.Sdk.Combat;
using Ducky.Sdk.Logging;
using ItemStatsSystem;

namespace Ducky.Sdk.GameEvents;

/// <summary>
/// Loot, pickup and player-inventory events of the game as disposable subscriptions. Handlers within one copy of the SDK share
/// one game-side subscription per event. Loot events leave out the player's pet unless a handler asks for it, because the
/// pet's inventory is also a loot box.
/// </summary>
public static class LootEvents
{
    private static readonly HandlerSet<LootStartInfo> LootStarts = new(
        onFirst: () => InteractableLootbox.OnStartLoot += RaiseLootStart,
        onLast: () => InteractableLootbox.OnStartLoot -= RaiseLootStart,
        onFailure: ex => Log.Error(ex, "[LootEvents] A loot-start handler failed; other subscribers still run"));

    private static readonly HandlerSet<(InteractableLootbox Box, bool IsPet)> LootStops = new(
        onFirst: () => InteractableLootbox.OnStopLoot += RaiseLootStop,
        onLast: () => InteractableLootbox.OnStopLoot -= RaiseLootStop,
        onFailure: ex => Log.Error(ex, "[LootEvents] A loot-stop handler failed; other subscribers still run"));

    private static readonly HandlerSet<PickupInfo> Pickups = new(
        onFirst: () => InteractablePickup.OnPickupSuccess += RaisePickup,
        onLast: () => InteractablePickup.OnPickupSuccess -= RaisePickup,
        onFailure: ex => Log.Error(ex, "[LootEvents] A pickup handler failed; other subscribers still run"));

    private static readonly HandlerSet<(Inventory Inventory, int Index)> PlayerInventory = new(
        onFirst: () => CharacterMainControl.OnMainCharacterInventoryChangedEvent += RaisePlayerInventory,
        onLast: () => CharacterMainControl.OnMainCharacterInventoryChangedEvent -= RaisePlayerInventory,
        onFailure: ex => Log.Error(ex, "[LootEvents] A player-inventory handler failed; other subscribers still run"));

    /// <summary>
    /// Runs <paramref name="handler"/> when a loot box starts being looted, with the box, whether it is the first loot of that
    /// box (<see cref="LootStartInfo.IsFirstLoot"/>, from <c>InteractableLootbox.Looted</c>) and whether it is the pet's. Pets are
    /// left out unless <paramref name="includePets"/> is true. Dispose the returned handle to unsubscribe.
    /// </summary>
    public static IDisposable OnLootStart(Action<LootStartInfo> handler, bool includePets = false)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return LootStarts.Add(info =>
        {
            if (LootRules.ShouldDeliver(info.IsPet, includePets))
            {
                handler(info);
            }
        });
    }

    /// <summary>
    /// Runs <paramref name="handler"/> with the box when the player stops looting it. Pets are left out unless
    /// <paramref name="includePets"/> is true. Dispose the returned handle to unsubscribe.
    /// </summary>
    public static IDisposable OnLootStop(Action<InteractableLootbox> handler, bool includePets = false)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return LootStops.Add(args =>
        {
            if (LootRules.ShouldDeliver(args.IsPet, includePets))
            {
                handler(args.Box);
            }
        });
    }

    /// <summary>
    /// Runs <paramref name="handler"/> when an object is picked up from the ground, with the pickup and the character that took
    /// it. Dispose the returned handle to unsubscribe.
    /// </summary>
    public static IDisposable OnPickup(Action<PickupInfo> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return Pickups.Add(handler);
    }

    /// <summary>
    /// Runs <paramref name="handler"/> with the inventory and the changed slot index when the player's inventory changes
    /// (the game's <c>OnMainCharacterInventoryChangedEvent</c> for the main character only). Dispose the returned handle to
    /// unsubscribe.
    /// </summary>
    public static IDisposable OnPlayerInventoryChanged(Action<Inventory, int> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return PlayerInventory.Add(args => handler(args.Inventory, args.Index));
    }

    /// <summary>True when <paramref name="box"/> is the player's pet inventory.</summary>
    public static bool IsPet(InteractableLootbox box)
    {
        var pet = PetProxy.PetInventory;
        return box != null && pet != null && box.Inventory == pet;
    }

    private static void RaiseLootStart(InteractableLootbox box)
    {
        // read once for every subscriber of this event
        var isFirstLoot = LootRules.IsFirstLoot(box.Looted);
        LootStarts.Raise(new LootStartInfo(box, isFirstLoot, IsPet(box)));
    }

    private static void RaiseLootStop(InteractableLootbox box) => LootStops.Raise((box, IsPet(box)));

    private static void RaisePickup(InteractablePickup pickup, CharacterMainControl character) =>
        Pickups.Raise(new PickupInfo(pickup, character));

    private static void RaisePlayerInventory(CharacterMainControl character, Inventory inventory, int index)
    {
        if (character != null && character.IsMainCharacter)
        {
            PlayerInventory.Raise((inventory, index));
        }
    }
}
