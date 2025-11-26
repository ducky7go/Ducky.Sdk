#nullable disable

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// Script library for ensuring info.ini exists and name matches ModName
/// Entry point: EnsureInfoIniLib.Execute(BuildContext context)
/// </summary>
public class EnsureInfoIniLib
{
    /// <summary>
    /// Main entry point for ensuring info.ini exists with correct name
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Ensure Info.ini Results ===");

            var assetsDir = context.AssetsDir;
            var modName = context.ModName;

            context.LogInfo($"Assets Directory: {assetsDir}");
            context.LogInfo($"Mod Name: {modName}");

            // Skip if no ModName (library projects)
            if (string.IsNullOrEmpty(modName))
            {
                context.LogInfo("Skipping info.ini generation: ModName not specified (library project)");
                return 0;
            }

            if (!Directory.Exists(assetsDir))
            {
                context.LogInfo($"Assets directory does not exist, creating: {assetsDir}");
                Directory.CreateDirectory(assetsDir!);
            }

            var infoPath = Path.Combine(assetsDir, "info.ini");

            if (File.Exists(infoPath))
            {
                context.LogInfo("info.ini already exists. Checking name synchronization...");
                return UpdateExistingInfoIni(infoPath, modName, context);
            }
            else
            {
                context.LogInfo("info.ini not found. Generating basic info.ini...");
                return CreateNewInfoIni(infoPath, modName, context);
            }
        }
        catch (Exception ex)
        {
            context.LogError($"Ensure info.ini exception: {ex.Message}");
            return 1;
        }
    }

    private static int UpdateExistingInfoIni(string infoPath, string modName, BuildContext context)
    {
        try
        {
            // Read existing INI content
            var existingLines = File.ReadAllLines(infoPath);
            var nameRegex = new Regex(@"^\s*name\s*=\s*(.+?)\s*$", RegexOptions.IgnoreCase);
            var nameLine = existingLines.FirstOrDefault(l => nameRegex.IsMatch(l));

            if (nameLine != null)
            {
                var match = nameRegex.Match(nameLine);
                var existingName = match.Groups[1].Value.Trim();

                if (existingName != modName)
                {
                    context.LogWarning($"Name mismatch found. INI name: '{existingName}', ModName: '{modName}'");
                    context.LogInfo("Updating name field to match ModName...");

                    // Replace the name line
                    var updatedLines = existingLines.Select(line =>
                        nameRegex.IsMatch(line) ? $"name = {modName}" : line
                    ).ToArray();

                    File.WriteAllText(infoPath, string.Join(Environment.NewLine, updatedLines), Encoding.UTF8);
                    context.LogInfo($"Successfully updated name field to: {modName}");
                }
                else
                {
                    context.LogInfo("Name field already matches ModName. No changes needed.");
                }
            }
            else
            {
                context.LogInfo("No name field found in existing info.ini. Adding name field...");
                var updatedContent = string.Join(Environment.NewLine, existingLines) +
                                    $"{Environment.NewLine}name = {modName}{Environment.NewLine}";
                File.WriteAllText(infoPath, updatedContent, Encoding.UTF8);
                context.LogInfo($"Successfully added name field: {modName}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            context.LogError($"Failed to update existing info.ini: {ex.Message}");
            return 1;
        }
    }

    private static int CreateNewInfoIni(string infoPath, string modName, BuildContext context)
    {
        try
        {
            // Generate basic info.ini content
            var iniContent = new StringBuilder();
            iniContent.AppendLine($"name = {modName}");
            iniContent.AppendLine($"displayName = {modName}");
            iniContent.AppendLine($"description = A mod for Escape from Duckov");

            // Write to file
            File.WriteAllText(infoPath, iniContent.ToString(), Encoding.UTF8);

            context.LogInfo("Successfully generated info.ini with basic metadata");
            context.LogInfo($"  name = {modName}");
            context.LogInfo($"  displayName = {modName}");
            context.LogInfo($"  description = A mod for Escape from Duckov");

            return 0;
        }
        catch (Exception ex)
        {
            context.LogError($"Failed to create new info.ini: {ex.Message}");
            return 1;
        }
    }
}

