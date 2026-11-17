using System.Collections;
using System.Collections.Generic;

namespace Ducky.Sdk.State;

/// <summary>
/// The set of things (usually enemies) a mod feature has already changed, so that a repeated report changes nothing a second
/// time and the feature knows what to put back when it is switched off. It has no game type: the key is the game's character
/// in production (<c>CharacterMainControl</c>) and a plain object in a test.
/// <para>
/// It is a straight wrapper of <see cref="HashSet{T}"/> (default equality, the same enumeration order), so moving a
/// <c>HashSet&lt;CharacterMainControl&gt;</c> field to it changes nothing about when a key counts as present. One thing it does
/// not and cannot do: the game's "destroyed" check (<c>character == null</c> on a Unity object). That check stays in the
/// caller, which is compiled against the game; the set only remembers keys.
/// </para>
/// </summary>
/// <typeparam name="TKey">What the feature remembers (the game's character).</typeparam>
public sealed class AppliedSet<TKey> : IEnumerable<TKey>
    where TKey : notnull
{
    private readonly HashSet<TKey> _keys = new();

    /// <summary>How many keys are remembered.</summary>
    public int Count => _keys.Count;

    /// <summary>Whether <paramref name="key"/> has been added and not removed since.</summary>
    public bool Contains(TKey key) => _keys.Contains(key);

    /// <summary>Remembers <paramref name="key"/>. Returns false, and changes nothing, when it is already remembered.</summary>
    public bool Add(TKey key) => _keys.Add(key);

    /// <summary>Forgets <paramref name="key"/>. Returns false when it was not remembered.</summary>
    public bool Remove(TKey key) => _keys.Remove(key);

    /// <summary>Forgets every key.</summary>
    public void Clear() => _keys.Clear();

    /// <summary>Remembers every key of <paramref name="keys"/> (the keys already remembered stay).</summary>
    public void UnionWith(IEnumerable<TKey> keys) => _keys.UnionWith(keys);

    /// <summary>A copy of the keys, in enumeration order, so a loop may add or remove keys while it runs.</summary>
    public List<TKey> Snapshot() => new(_keys);

    /// <summary>The keys, in the order <see cref="HashSet{T}"/> yields them. The set must not change while it is enumerated.</summary>
    public HashSet<TKey>.Enumerator GetEnumerator() => _keys.GetEnumerator();

    IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => _keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _keys.GetEnumerator();
}
