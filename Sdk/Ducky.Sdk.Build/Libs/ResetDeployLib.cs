#nullable disable

using System;
using System.IO;
using System.Linq;

/// <summary>
/// Script library for resetting deployment target directories
/// Entry point: ResetDeployLib.Execute(BuildContext context)
/// </summary>
public class ResetDeployLib
{
    public class ResetResult
    {
        public bool Success { get; set; }
        public string TargetModDir { get; set; } = "";
        public bool DirectoryExisted { get; set; }
        public List<string> RemovedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime ResetAt { get; set; } = DateTime.UtcNow;
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// Main entry point for resetting deployment directories
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Reset Deploy Directory Results ===");

            var result = Reset(context);

            context.LogInfo($"Success: {result.Success}");
            context.LogInfo($"Target Directory: {result.TargetModDir}");
            context.LogInfo($"Directory Existed: {result.DirectoryExisted}");
            context.LogInfo($"Stale Files Removed: {result.RemovedFiles.Count}");
            context.LogInfo($"Reset At: {result.ResetAt:u}");

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

            context.LogInfo("=== Reset Deploy Directory Results ===");

            return result.ExitCode;
        }
        catch (Exception ex)
        {
            context.LogError($"Reset deploy directory exception: {ex.Message}");
            context.LogError($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    /// <summary>
    /// Resets the deployment directory. It never deletes the mod folder. It removes only the stale files that
    /// the last successful deploy wrote and this build no longer plans to write (see DeployManifest).
    /// Files the SDK did not write, and copy outputs that are still planned, stay in place.
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>ResetResult with details of the operation</returns>
    public static ResetResult Reset(BuildContext context)
    {
        var result = new ResetResult();

        try
        {
            context.LogInfo("Starting deployment directory reset");

            // Check if this should be skipped
            if (ShouldSkipReset(context, result))
            {
                return result;
            }

            var targetModDir = GetTargetModDirectory(context);
            result.TargetModDir = targetModDir;
            result.DirectoryExisted = Directory.Exists(targetModDir);

            if (result.DirectoryExisted)
            {
                context.LogInfo($"Target deployment directory exists: {targetModDir}");
                RemoveStaleFiles(context, targetModDir, result);
            }
            else
            {
                context.LogInfo($"Target deployment directory does not exist: {targetModDir}");
            }

            if (result.Errors.Count > 0)
            {
                result.Success = false;
                result.ExitCode = 1;
                return result;
            }

            result.Success = true;
            result.ExitCode = 0;
            context.LogInfo($"Deployment directory reset completed: {result.RemovedFiles.Count} stale file(s) removed");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ExitCode = 1;
            result.Errors.Add($"Reset operation failed: {ex.Message}");
            context.LogError($"Exception: {ex}");
        }

        return result;
    }

    /// <summary>
    /// Deletes each manifest entry of the last successful deploy that this build does not plan to write.
    /// A build with no changes plans the same files as the last one, so nothing is removed.
    /// </summary>
    private static void RemoveStaleFiles(BuildContext context, string targetModDir, ResetResult result)
    {
        var previous = DeployManifest.Read(context, targetModDir);
        if (previous.Count == 0)
        {
            context.LogInfo("No deployed-files manifest applies to this folder; no SDK-written file is known to be stale");
            return;
        }

        var planned = DeployManifest.PlannedRelativeFiles(context, targetModDir);
        var stale = previous.Where(relative => !planned.Contains(relative))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .ToList();

        foreach (var relative in stale)
        {
            var file = DeployManifest.ToFullPath(targetModDir, relative);
            if (!File.Exists(file))
            {
                continue;
            }

            try
            {
                File.Delete(file);
                result.RemovedFiles.Add(file);
                context.LogInfo($"Removed stale file: {file}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to delete stale file '{file}': {ex.Message}");
                context.LogError($"Failed to delete stale file: {ex.Message}");
            }
        }

        RemoveEmptyFolders(context, targetModDir, result);
    }

    /// <summary>
    /// Removes folders that became empty because of a stale-file delete, walking up to the mod folder.
    /// The mod folder itself is never removed. A folder that cannot be removed is logged, not treated as a failure.
    /// </summary>
    private static void RemoveEmptyFolders(BuildContext context, string targetModDir, ResetResult result)
    {
        var root = Path.GetFullPath(targetModDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folders = result.RemovedFiles
            .Select(file => Path.GetDirectoryName(Path.GetFullPath(file)))
            .Where(folder => !string.IsNullOrEmpty(folder))
            .Distinct()
            .ToList();

        foreach (var folder in folders)
        {
            var current = folder;
            while (current != null && current != root && Directory.Exists(current) &&
                   !Directory.EnumerateFileSystemEntries(current).Any())
            {
                try
                {
                    Directory.Delete(current);
                    context.LogInfo($"Removed empty folder: {current}");
                }
                catch (Exception ex)
                {
                    context.LogWarning($"Could not remove empty folder '{current}': {ex.Message}");
                    break;
                }

                current = Path.GetDirectoryName(current);
            }
        }
    }

    private static bool ShouldSkipReset(BuildContext context, ResetResult result)
    {
        // Skip for library projects
        if (context.IsModLib)
        {
            context.LogInfo(
                "Skipping directory reset: IsModLib=true, library projects don't need deployment directory reset");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // Skip if no ModName
        if (string.IsNullOrEmpty(context.ModName))
        {
            result.Warnings.Add("ModName is not specified, cannot determine target directory");
            context.LogWarning("ModName is not specified, cannot determine target directory");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        return false;
    }

    /// <summary>
    /// The mod folder that the deploy steps write to. Shared with DeployManifest so both use the same folder.
    /// </summary>
    internal static string GetTargetModDirectory(BuildContext context)
    {
        // Prefer ModsDirectory (supports CI environment with Git root based paths)
        var modsDirectory = context.ModsDirectory;
        var modName = context.ModName;

        if (string.IsNullOrEmpty(modName))
        {
            throw new ArgumentException("ModName is not specified");
        }

        // If ModsDirectory is specified, use it directly (CI environment)
        if (!string.IsNullOrEmpty(modsDirectory))
        {
            return Path.Combine(modsDirectory, modName);
        }

        // Fallback to DuckovFolder for local development
        var duckovFolder = context.DuckovFolder;
        if (string.IsNullOrEmpty(duckovFolder))
        {
            throw new ArgumentException("Neither ModsDirectory nor DuckovFolder is specified");
        }

        return Path.Combine(duckovFolder, "Duckov_Data", "Mods", modName);
    }
}
