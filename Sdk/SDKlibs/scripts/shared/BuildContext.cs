#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

/// <summary>
/// Provides context and utilities for CSX build scripts.
/// Encapsulates MSBuild properties and provides common operations.
/// </summary>
public class BuildContext
{
    public required string ProjectDir { get; init; }
    public required string Configuration { get; init; }
    public required string TargetFramework { get; init; }
    public required string IntermediateOutputPath { get; init; }
    public required string BaseIntermediateOutputPath { get; init; }
    public required string OutputPath { get; init; }

    // Core SDK Properties

    /// <summary>
    /// OutputPath: Path to the compiled assembly output directory.
    /// AFFECTS: ILRepack input assembly location, assembly discovery
    /// USAGE: Contains the final compiled DLL for ILRepack processing
    /// EXAMPLE: "bin/Debug/netstandard2.1"
    /// </summary>
    /// <summary>
    /// ModName: The name of the mod being built.
    /// AFFECTS: info.ini generation, mod metadata, output assembly naming, mod deployment paths
    /// REQUIRED: Yes for mod projects, optional for pure libraries
    /// </summary>
    public string? ModName { get; init; }

    /// <summary>
    /// DuckovFolder: Path to the game installation directory.
    /// AFFECTS: Game dependency detection, mod deployment, managed assembly resolution
    /// FALLBACK: Can be derived from SteamFolder/steamapps/common/Escape from Duckov
    /// </summary>
    public string? DuckovFolder { get; init; }

    /// <summary>
    /// SteamFolder: Path to Steam installation directory.
    /// AFFECTS: Game path resolution (used to find DuckovFolder if not specified)
    /// USAGE: Optional - only used when DuckovFolder is not explicitly set
    /// </summary>
    public string? SteamFolder { get; init; }

    /// <summary>
    /// ManagedDirectory: Path to game's managed assemblies folder.
    /// Can be explicitly set for CI environments, otherwise derived from DuckovFolder.
    /// AFFECTS: Game dependency detection, assembly reference resolution
    /// USAGE: Pass via environment variable or MSBuild property
    /// DEFAULT: Derived from DuckovFolder + "Duckov_Data/Managed"
    /// </summary>
    public string? ManagedDirectory_Explicit { get; init; }

    /// <summary>
    /// ModsDirectory: Path to game's Mods folder.
    /// Can be explicitly set for CI environments, otherwise derived from DuckovFolder.
    /// AFFECTS: Final mod deployment location
    /// USAGE: Pass via environment variable or MSBuild property
    /// DEFAULT: Derived from DuckovFolder + "Duckov_Data/Mods"
    /// </summary>
    public string? ModsDirectory_Explicit { get; init; }

    /// <summary>
    /// IsCIEnvironment: Indicates if running in CI environment.
    /// AFFECTS: Validation behavior (relaxes SteamFolder requirement)
    /// DEFAULT: Detected from CI environment variable
    /// </summary>
    public bool IsCIEnvironment { get; init; } =
        Environment.GetEnvironmentVariable("CI") == "true" ||
        Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ||
        Environment.GetEnvironmentVariable("AZURE_PIPELINES") == "true" ||
        Environment.GetEnvironmentVariable("GITLAB_CI") == "true";

    /// <summary>
    /// AssetsDir: Primary directory for mod assets (info.ini, preview.png, etc.).
    /// AFFECTS: Asset generation, mod metadata file locations, preview image generation
    /// DEFAULT: "assets" if not specified
    /// </summary>
    public string? AssetsDir { get; init; }

    /// <summary>
    /// LocalizationAssetsDir: Directories where localization assets should be copied.
    /// SUPPORTS: Multiple directories separated by semicolon (;)
    /// AFFECTS: CSV/JSON localization file distribution, lkeys.json placement
    /// DEFAULT: Uses AssetsDir if not specified
    /// </summary>
    public string? LocalizationAssetsDir { get; init; }

    /// <summary>
    /// EnableILRepack: Controls whether to merge mod dependencies into single assembly.
    /// AFFECTS: ILRepack execution vs individual dependency copying
    /// IMPACT: Reduces dependency conflicts, larger single file vs multiple files
    /// DEFAULT: true
    /// </summary>
    public bool EnableILRepack { get; init; } = true;

    /// <summary>
    /// EnableGlobalUsing: Controls automatic global using directive injection.
    /// AFFECTS: Compilation - adds commonly used namespaces globally
    /// INCLUDES: SDK, game engine, third-party namespaces based on detected dependencies
    /// DEFAULT: true
    /// </summary>
    public bool EnableGlobalUsing { get; init; } = true;

    /// <summary>
    /// IncludeHarmony: Controls whether to include Harmony references.
    /// AFFECTS: Assembly references, patching capabilities
    /// USAGE: For mods that need Harmony-based modding framework
    /// DEFAULT: false
    /// </summary>
    public bool IncludeHarmony { get; init; }

