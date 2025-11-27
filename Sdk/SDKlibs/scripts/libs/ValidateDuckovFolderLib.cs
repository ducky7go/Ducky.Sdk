#nullable disable

using System;
using System.IO;
using System.Linq;

/// <summary>
/// Script library for Duckov folder validation
/// Entry point: ValidateDuckovFolderLib.Execute(BuildContext context)
/// </summary>
public class ValidateDuckovFolderLib
{
    /// <summary>
    /// Main entry point for Duckov folder validation
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Duckov Folder Validation Results ===");
            context.LogInfo($"Duckov Folder: {context.DuckovFolder}");
            context.LogInfo($"Steam Folder: {context.SteamFolder}");

            // Skip validation for library projects - they don't need game installation
            if (context.IsModLib)
            {
                context.LogInfo("Duckov Folder: Not required for library projects");
                context.LogInfo("Valid: True");
                context.LogInfo($"Validation Time: {DateTime.UtcNow:u}");
                context.LogInfo("✅ Duckov folder validation skipped (library project)");
                return SkipExitCode;
            }

            var result = ValidateDuckovFolder(context.DuckovFolder ?? "", context.SteamFolder ?? "");

            context.LogInfo($"Valid: {result.Valid}");
            context.LogInfo($"Validation Time: {result.ValidationTime:u}");

            if (result.Warnings.Any())
            {
                context.LogInfo("Warnings:");
                foreach (var warning in result.Warnings)
                {
                    context.LogInfo($"  ⚠️  {warning}");
                }
            }

            if (result.Errors.Any())
            {
                context.LogError("Errors:");
                foreach (var error in result.Errors)
                {
                    context.LogError($"  ❌ {error}");
                }
            }

            if (result.Valid)
            {
                context.LogInfo("✅ Duckov folder validation passed");
                return 0;
            }
            else
            {
                context.LogError("❌ Duckov folder validation failed");
                return 1;
            }
        }
        catch (Exception ex)
        {
            context.LogError($"Duckov folder validation exception: {ex.Message}");
            return 1;
        }
    }

    private static ValidationResponse ValidateDuckovFolder(string duckovFolder, string steamFolder)
    {
        var response = new ValidationResponse
        {
            Valid = true,
            ValidationTime = DateTime.UtcNow
        };

        if (string.IsNullOrEmpty(duckovFolder))
        {
            response.Valid = false;
            response.Errors.Add("DuckovFolder is not set");
            return response;
        }

        if (!Directory.Exists(duckovFolder))
        {
            response.Valid = false;
            response.Errors.Add($"DuckovFolder does not exist: {duckovFolder}");
            response.Warnings.Add("Recommendation: Ensure the game is installed and the path is correct");
            return response;
        }

        // Check for typical Duckov installation structure
        var managedDir = Path.Combine(duckovFolder, "Duckov_Data", "Managed");
        var modsDir = Path.Combine(duckovFolder, "Duckov_Data", "Mods");
        var steamApi = Path.Combine(duckovFolder, "steam_api.dll");

        if (!Directory.Exists(managedDir))
        {
            response.Warnings.Add("Managed directory not found (may indicate incomplete installation)");
        }

        if (!Directory.Exists(modsDir))
        {
            response.Warnings.Add("Mods directory not found (may need to create)");
        }

        if (!File.Exists(steamApi))
        {
            response.Warnings.Add("Steam API not found (may indicate non-Steam installation)");
        }

        // Validate Steam folder if provided
        if (!string.IsNullOrEmpty(steamFolder))
        {
            if (!Directory.Exists(steamFolder))
            {
                response.Warnings.Add($"Steam folder does not exist: {steamFolder}");
            }
        }

        return response;
    }

    private class ValidationResponse
    {
        public bool Valid { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
