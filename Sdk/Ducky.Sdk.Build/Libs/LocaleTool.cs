#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ducky.Sdk.Build.Localization;

/// <summary>
/// Translation commands of the build runner: <c>locales set</c>, <c>locales sync</c> and <c>locales validate</c>.
/// They read and write the same CSV files as the runtime (<see cref="CsvDocument"/>), so quotes, commas and line
/// breaks in values are kept. Every write goes to a temp file first, and a file whose content would not change is left
/// alone.
/// </summary>
internal static class LocaleTool
{
    internal const int ExitOk = 0;
    internal const int ExitFailure = 1;
    internal const int ExitUsage = 2;

    internal const string Usage =
        "Usage:\n" +
        "  locales set <Locales dir> <lang> <key> <value>\n" +
        "  locales set <Locales dir> <key> --lang <lang>=<value> [--lang <lang>=<value> ...]\n" +
        "  locales sync <Locales dir> [--fill english|empty] [--prune] [--dry-run]\n" +
        "  locales validate <Locales dir>... [--validation Off|Warn|Error] [--extra-keys Ignore|Warn|Error]";

    /// <summary>Runs one command. <paramref name="args"/> are the arguments after <c>locales</c>.</summary>
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            error.WriteLine(Usage);
            return ExitUsage;
        }

        var rest = args.Skip(1).ToArray();
        try
        {
            switch (args[0])
            {
                case "set":
                    return Set(rest, output, error);
                case "sync":
                    return Sync(rest, output, error);
                case "validate":
                    return Validate(rest, output, error);
                default:
                    error.WriteLine($"[locales] unknown command '{args[0]}'");
                    error.WriteLine(Usage);
                    return ExitUsage;
            }
        }
        catch (Exception ex)
        {
            error.WriteLine($"[locales][ERROR] {ex.Message}");
            return ExitFailure;
        }
    }

    // set -----------------------------------------------------------------

    private static int Set(string[] args, TextWriter output, TextWriter error)
    {
        string dir;
        string key;
        var values = new List<(string Language, string Value)>();

        if (args.Contains("--lang"))
        {
            // Multi form: <dir> <key> --lang <lang>=<value> ...
            if (args.Length < 4 || args[0].StartsWith("--", StringComparison.Ordinal))
            {
                return Usage_(error);
            }

            dir = args[0];
            key = args[1];
            for (var i = 2; i < args.Length; i++)
            {
                if (args[i] != "--lang" || i + 1 >= args.Length)
                {
                    return Usage_(error);
                }

                var pair = args[++i];
                var eq = pair.IndexOf('=');
                if (eq <= 0)
                {
                    return Usage_(error);
                }

                values.Add((pair.Substring(0, eq), pair.Substring(eq + 1)));
            }
        }
        else
        {
            // Single form: <dir> <lang> <key> <value>
            if (args.Length != 4)
            {
                return Usage_(error);
            }

            dir = args[0];
            values.Add((args[1], args[3]));
            key = args[2];
        }

        if (string.IsNullOrEmpty(key))
        {
            return Usage_(error);
        }

        foreach (var (language, value) in values)
        {
            if (!LanguageCodes.IsLanguageCode(language))
            {
                error.WriteLine($"[locales] '{language}' is not a language code");
                return ExitUsage;
            }
        }

        Directory.CreateDirectory(dir);
        foreach (var group in values.GroupBy(entry => LanguageCodes.Normalize(entry.Language)))
        {
            var file = LanguageFile(dir, group.Key);
            var map = ReadMap(file);
            foreach (var entry in group)
            {
                map[key] = entry.Value;
            }

            if (WriteIfChanged(file, map))
            {
                output.WriteLine($"set {key} in {file}");
            }
        }

        return ExitOk;
    }

    // sync ----------------------------------------------------------------

    private static int Sync(string[] args, TextWriter output, TextWriter error)
    {
        var options = ParseOptions(args, error, out var dirs);
        if (options == null || dirs.Count != 1)
        {
            return Usage_(error);
        }

        var dir = dirs[0];
        var english = LanguageFile(dir, "en");
        if (!File.Exists(english))
        {
            error.WriteLine($"[locales] no English file in {dir}: {english} is missing");
            return ExitFailure;
        }

        var baseline = ReadMap(english);
        var patterns = PatternsFor(dir);
        var englishPath = english;

        foreach (var file in LanguageFiles(dir).Where(file => LanguageCodes.Normalize(Path.GetFileNameWithoutExtension(file)) != "en"))
        {
            var map = ReadMap(file);
            var added = new List<string>();
            var removed = new List<string>();

            foreach (var entry in baseline)
            {
                if (!map.ContainsKey(entry.Key))
                {
                    map[entry.Key] = options.FillEmpty ? string.Empty : entry.Value;
                    added.Add(entry.Key);
                }
            }

            if (options.Prune)
            {
                foreach (var key in map.Keys.ToList())
                {
                    if (!baseline.ContainsKey(key) && !patterns.Any(pattern => pattern.IsMatch(key)))
                    {
                        map.Remove(key);
                        removed.Add(key);
                    }
                }
            }

            if (options.DryRun)
            {
                foreach (var key in added)
                {
                    output.WriteLine($"would add {key} to {file}");
                }

                foreach (var key in removed)
                {
                    output.WriteLine($"would remove {key} from {file}");
                }

                continue;
            }

            if (WriteIfChanged(file, map))
            {
                output.WriteLine($"sync {file}: +{added.Count} key(s), -{removed.Count} key(s)");
            }
        }

        // The English file is only re-sorted and re-quoted, never changed in content.
        if (!options.DryRun && WriteIfChanged(englishPath, baseline))
        {
            output.WriteLine($"sorted {englishPath}");
        }

        return ExitOk;
    }

    // validate ------------------------------------------------------------

    private static int Validate(string[] args, TextWriter output, TextWriter error)
    {
        var options = ParseOptions(args, error, out var dirs);
        if (options == null || dirs.Count == 0)
        {
            return Usage_(error);
        }

        var errors = 0;
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                error.WriteLine($"[locales] not a folder: {dir}");
                return ExitUsage;
            }

            // Same layout as the build: the assets folder holds lkeys.json and missing.csv, Locales is inside it.
            var assetsDir = Path.GetDirectoryName(Path.GetFullPath(dir)) ?? dir;
            var summary = ValidateLocalizationLib.RunValidation(
                assetsDir, Path.GetFullPath(dir), options.Validation, options.ExtraKeys, output, error);
            errors += summary.Errors;
            output.WriteLine($"validated {dir}: {summary.Findings} finding(s), {summary.Missing} missing translation(s)");
        }

        return errors > 0 ? ExitFailure : ExitOk;
    }

    // helpers -------------------------------------------------------------

    private sealed class Options
    {
        public bool FillEmpty { get; set; }
        public bool Prune { get; set; }
        public bool DryRun { get; set; }
        public ValidateLocalizationLib.Severity Validation { get; set; } = ValidateLocalizationLib.Severity.Warn;
        public ValidateLocalizationLib.Severity ExtraKeys { get; set; } = ValidateLocalizationLib.Severity.Warn;
    }

    /// <summary>Parses the options of sync and validate and collects the folder arguments. Null when an option is bad.</summary>
    private static Options ParseOptions(string[] args, TextWriter error, out List<string> dirs)
    {
        var options = new Options();
        dirs = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--fill":
                    if (i + 1 >= args.Length || !(args[i + 1] == "english" || args[i + 1] == "empty"))
                    {
                        error.WriteLine("[locales] --fill takes english or empty");
                        return null;
                    }

                    options.FillEmpty = args[++i] == "empty";
                    break;
                case "--prune":
                    options.Prune = true;
                    break;
                case "--dry-run":
                    options.DryRun = true;
                    break;
                case "--validation":
                    if (i + 1 >= args.Length) return null;
                    options.Validation = ValidateLocalizationLib.ParseSeverity(args[++i], ValidateLocalizationLib.Severity.Warn, allowIgnore: false);
                    break;
                case "--extra-keys":
                    if (i + 1 >= args.Length) return null;
                    options.ExtraKeys = ValidateLocalizationLib.ParseSeverity(args[++i], ValidateLocalizationLib.Severity.Warn, allowIgnore: true);
                    break;
                default:
                    if (args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        error.WriteLine($"[locales] unknown option '{args[i]}'");
                        return null;
                    }

                    dirs.Add(args[i]);
                    break;
            }
        }

        return options;
    }

    private static int Usage_(TextWriter error)
    {
        error.WriteLine(Usage);
        return ExitUsage;
    }

    /// <summary>The top-level CSV of a language (any spelling of its code), or <c>{code}.csv</c> when there is none yet.</summary>
    internal static string LanguageFile(string dir, string language)
    {
        var code = LanguageCodes.Normalize(language);
        var existing = Directory.Exists(dir)
            ? Directory.GetFiles(dir, "*.csv", SearchOption.TopDirectoryOnly)
                .Where(path => LanguageCodes.Normalize(Path.GetFileNameWithoutExtension(path)) == code)
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
                .FirstOrDefault()
            : null;
        return existing ?? Path.Combine(dir, code + ".csv");
    }

    /// <summary>The top-level language CSVs of a folder, in name order.</summary>
    private static IEnumerable<string> LanguageFiles(string dir)
    {
        return Directory.GetFiles(dir, "*.csv", SearchOption.TopDirectoryOnly)
            .Where(path => LanguageCodes.IsLanguageCode(Path.GetFileNameWithoutExtension(path)))
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Key/value rows of a CSV, keys ignoring case. Empty when the file does not exist.</summary>
    private static Dictionary<string, string> ReadMap(string file)
    {
        return File.Exists(file)
            ? CsvDocument.ToDictionary(File.ReadAllText(file))
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static List<Regex> PatternsFor(string dir)
    {
        var assetsDir = Path.GetDirectoryName(Path.GetFullPath(dir)) ?? dir;
        ValidateLocalizationLib.ReadStaticKeys(Path.Combine(assetsDir, "lkeys.json"), out var patterns);
        return patterns;
    }

    /// <summary>
    /// Writes the map as a Key,Value file sorted by key (ordinal), through a temp file. Returns false and writes nothing
    /// when the content is already the same.
    /// </summary>
    internal static bool WriteIfChanged(string file, IDictionary<string, string> map)
    {
        var content = CsvDocument.Serialize(map.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        if (File.Exists(file) && File.ReadAllText(file) == content)
        {
            return false;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(file));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = file + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(false));
            File.Move(temp, file, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }

        return true;
    }
}
