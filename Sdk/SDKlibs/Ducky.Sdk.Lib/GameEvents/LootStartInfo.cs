namespace Ducky.Sdk.GameEvents;

/// <summary>What a loot-start handler receives.</summary>
public readonly struct LootStartInfo
{
    /// <summary>Creates the information of one loot start.</summary>
    public LootStartInfo(InteractableLootbox box, bool isFirstLoot, bool isPet)
    {
        Box = box;
        IsFirstLoot = isFirstLoot;
        IsPet = isPet;
    }

    /// <summary>The box being looted.</summary>
    public InteractableLootbox Box { get; }

    /// <summary>
    /// True when the box had not been looted before this loot started, that is, before the game marked it as looted. The value
    /// is captured at interact start and does not depend on handler order; without a capture it falls back to the live read
    /// (see <c>LootRules.IsFirstLoot</c>).
    /// </summary>
    public bool IsFirstLoot { get; }

    /// <summary>True when the box is the player's pet inventory.</summary>
    public bool IsPet { get; }
}
