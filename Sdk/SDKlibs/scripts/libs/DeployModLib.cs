#nullable disable

using System;
using System.IO;
using System.Linq;

/// <summary>
/// Script library for enhanced mod deployment with validation and error handling
/// Entry point: DeployModLib.Execute(BuildContext context)
/// </summary>
public class DeployModLib
{
    public class DeploymentResult
    {
        public bool Success { get; set; }
        public string TargetModDir { get; set; } = "";
        public int AssetsCopied { get; set; }
        public int DependenciesCopied { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime DeployedAt { get; set; } = DateTime.UtcNow;
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// Main entry point for deploying mods
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Deploy Mod Results ===");

            var result = Deploy(context);

            context.LogInfo($"Success: {result.Success}");
            context.LogInfo($"Target Directory: {result.TargetModDir}");
            context.LogInfo($"Assets Copied: {result.AssetsCopied}");
            context.LogInfo($"Dependencies Copied: {result.DependenciesCopied}");
            context.LogInfo($"Deployed At: {result.DeployedAt:u}");

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
            context.LogError($"Deploy mod exception: {ex.Message}");
            return 1;
        }
    }

    public static DeploymentResult Deploy(BuildContext context)
    {
        var result = new DeploymentResult();

        try
        {
            context.LogInfo("Starting mod deployment");

            // Check if this should be skipped
            if (ShouldSkipDeployment(context, result))
            {
                return result;
            }

            var targetModDir = GetTargetModDirectory(context);
            result.TargetModDir = targetModDir;

            // Validate deployment prerequisites
            if (!ValidateDeploymentPrerequisites(context, result))
            {
                return result;
            }

            // Create target directory (ResetDeployLib should have already cleaned it)
            BuildUtils.EnsureDirectoryExists(targetModDir);

            // Copy assets
            var assetsResult = CopyAssets(targetModDir, context, result);
            result.AssetsCopied = assetsResult.FilesCopied;

            // Copy main DLL

            // Copy dependencies if ILRepack is disabled
            if (!context.EnableILRepack)
            {
                CopyMainDll(targetModDir, context, result);
                CopyDependencies(targetModDir, context, result);
                context.LogInfo("Main DLL and dependencies copied");
            }
            else
            {
                if (!context.HasDependencies)
                {
                    CopyMainDll(targetModDir, context, result);
                    context.LogInfo("Main DLL copied");
                }
                else
                {
                    context.LogInfo("ILRepack is enabled with dependencies, skipping main DLL copy");
                }
            }

            result.Success = result.Errors.Count == 0;

            if (result.Success)
            {
                context.LogInfo($"Deployment completed successfully to: {targetModDir}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Deployment error: {ex.Message}");
            context.LogError($"Exception: {ex}");
        }

        return result;
    }

    private static bool ShouldSkipDeployment(BuildContext context, DeploymentResult result)
    {
        // Skip if deployment is disabled
        if (!context.DeployMod)
        {
            context.LogInfo("Skipping deployment: DeployMod=false");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // Skip for library projects
        if (context.IsModLib)
        {
            context.LogInfo("Skipping deployment: IsModLib=true, library projects are not deployed to game directory");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // Skip if no ModName
        if (string.IsNullOrEmpty(context.ModName))
        {
            context.LogError("Skipping deployment: ModName not specified");
            result.Success = false;
            result.ExitCode = SkipExitCode;
            result.Errors.Add("ModName is required for deployment");
            return true;
        }

        return false;
    }

    private static bool ValidateDeploymentPrerequisites(BuildContext context, DeploymentResult result)
    {
        var isValid = true;

        // Check DuckovFolder
        if (string.IsNullOrEmpty(context.DuckovFolder))
        {
            result.Errors.Add("DuckovFolder is not specified");
            isValid = false;
        }
        else if (!Directory.Exists(context.DuckovFolder))
        {
            result.Errors.Add($"DuckovFolder does not exist: {context.DuckovFolder}");
            isValid = false;
        }

        // Check target DLL
        var targetDll = Path.Combine(context.ProjectDir, "bin", context.Configuration ?? "Debug",
            context.TargetFramework ?? "netstandard2.1", $"{Path.GetFileName(context.ProjectDir)}.dll");
        if (!File.Exists(targetDll))
        {
            result.Errors.Add($"Target DLL not found: {targetDll}");
            isValid = false;
        }

        if (isValid)
        {
            context.LogInfo("Deployment prerequisites validated successfully");
        }

        return isValid;
    }

    private static string GetTargetModDirectory(BuildContext context)
    {
        var modsDirectory = context.ModsDirectory;
        return Path.Combine(modsDirectory, context.ModName);
    }


    private class AssetsCopyResult
    {
        public bool Success { get; set; }
        public int FilesCopied { get; set; }
    }

    private static AssetsCopyResult CopyAssets(string targetDir, BuildContext context, DeploymentResult result)
    {
        var copyResult = new AssetsCopyResult();

        try
        {
            var assetsDir = string.IsNullOrEmpty(context.AssetsDir) ? "assets" : context.AssetsDir;
            var sourceAssetsDir = context.GetFullPath(assetsDir);

            if (!Directory.Exists(sourceAssetsDir))
            {
                context.LogInfo("No assets directory found, skipping assets copy");
                copyResult.Success = true;
                return copyResult;
            }

            context.LogInfo($"Copying assets from: {sourceAssetsDir}");

            var files = Directory.GetFiles(sourceAssetsDir, "*.*", SearchOption.AllDirectories);
            var copiedCount = 0;

            foreach (var sourceFile in files)
            {
                var relativePath = Path.GetRelativePath(sourceAssetsDir, sourceFile);
                var targetFile = Path.Combine(targetDir, relativePath.ToString());

                // Ensure target directory exists
                var targetFileDir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetFileDir))
                {
                    BuildUtils.EnsureDirectoryExists(targetFileDir);
                }

                File.Copy(sourceFile, targetFile, true);
                copiedCount++;
            }

            copyResult.Success = true;
            copyResult.FilesCopied = copiedCount;

            context.LogInfo($"Assets copied: {copiedCount} files");
        }
        catch (Exception ex)
        {
            copyResult.Success = false;
            result.Errors.Add($"Assets copy error: {ex.Message}");
            context.LogError($"Assets copy exception: {ex}");
        }

        return copyResult;
    }

    private static void CopyMainDll(string targetDir, BuildContext context, DeploymentResult result)
    {
        try
        {
            var targetDll = Path.Combine(context.ProjectDir, "bin", context.Configuration ?? "Debug",
                context.TargetFramework ?? "netstandard2.1", $"{Path.GetFileName(context.ProjectDir)}.dll");
            var destDll = Path.Combine(targetDir ?? "", $"{context.ModName ?? "mod"}.dll");

            if (File.Exists(targetDll))
            {
                File.Copy(targetDll, destDll, true);
                context.LogInfo($"Main DLL copied: {destDll}");

                // Copy PDB if exists
                var pdbFile = Path.ChangeExtension(targetDll, ".pdb");
                if (File.Exists(pdbFile))
                {
                    var destPdb = Path.ChangeExtension(destDll, ".pdb");
                    File.Copy(pdbFile, destPdb, true);
                }
            }
            else
            {
                result.Errors.Add($"Target DLL not found: {targetDll}");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Main DLL copy error: {ex.Message}");
            context.LogError($"Main DLL copy exception: {ex}");
        }
    }

    private static void CopyDependencies(string targetDir, BuildContext context, DeploymentResult result)
    {
        try
        {
            var targetDll = Path.Combine(context.ProjectDir, "bin", context.Configuration ?? "Debug",
                context.TargetFramework ?? "netstandard2.1", $"{Path.GetFileName(context.ProjectDir)}.dll");
            var targetDirName = Path.GetDirectoryName(targetDll) ?? "";
            var dependencyDir = Path.Combine(targetDir, "Dependency");

            if (!Directory.Exists(dependencyDir))
            {
                context.LogInfo("No dependencies directory found");
                return;
            }

            var depsPath = Path.Combine(targetDir, "Dependency");
            BuildUtils.EnsureDirectoryExists(depsPath);

            var dependencyFiles = Directory.GetFiles(dependencyDir, "*.dll");
            var copiedCount = 0;

            foreach (var depFile in dependencyFiles)
            {
                var fileName = Path.GetFileName(depFile);
                var destFile = Path.Combine(depsPath, fileName);

                File.Copy(depFile, destFile, true);
                copiedCount++;
            }

            result.DependenciesCopied = copiedCount;
            context.LogInfo($"Dependencies copied: {copiedCount} files");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Dependencies copy error: {ex.Message}");
            context.LogError($"Dependencies copy exception: {ex}");
        }
    }
}
