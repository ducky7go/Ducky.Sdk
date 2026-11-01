using System.Collections.Generic;
using System.Threading;

namespace Ducky.Sdk.Control;

/// <summary>
/// A reference count per key, where each hold is a ticket. The first hold of a key reports that it started the count and the
/// last release reports that it ended it. Releasing a ticket twice, releasing a ticket from before <see cref="Clear"/>, or
/// releasing a ticket that was never issued changes nothing. Touches no game object.
/// </summary>
internal sealed class HoldCounter<TKey>
    where TKey : notnull
{
    /// <summary>A ticket for one hold. Keep it to release that hold.</summary>
    public readonly struct Hold
    {
        internal Hold(TKey key, int generation, long id)
        {
            Key = key;
            Generation = generation;
            Id = id;
        }

        internal TKey Key { get; }

        internal int Generation { get; }

        internal long Id { get; }
    }

    private readonly Dictionary<TKey, int> _counts = new();
    private readonly HashSet<long> _live = new();
    private int _generation;
    // unique across every counter of this key type, so a ticket from another counter never matches; 0 is never issued
    private static long _lastId;

    /// <summary>How many holds are live across all keys.</summary>
    public int Total => _live.Count;

    /// <summary>How many holds of <paramref name="key"/> are live.</summary>
    public int CountOf(TKey key) => _counts.TryGetValue(key, out var count) ? count : 0;

    /// <summary>
    /// Takes a hold on <paramref name="key"/>. Returns true when this is the first hold of that key, that is when the count went
    /// from zero to one.
    /// </summary>
    public bool Acquire(TKey key, out Hold hold)
    {
        var first = CountOf(key) == 0;
        _counts[key] = CountOf(key) + 1;
        var id = Interlocked.Increment(ref _lastId);
        _live.Add(id);
        hold = new Hold(key, _generation, id);
        return first;
    }

    /// <summary>
    /// Releases <paramref name="hold"/>. Returns true when this was the last hold of its key, that is when the count went from one
    /// to zero. Returns false for a ticket that is already released, from before the last <see cref="Clear"/>, or unknown.
    /// </summary>
    public bool Release(Hold hold)
    {
        if (hold.Generation != _generation || !_live.Remove(hold.Id))
        {
            return false;
        }

        var remaining = CountOf(hold.Key) - 1;
        if (remaining > 0)
        {
            _counts[hold.Key] = remaining;
            return false;
        }

        _counts.Remove(hold.Key);
        return true;
    }

    /// <summary>Forgets every hold. Tickets taken before this call release nothing afterwards.</summary>
    public void Clear()
    {
        _generation++;
        _counts.Clear();
        _live.Clear();
    }
}
