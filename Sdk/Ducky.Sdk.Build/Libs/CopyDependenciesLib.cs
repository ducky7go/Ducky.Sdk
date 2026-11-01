#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Script library for enhanced dependency copying with better filtering and validation
/// Entry point: CopyDependenciesLib.Execute(BuildContext context)
/// </summary>
public class CopyDependenciesLib
{
    public class CopyResult
    {
        public bool Success { get; set; }
        public int DependenciesCopied { get; set; }
        public List<string> CopiedFiles { get; set; } = new();
        public List<string> SkippedFiles { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public DateTime CopiedAt { get; set; } = DateTime.UtcNow;
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// Main entry point for copying dependencies
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Copy Dependencies Results ===");

            var result = CopyDependencies(context);

            context.LogInfo($"Success: {result.Success}");
            context.LogInfo($"Dependencies Copied: {result.DependenciesCopied}");
            context.LogInfo($"Copied At: {result.CopiedAt:u}");

            if (result.CopiedFiles.Any())
            {
                context.LogInfo("Copied Files:");
                foreach (var file in result.CopiedFiles.Take(10)) // Show first 10
                {
                    context.LogInfo($"  - {file}");
                }

                if (result.CopiedFiles.Count > 10)
                {
                    context.LogInfo($"  ... and {result.CopiedFiles.Count - 10} more");
                }
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                context.LogError($"Error: {result.ErrorMessage}");
            }

            return result.ExitCode;
        }
        catch (Exception ex)
        {
            context.LogError($"Copy dependencies exception: {ex.Message}");
            return 1;
        }
    }

    public static CopyResult CopyDependencies(BuildContext context)
    {
        var result = new CopyResult();

        try
        {
            context.LogInfo("Starting dependency copying");

            // Check if this should be skipped
            if (ShouldSkipCopying(context, result))
            {
                return result;
            }

            // Find dependencies that need to be copied
            var dependencies = FindDependenciesToCopy(context);

            if (dependencies.Count == 0)
            {
                context.LogInfo("No dependencies found to copy");
                result.Success = true;
                result.ExitCode = 0;
                return result;
            }

            // Copy dependencies
            var targetDir = GetTargetDependencyDirectory(context);

            // Up-to-date check: same dependency files and destination as the last successful run, and every copy still exists
            var expectedOutputs = dependencies.Select(dependency => Path.Combine(targetDir, Path.GetFileName(dependency))).ToList();
            var fingerprint = BuildStamp.Compute(dependencies,
                BuildStamp.ContextKey(context) + "|dest=" + targetDir);
            if (BuildStamp.IsUpToDate(context, nameof(CopyDependenciesLib), fingerprint, expectedOutputs))
            {
                context.LogInfo("Dependencies are up to date (no source or destination change); skipping copy");
                result.SkippedFiles.AddRange(expectedOutputs);
                result.Success = true;
                result.ExitCode = SkipExitCode;
                return result;
            }

            BuildUtils.EnsureDirectoryExists(targetDir);

            var copyFailed = false;
            foreach (var dependency in dependencies)
            {
                var fileName = Path.GetFileName(dependency);
                var targetPath = Path.Combine(targetDir, fileName);

                try
                {
                    // Check if file needs to be copied
                    if (ShouldCopyFile(dependency, targetPath, context))
                    {
                        File.Copy(dependency, targetPath, true);
                        result.DependenciesCopied++;
                        result.CopiedFiles.Add(targetPath);
                    }
                    else
                    {
                        result.SkippedFiles.Add(targetPath);
                    }
                }
                catch (Exception ex)
                {
                    context.LogWarning($"Failed to copy dependency {dependency}: {ex.Message}");
                    result.SkippedFiles.Add(targetPath);
                    copyFailed = true;
                }
            }

            // A per-file copy failure is only logged, so it must block the stamp explicitly.
            if (!copyFailed)
            {
                BuildStamp.Write(context, nameof(CopyDependenciesLib), fingerprint);
            }

            result.Success = true;
            result.ExitCode = 0;
            context.LogInfo(
                $"Dependency copying completed: {result.DependenciesCopied} copied, {result.SkippedFiles.Count} skipped");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ExitCode = 1;
            result.ErrorMessage = $"Dependency copying error: {ex.Message}";
            context.LogError($"Exception: {ex}");
        }

        return result;
    }

    private static bool ShouldSkipCopying(BuildContext context, CopyResult result)
    {
        // Skip if ILRepack is enabled (dependencies are bundled)
        if (context.EnableILRepack)
        {
            context.LogInfo("Skipping dependency copying: ILRepack is enabled");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // Skip for library projects
        if (context.IsModLib)
        {
            context.LogInfo("Skipping dependency copying: IsModLib=true");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        return false;
    }

    /// <summary>
    /// The files this step writes into the destination Dependency folder in a successful run, from the same inputs
    /// and conditions as CopyDependencies. ResetDeployLib uses this list to decide which deployed files are stale.
    /// </summary>
    public static List<string> PlannedOutputs(BuildContext context)
    {
        if (context.EnableILRepack || context.IsModLib || string.IsNullOrEmpty(context.ModName))
        {
            return new List<string>();
        }

        var targetDir = GetTargetDependencyDirectory(context);
        return ListDependencies(context)
            .Select(dependency => Path.Combine(targetDir, Path.GetFileName(dependency)))
            .ToList();
    }

    private static List<string> FindDependenciesToCopy(BuildContext context)
    {
        if (!Directory.Exists(DependencySourceDirectory(context)))
        {
            context.LogInfo("No Dependency directory found");
            return new List<string>();
        }

        var dependencies = ListDependencies(context);
        context.LogInfo($"Found {dependencies.Count} dependencies to copy");
        return dependencies;
    }

    private static string DependencySourceDirectory(BuildContext context) =>
        Path.Combine(context.ProjectDir, "Dependency");

    /// <summary>
    /// The DLLs of the project's Dependency folder that get copied, without the system and Unity assemblies.
    /// Logs nothing, so it can be used for planning.
    /// </summary>
    private static List<string> ListDependencies(BuildContext context)
    {
        var dependencies = new List<string>();
        var dependencyDir = DependencySourceDirectory(context);

        if (!Directory.Exists(dependencyDir))
        {
            return dependencies;
        }

        var dllFiles = Directory.GetFiles(dependencyDir, "*.dll");

        // Filter out common system assemblies and runtime assemblies
        var exclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System.dll", "System.Core.dll", "System.Runtime.dll", "mscorlib.dll",
            "netstandard.dll", "Microsoft.CSharp.dll",
            "UnityEngine.dll", "UnityEngine.CoreModule.dll", "Unity assemblies",
            "Fusion.dll", "0Harmony.dll" // Common mod dependencies
        };

        foreach (var dll in dllFiles)
        {
            var fileName = Path.GetFileName(dll);
            if (!exclusions.Contains(fileName) && !fileName.StartsWith("System.") && !fileName.StartsWith("Microsoft."))
            {
                dependencies.Add(dll);
            }
        }

        return dependencies;
    }

    private static string GetTargetDependencyDirectory(BuildContext context)
    {
        var targetModDir = Path.Combine(context.ModsDirectory, context.ModName);
        return Path.Combine(targetModDir, "Dependency");
    }

    private static bool ShouldCopyFile(string sourceFile, string targetFile, BuildContext context)
    {
        // Always copy if target doesn't exist
        if (!File.Exists(targetFile))
        {
            return true;
        }

        // Compare modification times
        var sourceTime = File.GetLastWriteTime(sourceFile);
        var targetTime = File.GetLastWriteTime(targetFile);

        return sourceTime > targetTime;
    }
}
