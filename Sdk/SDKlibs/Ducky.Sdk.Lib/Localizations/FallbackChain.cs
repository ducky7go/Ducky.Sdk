using System;
using System.Collections.Generic;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// Builds the ordered list of languages a lookup walks: the requested language, then its configured or default
/// fallbacks, always ending with <c>en</c> and never repeating a code.
/// </summary>
internal static class FallbackChain
{
    /// <summary>The chain for a language: the configured chain when there is one, otherwise the default chain.</summary>
    public static IReadOnlyList<string> For(string language, IReadOnlyDictionary<string, IReadOnlyList<string>> configured)
    {
        var code = LanguageCodes.Normalize(language);
        if (configured.TryGetValue(code, out var chain))
        {
            return chain;
        }

        return LanguageCodes.DefaultChain(code);
    }

    /// <summary>
    /// Turns a configured list into a full chain: the language itself, then the given codes in order, then
    /// <c>en</c> last. Codes that are not language-shaped are dropped with a warning, and a repeated code is dropped
    /// with a warning. Naming the language itself again is ignored silently.
    /// </summary>
    public static IReadOnlyList<string> Build(string language, IReadOnlyList<string> configured, Action<string> warn)
    {
        var head = LanguageCodes.Normalize(language);
        var chain = new List<string> { head };
        foreach (var raw in configured)
        {
            var code = LanguageCodes.Normalize(raw);
            if (!LanguageCodes.IsLanguageCode(code))
            {
                warn($"[Localization] ignored fallback code '{raw}' for '{head}': not a language code");
                continue;
            }

            if (code == head)
            {
                continue;
            }

            if (chain.Contains(code))
            {
                warn($"[Localization] ignored repeated fallback code '{code}' for '{head}'");
                continue;
            }

            chain.Add(code);
        }

        chain.Remove("en");
        chain.Add("en");
        return chain;
    }
}
