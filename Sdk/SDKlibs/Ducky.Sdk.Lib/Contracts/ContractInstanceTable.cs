using System;
using System.Collections.Generic;
using System.Linq;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// The per-mod table of client contract instances of one mod copy. Holds no Unity types, so the rules of
/// <c>GetOrCreate</c>, owner-aware removal and release can be tested without Unity. The contracts pass Unity's null
/// check as <c>isLive</c>.
/// </summary>
internal sealed class ContractInstanceTable<T> where T : class
{
    private readonly Dictionary<string, T> _instances = new();

    /// <summary>Returns the live instance for <paramref name="modId"/>, or creates and records a new one.</summary>
    public T GetOrAdd(string modId, Func<string, T> create, Func<T, bool> isLive)
    {
        if (_instances.TryGetValue(modId, out var existing) && isLive(existing))
        {
            return existing;
        }

        var created = create(modId);
        _instances[modId] = created;
        return created;
    }

    /// <summary>Removes the record only when it still points to <paramref name="instance"/>. Returns true when removed.</summary>
    public bool RemoveIfOwned(string modId, T instance)
    {
        if (_instances.TryGetValue(modId, out var current) && ReferenceEquals(current, instance))
        {
            _instances.Remove(modId);
            return true;
        }

        return false;
    }

    /// <summary>Returns every recorded instance and empties the table. A second call returns nothing.</summary>
    public List<T> TakeAll()
    {
        var all = _instances.Values.ToList();
        _instances.Clear();
        return all;
    }
}
