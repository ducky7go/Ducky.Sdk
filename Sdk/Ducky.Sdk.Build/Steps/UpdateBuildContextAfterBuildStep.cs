#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Port of the former UpdateBuildContextAfterBuildLib.csx (post phase, after Build).
/// Finds the compiled main assembly and the dependency DLLs in the output directory and writes them back
/// into the BuildContext JSON. Missing outputs are logged as warnings, as before; the phase does not fail here.
/// </summary>
internal static class UpdateBuildContextAfterBuildStep
{
    /// <summary>
    /// Updates the context and writes it back to <paramref name="buildContextPath"/>. Returns 0 or 1.
    /// </summary>
    internal static int Execute(BuildContext context, string buildContextPath)
    {
        try
        {
            Console.WriteLine($"[UpdateBuildContext] Starting for project: {context.ProjectDir}");
            Console.WriteLine($"[UpdateBuildContext] Loaded BuildContext for: {context.ModName ?? "(unknown)"}");

            UpdateDependencies(context);
            SaveBuildContext(context, buildContextPath);

            Console.WriteLine("[UpdateBuildContext] Completed successfully");
            Console.WriteLine($"[UpdateBuildContext] Dependencies found: {context.DependencyAssemblies?.Count ?? 0}");
            Console.WriteLine($"[UpdateBuildContext] HasDependencies: {context.HasDependencies}");
            Console.WriteLine($"[UpdateBuildContext] ShouldUseILRepack: {context.ShouldUseILRepack}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[UpdateBuildContext][ERROR] {ex.Message}");
            Console.Error.WriteLine($"[UpdateBuildContext][ERROR] Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    internal static void UpdateDependencies(BuildContext context)
    {
        Console.WriteLine("[UpdateBuildContext] Updating dependencies...");

        // Find main assembly
        var mainAssembly = FindMainAssembly(context);
        if (string.IsNullOrEmpty(mainAssembly))
        {
            context.LogWarning("Main assembly not found, skipping dependency detection");
            return;
        }

        context.MainAssemblyPath = mainAssembly;
        Console.WriteLine($"[UpdateBuildContext] Main assembly: {Path.GetFileName(mainAssembly)}");

        // Scan for dependencies
        var outputPath = context.OutputPath;
        var outDir = Path.GetDirectoryName(outputPath);

        if (!Directory.Exists(outDir))
        {
            context.LogError($"Output directory does not exist: {outDir}");
            return;
        }

        var existingDlls = Directory.GetFiles(outDir, "*.dll");
        Console.WriteLine($"[UpdateBuildContext] Found {existingDlls.Length} DLLs in output directory");

        // Get managed DLLs from game directory
        var managedDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(context.ManagedDirectory) && Directory.Exists(context.ManagedDirectory))
        {
            managedDlls = Directory.GetFiles(context.ManagedDirectory, "*.dll")
                .Select(f => Path.GetFileName(f))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Console.WriteLine($"[UpdateBuildContext] Found {managedDlls.Count} DLLs in Managed directory");
        }
        else
        {
            context.LogWarning("Managed directory not available, cannot filter managed DLLs");
        }

        // Find dependencies
        var dependencies = new List<string>();
        var mainAssemblyName = Path.GetFileName(mainAssembly);

        foreach (var dll in existingDlls)
        {
            var fileName = Path.GetFileName(dll);

            // Skip if it's the main assembly itself or a managed DLL
            if (!string.Equals(fileName, mainAssemblyName, StringComparison.OrdinalIgnoreCase) &&
                !managedDlls.Contains(fileName) &&
                !IsHotReloadOnlyFile(context, fileName))
            {
                Console.WriteLine($"[UpdateBuildContext] Adding dependency: {fileName}");
                dependencies.Add(dll);
            }
        }

        context.DependencyAssemblies = dependencies;
        Console.WriteLine($"[UpdateBuildContext] Total dependencies: {dependencies.Count}");
    }

    /// <summary>
    /// In a hot reload build, Harmony is deployed once beside the shim and never merged, and the Core files of other
    /// builds (ModName.Core.*.dll) are removed by HotReloadCleanupStep, so neither may become a merge input.
    /// Always false in default builds.
    /// </summary>
    internal static bool IsHotReloadOnlyFile(BuildContext context, string fileName)
    {
        if (!context.HotReloadEnabled)
        {
            return false;
        }

        return string.Equals(fileName, HotReloadFiles.HarmonyFileName, StringComparison.OrdinalIgnoreCase) ||
               HotReloadFiles.IsCoreFileOf(context.ModName, fileName);
    }

    internal static string FindMainAssembly(BuildContext context)
    {
        if (context.HotReloadEnabled)
        {
            // A hot reload build has exactly one Core for this build; anything else is not this build's output.
            var core = Path.Combine(Path.GetDirectoryName(context.OutputPath), context.MainOutputFileName);
            return File.Exists(core) ? core : string.Empty;
        }

        // Try to find the main assembly based on project name
        var expectedDll = Path.Combine(
            Path.GetDirectoryName(context.OutputPath),
            $"{context.ModName ?? Path.GetFileName(context.ProjectDir)}.dll"
        );

        if (File.Exists(expectedDll))
        {
            return expectedDll;
        }

        // Fallback: look for any DLL in output path that's not a dependency
        var outDir = Path.GetDirectoryName(context.OutputPath);
        if (Directory.Exists(outDir))
        {
            var dlls = Directory.GetFiles(outDir, "*.dll");
            if (dlls.Length == 1)
            {
                return dlls[0];
            }
            else if (dlls.Length > 1)
            {
                // Try to find the one that matches project name
                var projectDlls = dlls.Where(d =>
                    Path.GetFileNameWithoutExtension(d).Contains(Path.GetFileNameWithoutExtension(context.ProjectDir))
                ).ToArray();

                if (projectDlls.Length == 1)
                {
                    return projectDlls[0];
                }
            }
        }

        return string.Empty;
    }

    internal static void SaveBuildContext(BuildContext context, string jsonFilePath)
    {
        Console.WriteLine("[UpdateBuildContext] Saving updated BuildContext...");

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));

        // Create temporary file for atomic write
        var tempFilePath = jsonFilePath + ".tmp";

        try
        {
            var jsonContent = context.ToJson();
            File.WriteAllText(tempFilePath, jsonContent, new System.Text.UTF8Encoding(false));

            // Atomic replace
            if (File.Exists(jsonFilePath))
            {
                File.Replace(tempFilePath, jsonFilePath, null);
            }
            else
            {
                File.Move(tempFilePath, jsonFilePath);
            }

            Console.WriteLine($"[UpdateBuildContext] Saved BuildContext to: {jsonFilePath}");
        }
        catch
        {
            // Clean up temp file if something went wrong
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            throw;
        }
    }
}
