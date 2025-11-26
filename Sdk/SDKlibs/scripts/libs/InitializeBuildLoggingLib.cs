#nullable disable

using System;
using System.IO;

/// <summary>
/// Script library for initializing build logging with detailed configuration capture
/// Entry point: InitializeBuildLoggingLib.Execute(BuildContext context)
/// </summary>
public class InitializeBuildLoggingLib
{
    /// <summary>
    /// Main entry point for initializing build logging
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Initialize Build Logging Results ===");

            var projectDir = context.ProjectDir;
            var logger = BuildLogger.CreateFromProject(projectDir);

            logger.LogInfo("=== Build Started ===", "Build");
            logger.LogInfo($"Project: {projectDir}", "Build");
            logger.LogInfo($"Configuration: {context.Configuration}", "Build");
            logger.LogInfo($"Target Framework: {context.TargetFramework}", "Build");
            logger.LogInfo($"Timestamp: {DateTime.Now:O}", "Build");

            // Log environment info
            logger.LogInfo($"Working Directory: {Environment.CurrentDirectory}", "Environment");
            logger.LogInfo($"Machine: {Environment.MachineName}", "Environment");
            logger.LogInfo($"User: {Environment.UserName}", "Environment");

            // Log build configuration
            logger.LogInfo($"ModName: {context.ModName}", "Build");
            logger.LogInfo($"AssetsDir: {context.AssetsDir}", "Build");
            logger.LogInfo($"DeployMod: {context.DeployMod}", "Build");
            logger.LogInfo($"IsModLib: {context.IsModLib}", "Build");
            logger.LogInfo($"DuckovFolder: {context.DuckovFolder}", "Build");
            logger.LogInfo($"SteamFolder: {context.SteamFolder}", "Build");
            logger.LogInfo($"EnableILRepack: {context.EnableILRepack}", "Build");
            logger.LogInfo($"IncludeHarmony: {context.IncludeHarmony}", "Build");
            logger.LogInfo($"EnableGlobalUsing: {context.EnableGlobalUsing}", "Build");

            // Log additional SDK properties
            logger.LogInfo($"LocalizationAssetsDir: {context.LocalizationAssetsDir}", "Build");

            // Create initial log file
            logger.SaveToFile();

            var logPath = logger.GetLogPath();
            context.LogInfo($"Build logging initialized for: {projectDir}");
            context.LogInfo($"Log file created at: {logPath}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InitializeBuildLoggingLib][ERROR] {ex.Message}");
            return 1;
        }
    }
}


/// <summary>
/// Simple Build Logger for tracking detailed build information
/// </summary>
public class BuildLogger
{
    private readonly string _logDirectory;
    private readonly string _logFile;
    private readonly List<string> _entries = new();

    public BuildLogger(string projectDirectory)
    {
        var objDir = Path.Combine(projectDirectory, "obj");
        _logDirectory = Path.Combine(objDir, "ducky-build");
        Directory.CreateDirectory(_logDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        _logFile = Path.Combine(_logDirectory, $"build-{timestamp}.log");
    }

    public void LogInfo(string message, string category = "General")
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] [{category}] {message}";
        _entries.Add(entry);
        Console.WriteLine(entry);
    }

    public void LogWarning(string message, string category = "General")
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [WARN] [{category}] {message}";
        _entries.Add(entry);
        Console.WriteLine(entry);
    }

    public void LogError(string message, string category = "General")
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] [{category}] {message}";
        _entries.Add(entry);
        Console.WriteLine(entry);
    }

    public void LogScriptExecution(string scriptName, string command, int exitCode, string category = "Script")
    {
        var status = exitCode == 0 ? "SUCCESS" : "FAILED";
        var entry =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] [{status}] {scriptName} - Exit Code: {exitCode}";
        _entries.Add(entry);
        _entries.Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] [COMMAND] {command}");
        Console.WriteLine(entry);
    }

    public void LogProperties(Dictionary<string, string> properties, string category = "Properties")
    {
        LogInfo($"=== Build Properties ({properties.Count} properties) ===", category);
        foreach (var kvp in properties)
        {
            LogInfo($"{kvp.Key}: {kvp.Value}", category);
        }
    }

    public void LogTargetExecution(string targetName, TimeSpan duration, string category = "Targets")
    {
        var entry =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] [EXECUTED] {targetName} - Duration: {duration.TotalMilliseconds}ms";
        _entries.Add(entry);
        Console.WriteLine(entry);
    }

    public string GetLogPath() => _logFile;

    public void SaveToFile()
    {
        try
        {
            File.WriteAllLines(_logFile, _entries);
            Console.WriteLine($"[BuildLogger] Log saved to: {_logFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BuildLogger][ERROR] Failed to save log: {ex.Message}");
        }
    }

    public static BuildLogger CreateFromProject(string projectDir)
    {
        return new BuildLogger(projectDir);
    }
}

/// <summary>
/// Utility for creating and managing build logs
/// </summary>
public static class BuildLogHelper
{
    public static void LogAndSave(string projectDir, Action<BuildLogger> action)
    {
        var logger = BuildLogger.CreateFromProject(projectDir);

        try
        {
            action(logger);
        }
        finally
        {
            logger.SaveToFile();
        }
    }
}
