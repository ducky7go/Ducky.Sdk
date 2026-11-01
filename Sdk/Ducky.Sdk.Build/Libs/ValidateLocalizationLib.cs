#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ducky.Sdk.Build.Localization;

/// <summary>
/// Build step: compares every language in <c>Locales</c> with the English baseline and reports missing keys, empty
/// values, placeholder mismatches and extra keys. The baseline is the static key list of <c>lkeys.json</c> when that file
/// exists, otherwise the English CSV. The step never edits a language file. It writes <c>missing.csv</c> next to the
/// Locales folder (not inside it) when something is missing.
/// Entry point: ValidateLocalizationLib.Execute(BuildContext context)
/// </summary>
public class ValidateLocalizationLib
{
    internal const string MissingReportFileName = "missing.csv";

    /// <summary>Severity of a finding kind, from the LocalizationValidation or LocalizationExtraKeys property.</summary>
    internal enum Severity
    {
        Ignore,
        Off,
        Warn,
        Error,
    }

    internal enum FindingKind
    {
        /// <summary>Neither lkeys.json nor an English CSV exists: nothing is compared.</summary>
        BaselineMissing,

        /// <summary>A baseline key is absent from a language, or has an empty value there.</summary>
        Missing,

        /// <summary>The numbered placeholders of a translation differ from the English text.</summary>
        Placeholder,

        /// <summary>A language has a key the baseline does not have, and no pattern matches it.</summary>
        Extra,
    }

    internal sealed class Finding
    {
        public FindingKind Kind { get; set; }
        public string Language { get; set; }
        public string Key { get; set; }
        public string File { get; set; }
        public string Detail { get; set; }
    }

    internal sealed class MissingRow
    {
        public string Language { get; set; }
        public string Key { get; set; }
        public string English { get; set; }
    }

    internal sealed class ValidationResult
    {
        public List<Finding> Findings { get; } = new();
        public List<MissingRow> Missing { get; } = new();
    }

    private static readonly Regex PlaceholderPattern =
        new Regex(@"\{(\d+)(?:,[^}:]*)?(?::[^}]*)?\}", RegexOptions.CultureInvariant);

