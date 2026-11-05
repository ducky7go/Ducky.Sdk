using System;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Combat;

/// <summary>
/// A set of handlers for one event. The first handler added calls <c>onFirst</c> (the game-side subscription) and the
/// last one removed calls <c>onLast</c>. A handler that throws is reported to <c>onFailure</c>, and the others still
/// run. Touches no Unity object, so the dispatch rules are tested with plain arguments.
/// </summary>
internal sealed class HandlerSet<TArgs>
{
    private readonly PrioritizedRegistry<Action<TArgs>> _registry;
    private readonly Action<Exception> _onFailure;

    public HandlerSet(Action onFirst, Action onLast, Action<Exception> onFailure)
    {
        _registry = new PrioritizedRegistry<Action<TArgs>>(onFirst, onLast);
        _onFailure = onFailure;
    }

    /// <summary>How many handlers are registered.</summary>
    public int Count => _registry.Count;

    /// <summary>
    /// Registers <paramref name="handler"/>. The returned handle removes this registration only; the same handler added
    /// twice is two registrations. Disposing the handle more than once has no further effect.
    /// </summary>
    public IDisposable Add(Action<TArgs> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        // a new delegate per registration, so removing one never removes another registration of the same method
        Action<TArgs> entry = args => handler(args);
        return _registry.Add(entry, 0);
    }

    /// <summary>Calls every handler with <paramref name="args"/>, in registration order.</summary>
    public void Raise(TArgs args)
    {
        foreach (var registration in _registry.Snapshot())
        {
            try
            {
                registration.Item(args);
            }
            catch (Exception ex)
            {
                _onFailure(ex);
            }
        }
    }
}
