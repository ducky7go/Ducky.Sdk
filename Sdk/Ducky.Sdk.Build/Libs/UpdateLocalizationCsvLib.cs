#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ducky.Sdk.Build.Localization;

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

            var (keys, keyFileExtensions, supportedLanguages, patterns) = LoadKeysFromJson(jsonKeyFile, context);
            if (keys.Count == 0)
            {
                context.LogInfo("No keys found in JSON file. Skipping CSV generation.");
                return SkipExitCode;
            }

            var distinctKeys = keys.Distinct().OrderBy(k => k).ToList();
            var keyPatterns = CompilePatterns(patterns);
            var assemblyHash = ComputeHash(distinctKeys);
            var hashFile = Path.Combine(assetsDir!, "keys.hash.txt");

            context.LogInfo($"Found {distinctKeys.Count} unique keys");
            context.LogInfo($"Hash: {assemblyHash}");

            // Resolve locales directory
            var localesDir = ResolveLocalesDirectory(assetsDir!);

            // Determine language entries
            var languageEntries = GetLanguageEntries(supportedLanguages, localesDir, context);

            // Check if update is needed
            if (!IsUpdateNeeded(hashFile, assemblyHash, languageEntries, localesDir, distinctKeys, keyPatterns))
            {
                context.LogInfo("Hash matches existing hash file and all CSVs exist; skipping CSV updates.");
                return SkipExitCode;
            }

            // Ensure locales dir exists
            Directory.CreateDirectory(localesDir);

            // Process languages
            var stats = ProcessLanguages(languageEntries, distinctKeys, keyFileExtensions, keyPatterns, localesDir, context);

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

    private static (List<string> keys, Dictionary<string, string> keyFileExtensions, List<string> supportedLanguages, List<string> patterns)
        LoadKeysFromJson(string jsonKeyFile, BuildContext context)
    {
        var keys = new List<string>();
        var keyFileExtensions = new Dictionary<string, string>();
        var patterns = new List<string>();
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

            // [KeyPattern] declarations: their rows are data-driven, so CSV updates keep them.
            if (root.TryGetProperty("patterns", out var patternsArray) && patternsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var patternElement in patternsArray.EnumerateArray())
                {
                    var pattern = patternElement.GetString();
                    if (!string.IsNullOrWhiteSpace(pattern))
                    {
                        patterns.Add(pattern);
                    }
                }
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

        return (keys, keyFileExtensions, supportedLanguages, patterns);
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
        List<string> distinctKeys, List<Regex> keyPatterns)
    {
        // Pre-scan language entries to determine if any CSV is missing or empty
        var needUpdateForMissing =
            languageEntries.Any(entry => !File.Exists(entry.CsvPath) || new FileInfo(entry.CsvPath).Length == 0);

        // Detect obsolete keys: rows that are neither a static key nor matched by a [KeyPattern]
        int obsoleteKeysDetected = 0;
        if (Directory.Exists(localesDir))
        {
            var keySet = new HashSet<string>(distinctKeys, StringComparer.OrdinalIgnoreCase);
            foreach (var csvFile in Directory.GetFiles(localesDir, "*.csv", SearchOption.AllDirectories))
            {
                try
                {
                    var fileObsolete = 0;
                    foreach (var row in CsvDocument.Parse(File.ReadAllText(csvFile)))
                    {
                        if (row.Count < 2) continue;
                        var existingKey = row[0].Trim();
                        if (existingKey.Length == 0 || string.Equals(existingKey, "key", StringComparison.OrdinalIgnoreCase)) continue;
                        if (!keySet.Contains(existingKey) && !MatchesAnyPattern(keyPatterns, existingKey))
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
            List<Regex> keyPatterns,
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

        var keySet = new HashSet<string>(distinctKeys, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in languageEntries)
        {
            var langCode = entry.LangCode;
            var csvPath = entry.CsvPath;
            var langDir = entry.LangDir;

            context.LogInfo($"Processing language: {langCode}");
            languagesProcessed++;

            // Existing rows, read with the shared CSV reader (quotes, commas and line breaks survive a rewrite).
            var csvMap = File.Exists(csvPath)
                ? CsvDocument.ToDictionary(File.ReadAllText(csvPath))
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Include standalone md/txt files as file references
            var localStandaloneCount = 0;
            if (!string.IsNullOrEmpty(langDir) && Directory.Exists(langDir))
            {
                var mdFiles = Directory.GetFiles(langDir, "*.md");
                var txtFiles = Directory.GetFiles(langDir, "*.txt");
                foreach (var f in mdFiles.Concat(txtFiles))
                {
                    var filename = Path.GetFileName(f);
                    var key = Path.GetFileNameWithoutExtension(f);
                    csvMap[key] = FileReference(filename);
                    localStandaloneCount++;
                }
            }

            totalStandaloneFilesIncluded += localStandaloneCount;

            // Ensure language directory exists
            if (!string.IsNullOrEmpty(langDir))
            {
                Directory.CreateDirectory(langDir);
            }

            var newRows = new List<KeyValuePair<string, string>>();
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
                            File.WriteAllText(filePath, FileContentFor(key, fileName, val), new UTF8Encoding(false));
                            filesCreated++;
                        }
                    }

                    newRows.Add(new KeyValuePair<string, string>(key, FileReference(fileName)));
                }
                else
                {
                    newRows.Add(new KeyValuePair<string, string>(key, val ?? string.Empty));
                }
            }

            // Rows matched by a [KeyPattern] are data-driven keys: keep them as they are. Any other unknown row is obsolete.
            foreach (var row in csvMap)
            {
                if (!keySet.Contains(row.Key) && MatchesAnyPattern(keyPatterns, row.Key))
                {
                    newRows.Add(row);
                }
            }

            var sorted = newRows.OrderBy(r => r.Key, StringComparer.Ordinal).ToList();

            // Prepare new content
            var newContent = CsvDocument.Serialize(sorted);
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

    /// <summary>The CSV value that points at a translation file in the language folder.</summary>
    private static string FileReference(string fileName) => "@file:" + fileName;

    /// <summary>
    /// Text of a newly created translation file: the key itself, unless the row holds real text. A missing row or a
    /// file reference gives the key.
    /// </summary>
    private static string FileContentFor(string key, string fileName, string value)
    {
        if (string.IsNullOrEmpty(value) || value.StartsWith("@file:", StringComparison.Ordinal) ||
            string.Equals(value, fileName, StringComparison.Ordinal))
        {
            return key;
        }

        return value;
    }

    private static List<Regex> CompilePatterns(IEnumerable<string> patterns)
    {
        return patterns
            .Select(pattern => KeyPatternSyntax.Parse(pattern).ToRegex())
            .Where(regex => regex != null)
            .ToList();
    }

    private static bool MatchesAnyPattern(List<Regex> keyPatterns, string key)
    {
        return keyPatterns.Any(regex => regex.IsMatch(key));
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
