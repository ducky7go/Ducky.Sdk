global using static BuildScriptGlobals;

/// <summary>
/// Stands in for script-level (csx) globals that the linked build scripts reference unqualified.
/// Keep in sync with SDKlibs/scripts/shared/BuildResult.cs.
/// </summary>
internal static class BuildScriptGlobals
{
    public const int SkipExitCode = 36524;
}
