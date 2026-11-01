/// <summary>
/// Runs the build steps of one phase, in the order the former dotnet-script pipeline used.
/// Every step that used to be its own dotnet-script process now runs inside the phase's process.
/// </summary>
internal static class BuildRunner
{
    /// <summary>
    /// Pre phase steps after CoreCompile, in legacy order. Context generation runs before them (see <see cref="RunPre"/>).
    /// </summary>
    internal static readonly string[] PreSteps =
    [
        "InitializeBuildLoggingLib",
        "ValidateProjectPathLib",
        "ResolveSdkPropertiesLib",
        "ExtractLocalizationKeysLib",
        "UpdateLocalizationCsvLib",
        "CopyLocalizationAssetsLib",
        "EnsureInfoIniLib",
        "GeneratePreviewLib",
        "ValidateDuckovFolderLib",
        "CollectFromModLib",
    ];

    /// <summary>
    /// Post phase steps after Build, in legacy order. The BuildContext update runs before them (see <see cref="RunPost"/>).
    /// </summary>
    internal static readonly string[] PostSteps =
    [
        "ResetDeployLib",
        "ILRepackAssembliesLib",
        "CopyDependenciesLib",
        "DeployModLib",
    ];

    /// <summary>
    /// Pre phase steps of a hot reload build: the legacy steps, then the shim source (HotReloadShimStep.PrepareSource).
    /// Default builds run <see cref="PreSteps"/> only, so their step list is unchanged.
    /// </summary>
    internal static readonly string[] HotReloadPreSteps = [.. PreSteps, "HotReloadShimSourceStep"];

    /// <summary>
    /// Post phase steps of a hot reload build. The stale Core cleanup runs before ILRepack so the merge never reads an
    /// older Core; the shim is deployed last. Default builds run <see cref="PostSteps"/> only.
    /// </summary>
    internal static readonly string[] HotReloadPostSteps =
    [
        "ResetDeployLib",
        "HotReloadCleanupStep",
        "ILRepackAssembliesLib",
        "CopyDependenciesLib",
        "DeployModLib",
        "HotReloadShimDeployStep",
    ];

    internal static readonly IReadOnlyDictionary<string, Func<BuildContext, int>> StepTable =
        new Dictionary<string, Func<BuildContext, int>>(StringComparer.Ordinal)
        {
            ["InitializeBuildLoggingLib"] = InitializeBuildLoggingLib.Execute,
            ["ValidateProjectPathLib"] = ValidateProjectPathLib.Execute,
            ["ResolveSdkPropertiesLib"] = ResolveSdkPropertiesLib.Execute,
            ["ExtractLocalizationKeysLib"] = ExtractLocalizationKeysLib.Execute,
            ["UpdateLocalizationCsvLib"] = UpdateLocalizationCsvLib.Execute,
            ["CopyLocalizationAssetsLib"] = CopyLocalizationAssetsLib.Execute,
            ["EnsureInfoIniLib"] = EnsureInfoIniLib.Execute,
            ["GeneratePreviewLib"] = GeneratePreviewLib.Execute,
            ["ValidateDuckovFolderLib"] = ValidateDuckovFolderLib.Execute,
            ["CollectFromModLib"] = CollectFromModLib.Execute,
            ["ResetDeployLib"] = ResetDeployLib.Execute,
            ["ILRepackAssembliesLib"] = ILRepackAssembliesLib.Execute,
            ["CopyDependenciesLib"] = CopyDependenciesLib.Execute,
            ["DeployModLib"] = DeployModLib.Execute,
            ["HotReloadShimSourceStep"] = HotReloadShimStep.PrepareSource,
            ["HotReloadCleanupStep"] = HotReloadCleanupStep.Execute,
            ["HotReloadShimDeployStep"] = HotReloadShimStep.Deploy,
        };

    /// <summary>
    /// Pre phase: writes obj/ducky-build-context.json from the property file, then runs the pre steps.
    /// </summary>
    internal static int RunPre(string buildContextPath)
    {
        var propertiesPath = Path.Combine(Path.GetDirectoryName(buildContextPath)!,
            BuildContextGenerationStep.PropertiesFileName);

        var generated = BuildContextGenerationStep.Execute(propertiesPath, buildContextPath);
        if (generated != Program.ExitSuccess)
        {
            return Program.ExitFailure;
        }

        var context = LoadContext(buildContextPath);
        var buildResult = BuildResult.LoadOrCreate(context.ProjectDir);
        return RunSteps(context.HotReloadEnabled ? HotReloadPreSteps : PreSteps, context, buildResult, StepTable);
    }

