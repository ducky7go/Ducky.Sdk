using System;
using System.Collections.Generic;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Control;

/// <summary>
/// Counts stops of mouse aiming per owner and calls the game's block for the first hold of an owner and its unblock for the
/// last release, while the owner is still live. Owners that stop being live are forgotten with their count, so a reopened owner
/// blocks again. Holds a count per owner, not a global one. Touches no Unity type: liveness, block and unblock are delegates,
/// and the tests use fake owners.
/// </summary>
/// <remarks>
/// A throwing delegate never escapes: it is reported through <c>onError</c> and the ledger carries on, so handle disposal never
/// throws. The count of a hold is kept even when its block fails, so a later release still matches it.
/// </remarks>
internal sealed class AimBlockLedger<TOwner>
    where TOwner : class
{
    private readonly Func<TOwner, int> _keyOf;
    private readonly Func<TOwner, bool> _isLive;
    private readonly Action<TOwner> _block;
    private readonly Action<TOwner> _unblock;
    private readonly Action<Exception> _onError;
    private readonly HoldCounter<int> _holds = new();
    private readonly Dictionary<int, TOwner> _owners = new();

    /// <summary>Creates a ledger. All delegates run on the caller's thread.</summary>
    /// <param name="keyOf">A key that identifies the owner, for example its instance id.</param>
    /// <param name="isLive">True while the owner still counts as an active stop (for example non-null and active in the scene).</param>
    /// <param name="block">Called on the first hold of an owner.</param>
    /// <param name="unblock">Called on the last release of an owner while it is live.</param>
    /// <param name="onError">Receives any exception thrown by a delegate; the ledger continues.</param>
    public AimBlockLedger(
        Func<TOwner, int> keyOf,
        Func<TOwner, bool> isLive,
        Action<TOwner> block,
        Action<TOwner> unblock,
        Action<Exception> onError)
    {
        _keyOf = keyOf ?? throw new ArgumentNullException(nameof(keyOf));
        _isLive = isLive ?? throw new ArgumentNullException(nameof(isLive));
        _block = block ?? throw new ArgumentNullException(nameof(block));
        _unblock = unblock ?? throw new ArgumentNullException(nameof(unblock));
        _onError = onError ?? throw new ArgumentNullException(nameof(onError));
    }

    /// <summary>True while at least one hold of a live owner is counted. Forgets dead owners first.</summary>
    public bool IsStopped
    {
        get
        {
            ForgetDeadOwners();
            return _holds.Total > 0;
        }
    }

    /// <summary>
    /// Takes a hold on <paramref name="owner"/>. The first hold of an owner calls <c>block</c>. The returned handle releases this
    /// hold once; disposing it again, or after <see cref="ReleaseAll"/> or <see cref="Clear"/>, does nothing.
    /// </summary>
    public IDisposable Hold(TOwner owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        ForgetDeadOwners();
        var key = _keyOf(owner);
        if (_holds.Acquire(key, out var ticket))
        {
            _owners[key] = owner;
            Isolate(() => _block(owner));
        }

        return new DisposeOnce(() => Release(key, ticket));
    }

    /// <summary>
    /// Releases every hold at once. Calls <c>unblock</c> once for each owner that is still live, then forgets every hold, so the
    /// handles taken before it do nothing when disposed. Does nothing when no hold is counted.
    /// </summary>
    public void ReleaseAll()
    {
        ForgetDeadOwners();
        foreach (var owner in new List<TOwner>(_owners.Values))
        {
            if (IsLive(owner))
            {
                Isolate(() => _unblock(owner));
            }
        }

        Clear();
    }

    /// <summary>
    /// Forgets every hold without calling <c>unblock</c>, for a game state that is already gone (a level change). Handles taken
    /// before it release nothing afterwards.
    /// </summary>
    public void Clear()
    {
        _holds.Clear();
        _owners.Clear();
    }

    /// <summary>
    /// Forgets the count of every owner that is no longer live, without <c>unblock</c>: the game already dropped that owner.
    /// The next hold of such an owner is a first hold again and calls <c>block</c>.
    /// </summary>
    internal void ForgetDeadOwners()
    {
        List<int>? dead = null;
        foreach (var pair in _owners)
        {
            if (!IsLive(pair.Value))
            {
                (dead ??= new List<int>()).Add(pair.Key);
            }
        }

        if (dead == null)
        {
            return;
        }

        foreach (var key in dead)
        {
            _holds.Forget(key);
            _owners.Remove(key);
        }
    }

    private void Release(int key, HoldCounter<int>.Hold ticket)
    {
        if (!_holds.Release(ticket))
        {
            return;
        }

        if (!_owners.TryGetValue(key, out var owner))
        {
            return;
        }

        _owners.Remove(key);
        if (IsLive(owner))
        {
            Isolate(() => _unblock(owner));
        }
    }

    // A liveness check that throws counts as not live: the game drops such an owner on its own, so not blocking is the safe side.
    private bool IsLive(TOwner owner)
    {
        try
        {
            return _isLive(owner);
        }
        catch (Exception e)
        {
            _onError(e);
            return false;
        }
    }

    private void Isolate(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            _onError(e);
        }
    }
}
