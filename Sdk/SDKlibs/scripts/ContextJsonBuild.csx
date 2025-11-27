#!/usr/bin/env dotnet-script
#nullable disable

#load "shared/BuildContext.cs"

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text;

/// <summary>
/// Standalone script for generating BuildContext JSON file
/// This replaces the complex MSBuild JSON serialization logic with clean C# code
/// Usage: dotnet script ContextJsonBuild.csx <projectDirectory> [ModName] [Configuration] [TargetFramework] [DuckovFolder] [SteamFolder] [AssetsDir] [LocalizationAssetsDir] [EnableILRepack] [EnableGlobalUsing] [IncludeHarmony] [DeployMod] [ExcludeSdkLib] [IsModLib]
/// </summary>

// Validate arguments
if (Args.Count < 2)
{
    Console.Error.WriteLine("Usage: dotnet script ContextJsonBuild.csx 1 <projectDirectory> 2 <ModName> 3 <Configuration> ...");
    Console.Error.WriteLine("Arguments must be in pairs: <number> <value>");
    Console.Error.WriteLine($"Error: Expected at least 2 arguments, got {Args.Count}");
    Environment.Exit(1);
}

if (Args.Count % 2 != 0)
{
    Console.Error.WriteLine("Usage: dotnet script ContextJsonBuild.csx 1 <projectDirectory> 2 <ModName> 3 <Configuration> ...");
    Console.Error.WriteLine("Arguments must be in pairs: <number> <value>");
    Console.Error.WriteLine($"Error: Expected even number of arguments, got {Args.Count}");
    Environment.Exit(1);
}

try
{
    Console.WriteLine($"[ContextJsonBuild] Starting with {Args.Count} arguments");

    // Parse arguments in format: 1 value1 2 value2 3 value3 ...
    var argsDict = new Dictionary<string, string>();
    for (int i = 0; i < Args.Count; i += 2)
    {
        if (i + 1 >= Args.Count)
        {
            Console.Error.WriteLine($"Error: Missing value for argument key '{Args[i]}' at position {i}");
            Environment.Exit(1);
        }

        var key = Args[i] ?? "";
        var value = Args[i + 1] ?? "";

        if (string.IsNullOrEmpty(key))
        {
            Console.Error.WriteLine($"Error: Empty argument key at position {i}");
            Environment.Exit(1);
        }

        argsDict[key] = value;
    }

    var projectDir = argsDict.GetValueOrDefault("1", "");
    if (string.IsNullOrEmpty(projectDir))
    {
        Console.Error.WriteLine("Error: Project directory (argument 1) cannot be null or empty");
        Environment.Exit(1);
    }

    if (!Directory.Exists(projectDir))
    {
        Console.Error.WriteLine($"Error: Project directory does not exist: {projectDir}");
        Environment.Exit(1);
    }

    var objDir = Path.Combine(projectDir, "obj");
    var jsonFilePath = Path.Combine(objDir, "ducky-build-context.json");

    // Create obj directory if it doesn't exist
    try
    {
        Directory.CreateDirectory(objDir);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: Failed to create obj directory '{objDir}': {ex.Message}");
        Environment.Exit(1);
    }

    // Parse parameters from dictionary with safe defaults
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

    // Parse boolean parameters safely
    var enableILRepack = ParseSafeBool(argsDict, "9", false);
    var enableGlobalUsing = ParseSafeBool(argsDict, "10", true);
    var includeHarmony = ParseSafeBool(argsDict, "11", false);
    var deployMod = ParseSafeBool(argsDict, "12", true);
    var excludeSdkLib = ParseSafeBool(argsDict, "13", false);
    var isModLib = ParseSafeBool(argsDict, "14", false);

    Console.WriteLine($"[ContextJsonBuild] ProjectDir: {projectDir}");
    Console.WriteLine($"[ContextJsonBuild] ModName: {modName}");
    Console.WriteLine($"[ContextJsonBuild] Args Count: {Args.Count}");
    Console.WriteLine($"[ContextJsonBuild] All Args: {string.Join(", ", Args)}");
    Console.WriteLine($"[ContextJsonBuild] Parsed: {string.Join(", ", argsDict.Select(kv => $"{kv.Key}={kv.Value}"))}");

    // Create BuildContext with direct initialization - no reflection needed!
    var context = new BuildContext
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
        AssetsDir = assetsDir,
        LocalizationAssetsDir = localizationAssetsDir,
        EnableILRepack = enableILRepack,
        EnableGlobalUsing = enableGlobalUsing,
        IncludeHarmony = includeHarmony,
        DeployMod = deployMod,
        ExcludeSdkLib = excludeSdkLib,
        IsModLib = isModLib
    };

    var jsonContent = context.ToJson();

    // Write to file
    File.WriteAllText(jsonFilePath, jsonContent, new UTF8Encoding(false));

    Console.WriteLine($"[ContextJsonBuild] Generated BuildContext JSON: {jsonFilePath}");
    Console.WriteLine($"[ContextJsonBuild] Project: {Path.GetFileName(projectDir)}");
    Console.WriteLine($"[ContextJsonBuild] Configuration: {context.Configuration}");
    Console.WriteLine($"[ContextJsonBuild] ModName: {context.ModName ?? "(none)"}");
    Console.WriteLine($"[ContextJsonBuild] IsModLib: {context.IsModLib}");

    Environment.Exit(0);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ContextJsonBuild][ERROR] {ex.Message}");
    Console.Error.WriteLine($"[ContextJsonBuild][ERROR] Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}

string GetSafeString(Dictionary<string, string> dict, string key, string defaultValue)
{
    if (!dict.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
    {
        return defaultValue;
    }
    // Trim whitespace and newlines to prevent issues in JSON generation
    return value.Trim();
}

bool ParseSafeBool(Dictionary<string, string> dict, string key, bool defaultValue)
{
    if (!dict.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
    {
        return defaultValue;
    }

    if (bool.TryParse(value, out var result))
    {
        return result;
    }

    Console.WriteLine($"[ContextJsonBuild][WARN] Invalid boolean value for {key}: '{value}', using default: {defaultValue}");
    return defaultValue;
}