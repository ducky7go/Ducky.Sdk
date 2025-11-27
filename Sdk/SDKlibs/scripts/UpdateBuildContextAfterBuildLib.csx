#!/usr/bin/env dotnet-script
#nullable disable

#load "shared/BuildContext.cs"

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Post-compilation BuildContext update script
/// Scans for dependencies and updates BuildContext with dependency information
/// Usage: dotnet script UpdateBuildContextAfterBuildLib.csx <projectDirectory>
/// </summary>

// Validate arguments
if (Args.Count < 1)
{
    Console.Error.WriteLine("Usage: dotnet script UpdateBuildContextAfterBuildLib.csx <projectDirectory>");
    Environment.Exit(1);
}

var projectDir = Args[0];

if (!Directory.Exists(projectDir))
{
    Console.Error.WriteLine($"Error: Project directory does not exist: {projectDir}");
    Environment.Exit(1);
}

try
{
    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Starting for project: {projectDir}");

    // Load existing BuildContext
    var context = BuildContext.LoadFromProjectDirectory(projectDir);

    if (context == null)
    {
        Console.Error.WriteLine("[UpdateBuildContextAfterBuildLib][ERROR] Failed to load BuildContext");
        Environment.Exit(1);
    }

    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Loaded BuildContext for: {context.ModName ?? "(unknown)"}");

    // Update dependencies
    UpdateDependencies(context);

    // Save updated context
    SaveBuildContext(context);

    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Completed successfully");
    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Dependencies found: {context.DependencyAssemblies?.Count ?? 0}");
    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] HasDependencies: {context.HasDependencies}");
    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] ShouldUseILRepack: {context.ShouldUseILRepack}");

    Environment.Exit(0);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[UpdateBuildContextAfterBuildLib][ERROR] {ex.Message}");
    Console.Error.WriteLine($"[UpdateBuildContextAfterBuildLib][ERROR] Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}

void UpdateDependencies(BuildContext context)
{
    Console.WriteLine("[UpdateBuildContextAfterBuildLib] Updating dependencies...");

    // Find main assembly
    var mainAssembly = FindMainAssembly(context);
    if (string.IsNullOrEmpty(mainAssembly))
    {
        context.LogWarning("Main assembly not found, skipping dependency detection");
        return;
    }

    context.MainAssemblyPath = mainAssembly;
    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Main assembly: {Path.GetFileName(mainAssembly)}");

    // Scan for dependencies
    var outputPath = context.OutputPath;
    var outDir = Path.GetDirectoryName(outputPath);

    if (!Directory.Exists(outDir))
    {
        context.LogError($"Output directory does not exist: {outDir}");
        return;
    }

    var existingDlls = Directory.GetFiles(outDir, "*.dll");
    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Found {existingDlls.Length} DLLs in output directory");

    // Get managed DLLs from game directory
    var managedDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!string.IsNullOrEmpty(context.ManagedDirectory) && Directory.Exists(context.ManagedDirectory))
    {
        managedDlls = Directory.GetFiles(context.ManagedDirectory, "*.dll")
            .Select(f => Path.GetFileName(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Found {managedDlls.Count} DLLs in Managed directory");
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
            !managedDlls.Contains(fileName))
        {
            Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Adding dependency: {fileName}");
            dependencies.Add(dll);
        }
    }

    context.DependencyAssemblies = dependencies;
    Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Total dependencies: {dependencies.Count}");
}

string FindMainAssembly(BuildContext context)
{
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

void SaveBuildContext(BuildContext context)
{
    Console.WriteLine("[UpdateBuildContextAfterBuildLib] Saving updated BuildContext...");

    var objDir = Path.Combine(context.ProjectDir, "obj");
    var jsonFilePath = Path.Combine(objDir, "ducky-build-context.json");

    // Ensure obj directory exists
    Directory.CreateDirectory(objDir);

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

        Console.WriteLine($"[UpdateBuildContextAfterBuildLib] Saved BuildContext to: {jsonFilePath}");
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