    /// <summary>
    /// ExcludeSdkLib: Controls inclusion of SDK-provided library functionality.
    /// AFFECTS (ONLY):
    /// - SDK-provided code inclusion (standard library functions, utilities)
    /// - Global using directives for SDK namespaces
    /// - Automatic reference addition for SDK assemblies
    /// DOES NOT AFFECT:
    /// - Mod-specific asset generation (preview.png, info.ini, lkeys.json)
    /// - Mod-specific localization processing (CSV files, translation assets)
    /// - Mod deployment and packaging
    /// USAGE:
    /// - true: For mods that want to avoid SDK standard features, use own implementations
    /// - false: For mods that benefit from SDK-provided common functionality
    /// DEFAULT: false
    /// </summary>
    public bool ExcludeSdkLib { get; init; } = false;


    /// <summary>
    /// IsModLib: Controls whether this project is treated as a library project vs mod project.
    /// EXTERNALLY SET: Should be explicitly configured in the project file, not auto-detected
    /// AFFECTS: Build automation decisions, asset generation scope, deployment behavior
    /// USAGE:
    /// - true: For library projects that may skip certain mod-specific automation
    /// - false: For regular mod projects with full automation
    /// NOTE: This replaces the old auto-detection logic based on project path
    /// DEFAULT: false (treat as mod project by default)
    /// </summary>
    public bool IsModLib { get; init; } = false;

    // Dependency Resolution Properties

    /// <summary>
    /// MainAssemblyPath: Path to the primary mod assembly.
    /// COMPUTED: Updated by UpdateBuildContextAfterBuildLib after compilation
    /// AFFECTS: ILRepack input identification, dependency filtering
    /// </summary>
    public string? MainAssemblyPath { get; set; }

    /// <summary>
    /// DependencyAssemblies: List of dependency assembly paths.
    /// COMPUTED: Scanned and populated by UpdateBuildContextAfterBuildLib after compilation
    /// AFFECTS: ILRepack merging, deployment decisions
    /// </summary>
    public List<string>? DependencyAssemblies { get; set; }

    // Computed Properties

    /// <summary>
    /// ModsDirectory: Computed path to game's Mods folder.
    /// Uses explicit value if provided, otherwise derives from DuckovFolder.
    /// AFFECTS: Final mod deployment location
    /// </summary>
    public string ModsDirectory => ModsDirectory_Explicit ?? (DuckovFolder != null ? Path.Combine(DuckovFolder, "Duckov_Data", "Mods") : "");

    /// <summary>
    /// ManagedDirectory: Computed path to game's managed assemblies folder.
    /// Uses explicit value if provided, otherwise derives from DuckovFolder.
    /// AFFECTS: Game dependency detection, assembly reference resolution
    /// </summary>
    public string ManagedDirectory => ManagedDirectory_Explicit ?? (DuckovFolder != null ? Path.Combine(DuckovFolder, "Duckov_Data", "Managed") : "");

    // Project Type Configuration

    // Deployment Configuration

    /// <summary>
    /// DeployMod: Controls whether to deploy built mod to game's Mods folder.
    /// AFFECTS: Final copy step (CopyToDuckov target) only
    /// SEPARATE: From asset generation - assets should be generated regardless of this setting
    /// DEFAULT: true
    /// </summary>
    public bool DeployMod { get; init; } = true;

    // Localization Processing

    /// <summary>
    /// ShouldProcessLocalization: Controls whether localization processing should run.
    /// COMPUTED: Based on project type and ExcludeSdkLib setting in CSX logic
    /// AFFECTS: Key extraction, CSV generation, asset copying
    /// SCOPE: Mod-specific localization only, not SDK standard library files
    /// </summary>
    public bool ShouldProcessLocalization => true; // Will be computed by CSX logic

    /// <summary>
    /// HasDependencies: Computed property indicating whether the mod has external dependencies.
    /// COMPUTED: Based on DependencyAssemblies list
    /// AFFECTS: Build decisions, deployment strategy
    /// </summary>
    public bool HasDependencies => DependencyAssemblies?.Count > 0;

    /// <summary>
    /// ShouldUseILRepack: Computed property indicating whether ILRepack should be used.
    /// COMPUTED: Based on EnableILRepack flag and HasDependencies
    /// AFFECTS: ILRepack execution decision
    /// </summary>
    public bool ShouldUseILRepack => EnableILRepack && HasDependencies;

    // Utility Methods
    public string GetFullPath(string relativePath) => Path.GetFullPath(Path.Combine(ProjectDir, relativePath));

    public bool Exists(string relativePath) =>
        File.Exists(GetFullPath(relativePath)) || Directory.Exists(GetFullPath(relativePath));

    public void LogInfo(string message) => Console.WriteLine($"[BuildContext] {message}");
    public void LogWarning(string message) => Console.WriteLine($"[BuildContext][WARN] {message}");
    public void LogError(string message) => Console.Error.WriteLine($"[BuildContext][ERROR] {message}");

