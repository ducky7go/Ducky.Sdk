/// <summary>
/// Entry point of the Ducky.Sdk build runner (tools/net10.0/Ducky.Sdk.Build.dll).
/// Usage: dotnet Ducky.Sdk.Build.dll &lt;build-context.json&gt; --phase pre|post
///   pre  - after CoreCompile: generate the BuildContext JSON from the property file, then run the pre steps
///   post - after Build: update the BuildContext with compiled dependencies, then run the post steps
/// Exit codes: 0 success (a skipped step counts as success), 1 step or runtime failure, 2 invalid arguments.
/// </summary>
public static class Program
{
    internal const int ExitSuccess = 0;
    internal const int ExitFailure = 1;
    internal const int ExitUsage = 2;

    public static int Main(string[] args) => Run(args);

    internal static int Run(string[] args)
    {
        // Translation commands (locales set | sync | validate) run before the build-phase parser, which is unchanged.
        if (args.Length > 0 && args[0] == "locales")
        {
            return LocaleTool.Run(args[1..], Console.Out, Console.Error);
        }

        if (!BuildRunnerArguments.TryParse(args, out var parsed, out var error))
        {
            Console.Error.WriteLine($"[BuildRunner][ERROR] {error}");
            Console.Error.WriteLine(BuildRunnerArguments.Usage);
            return ExitUsage;
        }

        try
        {
            return parsed.Phase == BuildRunnerArguments.PrePhase
                ? BuildRunner.RunPre(parsed.BuildContextPath)
                : BuildRunner.RunPost(parsed.BuildContextPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[BuildRunner][ERROR] {ex.Message}");
            Console.Error.WriteLine($"[BuildRunner][ERROR] Stack trace: {ex.StackTrace}");
            return ExitFailure;
        }
    }
}

/// <summary>
/// Parsed command line: a build-context JSON path and exactly one phase.
/// </summary>
internal sealed class BuildRunnerArguments
{
    internal const string PrePhase = "pre";
    internal const string PostPhase = "post";

    internal const string Usage =
        "Usage: dotnet Ducky.Sdk.Build.dll <build-context.json> --phase pre|post";

    private BuildRunnerArguments(string buildContextPath, string phase)
    {
        BuildContextPath = buildContextPath;
        Phase = phase;
    }

    public string BuildContextPath { get; }
    public string Phase { get; }

    /// <summary>
    /// Parses <c>&lt;build-context.json&gt; --phase pre|post</c>. The phase is required: there is no default,
    /// so a phase is never run by accident.
    /// </summary>
    internal static bool TryParse(string[] args, out BuildRunnerArguments parsed, out string error)
    {
        parsed = null!;
        error = string.Empty;

        if (args.Length == 0)
        {
            error = "Missing <build-context.json> argument.";
            return false;
        }

        var path = args[0];
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith("--", StringComparison.Ordinal))
        {
            error = "The first argument must be the path of the BuildContext JSON file.";
            return false;
        }

        if (args.Length < 3 || args[1] != "--phase")
        {
            error = "Missing '--phase pre|post' after the BuildContext JSON path.";
            return false;
        }

        var phase = args[2];
        if (phase != PrePhase && phase != PostPhase)
        {
            error = $"Unknown phase '{phase}'. Use --phase pre or --phase post.";
            return false;
        }

        if (args.Length > 3)
        {
            error = $"Unexpected argument '{args[3]}'.";
            return false;
        }

        parsed = new BuildRunnerArguments(Path.GetFullPath(path), phase);
        return true;
    }
}
