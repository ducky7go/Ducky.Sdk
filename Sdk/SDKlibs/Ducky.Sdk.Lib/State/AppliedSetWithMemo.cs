using System.Collections;
using System.Collections.Generic;

namespace Ducky.Sdk.State;

/// <summary>
/// An <see cref="AppliedSet{TKey}"/> that also keeps what the feature noted about each key when it changed it: the value to put
/// back, the modifier it added, the handler it hooked up, a hit count, the time a state began. It is the state of the features
/// that keep "one entry per enemy" (a <c>Dictionary&lt;CharacterMainControl, ...&gt;</c> before), with no game type.
/// <para>
/// It is a straight wrapper of <see cref="Dictionary{TKey, TValue}"/> (default equality, the same enumeration order, the same
/// exception when the map changes during an enumeration of <see cref="Keys"/> or <see cref="Values"/>), so moving a field to it
/// changes nothing about the decisions the feature makes from it.
/// </para>
/// </summary>
/// <typeparam name="TKey">What the feature remembers (the game's character).</typeparam>
/// <typeparam name="TMemo">What the feature noted about it.</typeparam>
public sealed class AppliedSet<TKey, TMemo> : IEnumerable<KeyValuePair<TKey, TMemo>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TMemo> _entries = new();

    /// <summary>How many keys are remembered.</summary>
    public int Count => _entries.Count;

    /// <summary>The keys. The map must not change while they are enumerated.</summary>
    public Dictionary<TKey, TMemo>.KeyCollection Keys => _entries.Keys;

    /// <summary>The notes. The map must not change while they are enumerated.</summary>
    public Dictionary<TKey, TMemo>.ValueCollection Values => _entries.Values;

    /// <summary>The note of <paramref name="key"/>; setting it adds the key or replaces its note. Getting a missing key throws, as <see cref="Dictionary{TKey, TValue}"/> does.</summary>
    public TMemo this[TKey key]
    {
        get => _entries[key];
        set => _entries[key] = value;
    }

    /// <summary>Whether <paramref name="key"/> has been added and not removed since.</summary>
    public bool ContainsKey(TKey key) => _entries.ContainsKey(key);

    /// <summary>Reads the note of <paramref name="key"/>; false (and the default note) when the key is not remembered.</summary>
    public bool TryGetValue(TKey key, out TMemo memo) => _entries.TryGetValue(key, out memo!);

    /// <summary>Forgets <paramref name="key"/>. Returns false when it was not remembered.</summary>
    public bool Remove(TKey key) => _entries.Remove(key);

    /// <summary>Forgets every key.</summary>
    public void Clear() => _entries.Clear();

    /// <summary>A copy of the entries, in enumeration order, so a loop may add or remove keys while it runs.</summary>
    public List<KeyValuePair<TKey, TMemo>> Snapshot() => new(_entries);

    /// <summary>A copy of the keys, in enumeration order, so a loop may add or remove keys while it runs.</summary>
    public List<TKey> SnapshotKeys() => new(_entries.Keys);

    /// <summary>The entries, in the order <see cref="Dictionary{TKey, TValue}"/> yields them. The map must not change while it is enumerated.</summary>
    public Dictionary<TKey, TMemo>.Enumerator GetEnumerator() => _entries.GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TMemo>> IEnumerable<KeyValuePair<TKey, TMemo>>.GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();
}
