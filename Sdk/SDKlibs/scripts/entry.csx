#!/usr/bin/env dotnet-script
#nullable enable

#r "nuget: SixLabors.ImageSharp, 3.1.6"
#r "nuget: SixLabors.ImageSharp.Drawing, 2.1.4"
#r "nuget: Newtonsoft.Json, 13.0.3"
#load "shared/BuildContext.cs"
#load "shared/BuildResult.cs"
#load "libs/PrintResultLib.cs"
#load "libs/ValidateProjectPathLib.cs"
#load "libs/ExtractLocalizationKeysLib.cs"
#load "libs/ValidateDuckovFolderLib.cs"
#load "libs/UpdateLocalizationCsvLib.cs"
#load "libs/EnsureInfoIniLib.cs"
#load "libs/CopyLocalizationAssetsLib.cs"
#load "libs/ResetDeployLib.cs"
#load "libs/DeployModLib.cs"
#load "libs/ILRepackAssembliesLib.cs"
#load "libs/GeneratePreviewLib.cs"
#load "libs/ResolveSdkPropertiesLib.cs"
#load "libs/CopyDependenciesLib.cs"
#load "libs/CollectFromModLib.cs"
#load "libs/InitializeBuildLoggingLib.cs"

using System;
using System.IO;
using System.Linq;
using System.Reflection;

/// <summary>
/// Unified entry point for all Ducky SDK scripts
/// Usage: dotnet script entry.csx <buildContextJsonPath> <scriptLibrary> [additionalArgs...]
/// Examples:
///   dotnet script entry.csx ./obj/ducky-build-context.json ValidateProjectPathLib
///   dotnet script entry.csx ./obj/ducky-build-context.json ExtractLocalizationKeysLib
/// </summary>

if (Args.Count < 2)
{
    Console.Error.WriteLine("Usage: dotnet script entry.csx <buildContextJsonPath> <scriptLibrary>");
    Console.Error.WriteLine("  buildContextJsonPath: Path to BuildContext JSON file");
    Console.Error.WriteLine("  scriptLibrary: Name of script library to execute");
    Console.Error.WriteLine("Available script libraries:");
    Console.Error.WriteLine("  ValidateProjectPathLib");
    Console.Error.WriteLine("  ExtractLocalizationKeysLib");
    Console.Error.WriteLine("  ValidateDuckovFolderLib");
    Console.Error.WriteLine("  UpdateLocalizationCsvLib");
    Console.Error.WriteLine("  EnsureInfoIniLib");
    Console.Error.WriteLine("  CopyLocalizationAssetsLib");
    Console.Error.WriteLine("  ResetDeployLib");
    Console.Error.WriteLine("  DeployModLib");
    Console.Error.WriteLine("  ILRepackAssembliesLib");
    Console.Error.WriteLine("  GeneratePreviewLib");
    Console.Error.WriteLine("  ResolveSdkPropertiesLib");
    Console.Error.WriteLine("  CopyDependenciesLib");
    Console.Error.WriteLine("  CollectFromModLib");
    Console.Error.WriteLine("  InitializeBuildLoggingLib");
    Console.Error.WriteLine("  PrintResultLib");
    return 1;
}

try
{
    var buildContextJsonPath = Args[0];
    var scriptLibrary = Args[1];
    var additionalArgs = Args.Skip(2).ToArray();

    Console.WriteLine($"[ScriptEntry] Loading BuildContext from: {buildContextJsonPath}");
    Console.WriteLine($"[ScriptEntry] ScriptLibrary: {scriptLibrary}");

    // Load BuildContext from specified JSON path
    BuildContext context;
    if (File.Exists(buildContextJsonPath))
    {
        try
        {
            var jsonContent = File.ReadAllText(buildContextJsonPath);
            var parsedContext = BuildContext.FromJson(jsonContent);
            if (parsedContext == null)
            {
                Console.Error.WriteLine($"[ScriptEntry][ERROR] Failed to parse BuildContext JSON: null result");
                return 1;
            }
            context = parsedContext!;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ScriptEntry][ERROR] Failed to parse BuildContext JSON: {ex.Message}");
            Console.Error.WriteLine($"[ScriptEntry][ERROR] File: {buildContextJsonPath}");
            return 1;
        }
    }
    else
    {
        Console.Error.WriteLine($"[ScriptEntry][ERROR] BuildContext JSON file not found: {buildContextJsonPath}");
        return 1;
    }

    if (string.IsNullOrEmpty(scriptLibrary))
    {
        Console.Error.WriteLine("[ScriptEntry][ERROR] ScriptLibrary parameter not provided");
        return 1;
    }

    Console.WriteLine($"[ScriptEntry] Executing {scriptLibrary}");

    // Load or create BuildResult for tracking
    var buildResult = BuildResult.LoadOrCreate(context.ProjectDir);

    // Handle special case for PrintResultLib
    if (scriptLibrary == "PrintResultLib")
    {
        return PrintResultLib.Execute(context, buildResult);
    }

    // Execute the script library and track the result
    int exitCode = BuildResultUtils.ExecuteAndRecord(buildResult, scriptLibrary, () =>
    {
        return scriptLibrary switch
        {
            "ValidateProjectPathLib" => ValidateProjectPathLib.Execute(context),
            "ExtractLocalizationKeysLib" => ExtractLocalizationKeysLib.Execute(context),
            "ValidateDuckovFolderLib" => ValidateDuckovFolderLib.Execute(context),
            "UpdateLocalizationCsvLib" => UpdateLocalizationCsvLib.Execute(context),
            "EnsureInfoIniLib" => EnsureInfoIniLib.Execute(context),
            "CopyLocalizationAssetsLib" => CopyLocalizationAssetsLib.Execute(context),
            "ResetDeployLib" => ResetDeployLib.Execute(context),
            "DeployModLib" => DeployModLib.Execute(context),
            "ILRepackAssembliesLib" => ILRepackAssembliesLib.Execute(context),
            "GeneratePreviewLib" => GeneratePreviewLib.Execute(context),
            "ResolveSdkPropertiesLib" => ResolveSdkPropertiesLib.Execute(context),
            "CopyDependenciesLib" => CopyDependenciesLib.Execute(context),
            "CollectFromModLib" => CollectFromModLib.Execute(context),
            "InitializeBuildLoggingLib" => InitializeBuildLoggingLib.Execute(context),
            _ => throw new ArgumentException($"Unknown script library: {scriptLibrary}")
        };
    });

    Console.WriteLine($"[ScriptEntry] {scriptLibrary} completed with exit code: {exitCode}");

    // Convert skip exit code to 0 for MSBuild to avoid compilation interruption
    if (exitCode == SkipExitCode)
    {
        Console.WriteLine($"[ScriptEntry] Converting skip exit code {SkipExitCode} to 0 for MSBuild compatibility");
        return 0;
    }

    return exitCode;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ScriptEntry][ERROR] {ex.Message}");
    Console.Error.WriteLine($"[ScriptEntry][ERROR] Stack trace: {ex.StackTrace}");
    return 1;
}