    /// <summary>
    /// Main entry point. Exit codes: 0 success (warnings allowed), 1 at least one Error finding, SkipExitCode when
    /// validation is Off or there is no Locales folder.
    /// </summary>
    public static int Execute(BuildContext context)
    {
        try
        {
            var mode = ParseSeverity(context.LocalizationValidation, Severity.Warn, allowIgnore: false);
            var extraMode = ParseSeverity(context.LocalizationExtraKeys, Severity.Warn, allowIgnore: true);
            if (mode == Severity.Off)
            {
                context.LogInfo("Localization validation is Off; skipped");
                return SkipExitCode;
            }

            var assetsDir = context.AssetsDir;
            var localesDir = Path.Combine(assetsDir, "Locales");
            if (!Directory.Exists(localesDir))
            {
                context.LogInfo($"No Locales folder at {localesDir}; localization validation skipped");
                return SkipExitCode;
            }

            var lkeysPath = Path.Combine(assetsDir, "lkeys.json");
            var staticKeys = ReadStaticKeys(lkeysPath, out var patterns);

            var result = Validate(localesDir, staticKeys, patterns);
            var errors = Report(result, mode, extraMode, context);
            WriteMissingReport(assetsDir, result.Missing, mode);

            context.LogInfo(
                $"Localization validation: {result.Findings.Count} finding(s), {result.Missing.Count} missing translation(s)");
            return errors > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            context.LogError($"Localization validation exception: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Compares the languages of a Locales folder with the baseline. Works from the files alone when there is no static
    /// key list. Keys matched by a pattern are never reported as extra.
    /// </summary>
    internal static ValidationResult Validate(string localesDir, IReadOnlyCollection<string> staticKeys, IReadOnlyList<Regex> patterns)
    {
        var result = new ValidationResult();
        var languages = DiscoverLanguages(localesDir);
        var loaded = languages.ToDictionary(pair => pair.Key, pair => LoadLanguage(pair.Value), StringComparer.Ordinal);

        IEnumerable<string> baseline;
        if (staticKeys != null)
        {
            baseline = staticKeys;
        }
        else if (loaded.TryGetValue("en", out var english0))
        {
            baseline = english0.Keys;
        }
        else
        {
            result.Findings.Add(new Finding
            {
                Kind = FindingKind.BaselineMissing,
                Detail = "no English baseline: neither lkeys.json nor en.csv exists, nothing was compared",
            });
            return result;
        }

        var baselineKeys = new HashSet<string>(baseline, StringComparer.OrdinalIgnoreCase);
        loaded.TryGetValue("en", out var english);

        foreach (var language in loaded.Keys.Where(code => code != "en").OrderBy(code => code, StringComparer.Ordinal))
        {
            var values = loaded[language];
            var file = languages[language].DisplayPath;

            foreach (var key in baselineKeys.OrderBy(key => key, StringComparer.Ordinal))
            {
                if (!values.TryGetValue(key, out var text) || string.IsNullOrEmpty(text))
                {
                    result.Findings.Add(new Finding
                    {
                        Kind = FindingKind.Missing, Language = language, Key = key, File = file,
                        Detail = $"'{key}' is missing in '{language}'",
                    });
                    result.Missing.Add(new MissingRow
                    {
                        Language = language,
                        Key = key,
                        English = EnglishText(english, key),
                    });
                    continue;
                }

                var englishText = EnglishText(english, key);
                if (englishText.Length > 0 && !PlaceholderIndices(englishText).SetEquals(PlaceholderIndices(text)))
                {
                    result.Findings.Add(new Finding
                    {
                        Kind = FindingKind.Placeholder, Language = language, Key = key, File = file,
                        Detail = $"placeholders differ: English {FormatSet(PlaceholderIndices(englishText))}, '{language}' {FormatSet(PlaceholderIndices(text))}",
                    });
                }
            }

            foreach (var key in values.Keys.OrderBy(key => key, StringComparer.Ordinal))
            {
                if (baselineKeys.Contains(key) || (patterns != null && patterns.Any(pattern => pattern.IsMatch(key))))
                {
                    continue;
                }

                result.Findings.Add(new Finding
                {
                    Kind = FindingKind.Extra, Language = language, Key = key, File = file,
                    Detail = $"'{key}' is not in the English baseline",
                });
            }
        }

        result.Missing.Sort((a, b) =>
        {
            var byLanguage = string.CompareOrdinal(a.Language, b.Language);
            return byLanguage != 0 ? byLanguage : string.CompareOrdinal(a.Key, b.Key);
        });
        return result;
    }

    /// <summary>The placeholder indices of a text, with <c>{{</c> and <c>}}</c> removed first. Order and repeats do not matter.</summary>
    internal static HashSet<int> PlaceholderIndices(string text)
    {
        var withoutEscapes = (text ?? string.Empty).Replace("{{", string.Empty).Replace("}}", string.Empty);
        var indices = new HashSet<int>();
        foreach (Match match in PlaceholderPattern.Matches(withoutEscapes))
        {
            indices.Add(int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        }

        return indices;
    }

    /// <summary>
    /// Writes <c>missing.csv</c> (Language,Key,English) into the assets folder, sorted by language then key. Removes an
    /// old report when nothing is missing. Writes nothing when validation is Off.
    /// </summary>
    internal static void WriteMissingReport(string assetsDir, IReadOnlyList<MissingRow> rows, Severity mode)
    {
        if (mode == Severity.Off)
        {
            return;
        }

        var path = Path.Combine(assetsDir, MissingReportFileName);
        if (rows.Count == 0)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        var sb = new StringBuilder("Language,Key,English\n");
        foreach (var row in rows)
        {
            sb.Append(Field(row.Language)).Append(',')
                .Append(Field(row.Key)).Append(',')
                .Append(CsvDocument.Quote(row.English ?? string.Empty)).Append('\n');
        }

        var content = sb.ToString();
        if (File.Exists(path) && File.ReadAllText(path) == content)
        {
            return;
        }

        Directory.CreateDirectory(assetsDir);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    /// <summary>Prints the findings as warning: and error: lines and returns the number of errors.</summary>
    private static int Report(ValidationResult result, Severity mode, Severity extraMode, BuildContext context)
    {
        var errors = 0;
        foreach (var finding in result.Findings)
        {
            var severity = finding.Kind == FindingKind.Extra ? extraMode : mode;
            if (severity == Severity.Ignore || severity == Severity.Off)
            {
                continue;
            }

            var location = string.IsNullOrEmpty(finding.File) ? string.Empty : $" ({finding.File})";
            var line = $"[LocalizationValidation] {finding.Detail}{location}";
            if (severity == Severity.Error)
            {
                errors++;
                Console.Error.WriteLine($"error: {line}");
            }
            else
            {
                Console.WriteLine($"warning: {line}");
            }
        }

        return errors;
    }

    /// <summary>Parses Off/Warn/Error (or Ignore/Warn/Error for extra keys). An unknown value gives the fallback and a warning.</summary>
    internal static Severity ParseSeverity(string value, Severity fallback, bool allowIgnore)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "off":
                return allowIgnore ? fallback : Severity.Off;
            case "ignore":
                return allowIgnore ? Severity.Ignore : fallback;
            case "warn":
                return Severity.Warn;
            case "error":
                return Severity.Error;
            default:
                Console.WriteLine($"warning: [LocalizationValidation] unknown severity '{value}', using {fallback}");
                return fallback;
        }
    }

    /// <summary>Static keys and patterns from lkeys.json. Returns null for keys when the file does not exist.</summary>
    private static List<string> ReadStaticKeys(string lkeysPath, out List<Regex> patterns)
    {
        patterns = new List<Regex>();
        if (!File.Exists(lkeysPath))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(lkeysPath));
        var root = doc.RootElement;
        if (root.TryGetProperty("patterns", out var patternArray) && patternArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in patternArray.EnumerateArray())
            {
                var regex = KeyPatternSyntax.Parse(element.GetString()).ToRegex();
                if (regex != null)
                {
                    patterns.Add(regex);
                }
            }
        }

        var keys = new List<string>();
        if (root.TryGetProperty("keys", out var keyArray) && keyArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in keyArray.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(element.GetString()))
                {
                    keys.Add(element.GetString());
                }
            }
        }

