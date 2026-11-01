/// <summary>
/// Actionable messages for runner input and output failures. Each message names the path and the fix.
/// </summary>
internal static class RunnerErrors
{
    internal static string MissingBuildContext(string path) =>
        $"BuildContext JSON not found: {path}. " +
        "The post phase needs the file written by the pre phase. Rebuild the project so that both phases run; " +
        "if the path is wrong, check the project's BaseIntermediateOutputPath.";

    internal static string MissingBuildProperties(string path) =>
        $"Build properties file not found: {path}. " +
        "GenerateBuildContext writes it before compilation. Rebuild the project; if it still fails, " +
        "make sure the Ducky.Sdk package is restored and that the build does not run the runner by hand.";

    internal static string MissingBuildOutput(string path) =>
        $"Build output not found: {path}. " +
        "Compilation did not produce this file. Fix the compiler errors above and rebuild the project, " +
        "or set AssemblyName/ModName so the output name matches.";
}