    /// <summary>
    /// Creates a BuildContext from command line arguments
    /// Expected format: script.csx -- --project-dir <path> --configuration <config> ...
    /// </summary>
    public static BuildContext CreateFromArgs(string[] args)
    {
        var context = new Dictionary<string, string>();

        for (int i = 0; i < args.Length - 1; i += 2)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i].Substring(2).Replace("-", "");
                context[key] = args[i + 1];
            }
        }

        return new BuildContext
        {
            ProjectDir = context.GetValueOrDefault("projectdir", Directory.GetCurrentDirectory()),
            Configuration = context.GetValueOrDefault("configuration", "Debug"),
            TargetFramework = context.GetValueOrDefault("targetframework", "net8.0"),
            IntermediateOutputPath = context.GetValueOrDefault("intermediateoutputpath", "obj/Debug/net8.0"),
            BaseIntermediateOutputPath = context.GetValueOrDefault("baseintermediateoutputpath", "obj"),
            ModName = context.GetValueOrDefault("modname"),
            DuckovFolder = context.GetValueOrDefault("duckovfolder"),
            SteamFolder = context.GetValueOrDefault("steamfolder"),
            ManagedDirectory_Explicit = context.GetValueOrDefault("manageddirectory"),
            ModsDirectory_Explicit = context.GetValueOrDefault("modsdirectory"),
            AssetsDir = context.GetValueOrDefault("assetsdir", "assets"),
            LocalizationAssetsDir = context.GetValueOrDefault("localizationassetsdir"),
            EnableILRepack = bool.Parse(context.GetValueOrDefault("enableilrepack", "true")),
            EnableGlobalUsing = bool.Parse(context.GetValueOrDefault("enableglobalusing", "true")),
            IncludeHarmony = bool.Parse(context.GetValueOrDefault("includeharmony", "false")),
            DeployMod = bool.Parse(context.GetValueOrDefault("deploymod", "true")),
            ExcludeSdkLib = bool.Parse(context.GetValueOrDefault("excludesdklib", "false")),
            IsModLib = bool.Parse(context.GetValueOrDefault("ismodlib", "false")),
            OutputPath = context.GetValueOrDefault("outputpath",
                Path.Combine(context.GetValueOrDefault("projectdir", Directory.GetCurrentDirectory()), "bin",
                    context.GetValueOrDefault("configuration", "Debug"),
                    context.GetValueOrDefault("targetframework", "net8.0"),
                    $"{context.GetValueOrDefault("modname", "mod")}.dll")),
        };
    }

    private const int SkipExitCode = 0;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes the context to JSON for caching or passing between processes
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, _jsonOptions);

    /// <summary>
    /// Creates a BuildContext from JSON string
    /// </summary>
    public static BuildContext? FromJson(string json) => JsonSerializer.Deserialize<BuildContext>(json, _jsonOptions);

    /// <summary>
    /// Creates a BuildContext by loading JSON from obj folder
    /// Prioritizes JSON file over command line arguments for consistency
    /// </summary>
    public static BuildContext LoadFromProjectDirectory(string projectDir)
    {
        var objDir = Path.Combine(projectDir, "obj");
        var contextJsonFile = Path.Combine(objDir, "ducky-build-context.json");

        if (File.Exists(contextJsonFile))
        {
            try
            {
                var json = File.ReadAllText(contextJsonFile);
                var context = FromJson(json);
                if (context != null)
                {
                    Console.WriteLine($"[BuildContext] Loaded context from: {contextJsonFile}");
                    return context;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BuildContext][WARN] Failed to load JSON context: {ex.Message}");
            }
        }

        // Fallback to command line arguments
        Console.WriteLine($"[BuildContext] JSON context not found, using command line arguments");
        return CreateFromArgs(Environment.GetCommandLineArgs().Skip(1).ToArray());
    }
}

/// <summary>
/// Common utilities for build operations
/// </summary>
public static class BuildUtils
{
    /// <summary>
    /// Safely creates a directory if it doesn't exist
    /// </summary>
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <summary>
    /// Checks if a file has changed based on timestamp
    /// </summary>
    public static bool HasFileChanged(string filePath, DateTime lastCheck)
    {
        return File.Exists(filePath) && File.GetLastWriteTime(filePath) > lastCheck;
    }

    /// <summary>
    /// Gets all files matching pattern in directory and subdirectories
    /// </summary>
    public static IEnumerable<string> GetFiles(string path, string searchPattern,
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(path))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    /// <summary>
    /// Executes a command and returns output
    /// </summary>
    public static (int exitCode, string output) ExecuteCommand(string command, string workingDirectory,
        params string[] args)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = string.Join(" ", args.Select(arg => $"\"{arg}\"")),
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(error))
        {
            output += Environment.NewLine + error;
        }

        return (process.ExitCode, output);
    }
}
