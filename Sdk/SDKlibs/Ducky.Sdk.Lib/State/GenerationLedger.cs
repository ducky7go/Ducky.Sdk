using System.Collections.Generic;

namespace Ducky.Sdk.State;

/// <summary>
/// The latest of several timers that act on the same key (an enemy slowed again by each hit): every <see cref="Begin"/> gets a new
/// generation number, and a timer that finishes only acts when its number is still the latest one of the key
/// (<see cref="TryFinish"/>), so an older timer does nothing after a newer one has restarted the state.
/// <para>The numbers only grow; <see cref="Clear"/> forgets the keys but not the counter.</para>
/// </summary>
/// <typeparam name="TKey">What the feature remembers (the game's character).</typeparam>
public sealed class GenerationLedger<TKey>
    where TKey : notnull
{
    private readonly AppliedSet<TKey, int> _latest = new();
    private int _next;

    /// <summary>How many keys have a running state.</summary>
    public int Count => _latest.Count;

    /// <summary>The keys that have a running state. The ledger must not change while they are enumerated.</summary>
    public Dictionary<TKey, int>.KeyCollection Keys => _latest.Keys;

    /// <summary>Starts (or restarts) the state of <paramref name="key"/> and returns its new generation, one more than the last one handed out.</summary>
    public int Begin(TKey key)
    {
        var generation = ++_next;
        _latest[key] = generation;
        return generation;
    }

    /// <summary>
    /// Ends the state of <paramref name="key"/> when <paramref name="generation"/> is still its latest one, and returns true. Returns
    /// false, and changes nothing, when the key has a newer generation or none (it was released).
    /// </summary>
    public bool TryFinish(TKey key, int generation)
    {
        if (_latest.TryGetValue(key, out var current) && current == generation)
        {
            _latest.Remove(key);
            return true;
        }

        return false;
    }

    /// <summary>Forgets every key (the generation counter keeps counting).</summary>
    public void Clear() => _latest.Clear();
}
