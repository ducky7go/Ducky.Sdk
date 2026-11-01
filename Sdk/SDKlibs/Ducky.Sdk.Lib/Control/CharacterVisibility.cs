using System;
using Ducky.Sdk.GameEvents;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Control;

/// <summary>
/// Hides non-player characters with reference counting: several effects can hide the same character, and it is shown again
/// only when the last of them releases its handle. Holds are forgotten on every level change.
/// </summary>
/// <remarks>
/// Hiding does not make AI blind or stop it from firing, and a direct call to the game's <c>Hide()</c> or <c>Show()</c> by other
/// code bypasses the count. The game's hide applies to non-player teams only; the player's pet is on the player team and cannot be
/// hidden.
/// </remarks>
public static class CharacterVisibility
{
    private static readonly HoldCounter<int> Holds = new();

    private static IDisposable? _levelReset;

    /// <summary>True when <paramref name="character"/> can be hidden: it is not on the player team (the game's own rule).</summary>
    public static bool CanHide(CharacterMainControl character) =>
        character != null && character.Team != Teams.player;

    /// <summary>
    /// Hides <paramref name="character"/> until the returned handle is disposed. The first hold hides it and the last release
    /// shows it again. For a character that cannot be hidden, returns a handle that does nothing and logs it. Disposing a handle
    /// more than once, or after a level change, has no further effect.
    /// </summary>
    public static IDisposable Hide(CharacterMainControl character)
    {
        if (character == null)
        {
            throw new ArgumentNullException(nameof(character));
        }

        if (!CanHide(character))
        {
            Log.Warn($"[CharacterVisibility] {character.name} is on the player team and cannot be hidden");
            return new DisposeOnce(() => { });
        }

        EnsureLevelReset();
        if (Holds.Acquire(character.GetInstanceID(), out var hold))
        {
            character.Hide();
        }

        return new DisposeOnce(() => Release(character, hold));
    }

    private static void Release(CharacterMainControl character, HoldCounter<int>.Hold hold)
    {
        if (!Holds.Release(hold))
        {
            return;
        }

        // the character may have been destroyed while it was hidden; nothing is shown then
        if (character != null)
        {
            character.Show();
        }

        ReleaseLevelResetIfIdle();
    }

    private static void EnsureLevelReset()
    {
        _levelReset ??= LevelEvents.OnInitialized(Holds.Clear);
    }

    private static void ReleaseLevelResetIfIdle()
    {
        if (Holds.Total == 0 && _levelReset != null)
        {
            _levelReset.Dispose();
            _levelReset = null;
        }
    }
}
