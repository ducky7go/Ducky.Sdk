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
        public bool DirectoryDeleted { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime ResetAt { get; set; } = DateTime.UtcNow;
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
            context.LogInfo($"Directory Deleted: {result.DirectoryDeleted}");
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

            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            context.LogError($"Reset deploy directory exception: {ex.Message}");
            context.LogError($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    /// <summary>
    /// Reset deployment directory by removing the target mod directory completely
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

                // Delete the entire directory
                try
                {
                    Directory.Delete(targetModDir, true);
                    result.DirectoryDeleted = true;
                    context.LogInfo($"Successfully deleted deployment directory: {targetModDir}");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to delete deployment directory '{targetModDir}': {ex.Message}");
                    context.LogError($"Failed to delete deployment directory: {ex.Message}");
                    return result;
                }
            }
            else
            {
                context.LogInfo($"Target deployment directory does not exist: {targetModDir}");
                result.DirectoryDeleted = true; // No need to delete
            }

            result.Success = true;
            context.LogInfo("Deployment directory reset completed successfully");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Reset operation failed: {ex.Message}");
            context.LogError($"Exception: {ex}");
        }

        return result;
    }

    private static bool ShouldSkipReset(BuildContext context, ResetResult result)
    {
        // Skip for library projects
        if (context.IsModLib)
        {
            context.LogInfo(
                "Skipping directory reset: IsModLib=true, library projects don't need deployment directory reset");
            result.Success = true;
            return true;
        }

        // Skip if no ModName
        if (string.IsNullOrEmpty(context.ModName))
        {
            result.Warnings.Add("ModName is not specified, cannot determine target directory");
            context.LogWarning("ModName is not specified, cannot determine target directory");
            result.Success = true;
            return true;
        }

        return false;
    }

    private static string GetTargetModDirectory(BuildContext context)
    {
        // Calculate the target mod directory: {DuckovFolder}/Mods/{ModName}
        var duckovFolder = context.DuckovFolder;
        var modName = context.ModName;

        if (string.IsNullOrEmpty(duckovFolder))
        {
            throw new ArgumentException("DuckovFolder is not specified");
        }

        if (string.IsNullOrEmpty(modName))
        {
            throw new ArgumentException("ModName is not specified");
        }

        return Path.Combine(duckovFolder, "Duckov_Data", "Mods", modName);
    }
}
