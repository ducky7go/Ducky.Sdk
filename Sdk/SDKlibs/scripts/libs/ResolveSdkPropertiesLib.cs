#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Script library for centralized SDK property resolution logic
/// Entry point: ResolveSdkPropertiesLib.Execute(BuildContext context)
/// </summary>
public class ResolveSdkPropertiesLib
{
    public class SdkProperties
    {
        // Core Project Properties
        public bool ShouldProcessLocalization { get; set; }
        public string EffectiveLocalizationAssetsDir { get; set; } = "";
        public bool HasMultipleLocalizationDirs { get; set; }
        public string PrimaryLocalizationDir { get; set; } = "";

        // Path Resolution
        public string DuckovFolder { get; set; } = "";
        public string ModsDirectory { get; set; } = "";
        public string ManagedDirectory { get; set; } = "";

        // CI Support - Track if paths were explicitly set
        public bool HasExplicitManagedDirectory { get; set; }
        public bool HasExplicitModsDirectory { get; set; }

        // Project Type Configuration
        public bool IsModLib { get; set; } = false;
        public bool DeployMod { get; set; } = true;

        // Configuration Validation
        public bool HasValidConfiguration { get; set; }
        public List<string> ValidationErrors { get; set; } = new();

        // Game Dependency Detection
        public bool HasTeamSoda { get; set; }
        public bool HasUnity { get; set; }
        public bool HasFOW { get; set; }
        public bool HasSodaLocalization { get; set; }

        // Performance Metrics
        public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Main entry point for resolving SDK properties
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Resolve SDK Properties Results ===");

            var properties = Resolve(context);

            context.LogInfo($"Success: {properties.HasValidConfiguration}");
            context.LogInfo($"Should Process Localization: {properties.ShouldProcessLocalization}");
            context.LogInfo($"Effective Localization Assets Dir: {properties.EffectiveLocalizationAssetsDir}");
            context.LogInfo($"Duckov Folder: {properties.DuckovFolder}");
            context.LogInfo($"Managed Directory: {properties.ManagedDirectory} {(properties.HasExplicitManagedDirectory ? "(explicitly set)" : "(derived)")}");
            context.LogInfo($"Mods Directory: {properties.ModsDirectory} {(properties.HasExplicitModsDirectory ? "(explicitly set)" : "(derived)")}");
            context.LogInfo($"Resolved At: {properties.ResolvedAt:u}");

            if (properties.ValidationErrors.Any())
            {
                context.LogError("Validation Errors:");
                foreach (var error in properties.ValidationErrors)
                {
                    context.LogError($"  ❌ {error}");
                }
            }

            context.LogInfo("Game Dependencies:");
            context.LogInfo($"  TeamSoda: {properties.HasTeamSoda}");
            context.LogInfo($"  Unity: {properties.HasUnity}");
            context.LogInfo($"  FOW: {properties.HasFOW}");
            context.LogInfo($"  SodaLocalization: {properties.HasSodaLocalization}");

