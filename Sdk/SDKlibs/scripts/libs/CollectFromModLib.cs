#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Script library for collecting data from deployed mods
/// Behavior: Copies publishedFileId from mod directory info.ini to assets info.ini if not already present
/// Entry point: CollectFromModLib.Execute(BuildContext context)
/// </summary>
public class CollectFromModLib
{
    public class CollectionResult
    {
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public string PublishedFileId { get; set; } = "";
        public string ModInfoPath { get; set; } = "";
        public string AssetsInfoPath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Main entry point for collecting data from mods
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            if(context.DeployMod == false)
            {
                context.LogInfo("Mod deployment is disabled. Skipping collection.");
                return 0;
            }

            context.LogInfo("=== Collect From Mod Results ===");

            var result = CollectFromMod(context);

            context.LogInfo($"Success: {result.Success}");
            context.LogInfo($"Skipped: {result.Skipped}");
            context.LogInfo($"Collected At: {result.CollectedAt:u}");

            if (!string.IsNullOrEmpty(result.PublishedFileId))
            {
                context.LogInfo($"Published File ID: {result.PublishedFileId}");
            }

            if (!string.IsNullOrEmpty(result.ModInfoPath))
            {
                context.LogInfo($"Mod Info Path: {result.ModInfoPath}");
            }

            if (!string.IsNullOrEmpty(result.AssetsInfoPath))
            {
                context.LogInfo($"Assets Info Path: {result.AssetsInfoPath}");
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                context.LogError($"Error: {result.ErrorMessage}");
            }

            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            context.LogError($"Collect from mod exception: {ex.Message}");
            return 1;
        }
    }

    public static CollectionResult CollectFromMod(BuildContext context)
    {
        var result = new CollectionResult();

        try
        {
            context.LogInfo("Starting mod data collection");

            // Check if this should be skipped
            if (ShouldSkipCollection(context, result))
            {
                return result;
            }

            // Determine mod directory
            var modDir = GetModDirectory(context);
            var assetsDir = context.AssetsDir;
            var modInfoPath = Path.Combine(modDir, "info.ini");
            var assetsInfoPath = Path.Combine(assetsDir, "info.ini");

            result.ModInfoPath = modInfoPath;
            result.AssetsInfoPath = assetsInfoPath;

            context.LogInfo($"Mod directory: {modDir}");
            context.LogInfo($"Assets directory: {assetsDir}");
            context.LogInfo($"Mod info.ini: {modInfoPath}");
            context.LogInfo($"Assets info.ini: {assetsInfoPath}");

            // Validate paths
            if (!Directory.Exists(modDir))
            {
                result.ErrorMessage = $"Mod directory not found: {modDir}";
                return result;
            }

            if (!Directory.Exists(assetsDir))
            {
                result.ErrorMessage = $"Assets directory not found: {assetsDir}";
                return result;
            }

            if (!File.Exists(modInfoPath))
            {
                result.ErrorMessage = $"Mod info.ini not found: {modInfoPath}";
                return result;
            }

            // Read published file ID from mod info.ini
            var publishedFileId = ExtractPublishedFileId(modInfoPath);
            if (string.IsNullOrEmpty(publishedFileId))
            {
                context.LogInfo("No publishedFileId found in mod info.ini");
                result.Skipped = true;
                result.Success = true;
                return result;
            }

            result.PublishedFileId = publishedFileId;

            // Check if assets info.ini already has a publishedFileId
            if (File.Exists(assetsInfoPath) && HasPublishedFileId(assetsInfoPath))
            {
                context.LogInfo("Assets info.ini already has publishedFileId - skipping");
                result.Skipped = true;
                result.Success = true;
                return result;
            }

            // Add publishedFileId to assets info.ini
            AddPublishedFileIdToAssetsInfo(assetsInfoPath, publishedFileId, context);

            result.Success = true;
            context.LogInfo($"Successfully added publishedFileId {publishedFileId} to assets info.ini");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Collection error: {ex.Message}";
            context.LogError($"Exception: {ex}");
        }

        return result;
    }

    private static bool ShouldSkipCollection(BuildContext context, CollectionResult result)
    {
        // Skip for library projects
        if (context.IsModLib)
        {
            context.LogInfo("Skipping collection: IsModLib=true");
            result.Skipped = true;
            result.Success = true;
            return true;
        }

        // Skip if no ModName
        if (string.IsNullOrEmpty(context.ModName))
        {
            context.LogInfo("Skipping collection: No ModName specified");
            result.Skipped = true;
            result.Success = true;
            return true;
        }

        // Skip if no DuckovFolder
        if (string.IsNullOrEmpty(context.DuckovFolder))
        {
            context.LogInfo("Skipping collection: No DuckovFolder specified");
            result.Skipped = true;
            result.Success = true;
            return true;
        }

        return false;
    }

    private static string GetModDirectory(BuildContext context)
    {
        return Path.Combine(context.DuckovFolder, "Duckov_Data", "Mods", context.ModName);
    }

    private static string ExtractPublishedFileId(string infoPath)
    {
        try
        {
            var content = File.ReadAllText(infoPath);
            var match = Regex.Match(content, @"^\s*publishedFileId\s*=\s*(\d+)", RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value : "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static bool HasPublishedFileId(string infoPath)
    {
        try
        {
            var content = File.ReadAllText(infoPath);
            return Regex.IsMatch(content, @"^\s*publishedFileId\s*=", RegexOptions.Multiline);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void AddPublishedFileIdToAssetsInfo(string infoPath, string publishedFileId, BuildContext context)
    {
        try
        {
            string content;
            if (File.Exists(infoPath))
            {
                content = File.ReadAllText(infoPath);
            }
            else
            {
                content = "";
                Directory.CreateDirectory(Path.GetDirectoryName(infoPath));
            }

            // Remove existing publishedFileId if present
            content = Regex.Replace(content, @"^\s*publishedFileId\s*=.*?\r?\n", "", RegexOptions.Multiline);

            // Add new publishedFileId at the end
            content = content.TrimEnd();
            if (!string.IsNullOrEmpty(content))
            {
                content += Environment.NewLine;
            }
            content += $"publishedFileId = {publishedFileId}{Environment.NewLine}";

            File.WriteAllText(infoPath, content);
            context.LogInfo($"Added publishedFileId to {infoPath}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to add publishedFileId to assets info.ini: {ex.Message}", ex);
        }
    }

}