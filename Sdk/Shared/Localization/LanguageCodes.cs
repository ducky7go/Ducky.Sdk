// LanguageCodes.cs
//
// Shared source file. The Build copy compiles into its own namespace (DUCKY_SDK_BUILD_SHARED) so the test
// assembly, which sees both Lib and Build internals, never gets two types with one name. Linked into Ducky.Sdk.Lib (netstandard2.1), Ducky.Sdk.Analyser (netstandard2.0) and
// Ducky.Sdk.Build (net10.0) with <Compile Include Link>. Keep to netstandard2.0 APIs only.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if DUCKY_SDK_BUILD_SHARED
namespace Ducky.Sdk.Build.Localization;
#else
namespace Ducky.Sdk.Localizations;
#endif

/// <summary>
/// The single list of supported languages and the one rule that turns any spelling of a language code into the
/// canonical form used for file names, lookups and the generated metadata.
/// </summary>
internal static class LanguageCodes
{
    /// <summary>
    /// Every language the SDK knows. Used by <c>[LanguageSupport("all")]</c>, the source generator and the build steps.
    /// Order is part of the contract (it is the order written to <c>lkeys.json</c>).
    /// </summary>
    public static readonly string[] AllLanguages = { "de", "en", "es", "fr", "ja", "ko", "pt", "ru", "zh-hant", "zh" };

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["zh-hans"] = "zh",
        ["zh-cn"] = "zh",
        ["zh-sg"] = "zh",
        ["zh-tw"] = "zh-hant",
        ["zh-hk"] = "zh-hant",
        ["zh-mo"] = "zh-hant",
    };

    private static readonly Regex Shape = new("^[a-z]{2,3}(-[a-z0-9]{2,8})*$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Trims, turns <c>_</c> into <c>-</c>, lower-cases and applies the alias table.
    /// <c>zh-Hant</c>, <c>zh_hant</c> and <c>ZH-HANT</c> all become <c>zh-hant</c>. Returns an empty string for null or blank input.
    /// </summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        var text = code!.Trim().Replace('_', '-').ToLowerInvariant();
        return Aliases.TryGetValue(text, out var alias) ? alias : text;
    }

    /// <summary>True when the normalized code has the shape of a language code (2-3 letters, optional subtags).</summary>
    public static bool IsLanguageCode(string? code) => Shape.IsMatch(Normalize(code));

    /// <summary>
    /// The default fallback chain: the code, then each shorter form made by dropping the last <c>-</c> part,
    /// then <c>en</c>. A code appears at most once, so <c>en</c> gives just <c>en</c>.
    /// </summary>
    public static IReadOnlyList<string> DefaultChain(string? code)
    {
        var chain = new List<string>();
        var current = Normalize(code);
        while (current.Length > 0)
        {
            AddUnique(chain, current);
            var cut = current.LastIndexOf('-');
            if (cut <= 0)
            {
                break;
            }

            current = current.Substring(0, cut);
        }

        AddUnique(chain, "en");
        return chain;
    }

    private static void AddUnique(List<string> chain, string code)
    {
        if (!chain.Contains(code))
        {
            chain.Add(code);
        }
    }
}