            return properties.HasValidConfiguration ? 0 : 1;
        }
        catch (Exception ex)
        {
            context.LogError($"Resolve SDK properties exception: {ex.Message}");
            return 1;
        }
    }

    public static SdkProperties Resolve(BuildContext context)
    {
        var properties = new SdkProperties();

        try
        {
            context.LogInfo("Starting SDK property resolution");

            // Copy basic properties from context
            properties.IsModLib = context.IsModLib;
            properties.DeployMod = context.DeployMod;

            // Resolve localization properties
            ResolveLocalizationProperties(context, properties);

            // Resolve path properties
            ResolvePathProperties(context, properties);

            // Detect game dependencies
            DetectGameDependencies(context, properties);

            // Validate configuration
            ValidateConfiguration(context, properties);

            context.LogInfo("SDK property resolution completed successfully");
        }
        catch (Exception ex)
        {
            properties.HasValidConfiguration = false;
            properties.ValidationErrors.Add($"Property resolution error: {ex.Message}");
            context.LogError($"Exception: {ex}");
        }

        return properties;
    }

    private static void ResolveLocalizationProperties(BuildContext context, SdkProperties properties)
    {
        context.LogInfo("Resolving localization properties...");

        // Determine if localization should be processed
        if (context.IsModLib && string.IsNullOrEmpty(context.LocalizationAssetsDir))
        {
            properties.ShouldProcessLocalization = false;
            context.LogInfo("Skipping localization for library project without LocalizationAssetsDir");
            return;
        }

        properties.ShouldProcessLocalization = !string.IsNullOrEmpty(context.LocalizationAssetsDir) ||
                                               !string.IsNullOrEmpty(context.AssetsDir);

        if (!properties.ShouldProcessLocalization)
        {
            context.LogInfo("Localization processing disabled: no directories specified");
            return;
        }

        // Resolve effective localization assets directory
        if (!string.IsNullOrEmpty(context.LocalizationAssetsDir))
        {
            properties.EffectiveLocalizationAssetsDir = context.LocalizationAssetsDir;
            properties.HasMultipleLocalizationDirs = context.LocalizationAssetsDir.Contains(';');
            properties.PrimaryLocalizationDir = context.LocalizationAssetsDir.Split(';')[0].Trim();
        }
        else
        {
            properties.EffectiveLocalizationAssetsDir = context.AssetsDir ?? "";
            properties.PrimaryLocalizationDir = properties.EffectiveLocalizationAssetsDir;
        }

        context.LogInfo($"Should Process Localization: {properties.ShouldProcessLocalization}");
        context.LogInfo($"Effective Localization Assets Dir: {properties.EffectiveLocalizationAssetsDir}");
        context.LogInfo($"Primary Localization Dir: {properties.PrimaryLocalizationDir}");
        context.LogInfo($"Has Multiple Localization Dirs: {properties.HasMultipleLocalizationDirs}");
    }

    private static void ResolvePathProperties(BuildContext context, SdkProperties properties)
    {
        context.LogInfo("Resolving path properties...");

        // Duckov folder
        properties.DuckovFolder = context.DuckovFolder ?? "";

        // Managed directory - preserve explicitly set value
        if (!string.IsNullOrEmpty(context.ManagedDirectory_Explicit))
        {
            properties.ManagedDirectory = context.ManagedDirectory_Explicit;
            properties.HasExplicitManagedDirectory = true;
            context.LogInfo("Using explicitly set ManagedDirectory");
        }
        else if (!string.IsNullOrEmpty(properties.DuckovFolder))
        {
            properties.ManagedDirectory = Path.Combine(properties.DuckovFolder, "Duckov_Data", "Managed");
            properties.HasExplicitManagedDirectory = false;
            context.LogInfo("Derived ManagedDirectory from DuckovFolder");
        }

        // Mods directory - preserve explicitly set value
        if (!string.IsNullOrEmpty(context.ModsDirectory_Explicit))
        {
            properties.ModsDirectory = context.ModsDirectory_Explicit;
            properties.HasExplicitModsDirectory = true;
            context.LogInfo("Using explicitly set ModsDirectory");
        }
        else if (!string.IsNullOrEmpty(properties.DuckovFolder))
        {
            properties.ModsDirectory = Path.Combine(properties.DuckovFolder, "Duckov_Data", "Mods");
            properties.HasExplicitModsDirectory = false;
            context.LogInfo("Derived ModsDirectory from DuckovFolder");
        }

        context.LogInfo($"Duckov Folder: {properties.DuckovFolder}");
        context.LogInfo($"Mods Directory: {properties.ModsDirectory}");
        context.LogInfo($"Managed Directory: {properties.ManagedDirectory}");
    }

    private static void DetectGameDependencies(BuildContext context, SdkProperties properties)
    {
        context.LogInfo("Detecting game dependencies...");

        var projectDir = context.ProjectDir;
        var objDir = context.BaseIntermediateOutputPath;

        // Check for TeamSoda references
        var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);
        properties.HasTeamSoda = csFiles.Any(f => File.ReadAllText(f).Contains("TeamSoda"));

        // Check for Unity references
        properties.HasUnity = csFiles.Any(f => File.ReadAllText(f).Contains("UnityEngine."));

        // Check for FOW references
        properties.HasFOW = csFiles.Any(f => File.ReadAllText(f).Contains("FOW."));

        // Check for SodaLocalization references
        properties.HasSodaLocalization = csFiles.Any(f => File.ReadAllText(f).Contains("SodaLocalization"));

        context.LogInfo(
            $"Game dependencies detected - TeamSoda: {properties.HasTeamSoda}, Unity: {properties.HasUnity}, FOW: {properties.HasFOW}, SodaLocalization: {properties.HasSodaLocalization}");
    }

    private static void ValidateConfiguration(BuildContext context, SdkProperties properties)
    {
        context.LogInfo("Validating configuration...");

        var isValid = true;
        var errors = new List<string>();

        // Validate Duckov folder - relaxed when paths are explicitly set
        if (properties.DeployMod && !properties.IsModLib)
        {
            // If paths are explicitly set, DuckovFolder is optional
            if (properties.HasExplicitManagedDirectory && properties.HasExplicitModsDirectory)
            {
                context.LogInfo("ManagedDirectory and ModsDirectory explicitly set - DuckovFolder not required");
            }
            else if (string.IsNullOrEmpty(properties.DuckovFolder))
            {
                errors.Add("DuckovFolder is required for mod deployment but not specified (or set ManagedDirectory/ModsDirectory explicitly)");
                isValid = false;
            }
            else if (!Directory.Exists(properties.DuckovFolder))
            {
                errors.Add($"DuckovFolder does not exist: {properties.DuckovFolder}");
                isValid = false;
            }
        }

        // Validate ModName if deployment is enabled
        if (properties.DeployMod && !properties.IsModLib && string.IsNullOrEmpty(context.ModName))
        {
            errors.Add("ModName is required for mod deployment but not specified");
            isValid = false;
        }

        // Validate localization directories
        if (properties.ShouldProcessLocalization)
        {
            if (string.IsNullOrEmpty(properties.EffectiveLocalizationAssetsDir))
            {
                errors.Add("Localization processing enabled but no valid directory found");
                isValid = false;
            }
            else if (!Directory.Exists(properties.EffectiveLocalizationAssetsDir))
            {
                context.LogWarning(
                    $"Localization assets directory does not exist: {properties.EffectiveLocalizationAssetsDir}");
            }
        }

        properties.HasValidConfiguration = isValid;
        properties.ValidationErrors = errors;

        if (isValid)
        {
            context.LogInfo("Configuration validation passed");
        }
        else
        {
            context.LogError($"Configuration validation failed with {errors.Count} error(s)");
        }
    }
}
