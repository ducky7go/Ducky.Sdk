namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Shared helpers for the per-lib tests: temp roots with spaces, console capture,
/// golden file lookup and a BuildContext builder that sets IsCIEnvironment explicitly.
/// </summary>
internal static class LibTestSupport
{
    /// <summary>Creates a fresh temp root. The "ducky runner libs" folder name contains spaces on purpose.</summary>
    public static string NewTempRoot(string label)
    {
        var root = Path.Combine(Path.GetTempPath(), "ducky runner libs", label, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>Deletes a temp root, restoring write permission on any folder a test locked down first.</summary>
    public static void DeleteTempRoot(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories).Prepend(root))
        {
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        Directory.Delete(root, recursive: true);
    }

    /// <summary>Path of a file under the copied Golden folder (bin output).</summary>
    public static string Golden(params string[] parts) =>
        Path.Combine(new[] { AppContext.BaseDirectory, "Golden" }.Concat(parts).ToArray());

    /// <summary>Normalizes Windows line endings so golden comparisons work on every platform.</summary>
    public static string NormalizeNewlines(string text) => text.Replace("\r\n", "\n");

    public static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    /// <summary>Runs an action while capturing Console.Out and Console.Error (BuildContext logs to both).</summary>
    public static (int Exit, string Out, string Err) Capture(Func<int> action)
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errWriter);
        try
        {
            var exit = action();
            return (exit, outWriter.ToString(), errWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    /// <summary>
    /// Builds a BuildContext for a project folder. IsCIEnvironment is always set from <paramref name="isCi"/>,
    /// so the result never depends on the CI / GITHUB_ACTIONS environment variables.
    /// </summary>
    public static BuildContext NewContext(
        string projectDir,
        string? modName = "Ducky.EntranceMod",
        string? assetsDir = null,
        bool isModLib = false,
        bool deployMod = true,
        bool enableIlRepack = false,
        bool isCi = false,
        string? duckovFolder = null,
        string? steamFolder = null,
        string? modsDirectory = null,
        string? managedDirectory = null,
        string? localizationAssetsDir = null,
        string? baseIntermediateOutputPath = null) => new()
    {
        ProjectDir = projectDir,
        Configuration = "Debug",
        TargetFramework = "netstandard2.1",
        IntermediateOutputPath = Path.Combine(projectDir, "obj", "Debug", "netstandard2.1") + "/",
        BaseIntermediateOutputPath = baseIntermediateOutputPath ?? Path.Combine(projectDir, "obj") + "/",
        OutputPath = Path.Combine(projectDir, "bin", "Debug", "netstandard2.1") + "/",
        ModName = modName,
        AssetsDir = assetsDir ?? Path.Combine(projectDir, "assets"),
        LocalizationAssetsDir = localizationAssetsDir,
        EnableILRepack = enableIlRepack,
        IsModLib = isModLib,
        DeployMod = deployMod,
        IsCIEnvironment = isCi,
        DuckovFolder = duckovFolder,
        SteamFolder = steamFolder,
        ModsDirectory_Explicit = modsDirectory,
        ManagedDirectory_Explicit = managedDirectory,
    };
}
