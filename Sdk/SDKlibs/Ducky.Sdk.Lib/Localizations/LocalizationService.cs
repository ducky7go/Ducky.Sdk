// LocalizationService.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Ducky.Sdk.Logging;
using SodaCraft.Localizations;
using UnityEngine;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// Central localization service that:
/// - Loads CSV files from a Locales folder on demand (supports Locales/en.csv, Locales/en/*.csv, Locales/lang/*.csv)
/// - Exposes Get(key, fallback), SetLanguage with lazy loading
/// - Only loads language files when needed, reducing memory footprint
/// - English is loaded lazily as fallback when accessing missing keys
/// This mirrors the folder/file-based approach used by WorldEffectLocalizationManager.
/// </summary>
internal sealed class LocalizationService
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private readonly Dictionary<string, Dictionary<string, string>> _db =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string _localesPath;
    private SystemLanguage _currentLanguage = SystemLanguage.English;
    private string _currentLanguageCode = "en";

    private LocalizationService()
    {
        try
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? Application.dataPath;
            _localesPath = Path.Combine(assemblyDirectory, "Locales");
        }
        catch (Exception)
        {
            _localesPath = Path.Combine(Application.dataPath, "Locales");
        }

        SelectLanguage(LocalizationManager.CurrentLanguage);
    }

    /// <summary>
    /// Load localization files for a specific language on demand.
    /// </summary>
    private void LoadLanguage(string langCode)
    {
        if (_db.ContainsKey(langCode))
        {
            Log.Debug($"[LocalizationService] Language '{langCode}' already loaded, skipping");
            return;
        }

        if (string.IsNullOrEmpty(_localesPath) || !Directory.Exists(_localesPath))
        {
            Debug.LogWarning($"[LocalizationService] Locales directory not found: {_localesPath}");
            return;
        }

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _db[langCode] = dict;

        var csvFiles = Directory.GetFiles(_localesPath, "*.csv", SearchOption.AllDirectories);
        var loadedCount = 0;

        foreach (var file in csvFiles)
        {
            try
            {
                var relative = Path.GetRelativePath(_localesPath, file);
                var parts = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                    StringSplitOptions.RemoveEmptyEntries);
                string fileLangCode;
                if (parts.Length >= 2)
                {
                    // Locales/en/xxx.csv -> en
                    fileLangCode = parts[0].ToLowerInvariant();
                }
                else
                {
                    // Locales/en.csv -> en
                    fileLangCode = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                }

                // Only load files for the requested language
                if (!string.Equals(fileLangCode, langCode, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var content = File.ReadAllText(file, Encoding.UTF8);
                var map = CsvParser.ParseCsvToDictionary(content);
                if (map.Count == 0) continue;

                // Merge (later files override)
                foreach (var kv in map)
                {
                    try
                    {
                        var value = kv.Value;
                        // If value looks like a filename (has an extension) and does not contain directory separators,
                        // try to load the referenced file in this order:
                        // 1) {csvFolder}/{langCode}/{filename}
                        // 2) {localesRoot}/{langCode}/{filename}
                        // 3) {csvFolder}/{filename}
                        if (!string.IsNullOrEmpty(value) &&
                            value.Length <= 255 &&
                            (value.Length == kv.Key.Length + 3 + 1 ||
                             value.Length == kv.Key.Length + 4 + 1) && // reasonable length for a filename
                            !value.Contains(Path.DirectorySeparatorChar) &&
                            !value.Contains(Path.AltDirectorySeparatorChar) &&
                            !string.IsNullOrEmpty(Path.GetExtension(value)))
                        {
                            var csvDir = Path.GetDirectoryName(file) ?? _localesPath;
                            var candidateCsvSubLang = Path.Combine(csvDir, langCode, value);

                            if (File.Exists(candidateCsvSubLang))
                            {
                                value = File.ReadAllText(candidateCsvSubLang, Encoding.UTF8);
                            }
                        }

                        dict[kv.Key] = value;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(
                            $"[LocalizationService] Failed to load referenced file for key '{kv.Key}' in {file}: {ex.Message}");
                        dict[kv.Key] = kv.Value;
                    }
                }

                loadedCount += map.Count;
                Log.Debug($"[LocalizationService] Loaded {map.Count} entries for '{langCode}' from {file}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationService] Failed to load {file}: {ex.Message}");
            }
        }

        if (loadedCount == 0)
        {
            Debug.LogWarning($"[LocalizationService] No localization entries loaded for language: {langCode}");
        }
        else
        {
            Log.Info($"[LocalizationService] Successfully loaded {loadedCount} total entries for language: {langCode}");
        }
    }

    private void SelectLanguage(SystemLanguage language)
    {
        Log.Debug("[LocalizationService] Selecting language: " + language);
        _currentLanguage = language;
        _currentLanguageCode = GetLanguageCode(language);

        // Load the requested language on demand
        LoadLanguage(_currentLanguageCode);

        // If the requested language has no entries, ensure English is loaded as fallback
        if (!_db.TryGetValue(_currentLanguageCode, out var dict) || dict.Count == 0)
        {
            Debug.LogWarning(
                $"[LocalizationService] Localization not found for language: {_currentLanguageCode}, falling back to English");
            _currentLanguageCode = "en";
            LoadLanguage("en");
        }

        Log.Debug($"[LocalizationService] Selected language: {_currentLanguageCode}");

        // sync all keys to game localization manager
        if (_db.TryGetValue(_currentLanguageCode, out var currentDict))
        {
            foreach (var key in currentDict.Keys)
            {
                var value = Get(_currentLanguage, key);
                LocalizationManager.SetOverrideText(key, value);
            }
        }
    }

    public void SetLanguage(SystemLanguage language)
    {
        SelectLanguage(language);
    }

    public string Get(string key, string? fallback = null)
        => Get(_currentLanguage, key, fallback);

    public string Get(SystemLanguage language, string key, string? fallback = null)
    {
        var langCode = GetLanguageCode(language);
        if (!string.IsNullOrEmpty(langCode) && _db.TryGetValue(langCode, out var dict)
                                            && dict.TryGetValue(key, out var value) &&
                                            !string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Lazy load English as fallback if not already loaded
        if (!_db.ContainsKey("en"))
        {
            LoadLanguage("en");
        }

        if (_db.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enValue) &&
            !string.IsNullOrEmpty(enValue))
        {
            return enValue;
        }

        return fallback ?? $"[{key}]";
    }

    private static string GetLanguageCode(SystemLanguage language)
    {
        return language switch
        {
            SystemLanguage.Chinese => "zh",
            SystemLanguage.ChineseSimplified => "zh",
            SystemLanguage.ChineseTraditional => "zh-hant",
            SystemLanguage.English => "en",
            SystemLanguage.French => "fr",
            SystemLanguage.German => "de",
            SystemLanguage.Japanese => "ja",
            SystemLanguage.Korean => "ko",
            SystemLanguage.Portuguese => "pt",
            SystemLanguage.Russian => "ru",
            SystemLanguage.Spanish => "es",
            _ => "en"
        };
    }
}
