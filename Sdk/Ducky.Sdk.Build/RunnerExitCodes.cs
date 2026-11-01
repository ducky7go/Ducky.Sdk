// Project-level definitions for the build steps in Libs/ and Shared/.
// These files were written as dotnet-script sources, where SkipExitCode was a script-level global.
// Keep this the only definition of SkipExitCode in the runner.
global using static RunnerExitCodes;

/// <summary>
/// Exit code for build steps that are intentionally skipped.
/// The runner maps it to 0 so MSBuild does not treat a skipped step as a failure.
/// </summary>
public static class RunnerExitCodes
{
    public const int SkipExitCode = 36524;
}
