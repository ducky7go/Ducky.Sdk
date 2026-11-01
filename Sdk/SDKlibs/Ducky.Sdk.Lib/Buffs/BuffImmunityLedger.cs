using System;
using System.Collections.Generic;

namespace Ducky.Sdk.Buffs;

/// <summary>
/// Which resistance tags are granted, how many grants each has, and whether the tag's entry in the game's resistance
/// list is the helper's own. It touches no game object, so the rules are tested with plain tags. The shell reads the
/// game list, and asks this ledger what to add, remove or put back.
/// </summary>
internal sealed class BuffImmunityLedger<TTag> where TTag : notnull
{
    private readonly TTag _notExclusive;
    private readonly Dictionary<TTag, Grant> _grants = new();

    public BuffImmunityLedger(TTag notExclusive) => _notExclusive = notExclusive;

    private sealed class Grant
    {
        public int Count;
        public bool Owned;
    }

    /// <summary>The tags that have at least one grant, in no particular order.</summary>
    public IReadOnlyList<TTag> GrantedTags
    {
        get
        {
            var tags = new List<TTag>(_grants.Count);
            foreach (var tag in _grants.Keys)
            {
                tags.Add(tag);
            }

            return tags;
        }
    }

    public bool IsGranted(TTag tag) => _grants.ContainsKey(tag);

    /// <summary>How many grants of the tag are alive.</summary>
    public int ReferenceCount(TTag tag) => _grants.TryGetValue(tag, out var grant) ? grant.Count : 0;

    /// <summary>True when the helper owns the tag's entry in the game's list, so disabling it may remove that entry.</summary>
    public bool Owns(TTag tag) => _grants.TryGetValue(tag, out var grant) && grant.Owned;

    /// <summary>Throws for <c>NotExclusive</c>, which cannot be resisted. Call before any state changes.</summary>
    /// <exception cref="ArgumentException">The tag is <c>NotExclusive</c>.</exception>
    public void ThrowIfNotExclusive(TTag tag)
    {
        if (EqualityComparer<TTag>.Default.Equals(tag, _notExclusive))
        {
            throw new ArgumentException("NotExclusive cannot be resisted: it does not describe a class of buffs.", nameof(tag));
        }
    }

    /// <summary>
    /// Records one grant. A new tag is owned by the helper unless <paramref name="presentInList"/> says the game's list
    /// already held it when the first grant came.
    /// </summary>
    public void Retain(TTag tag, bool presentInList)
    {
        if (_grants.TryGetValue(tag, out var grant))
        {
            grant.Count++;
            return;
        }

        _grants[tag] = new Grant { Count = 1, Owned = !presentInList };
    }

    /// <summary>
    /// Records that the helper put the tag into the game's list (on the first grant or after a level initializes). The
    /// entry is the helper's own from then on.
    /// </summary>
    public void MarkAdded(TTag tag)
    {
        if (_grants.TryGetValue(tag, out var grant))
        {
            grant.Owned = true;
        }
    }

    /// <summary>
    /// Records one release. Returns true when the tag has no grant left and the helper owns its entry, so the caller
    /// removes that entry. A release of a tag with no grant does nothing and returns false.
    /// </summary>
    public bool Release(TTag tag)
    {
        if (!_grants.TryGetValue(tag, out var grant))
        {
            return false;
        }

        grant.Count--;
        if (grant.Count > 0)
        {
            return false;
        }

        _grants.Remove(tag);
        return grant.Owned;
    }
}
