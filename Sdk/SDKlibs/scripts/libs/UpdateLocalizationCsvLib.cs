#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Script library for updating localization CSV files from JSON keys
/// Entry point: UpdateLocalizationCsvLib.Execute(BuildContext context)
/// </summary>
public class UpdateLocalizationCsvLib
{
    /// <summary>
    /// Main entry point for updating localization CSV files
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Update Localization CSV Results ===");

            var assetsDir = context.AssetsDir;

            // Read keys from JSON file
            var jsonKeyFile = Path.Combine(assetsDir!, "lkeys.json");
            if (!File.Exists(jsonKeyFile))
            {
                context.LogInfo($"Key JSON file not found: {jsonKeyFile}. Skipping CSV generation.");
                return SkipExitCode;
            }

            var (keys, keyFileExtensions, supportedLanguages) = LoadKeysFromJson(jsonKeyFile, context);
            if (keys.Count == 0)
            {
                context.LogInfo("No keys found in JSON file. Skipping CSV generation.");
                return SkipExitCode;
            }

            var distinctKeys = keys.Distinct().OrderBy(k => k).ToList();
            var assemblyHash = ComputeHash(distinctKeys);
            var hashFile = Path.Combine(assetsDir!, "keys.hash.txt");

            context.LogInfo($"Found {distinctKeys.Count} unique keys");
            context.LogInfo($"Hash: {assemblyHash}");

            // Resolve locales directory
            var localesDir = ResolveLocalesDirectory(assetsDir!);

            // Determine language entries
            var languageEntries = GetLanguageEntries(supportedLanguages, localesDir, context);

            // Check if update is needed
            if (!IsUpdateNeeded(hashFile, assemblyHash, languageEntries, localesDir, distinctKeys))
            {
                context.LogInfo("Hash matches existing hash file and all CSVs exist; skipping CSV updates.");
                return SkipExitCode;
            }

            // Ensure locales dir exists
            Directory.CreateDirectory(localesDir);

            // Process languages
            var stats = ProcessLanguages(languageEntries, distinctKeys, keyFileExtensions, localesDir, context);

            // Save hash
            File.WriteAllText(hashFile, assemblyHash);
            context.LogInfo($"Hash saved to: {hashFile}");

            // Print stats
            PrintStats(stats, context);

