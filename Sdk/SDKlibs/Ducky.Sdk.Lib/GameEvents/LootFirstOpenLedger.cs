using System.Runtime.CompilerServices;

namespace Ducky.Sdk.GameEvents;

/// <summary>
/// The <c>Looted</c> value of each box captured when the player starts interacting with it, so that the first-loot flag does
/// not depend on whether the game has already marked the box as looted during the loot-start dispatch. Each captured value is
/// used by one loot start only. The map is weak on the key, so a destroyed box is not kept alive by its entry. Touches no game
/// object, so the tests can use plain objects as keys.
/// </summary>
internal sealed class LootFirstOpenLedger<TBox>
    where TBox : class
{
    private ConditionalWeakTable<TBox, StrongBox<bool>> _captured = new();

    /// <summary>Captures <paramref name="looted"/> for <paramref name="box"/>, replacing any earlier value for it.</summary>
    public void Record(TBox box, bool looted)
    {
        if (box == null)
        {
            return;
        }

        _captured.Remove(box);
        _captured.Add(box, new StrongBox<bool>(looted));
    }

    /// <summary>
    /// The first-loot flag for a loot start of <paramref name="box"/>. When a value was captured, it is used and removed, so a
    /// later loot start of the same box falls back to the live value unless it was captured again. Without a captured value,
    /// the flag is decided from <paramref name="liveLooted"/>.
    /// </summary>
    public bool Resolve(TBox box, bool liveLooted)
    {
        if (box != null && _captured.TryGetValue(box, out var captured))
        {
            _captured.Remove(box);
            return !captured.Value;
        }

        return LootRules.IsFirstLoot(liveLooted);
    }

    /// <summary>Forgets every captured value.</summary>
    public void Clear()
    {
        // a new table instead of ConditionalWeakTable.Clear, which the game's runtime may not provide
        _captured = new ConditionalWeakTable<TBox, StrongBox<bool>>();
    }
}
