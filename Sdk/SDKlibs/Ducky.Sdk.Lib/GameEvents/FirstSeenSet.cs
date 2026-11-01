using System.Collections.Generic;

namespace Ducky.Sdk.GameEvents;

/// <summary>
/// The keys that have already been seen once, for the "first time" callbacks. <see cref="TryMarkFirst"/> is true only for the
/// first call with a key, and a repeated key allocates nothing. Touches no game object.
/// </summary>
internal sealed class FirstSeenSet<TKey>
    where TKey : notnull
{
    private readonly HashSet<TKey> _seen = new();

    /// <summary>How many keys are recorded.</summary>
    public int Count => _seen.Count;

    /// <summary>Records <paramref name="key"/>. True only the first time this key is recorded.</summary>
    public bool TryMarkFirst(TKey key) => _seen.Add(key);

    /// <summary>Forgets every recorded key, so each one can be seen as first again.</summary>
    public void Clear() => _seen.Clear();
}
