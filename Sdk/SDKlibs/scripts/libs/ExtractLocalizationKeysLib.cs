#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Script library for localization key extraction
/// Entry point: ExtractLocalizationKeysLib.Execute(BuildContext context)
/// </summary>
public class ExtractLocalizationKeysLib
{
    public class ExtractionResult
    {
        public bool Success { get; set; }
        public string KeysJsonPath { get; set; } = "";
        public int KeyCount { get; set; }
        public List<string> SourceFiles { get; set; } = new();
        public string ErrorMessage { get; set; } = "";
        public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Main entry point for localization key extraction
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure, 2 = no keys found)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("Starting localization key extraction");

            var result = ExtractKeysFromGeneratedSource(context);

            Console.WriteLine("=== Localization Key Extraction Results ===");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Keys Count: {result.KeyCount}");
            Console.WriteLine($"Keys JSON: {result.KeysJsonPath}");
            Console.WriteLine($"Extracted At: {result.ExtractedAt:u}");

            if (result.SourceFiles.Any())
            {
                Console.WriteLine("Source Files:");
                foreach (var file in result.SourceFiles.Take(5))
                {
                    Console.WriteLine($"  - {file}");
                }

                if (result.SourceFiles.Count > 5)
                {
                    Console.WriteLine($"  ... and {result.SourceFiles.Count - 5} more");
                }
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }

            return result.Success ? 0 : (result.KeyCount == 0 ? 2 : 1);
        }
        catch (Exception ex)
        {
            context.LogError($"Exception: {ex}");
            return 1;
        }
    }

    private static ExtractionResult ExtractKeysFromGeneratedSource(BuildContext context)
    {
        var result = new ExtractionResult();

        try
        {
            var objDir = context.BaseIntermediateOutputPath;

            if (!Directory.Exists(objDir))
            {
                context.LogInfo($"BaseIntermediateOutputPath not found: {objDir}");
                result.Success = false;
                result.ErrorMessage = "Object directory not found";
                return result;
            }

            context.LogInfo($"Searching for LKeys.*.metadata.g.cs files in: {objDir}");

            var metadataFiles = Directory.GetFiles(objDir, "LKeys.*.metadata.g.cs", SearchOption.AllDirectories);

            context.LogInfo($"Found {metadataFiles.Length} metadata file(s)");

            if (metadataFiles.Length == 0)
            {
                context.LogInfo("No LKeys metadata files found. This project may not have localization keys.");
                result.Success = true;
                result.KeyCount = 0;
                return result;
            }

            var metadataFile = metadataFiles[0];
            context.LogInfo($"Extracting JSON from: {metadataFile}");

            var content = File.ReadAllText(metadataFile);

            var pattern = @"internal\s+const\s+string\s+JsonData\s*=\s*@""((?:[^""]|"""")*)""";
            var match = Regex.Match(content, pattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                context.LogError("Could not find JsonData constant in metadata file");
                result.ErrorMessage = "JsonData constant not found";
                return result;
            }

            var escapedJson = match.Groups[1].Value;
            var jsonContent = escapedJson.Replace("\"\"", "\"");

            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                var keyCount = doc.RootElement.GetProperty("keyCount").GetInt32();
                context.LogInfo($"Successfully extracted JSON with {keyCount} keys");
                result.KeyCount = keyCount;
            }
            catch (JsonException ex)
            {
                context.LogWarning($"Extracted content is not valid JSON: {ex.Message}");
            }

            var keysJsonPath = Path.Combine(context.AssetsDir, "lkeys.json");
            Directory.CreateDirectory(Path.GetDirectoryName(keysJsonPath) ?? ".");
            File.WriteAllText(keysJsonPath, jsonContent);

            result.Success = true;
            result.KeysJsonPath = keysJsonPath;
            result.SourceFiles = Directory.GetFiles(context.ProjectDir, "*.cs", SearchOption.AllDirectories).ToList();

            context.LogInfo($"Keys saved to: {keysJsonPath}");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Extraction error: {ex.Message}";
            context.LogError($"Exception: {ex}");
        }

        return result;
    }
}
