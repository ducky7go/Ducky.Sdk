using System;
using System.Collections.Generic;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Combat;

/// <summary>
/// A set of handlers for one event. The first handler added calls <c>onFirst</c> (the game-side subscription) and the
/// last one removed calls <c>onLast</c>. A handler that throws is reported to <c>onFailure</c>, and the others still
/// run. Touches no Unity object, so the dispatch rules are tested with plain arguments.
/// </summary>
internal sealed class HandlerSet<TArgs>
{
    private readonly Action _onFirst;
    private readonly Action _onLast;
    private readonly Action<Exception> _onFailure;
    private readonly List<Action<TArgs>> _handlers = new();

    public HandlerSet(Action onFirst, Action onLast, Action<Exception> onFailure)
    {
        _onFirst = onFirst;
        _onLast = onLast;
        _onFailure = onFailure;
    }

    /// <summary>How many handlers are registered.</summary>
    public int Count => _handlers.Count;

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

        if (_handlers.Count == 0)
        {
            _onFirst();
        }

        // a new delegate per registration, so removing one never removes another registration of the same method
        Action<TArgs> entry = args => handler(args);
        _handlers.Add(entry);
        return new DisposeOnce(() => Remove(entry));
    }

    /// <summary>Calls every handler with <paramref name="args"/>, in registration order.</summary>
    public void Raise(TArgs args)
    {
        if (_handlers.Count == 0)
        {
            return;
        }

        foreach (var handler in _handlers.ToArray())
        {
            try
            {
                handler(args);
            }
            catch (Exception ex)
            {
                _onFailure(ex);
            }
        }
    }

    private void Remove(Action<TArgs> entry)
    {
        if (!_handlers.Remove(entry))
        {
            return;
        }

        if (_handlers.Count == 0)
        {
            _onLast();
        }
    }
}
