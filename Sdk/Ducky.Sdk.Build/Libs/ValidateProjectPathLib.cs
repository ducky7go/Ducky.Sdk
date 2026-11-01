#nullable disable

using System;
using System.IO;
using System.Linq;

/// <summary>
/// Script library for project path validation
/// Entry point: ValidateProjectPathLib.ValidateProjectPath(BuildContext context)
/// </summary>
public class ValidateProjectPathLib
{
    /// <summary>
    /// Main entry point for project path validation
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Project Path Validation Results ===");

            // Use project directory from BuildContext
            var projectDir = context.ProjectDir;

            // Basic validation - check if directory exists
            var directoryExists = Directory.Exists(projectDir);
            var isAbsolutePath = Path.IsPathRooted(projectDir);
            var isValidFormat = !string.IsNullOrEmpty(projectDir) && !projectDir.Contains("..") &&
                                !projectDir.Contains("//");

            var valid = directoryExists && isAbsolutePath && isValidFormat;

            Console.WriteLine($"Path: {projectDir}");
            Console.WriteLine($"Valid: {valid}");
            Console.WriteLine($"Validation Time: {DateTime.UtcNow:u}");

            if (!valid)
            {
                Console.WriteLine("");
                Console.WriteLine("Issues found:");
                if (!directoryExists)
                    Console.WriteLine("  ❌ Project directory does not exist");
                if (!isAbsolutePath)
                    Console.WriteLine("  ⚠️  Path is not absolute");
                if (!isValidFormat)
                    Console.WriteLine("  ❌ Path format is invalid");
            }

            if (valid)
            {
                Console.WriteLine("✅ Project path validation passed");
            }
            else
            {
                context.LogError("Project path validation failed");
                return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            context.LogError($"Project path validation exception: {ex.Message}");
            return 1;
        }
    }
}
