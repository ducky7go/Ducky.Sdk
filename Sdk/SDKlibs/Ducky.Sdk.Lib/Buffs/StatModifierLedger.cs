using System;
using System.Collections.Generic;

namespace Ducky.Sdk.Buffs;

/// <summary>
/// The modifiers one buff applied, kept as (stat, modifier) pairs, so the buff can take exactly those back. It knows
/// nothing about the game: the caller supplies the remove action. Touches no Unity object, so the add and remove rules
/// are tested with plain types.
/// </summary>
internal sealed class StatModifierLedger<TStat, TModifier>
{
    private readonly List<(TStat Stat, TModifier Modifier)> _applied = new();

    /// <summary>How many modifiers are recorded.</summary>
    public int Count => _applied.Count;

    /// <summary>Records one applied modifier.</summary>
    public void Add(TStat stat, TModifier modifier) => _applied.Add((stat, modifier));

    /// <summary>
    /// Passes every recorded pair to <paramref name="remove"/>, in the order they were added, then forgets them. Modifiers
    /// that were not recorded are never passed, so they stay where they are.
    /// </summary>
    public void RemoveAll(Action<TStat, TModifier> remove)
    {
        var pending = _applied.ToArray();
        _applied.Clear();
        foreach (var (stat, modifier) in pending)
        {
            remove(stat, modifier);
        }
    }
}
