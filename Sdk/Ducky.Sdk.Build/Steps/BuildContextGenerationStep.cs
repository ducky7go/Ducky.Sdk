#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Port of the former ContextJsonBuild.csx. GenerateBuildContext (MSBuild, BeforeBuild) writes the 19 property
/// values to a file next to the BuildContext JSON as lines "n=value"; this step turns them into the same
/// obj/ducky-build-context.json that the dotnet-script version wrote.
/// </summary>
internal static class BuildContextGenerationStep
{
    /// <summary>File name, placed in the same directory as ducky-build-context.json.</summary>
    internal const string PropertiesFileName = "ducky-build-properties.txt";

    /// <summary>
    /// Reads the property file and writes the BuildContext JSON. Returns 0 on success, 1 on failure.
    /// </summary>
    internal static int Execute(string propertiesPath, string buildContextPath)
    {
        if (!File.Exists(propertiesPath))
        {
            Console.Error.WriteLine($"[ContextGeneration][ERROR] {RunnerErrors.MissingBuildProperties(propertiesPath)}");
            return 1;
        }

        try
        {
            var values = ReadProperties(File.ReadAllLines(propertiesPath, Encoding.UTF8));
            var context = CreateContext(values, out var projectDir);

            var objDir = Path.GetDirectoryName(buildContextPath);
            Directory.CreateDirectory(objDir);

            File.WriteAllText(buildContextPath, context.ToJson(), new UTF8Encoding(false));

            Console.WriteLine($"[ContextGeneration] Generated BuildContext JSON: {buildContextPath}");
            Console.WriteLine($"[ContextGeneration] Project: {Path.GetFileName(projectDir)}");
            Console.WriteLine($"[ContextGeneration] Configuration: {context.Configuration}");
            Console.WriteLine($"[ContextGeneration] ModName: {context.ModName ?? "(none)"}");
            Console.WriteLine($"[ContextGeneration] IsModLib: {context.IsModLib}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ContextGeneration][ERROR] {ex.Message}");
            Console.Error.WriteLine($"[ContextGeneration][ERROR] Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    /// <summary>Parses "n=value" lines. The value is everything after the first '='; it may be empty.</summary>
    internal static Dictionary<string, string> ReadProperties(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new FormatException($"Invalid line in the build properties file: '{line}'. Expected 'n=value'.");
            }

            values[line.Substring(0, separator)] = line.Substring(separator + 1);
        }

        return values;
    }

    /// <summary>
    /// Builds the BuildContext from the property values. Same defaults and parsing as ContextJsonBuild.csx.
    /// </summary>
    internal static BuildContext CreateContext(IReadOnlyDictionary<string, string> argsDict, out string projectDir)
    {
        projectDir = argsDict.GetValueOrDefault("1", "");
        if (string.IsNullOrEmpty(projectDir))
        {
            throw new InvalidOperationException("Project directory (property 1) is empty in the build properties file.");
        }

        if (!Directory.Exists(projectDir))
        {
            throw new DirectoryNotFoundException($"Project directory does not exist: {projectDir}");
        }

        var modName = argsDict.GetValueOrDefault("2", "");
        var configuration = GetSafeString(argsDict, "3", "Debug");
        var targetFramework = GetSafeString(argsDict, "4", "netstandard2.1");
        var duckovFolder = GetSafeString(argsDict, "5", "");
        var steamFolder = GetSafeString(argsDict, "6", "");
        var assetsDir = GetSafeString(argsDict, "7", Path.Combine(projectDir, "assets"));
        var localizationAssetsDir = GetSafeString(argsDict, "8", "");
        var intermediateOutputPath = GetSafeString(argsDict, "15", Path.Combine(projectDir, "obj", configuration, targetFramework));
        var baseIntermediateOutputPath = GetSafeString(argsDict, "16", Path.Combine(projectDir, "obj"));
        var outputPath = GetSafeString(argsDict, "17", Path.Combine(projectDir, "bin", configuration, targetFramework));
        var managedDirectory = GetSafeString(argsDict, "18", "");
        var modsDirectory = GetSafeString(argsDict, "19", "");

        return new BuildContext
        {
            ProjectDir = projectDir,
            Configuration = configuration,
            TargetFramework = targetFramework,
            IntermediateOutputPath = intermediateOutputPath,
            BaseIntermediateOutputPath = baseIntermediateOutputPath,
            OutputPath = outputPath,
            ModName = modName,
            DuckovFolder = duckovFolder,
            SteamFolder = steamFolder,
            ManagedDirectory_Explicit = string.IsNullOrEmpty(managedDirectory) ? null : managedDirectory,
            ModsDirectory_Explicit = string.IsNullOrEmpty(modsDirectory) ? null : modsDirectory,
            AssetsDir = assetsDir,
            LocalizationAssetsDir = localizationAssetsDir,
            EnableILRepack = ParseSafeBool(argsDict, "9", false),
            EnableGlobalUsing = ParseSafeBool(argsDict, "10", true),
            IncludeHarmony = ParseSafeBool(argsDict, "11", false),
            DeployMod = ParseSafeBool(argsDict, "12", true),
            ExcludeSdkLib = ParseSafeBool(argsDict, "13", false),
            IsModLib = ParseSafeBool(argsDict, "14", false),
        };
    }

    private static string GetSafeString(IReadOnlyDictionary<string, string> dict, string key, string defaultValue)
    {
        if (!dict.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        // Trim whitespace and newlines to prevent issues in JSON generation
        return value.Trim();
    }

    private static bool ParseSafeBool(IReadOnlyDictionary<string, string> dict, string key, bool defaultValue)
    {
        if (!dict.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        Console.WriteLine($"[ContextGeneration][WARN] Invalid boolean value for {key}: '{value}', using default: {defaultValue}");
        return defaultValue;
    }
}