    /// <summary>
    /// Post phase: updates the BuildContext from the compiled outputs, runs the post steps, then prints the result.
    /// Printing is never fatal.
    /// </summary>
    internal static int RunPost(string buildContextPath, Func<BuildContext, BuildResult, int>? printResult = null)
    {
        if (!File.Exists(buildContextPath))
        {
            Console.Error.WriteLine($"[BuildRunner][ERROR] {RunnerErrors.MissingBuildContext(buildContextPath)}");
            return Program.ExitFailure;
        }

        var context = LoadContext(buildContextPath);
        var buildResult = BuildResult.LoadOrCreate(context.ProjectDir);

        var updated = UpdateBuildContextAfterBuildStep.Execute(context, buildContextPath);
        if (updated != Program.ExitSuccess)
        {
            return Program.ExitFailure;
        }

        if (!RequireDeployableOutput(context))
        {
            return Program.ExitFailure;
        }

        var code = RunSteps(context.HotReloadEnabled ? HotReloadPostSteps : PostSteps, context, buildResult, StepTable);
        if (code != Program.ExitSuccess)
        {
            return code;
        }

        // The next reset removes only the files this successful build no longer plans to write.
        DeployManifest.Write(context);

        PrintResultNonFatal(context, buildResult, printResult ?? PrintResultLib.Execute);
        return Program.ExitSuccess;
    }

    /// <summary>
    /// Fails before ResetDeployLib wipes the mod folder when DeployModLib would have no DLL to deploy.
    /// The path is the one DeployModLib checks, so this only moves the same failure earlier and names the path.
    /// </summary>
    private static bool RequireDeployableOutput(BuildContext context)
    {
        if (!context.DeployMod || context.IsModLib || string.IsNullOrEmpty(context.ModName))
        {
            return true;
        }

        var requiredDll = Path.Combine(context.ProjectDir, "bin", context.Configuration ?? "Debug",
            context.TargetFramework ?? "netstandard2.1", context.MainOutputFileName);
        if (File.Exists(requiredDll))
        {
            return true;
        }

        Console.Error.WriteLine($"[BuildRunner][ERROR] {RunnerErrors.MissingBuildOutput(requiredDll)}");
        return false;
    }

    /// <summary>
    /// Runs the named steps in order and stops at the first failure. A step that returns
    /// <see cref="RunnerExitCodes.SkipExitCode"/> counts as success. Every step is recorded in BuildResult.
    /// </summary>
    internal static int RunSteps(IEnumerable<string> names, BuildContext context, BuildResult buildResult,
        IReadOnlyDictionary<string, Func<BuildContext, int>> table)
    {
        foreach (var name in names)
        {
            if (!table.TryGetValue(name, out var step))
            {
                Console.Error.WriteLine($"[BuildRunner][ERROR] Unknown build step: {name}");
                return Program.ExitFailure;
            }

            var exitCode = BuildResultUtils.ExecuteAndRecord(buildResult, name, () => step(context));
            if (exitCode != Program.ExitSuccess && exitCode != RunnerExitCodes.SkipExitCode)
            {
                Console.Error.WriteLine($"[BuildRunner][ERROR] Step {name} failed with exit code {exitCode}");
                return Program.ExitFailure;
            }
        }

        return Program.ExitSuccess;
    }

    private static void PrintResultNonFatal(BuildContext context, BuildResult buildResult,
        Func<BuildContext, BuildResult, int> printResult)
    {
        try
        {
            var printCode = printResult(context, buildResult);
            if (printCode != Program.ExitSuccess)
            {
                Console.WriteLine($"[BuildRunner][WARN] PrintResultLib returned exit code {printCode}; the build continues.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BuildRunner][WARN] Printing the build result failed: {ex.Message}; the build continues.");
        }
    }

    internal static BuildContext LoadContext(string buildContextPath)
    {
        BuildContext? context;
        try
        {
            context = BuildContext.FromJson(File.ReadAllText(buildContextPath));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to parse BuildContext JSON {buildContextPath}: {ex.Message}. Rebuild the project to regenerate it.", ex);
        }

        return context ?? throw new InvalidOperationException(
            $"Failed to parse BuildContext JSON {buildContextPath}: the file is empty. Rebuild the project to regenerate it.");
    }
}
