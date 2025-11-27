#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Script library for enhanced ILRepack assembly merging with better dependency resolution and error handling
/// Entry point: ILRepackAssembliesLib.Execute(BuildContext context)
/// </summary>
public class ILRepackAssembliesLib
{
    public class RepackResult
    {
        public bool Success { get; set; }
        public bool RepackSkipped { get; set; }
        public string OutputPath { get; set; } = "";
        public int AssembliesMerged { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// Main entry point for ILRepack assembly merging
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== ILRepack Assemblies Results ===");

            var result = RepackAssemblies(context);

            context.LogInfo($"Success: {result.Success}");
            context.LogInfo($"Repack Skipped: {result.RepackSkipped}");
            context.LogInfo($"Output Path: {result.OutputPath}");
            context.LogInfo($"Assemblies Merged: {result.AssembliesMerged}");
            context.LogInfo($"Processed At: {result.ProcessedAt:u}");

            if (result.Errors.Any())
            {
                context.LogError("Errors:");
                foreach (var error in result.Errors)
                {
                    context.LogError($"  ❌ {error}");
                }
            }

            if (result.Warnings.Any())
            {
                context.LogWarning("Warnings:");
                foreach (var warning in result.Warnings)
                {
                    context.LogWarning($"  ⚠️  {warning}");
                }
            }

            return result.ExitCode;
        }
        catch (Exception ex)
        {
            context.LogError($"ILRepack assemblies exception: {ex.Message}");
            return 1;
        }
    }

    public static RepackResult RepackAssemblies(BuildContext context)
    {
        var result = new RepackResult();

        try
        {
            context.LogInfo("Starting ILRepack assembly merging");

            // Check if this should be skipped
            if (ShouldSkipRepack(context, result))
            {
                return result;
            }

            // Get target assembly and dependencies from BuildContext
            var mainAssembly = context.MainAssemblyPath ?? FindMainAssembly(context);
            if (string.IsNullOrEmpty(mainAssembly))
            {
                result.Errors.Add("Main assembly not found");
                return result;
            }

            var dependencies = context.DependencyAssemblies ?? new List<string>();
            // Prepare output paths
            var targetModDir = GetTargetModDirectory(context);
            var tempDir = Path.Combine(targetModDir, ".ilrepack_temp");
            var tempOutput = Path.Combine(tempDir, $"{context.ModName}.dll");
            var finalOutput = Path.Combine(targetModDir, $"{context.ModName}.dll");

            BuildUtils.EnsureDirectoryExists(tempDir);
            BuildUtils.EnsureDirectoryExists(targetModDir);

            // Build ILRepack command
            var ilrepackArgs = BuildILRepackArgs(mainAssembly, dependencies, tempOutput, context);
            context.LogInfo($"ILRepack command: ilrepack {ilrepackArgs}");

            // Execute ILRepack
            var (exitCode, output) = BuildUtils.ExecuteCommand("ilrepack", context.ProjectDir, ilrepackArgs.Split(' '));

            if (exitCode == 0 && File.Exists(tempOutput))
            {
                // Move to final location
                // Delete target if it exists to allow overwrite
                if (File.Exists(finalOutput))
                {
                    File.Delete(finalOutput);
                }

                context.LogInfo($"Moving {tempOutput} to {finalOutput}");
                File.Move(tempOutput, finalOutput);

                // Move PDB if exists
                var tempPdb = Path.ChangeExtension(tempOutput, ".pdb");
                if (File.Exists(tempPdb))
                {
                    var finalPdb = Path.ChangeExtension(finalOutput, ".pdb");
                    if (File.Exists(finalPdb))
                    {
                        File.Delete(finalPdb);
                    }

                    context.LogInfo($"Moving {tempPdb} to {finalPdb}");
                    File.Move(tempPdb, finalPdb);
                }

                result.Success = true;
                result.OutputPath = finalOutput;
                result.AssembliesMerged = dependencies.Count + 1; // +1 for main assembly
                result.ExitCode = 0;
                context.LogInfo($"ILRepack completed successfully: {result.AssembliesMerged} assemblies merged");
            }
            else
            {
                result.Success = false;
                result.ExitCode = 1;
                result.Errors.Add($"ILRepack failed with exit code: {exitCode}");
                result.Errors.Add($"Output: {output}");
            }

            // Cleanup temp directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to cleanup temp directory: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ExitCode = 1;
            result.Errors.Add($"ILRepack error: {ex.Message}");
            context.LogError($"Exception: {ex}");
        }

        return result;
    }

    private static bool ShouldSkipRepack(BuildContext context, RepackResult result)
    {
        // skip if no deploy mod
        if (!context.DeployMod)
        {
            context.LogInfo("Skipping ILRepack: DeployMod=false");
            result.RepackSkipped = true;
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // Skip if ILRepack is disabled or no dependencies
        if (!context.ShouldUseILRepack)
        {
            context.LogInfo($"Skipping ILRepack: ShouldUseILRepack={context.ShouldUseILRepack} (EnableILRepack={context.EnableILRepack}, HasDependencies={context.HasDependencies})");
            result.RepackSkipped = true;
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // Skip for library projects
        if (context.IsModLib)
        {
            context.LogInfo("Skipping ILRepack: IsModLib=true, library projects are not repacked");
            result.RepackSkipped = true;
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // Skip if no ModName
        if (string.IsNullOrEmpty(context.ModName))
        {
            result.Errors.Add("ModName is required for ILRepack");
            return false;
        }

        return false;
    }

    private static string FindMainAssembly(BuildContext context)
    {
        var targetDll = Path.Combine(context.ProjectDir, "bin", context.Configuration ?? "Debug",
            context.TargetFramework ?? "netstandard2.1", $"{Path.GetFileName(context.ProjectDir)}.dll");
        return File.Exists(targetDll) ? targetDll : "";
    }

    private static string GetTargetModDirectory(BuildContext context)
    {
        var modsDirectory = context.ModsDirectory;
        return Path.Combine(modsDirectory, context.ModName);
    }

    private static string BuildILRepackArgs(string mainAssembly, List<string> dependencies, string outputPath,
        BuildContext context)
    {
        var args = new List<string>();

//  Command="ilrepack /out:&quot;$(_TempPackedDll)&quot; &quot;$(TargetPath)&quot; $(_DllsToPackList) /internalize /wildcards /log $(_LibPathArgs)"
        // Basic options
        args.Add($"/out:\"{outputPath}\"");

        // Add main assembly
        args.Add($"\"{mainAssembly}\"");

        // Add dependencies
        foreach (var dep in dependencies)
        {
            args.Add($"\"{Path.GetFullPath(dep)}\"");
        }

        args.Add("/target:library");
        args.Add("/wildcards");
        args.Add("/internalize");
        args.Add("/ndebug");
        args.Add("/parallel");

        // reference resolution
        args.Add($"/lib:\"{Path.GetDirectoryName(mainAssembly)}\"");
        args.Add($"/lib:\"{context.ManagedDirectory}\"");


        return string.Join(" ", args);
    }
}
