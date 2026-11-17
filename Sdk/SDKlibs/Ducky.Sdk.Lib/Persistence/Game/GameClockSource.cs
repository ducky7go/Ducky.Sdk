using Duckov;

namespace Ducky.Sdk.Persistence;

/// <summary>The production <see cref="IGameClock"/>: reads the game clock's day at every call, nothing cached.</summary>
public sealed class GameClockSource : IGameClock
{
    /// <inheritdoc />
    public long Day => GameClock.Day;
}
