using System;

namespace Ducky.Sdk.GameEvents;

/// <summary>The broad phase the game is in, derived from the active scene name.</summary>
public enum ScenePhase
{
    /// <summary>Neither the base nor a raid (menus, loading scenes, unknown names).</summary>
    Other,

    /// <summary>The base: the scene names start with <c>Base</c>.</summary>
    Base,

    /// <summary>A raid level: the scene names start with <c>Level</c>.</summary>
    Raid,
}

/// <summary>Pure classification of scene names into <see cref="ScenePhase"/>. Touches no Unity object.</summary>
public static class ScenePhases
{
    /// <summary>
    /// The game's main menu scene name, carried over unchanged from Newbe.Dod. Newbe.Dod's own comment says it is still
    /// to be confirmed by an in-game trace, so treat it as unverified until that trace exists.
    /// </summary>
    public const string MainMenuSceneName = "MainMenu";

    /// <summary>True for the main menu scene only. Matching is ordinal and case-sensitive.</summary>
    public static bool IsMainMenu(string? sceneName) => sceneName == MainMenuSceneName;

    /// <summary>
    /// The phase a scene name stands for. Matching is ordinal and case-sensitive: <c>base_x</c> is <see cref="ScenePhase.Other"/>.
    /// </summary>
    public static ScenePhase Of(string? sceneName)
    {
        if (sceneName == null)
        {
            return ScenePhase.Other;
        }

        if (sceneName.StartsWith("Base", StringComparison.Ordinal))
        {
            return ScenePhase.Base;
        }

        return sceneName.StartsWith("Level", StringComparison.Ordinal) ? ScenePhase.Raid : ScenePhase.Other;
    }
}
