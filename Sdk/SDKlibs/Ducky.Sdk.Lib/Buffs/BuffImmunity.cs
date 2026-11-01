using System;
using System.Collections.Generic;
using Duckov;
using Duckov.Buffs;
using Ducky.Sdk.GameApis;
using Ducky.Sdk.GameEvents;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Buffs;

/// <summary>
/// Immunity of the main character to a whole class of buffs (one exclusive tag). Granting it adds the tag to the
/// character's resistance list, removes the buffs that already carry the tag, and puts the tag back after every level
/// initializes, because the game clears the list on each scene change.
/// </summary>
/// <remarks>
/// Each compiled copy of the SDK has its own grants, so one mod's immunity never removes another mod's resistance.
/// Call from the main thread only: it reads and changes game objects. Each grant returns a handle; disposing it (more
/// than once is fine) releases that grant. The resistance entry is removed only when the helper itself added it, so an
/// entry that the game or another mod put there stays.
/// </remarks>
public static class BuffImmunity
{
    private static readonly BuffImmunityLedger<Buff.BuffExclusiveTags> Ledger =
        new(Buff.BuffExclusiveTags.NotExclusive);

    private static IDisposable? _levelSubscription;

    /// <summary>
    /// Grants immunity to every buff with <paramref name="tag"/>. Existing buffs with the tag are removed now, and the
    /// resistance is reapplied after each level initializes until the last grant is released.
    /// </summary>
    /// <returns>A handle that releases this grant when disposed.</returns>
    /// <exception cref="ArgumentException"><paramref name="tag"/> is <c>NotExclusive</c>; no state is changed.</exception>
    public static IDisposable Grant(Buff.BuffExclusiveTags tag)
    {
        Ledger.ThrowIfNotExclusive(tag);

        var main = CharacterMainControl.Main;
        var presentInList = main != null && (main.GetBuffResist()?.Contains(tag) ?? false);
        Ledger.Retain(tag, presentInList);

        if (main != null)
        {
            EnsureInList(main, tag);
            main.GetBuffManager()?.RemoveBuffsByTag(tag, false);
        }

        _levelSubscription ??= LevelEvents.OnAfterInitialized(Reapply);
        return new DisposeOnce(() => Release(tag));
    }

    // Puts the tag into the character's list when it is missing, and records that the helper added it.
    private static void EnsureInList(CharacterMainControl main, Buff.BuffExclusiveTags tag)
    {
        var list = main.GetBuffResist() ?? new List<Buff.BuffExclusiveTags>();
        if (!list.Contains(tag))
        {
            list.Add(tag);
            Ledger.MarkAdded(tag);
        }

        main.SetBuffResist(list);
    }

    // Runs after each level initializes: the game has cleared the list, so put back every granted tag.
    private static void Reapply()
    {
        var main = CharacterMainControl.Main;
        if (main == null)
        {
            Log.Info("[BuffImmunity] the character is not available yet; resistance is applied at the next level");
            return;
        }

        foreach (var tag in Ledger.GrantedTags)
        {
            EnsureInList(main, tag);
        }
    }

    private static void Release(Buff.BuffExclusiveTags tag)
    {
        if (Ledger.Release(tag))
        {
            CharacterMainControl.Main?.GetBuffResist()?.Remove(tag);
        }

        if (Ledger.GrantedTags.Count == 0)
        {
            _levelSubscription?.Dispose();
            _levelSubscription = null;
        }
    }
}
