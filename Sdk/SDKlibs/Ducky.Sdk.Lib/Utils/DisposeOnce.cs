using System;
using System.Threading;

namespace Ducky.Sdk.Utils;

/// <summary>Runs its action the first time it is disposed and does nothing after that. Touches no game object.</summary>
internal sealed class DisposeOnce : IDisposable
{
    private Action? _action;

    public DisposeOnce(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));

    public void Dispose()
    {
        var action = Interlocked.Exchange(ref _action, null);
        action?.Invoke();
    }
}
