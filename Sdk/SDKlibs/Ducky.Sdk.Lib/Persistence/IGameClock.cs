namespace Ducky.Sdk.Persistence;

/// <summary>The game's calendar clock, reduced to the day number that saved state is usually keyed by.</summary>
public interface IGameClock
{
    /// <summary>The current game day. May throw when the clock is not available; callers handle that.</summary>
    long Day { get; }
}
