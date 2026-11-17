namespace Ducky.Sdk.State;

/// <summary>
/// Throttles a search for scene objects: until it succeeds, the real search runs only once every
/// <see cref="IntervalSeconds"/> seconds. The clock is whatever the caller passes in, so it has no game type.
/// </summary>
public sealed class SearchThrottle
{
    /// <summary>The seconds between two real searches.</summary>
    public const float IntervalSeconds = 0.5f;

    private float _next;

    /// <summary>Returns true when a search may run now, and schedules the next one at <paramref name="now"/> plus <see cref="IntervalSeconds"/>.</summary>
    public bool TryBegin(float now)
    {
        if (now < _next)
        {
            return false;
        }

        _next = now + IntervalSeconds;
        return true;
    }
}
