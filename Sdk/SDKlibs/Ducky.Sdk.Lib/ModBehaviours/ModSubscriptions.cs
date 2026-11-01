using System;
using System.Collections.Generic;
using System.Threading;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.ModBehaviours;

/// <summary>
/// Disposables and unsubscribe actions that live exactly as long as one enabled <see cref="ModBehaviourBase"/>.
/// <see cref="ModBehaviourBase"/> activates the container before <c>ModEnabled()</c> and disposes it right after
/// <c>ModDisabled()</c>, also when <c>ModDisabled()</c> throws and on hot reload. Items are disposed last-in,
/// first-out. One failing disposal is logged and does not stop the rest.
/// </summary>
/// <remarks>
/// An item added while the behaviour is not enabled is disposed immediately, so it cannot outlive the next disable.
/// The container is empty after each disposal and can be used again when the behaviour is enabled again.
/// </remarks>
public sealed class ModSubscriptions : IDisposable
{
    private readonly object _sync = new();
    private readonly List<IDisposable> _items = new();
    private bool _accepting;

    /// <summary>Created by <see cref="ModBehaviourBase"/> for each behaviour instance.</summary>
    internal ModSubscriptions()
    {
    }

    /// <summary>Whether the container is currently accepting items (between enable and disable).</summary>
    internal bool IsAccepting
    {
        get
        {
            lock (_sync)
            {
                return _accepting;
            }
        }
    }

    /// <summary>Starts accepting items. Called when the behaviour is enabled.</summary>
    internal void Activate()
    {
        lock (_sync)
        {
            _accepting = true;
        }
    }

    /// <summary>
    /// Adds a disposable that is disposed when the behaviour is disabled. Returns the same instance so the call can be
    /// used inline. Disposed immediately when the behaviour is not enabled.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="subscription"/> is null.</exception>
    public T Add<T>(T subscription) where T : IDisposable
    {
        if (subscription == null)
        {
            throw new ArgumentNullException(nameof(subscription));
        }

        bool accepted;
        lock (_sync)
        {
            accepted = _accepting;
            if (accepted)
            {
                _items.Add(subscription);
            }
        }

        if (!accepted)
        {
            DisposeItem(subscription);
        }

        return subscription;
    }

    /// <summary>
    /// Adds an unsubscribe action, run when the behaviour is disabled. Returns a disposable that runs the action once if
    /// disposed earlier by the caller.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="unsubscribe"/> is null.</exception>
    public IDisposable Add(Action unsubscribe)
    {
        if (unsubscribe == null)
        {
            throw new ArgumentNullException(nameof(unsubscribe));
        }

        return Add(new ActionDisposable(unsubscribe));
    }

    /// <summary>
    /// Stops accepting items and disposes everything added so far, last-in first-out. Safe to call more than once.
    /// Called by <see cref="ModBehaviourBase"/> when the behaviour is disabled.
    /// </summary>
    public void DisposeAll()
    {
        List<IDisposable> snapshot;
        lock (_sync)
        {
            _accepting = false;
            snapshot = new List<IDisposable>(_items);
            _items.Clear();
        }

        for (var i = snapshot.Count - 1; i >= 0; i--)
        {
            DisposeItem(snapshot[i]);
        }
    }

    /// <summary>Same as <see cref="DisposeAll"/>.</summary>
    public void Dispose() => DisposeAll();

    private static void DisposeItem(IDisposable item)
    {
        try
        {
            item.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ModSubscriptions] A subscription failed to dispose; the others are still disposed");
        }
    }

    private sealed class ActionDisposable : IDisposable
    {
        private Action? _action;

        public ActionDisposable(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            var action = Interlocked.Exchange(ref _action, null);
            action?.Invoke();
        }
    }
}
