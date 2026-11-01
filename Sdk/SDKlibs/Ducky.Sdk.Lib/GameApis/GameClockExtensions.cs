using System;

namespace Ducky.Sdk.GameApis;

/// <summary>Game clock calls, forwarded to <c>GameClock.Instance</c>.</summary>
public static class GameClockExtensions
{
    /// <summary>
    /// Steps the game clock until <paramref name="time"/> of the day. Forwards to <c>GameClock.Instance.StepTimeTil</c>,
    /// so the game clock must exist (it does once a level or the base is loaded).
    /// </summary>
    public static void StepTimeTil(TimeSpan time) => GameClock.Instance.StepTimeTil(time);
}
