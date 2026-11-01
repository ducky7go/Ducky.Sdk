namespace Ducky.Sdk.GameEvents;

/// <summary>
/// The decision of a loot event, with no game object. A loot box is delivered to a handler unless it is the player's pet and
/// the handler did not ask for pets. The first-loot flag is taken from whether the box was looted before, which is the
/// <c>InteractableLootbox.Looted</c> value at the moment the loot starts.
/// </summary>
/// <remarks>
/// <c>Inventory.hasBeenInspectedInLootBox</c> is not used: it becomes true only after the loot stops, so it cannot say
/// whether the box was opened for the first time when the loot starts.
/// </remarks>
internal static class LootRules
{
    /// <summary>Whether a box with this pet status goes to a handler that asked for <paramref name="includePets"/>.</summary>
    public static bool ShouldDeliver(bool isPet, bool includePets) => includePets || !isPet;

    /// <summary>True exactly when the box was not looted before the loot starts.</summary>
    public static bool IsFirstLoot(bool looted) => !looted;
}
