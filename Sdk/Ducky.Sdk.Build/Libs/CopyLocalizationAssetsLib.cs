#nullable disable

using System;
using System.IO;
using System.Linq;

/// <summary>
/// Script library for copying localization assets with change detection and optimization
/// Entry point: CopyLocalizationAssetsLib.Execute(BuildContext context)
/// </summary>
public class CopyLocalizationAssetsLib
{
    public class CopyResult
    {
        public bool Success { get; set; }
        public int FilesCopied { get; set; }
        public int FilesSkipped { get; set; }
        public List<string> CopiedFiles { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public DateTime CopiedAt { get; set; } = DateTime.UtcNow;
        public int ExitCode { get; set; }
    }

    /// <summary>
    /// Main entry point for copying localization assets
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Copy Localization Assets Results ===");

            var result = CopyAssets(context);

            context.LogInfo($"Success: {result.Success}");
            context.LogInfo($"Files Copied: {result.FilesCopied}");
            context.LogInfo($"Files Skipped: {result.FilesSkipped}");
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
            context.LogError($"Copy localization assets exception: {ex.Message}");
            return 1;
        }
    }

    public static CopyResult CopyAssets(BuildContext context)
    {
        var result = new CopyResult();

        try
        {
            context.LogInfo("Starting localization asset copying");

            // Check if this should be skipped
            if (ShouldSkipCopying(context, result))
            {
                return result;
            }

            var sourceDir = GetSourceDirectory(context);
            var targetDirs = GetTargetDirectories(context);

            if (string.IsNullOrEmpty(sourceDir))
            {
                context.LogInfo("No localization source directory found (assets/Locales doesn't exist)");
                result.Success = true;
                return result;
            }

            if (targetDirs.Count == 0)
            {
                context.LogInfo("No localization target directories specified");
                result.Success = true;
                return result;
            }

            foreach (var targetDir in targetDirs)
            {
                BuildUtils.EnsureDirectoryExists(targetDir);
                CopyFromDirectory(sourceDir!, targetDir, context, result);
            }

            result.Success = true;
            context.LogInfo(
                $"Localization asset copying completed: {result.FilesCopied} copied, {result.FilesSkipped} skipped");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Asset copying error: {ex.Message}";
            context.LogError($"Exception: {ex}");
        }

        return result;
    }

    private static bool ShouldSkipCopying(BuildContext context, CopyResult result)
    {
        // Check if localization processing is enabled
        if (!context.ShouldProcessLocalization)
        {
            context.LogInfo("Skipping asset copying: ShouldProcessLocalization=false");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // Skip if no localization assets directory configured
        if (string.IsNullOrEmpty(context.LocalizationAssetsDir))
        {
            context.LogInfo("Skipping asset copying: LocalizationAssetsDir not specified");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        // skip if any LocalizationAssetsDir is the same as assets/Locales
        var targetDirs = GetTargetDirectories(context);
        var sourceDir = GetSourceDirectory(context);
        if (!string.IsNullOrEmpty(sourceDir))
        {
            foreach (var targetDir in targetDirs)
            {
                if (string.Equals(Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar),
                        Path.GetFullPath(targetDir).TrimEnd(Path.DirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase))
                {
                    context.LogInfo(
                        "Skipping asset copying: LocalizationAssetsDir is the same as assets/Locales directory");
                    result.Success = true;
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetSourceDirectory(BuildContext context)
    {
        // Source is always the project's assets/Locales directory
        var assetsDir = string.IsNullOrEmpty(context.AssetsDir) ? "assets" : context.AssetsDir;
        var projectAssetsDir = context.GetFullPath(assetsDir);
        var projectLocalesDir = Path.Combine(projectAssetsDir, "Locales");

        if (Directory.Exists(projectLocalesDir))
        {
            return projectLocalesDir;
        }

        return null;
    }

    private static List<string> GetTargetDirectories(BuildContext context)
    {
        var targetDirs = new List<string>();

        if (!string.IsNullOrEmpty(context.LocalizationAssetsDir))
        {
            // Support multiple directories separated by semicolon
            var dirs = context.LocalizationAssetsDir.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var dir in dirs)
            {
                var fullPath = context.GetFullPath(dir.Trim());

                // Ensure target directory ends with "Locales"
                if (!fullPath.EndsWith("Locales", StringComparison.OrdinalIgnoreCase))
                {
                    fullPath = Path.Combine(fullPath, "Locales");
                }

                targetDirs.Add(fullPath);
            }
        }

        return targetDirs.Distinct().ToList();
    }

    private static void CopyFromDirectory(string sourceDir, string targetDir, BuildContext context, CopyResult result)
    {
        try
        {
            context.LogInfo($"Copying from: {sourceDir}");
            context.LogInfo($"Copying to: {targetDir}");

            // Copy all files recursively
            var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

            foreach (var sourceFile in files)
            {
                var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                var targetFile = Path.Combine(targetDir, relativePath.ToString());

                // Ensure target directory exists
                var targetFileDir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetFileDir))
                {
                    BuildUtils.EnsureDirectoryExists(targetFileDir);
                }

                // Check if file needs to be copied
                if (ShouldCopyFile(sourceFile, targetFile, context))
                {
                    File.Copy(sourceFile, targetFile, true);
                    result.FilesCopied++;
                    result.CopiedFiles.Add(targetFile);
                }
                else
                {
                    result.FilesSkipped++;
                }
            }
        }
        catch (Exception ex)
        {
            context.LogError($"Error copying from {sourceDir}: {ex.Message}");
            throw;
        }
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