            return 0;
        }
        catch (Exception ex)
        {
            context.LogError($"Update localization CSV exception: {ex.Message}");
            return 1;
        }
    }

    private static (List<string> keys, Dictionary<string, string> keyFileExtensions, List<string> supportedLanguages)
        LoadKeysFromJson(string jsonKeyFile, BuildContext context)
    {
        var keys = new List<string>();
        var keyFileExtensions = new Dictionary<string, string>();
        List<string> supportedLanguages = null;

        try
        {
            context.LogInfo($"Reading keys from: {jsonKeyFile}");
            var jsonContent = File.ReadAllText(jsonKeyFile);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Read supported languages if present
            if (root.TryGetProperty("supportedLanguages", out var langsArray))
            {
                supportedLanguages = new List<string>();
                foreach (var langElement in langsArray.EnumerateArray())
                {
                    var lang = langElement.GetString();
                    if (!string.IsNullOrWhiteSpace(lang))
                    {
                        supportedLanguages.Add(lang.ToLowerInvariant());
                    }
                }

                context.LogInfo(
                    $"Found {supportedLanguages.Count} supported languages: {string.Join(", ", supportedLanguages)}");
            }

            if (root.TryGetProperty("keys", out var keysArray) || root.TryGetProperty("Keys", out keysArray))
            {
                if (keysArray.ValueKind == JsonValueKind.Array)
                {
                    // Legacy array format
                    foreach (var keyElement in keysArray.EnumerateArray())
                    {
                        if (keyElement.ValueKind == JsonValueKind.String)
                        {
                            var key = keyElement.GetString();
                            if (!string.IsNullOrWhiteSpace(key))
                            {
                                keys.Add(key);
                            }
                        }
                        else if (keyElement.ValueKind == JsonValueKind.Object)
                        {
                            if (keyElement.TryGetProperty("key", out var keyProp))
                            {
                                var key = keyProp.GetString();
                                if (!string.IsNullOrWhiteSpace(key))
                                {
                                    keys.Add(key);

                                    if (keyElement.TryGetProperty("fileExtension", out var extProp))
                                    {
                                        var ext = extProp.GetString();
                                        if (!string.IsNullOrWhiteSpace(ext))
                                        {
                                            keyFileExtensions[key] = ext;
                                            context.LogInfo(
                                                $"Key '{key}' marked as file reference with extension '.{ext}'");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (keysArray.ValueKind == JsonValueKind.Object)
                {
                    // New object format
                    foreach (var keyProperty in keysArray.EnumerateObject())
                    {
                        var key = keyProperty.Name;
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            keys.Add(key);

                            var keyObject = keyProperty.Value;
                            if (keyObject.TryGetProperty("FileExtension", out var extProp))
                            {
                                var ext = extProp.GetString();
                                if (!string.IsNullOrWhiteSpace(ext))
                                {
                                    keyFileExtensions[key] = ext;
                                    context.LogInfo($"Key '{key}' marked as file reference with extension '.{ext}'");
                                }
                            }
                        }
                    }
                }

                context.LogInfo(
                    $"Loaded {keys.Count} keys from JSON file ({keyFileExtensions.Count} with file references)");
            }
            else
            {
                throw new Exception("'keys' property not found in JSON file");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to read or parse JSON file: {ex.Message}", ex);
        }

        return (keys, keyFileExtensions, supportedLanguages);
    }

    private static string ComputeHash(List<string> distinctKeys)
    {
        string JoinKeys(IEnumerable<string> ks) => string.Join('|', ks);

        string ComputeCrc32Hex(string input)
        {
            var table = MakeCrc32Table();
            uint crc = 0xFFFFFFFFu;
            var b = Encoding.UTF8.GetBytes(input ?? string.Empty);
            foreach (var by in b)
            {
                var idx = (byte)((crc ^ by) & 0xFF);
                crc = (crc >> 8) ^ table[idx];
            }

            crc ^= 0xFFFFFFFFu;
            return crc.ToString("x8");
        }

        static uint[] MakeCrc32Table()
        {
            uint[] table = new uint[256];
            const uint poly = 0xEDB88320u;
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ poly;
                    else
                        crc >>= 1;
                }

                table[i] = crc;
            }

            return table;
        }

        var allKeysJoined = JoinKeys(distinctKeys);
        return ComputeCrc32Hex(allKeysJoined);
    }

    private static string ResolveLocalesDirectory(string assetsDir)
    {
        try
        {
            // Validate assetsDir parameter
            if (string.IsNullOrEmpty(assetsDir))
            {
                throw new ArgumentException("AssetsDir cannot be null or empty");
            }

            // Ensure assetsDir exists
            if (!Directory.Exists(assetsDir))
            {
                Directory.CreateDirectory(assetsDir);
            }

            // This logic mirrors the original script's directory resolution
            var providedInfo = new DirectoryInfo(assetsDir);
            var providedName = providedInfo.Name ?? string.Empty;

            if (string.Equals(providedName, "Locales", StringComparison.OrdinalIgnoreCase) && providedInfo.Exists)
            {
                return Path.GetFullPath(providedInfo.FullName);
            }

            var candidate = Path.Combine(assetsDir, "Locales");
            if (Directory.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            if (providedInfo.Exists && Directory.GetFiles(assetsDir, "*.csv", SearchOption.TopDirectoryOnly).Any())
            {
                return Path.GetFullPath(assetsDir);
            }

            // Ensure candidate directory exists for future use
            Directory.CreateDirectory(candidate);
            return Path.GetFullPath(candidate);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to resolve locales directory from assetsDir '{assetsDir}': {ex.Message}", ex);
        }
    }

    private static List<(string LangCode, string CsvPath, string LangDir)> GetLanguageEntries(
        List<string> supportedLanguages, string localesDir, BuildContext context)
    {
        var languageEntries = new List<(string LangCode, string CsvPath, string LangDir)>();

        if (supportedLanguages != null && supportedLanguages.Count > 0)
        {
            context.LogInfo(
                $"Using languages specified in LanguageSupport attribute: {string.Join(", ", supportedLanguages)}");
            foreach (var langCode in supportedLanguages)
            {
                var csvPath = Path.Combine(localesDir, langCode + ".csv");
                var langDir = Path.Combine(localesDir, langCode);
                languageEntries.Add((langCode, csvPath, langDir));
            }
        }
        else
        {
            // Default to zh and en when no LanguageSupport attribute is specified
            context.LogInfo("No LanguageSupport attribute found, defaulting to: zh, en");
            var languages = new[] { "zh", "en" };

            foreach (var langCode in languages)
            {
                var csvPath = Path.Combine(localesDir, langCode + ".csv");
                var langDir = Path.Combine(localesDir, langCode);
                languageEntries.Add((langCode, csvPath, langDir));
            }

            // Also discover existing files to include any additional languages
            if (Directory.Exists(localesDir))
            {
                context.LogInfo("Discovering existing language files...");
                var existingLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var csvFilesRoot = Directory.GetFiles(localesDir, "*.csv", SearchOption.TopDirectoryOnly);
                foreach (var csv in csvFilesRoot)
                {
                    var code = Path.GetFileNameWithoutExtension(csv);
                    if (!existingLanguages.Contains(code))
                    {
                        var langDirCandidate = Path.Combine(localesDir, code);
                        languageEntries.Add((code, csv, langDirCandidate));
                        existingLanguages.Add(code);
                    }
                }

                var subdirs = Directory.GetDirectories(localesDir, "*", SearchOption.TopDirectoryOnly);
                foreach (var sd in subdirs)
                {
                    var code = Path.GetFileName(sd);
                    if (!existingLanguages.Contains(code))
                    {
                        var csvPath = Path.Combine(localesDir, code + ".csv");
                        languageEntries.Add((code, csvPath, sd));
                        existingLanguages.Add(code);
                    }
                }
            }
        }

        return languageEntries;
    }

    private static bool IsUpdateNeeded(string hashFile, string assemblyHash,
        List<(string LangCode, string CsvPath, string LangDir)> languageEntries, string localesDir,
        List<string> distinctKeys)
    {
        // Pre-scan language entries to determine if any CSV is missing or empty
        var needUpdateForMissing =
            languageEntries.Any(entry => !File.Exists(entry.CsvPath) || new FileInfo(entry.CsvPath).Length == 0);

        // Detect obsolete keys
        int obsoleteKeysDetected = 0;
        if (Directory.Exists(localesDir))
        {
            var keySet = new HashSet<string>(distinctKeys, StringComparer.Ordinal);
            foreach (var csvFile in Directory.GetFiles(localesDir, "*.csv", SearchOption.AllDirectories))
            {
                try
                {
                    var lines = File.ReadAllLines(csvFile);
                    var fileObsolete = 0;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.Trim().Equals("Key,Value", StringComparison.OrdinalIgnoreCase)) continue;
                        var idx = line.IndexOf(',');
                        if (idx < 0) continue;
                        var existingKey = line.Substring(0, idx);
                        if (!keySet.Contains(existingKey))
                        {
                            fileObsolete++;
                        }
                    }

                    if (fileObsolete > 0)
                    {
                        obsoleteKeysDetected += fileObsolete;
                        needUpdateForMissing = true;
                    }
                }
                catch (Exception)
                {
                    // ignore parse errors and continue
                }
            }
        }

        if (File.Exists(hashFile))
        {
            var existingHash = File.ReadAllText(hashFile).Trim();
            return existingHash != assemblyHash || needUpdateForMissing;
        }

        return true;
    }

    private static (int languagesProcessed, int csvsUpdated, int totalStandaloneFilesIncluded, int totalRowsWritten)
        ProcessLanguages(
            List<(string LangCode, string CsvPath, string LangDir)> languageEntries,
            List<string> distinctKeys,
            Dictionary<string, string> keyFileExtensions,
            string localesDir,
            BuildContext context)
    {
        int languagesProcessed = 0;
        int csvsUpdated = 0;
        int totalStandaloneFilesIncluded = 0;
        int totalRowsWritten = 0;

        if (languageEntries.Count == 0)
        {
            context.LogInfo("No language CSVs or language directories found under localesDir; nothing to update.");
            return (0, 0, 0, 0);
        }

        foreach (var entry in languageEntries)
        {
            var langCode = entry.LangCode;
            var csvPath = entry.CsvPath;
            var langDir = entry.LangDir;

            context.LogInfo($"Processing language: {langCode}");
            languagesProcessed++;

            var csvMap = new Dictionary<string, string>();
            if (File.Exists(csvPath))
            {
                var lines = File.ReadAllLines(csvPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.Trim().Equals("Key,Value", StringComparison.OrdinalIgnoreCase)) continue;
                    var idx = line.IndexOf(',');
                    if (idx < 0) continue;
                    var key = line.Substring(0, idx);
                    var value = line.Substring(idx + 1);
                    if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2).Replace("\"\"", '"'.ToString());
                    }

                    value = value.Replace("\"\"", '"'.ToString());
                    csvMap[key] = value;
                }
            }

            // Include standalone md/txt files
            var localStandaloneCount = 0;
            if (!string.IsNullOrEmpty(langDir) && Directory.Exists(langDir))
            {
                var mdFiles = Directory.GetFiles(langDir, "*.md");
                var txtFiles = Directory.GetFiles(langDir, "*.txt");
                foreach (var f in mdFiles.Concat(txtFiles))
                {
                    var filename = Path.GetFileName(f);
                    var key = Path.GetFileNameWithoutExtension(f);
                    csvMap[key] = filename;
                    localStandaloneCount++;
                }
            }

            totalStandaloneFilesIncluded += localStandaloneCount;

            // Ensure language directory exists
            if (!string.IsNullOrEmpty(langDir))
            {
                Directory.CreateDirectory(langDir);
            }

            var newRows = new List<(string Key, string Value)>();
            var filesCreated = 0;

            foreach (var key in distinctKeys)
            {
                csvMap.TryGetValue(key, out var val);

                if (keyFileExtensions.TryGetValue(key, out var fileExt))
                {
                    var fileName = $"{key}.{fileExt}";

                    if (!string.IsNullOrEmpty(langDir))
                    {
                        var filePath = Path.Combine(langDir, fileName);

                        if (!File.Exists(filePath))
                        {
                            File.WriteAllText(filePath, val ?? key, new UTF8Encoding(false));
                            filesCreated++;
                        }
                    }

                    newRows.Add((key, fileName));
                }
                else
                {
                    newRows.Add((key, val ?? string.Empty));
                }
            }

            var sorted = newRows.OrderBy(r => r.Key, StringComparer.Ordinal).ToList();

            // Prepare new content
            var sbNew = new StringBuilder();
            sbNew.AppendLine("Key,Value");
            foreach (var r in sorted)
            {
                var v = r.Value?.Replace("\"", "\"\"") ?? string.Empty;
                sbNew.AppendLine($"{r.Key},\"{v}\"");
            }

            var newContent = sbNew.ToString();
            var oldContent = File.Exists(csvPath) ? File.ReadAllText(csvPath) : string.Empty;

            if (!string.Equals(newContent, oldContent, StringComparison.Ordinal))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(csvPath) ?? localesDir);
                File.WriteAllText(csvPath, newContent, new UTF8Encoding(false));
                csvsUpdated++;
                totalRowsWritten += sorted.Count;
                context.LogInfo($"Updated {csvPath} ({sorted.Count} keys, {filesCreated} files created)");
            }
            else
            {
                if (filesCreated > 0)
                {
                    context.LogInfo($"No changes to {csvPath} but created {filesCreated} translation file(s)");
                }
                else
                {
                    context.LogInfo($"No changes for {csvPath} ({sorted.Count} keys)");
                }
            }
        }

        return (languagesProcessed, csvsUpdated, totalStandaloneFilesIncluded, totalRowsWritten);
    }

    private static void PrintStats(
        (int languagesProcessed, int csvsUpdated, int totalStandaloneFilesIncluded, int totalRowsWritten) stats,
        BuildContext context)
    {
        context.LogInfo("--- Update stats ---");
        context.LogInfo($"Languages processed: {stats.languagesProcessed}");
        context.LogInfo($"CSVs updated:        {stats.csvsUpdated}");
        context.LogInfo($"Standalone files:    {stats.totalStandaloneFilesIncluded}");
        context.LogInfo($"Total rows written:  {stats.totalRowsWritten}");
        context.LogInfo("---------------------");
    }
}
