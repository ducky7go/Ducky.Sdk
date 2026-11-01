using System;
using System.Threading;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.GameEvents;

/// <summary>
/// A subscription to a game event made through explicit add and remove delegates, so the registration logic can be
/// tested with a fake event. Disposing removes the handler once; later calls do nothing.
/// </summary>
internal sealed class EventSubscription : IDisposable
{
    private Action? _remove;

    private EventSubscription(Action remove)
    {
        _remove = remove;
    }

    /// <summary>Registers <paramref name="handler"/> through <paramref name="add"/> and returns the subscription.</summary>
    internal static EventSubscription Create<TDelegate>(TDelegate handler, Action<TDelegate> add, Action<TDelegate> remove)
        where TDelegate : class
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        add(handler);
        return new EventSubscription(() => remove(handler));
    }

    /// <summary>
    /// Wraps a handler so that its exception is logged instead of reaching the game's event raiser, so the other
    /// subscribers of the event still run.
    /// </summary>
    internal static Action Isolate(Action handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return () =>
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GameEvents] A handler failed; other subscribers still run");
            }
        };
    }

    public void Dispose()
    {
        var remove = Interlocked.Exchange(ref _remove, null);
        remove?.Invoke();
    }
}