        return keys;
    }

    private static Dictionary<string, LanguageFiles> DiscoverLanguages(string localesDir)
    {
        var languages = new Dictionary<string, LanguageFiles>(StringComparer.Ordinal);
        foreach (var csv in Directory.GetFiles(localesDir, "*.csv", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal))
        {
            var raw = Path.GetFileNameWithoutExtension(csv);
            if (!LanguageCodes.IsLanguageCode(raw))
            {
                continue;
            }

            var code = LanguageCodes.Normalize(raw);
            if (!languages.TryGetValue(code, out var files))
            {
                files = new LanguageFiles { DisplayPath = csv };
                languages[code] = files;
            }

            files.Csv.Add(csv);
        }

        foreach (var folder in Directory.GetDirectories(localesDir, "*", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal))
        {
            var raw = Path.GetFileName(folder);
            if (!LanguageCodes.IsLanguageCode(raw))
            {
                continue;
            }

            var code = LanguageCodes.Normalize(raw);
            if (!languages.TryGetValue(code, out var files))
            {
                files = new LanguageFiles { DisplayPath = folder };
                languages[code] = files;
            }

            files.Csv.AddRange(Directory.GetFiles(folder, "*.csv", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal));
        }

        return languages;
    }

    private static Dictionary<string, string> LoadLanguage(LanguageFiles files)
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files.Csv)
        {
            foreach (var row in CsvDocument.ToDictionary(File.ReadAllText(file)))
            {
                merged[row.Key] = row.Value;
            }
        }

        return merged;
    }

    private static string EnglishText(Dictionary<string, string> english, string key)
    {
        return english != null && english.TryGetValue(key, out var text) ? text ?? string.Empty : string.Empty;
    }

    private static string FormatSet(IEnumerable<int> indices)
    {
        return "{" + string.Join(", ", indices.OrderBy(i => i).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "}";
    }

    private static string Field(string value)
    {
        var needsQuotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 || value.Trim() != value;
        return needsQuotes ? CsvDocument.Quote(value) : value;
    }

    private sealed class LanguageFiles
    {
        public List<string> Csv { get; } = new();

        /// <summary>The top-level CSV, or the language folder when the language has only a folder.</summary>
        public string DisplayPath { get; set; }
    }
}
