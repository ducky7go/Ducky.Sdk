using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// Reads a <c>Locales</c> folder. The folder is enumerated once, when the provider is created or <see cref="Reindex"/>
/// is called; loading a language then opens only that language's files.
/// Files are recognized as: <c>{lang}.csv</c> at the top level, and <c>{lang}/**/*.csv</c> inside a language folder.
/// Names that are not language codes (for example <c>missing.csv</c>) are ignored.
/// </summary>
internal sealed class DirectoryLocalizationProvider : ILocalizationProvider
{
    private readonly string _root;
    private Dictionary<string, List<string>> _index = new(StringComparer.Ordinal);

    public DirectoryLocalizationProvider(ModId modId, string localesDirectory)
    {
        ModId = modId;
        _root = localesDirectory ?? string.Empty;
        Reindex();
    }

    public ModId ModId { get; }

    /// <summary>The folder this provider reads.</summary>
    public string Directory => _root;

    /// <summary>How many times the folder has been enumerated (once per index build). Used by tests.</summary>
    internal int EnumerationCount { get; private set; }

    public IReadOnlyCollection<string> Languages => _index.Keys.ToList();

    /// <summary>Rebuilds the language index from the folder.</summary>
    public void Reindex()
    {
        var index = new Dictionary<string, List<(string File, bool InLanguageFolder)>>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(_root) || !System.IO.Directory.Exists(_root))
        {
            Log.Warn($"[Localization] Locales directory not found, source is empty: {_root}");
            _index = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            return;
        }

        EnumerationCount++;
        foreach (var file in System.IO.Directory.GetFiles(_root, "*.csv", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_root, file);
            var parts = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries);
            var inLanguageFolder = parts.Length >= 2;
            var rawLanguage = inLanguageFolder ? parts[0] : Path.GetFileNameWithoutExtension(file);
            if (!LanguageCodes.IsLanguageCode(rawLanguage))
            {
                Log.Debug($"[Localization] ignored '{relative}': '{rawLanguage}' is not a language code");
                continue;
            }

            var code = LanguageCodes.Normalize(rawLanguage);
            if (!index.TryGetValue(code, out var files))
            {
                files = new List<(string, bool)>();
                index[code] = files;
            }

            files.Add((file, inLanguageFolder));
        }

        _index = index.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                // {lang}.csv first, then the files of {lang}/, each group in ordinal order of its path
                .OrderBy(entry => entry.InLanguageFolder ? 1 : 0)
                .ThenBy(entry => entry.File, StringComparer.Ordinal)
                .Select(entry => entry.File)
                .ToList(),
            StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, string> Load(string language)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!_index.TryGetValue(language, out var files))
        {
            return result;
        }

        foreach (var file in files)
        {
            string text;
            try
            {
                text = File.ReadAllText(file, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log.Warn($"[Localization] failed to read {file}: {ex.Message}");
                continue;
            }

            var map = CsvDocument.ToDictionary(text, message => Log.Warn($"[Localization] {file}: {message}"));
            foreach (var entry in map)
            {
                var value = ResolveValue(language, file, entry.Key, entry.Value);
                if (value != null)
                {
                    result[entry.Key] = value;
                }
            }

            Log.Debug($"[Localization] loaded {map.Count} entries for '{language}' from {file}");
        }

        return result;
    }

    /// <summary>
    /// Resolves a CSV value. A value starting with <c>@file:</c> is a reference to a text file in the language folder.
    /// The exact legacy form <c>{key}.md|txt</c> is still read when that file exists, with a deprecation notice.
    /// Any other value is text. Returns null when a reference cannot be read, so the entry is skipped.
    /// </summary>
    private string? ResolveValue(string language, string csvFile, string key, string value)
    {
        const string marker = "@file:";
        if (value.StartsWith(marker, StringComparison.Ordinal))
        {
            var name = value.Substring(marker.Length).Trim();
            if (!IsSafeFileName(name))
            {
                Log.Warn($"[Localization] rejected file reference '{value}' for key '{key}' in {csvFile}: it must name a file in the language folder");
                return null;
            }

            var candidate = FindReferencedFile(language, csvFile, name);
            if (candidate == null)
            {
                Log.Warn($"[Localization] missing file '{name}' for key '{key}' in language '{language}' (from {csvFile})");
                return null;
            }

            return ReadReferenced(candidate);
        }

        if (IsLegacyFileReference(key, value))
        {
            var candidate = FindReferencedFile(language, csvFile, value);
            if (candidate != null)
            {
                Log.Warn($"[Localization] deprecated: '{value}' for key '{key}' is read as a file. Rewrite it as '@file:{value}'.");
                return ReadReferenced(candidate);
            }
        }

        return value;
    }

    private string? FindReferencedFile(string language, string csvFile, string name)
    {
        var csvDirectory = Path.GetDirectoryName(csvFile) ?? _root;
        var fileStem = Path.GetFileNameWithoutExtension(csvFile);
        var languageFolders = new List<string>();
        // A CSV inside {lang}/ already sits in its language folder; a top-level {lang}.csv owns the folder {lang}/.
        languageFolders.Add(string.Equals(Path.GetFullPath(csvDirectory), Path.GetFullPath(_root), StringComparison.Ordinal)
            ? Path.Combine(_root, fileStem)
            : csvDirectory);
        languageFolders.Add(Path.Combine(_root, language));
        languageFolders.Add(csvDirectory);

        foreach (var folder in languageFolders.Distinct())
        {
            var candidate = Path.Combine(folder, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsLegacyFileReference(string key, string value)
    {
        foreach (var extension in new[] { "md", "txt" })
        {
            if (string.Equals(value, key + "." + extension, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSafeFileName(string name)
    {
        if (name.Length == 0 || Path.IsPathRooted(name))
        {
            return false;
        }

        return name.IndexOfAny(new[] { '/', '\\' }) < 0 && !name.Contains("..");
    }

    private static string? ReadReferenced(string path)
    {
        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Log.Warn($"[Localization] failed to read referenced file {path}: {ex.Message}");
            return null;
        }
    }
}
