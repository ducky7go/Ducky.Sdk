namespace Ducky.Sdk.Contracts.ModProtocols;

/// <summary>
/// Which MessageHub host this assembly started. The first claim wins; only the holder can release it, so a host that
/// another mod started is never stopped. Plain C# with no Unity dependency, so the rule is unit tested directly.
/// </summary>
internal sealed class HostOwnership<T> where T : class
{
    private readonly object _sync = new();
    private T? _owned;

    /// <summary>Claims <paramref name="host"/> if nothing is claimed yet. Returns false when another host is already owned.</summary>
    public bool TryClaim(T host)
    {
        lock (_sync)
        {
            if (_owned != null)
            {
                return false;
            }

            _owned = host;
            return true;
        }
    }

    public bool IsOwned(T host)
    {
        lock (_sync)
        {
            return ReferenceEquals(_owned, host);
        }
    }

    /// <summary>Releases and returns the claimed host, or null when this assembly owns none.</summary>
    public T? Release()
    {
        lock (_sync)
        {
            var owned = _owned;
            _owned = null;
            return owned;
        }
    }
}
