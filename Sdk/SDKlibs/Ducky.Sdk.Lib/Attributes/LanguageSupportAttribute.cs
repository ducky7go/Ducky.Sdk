using System;
using System.Linq;

namespace Ducky.Sdk.Attributes;

/// <summary>
/// Specifies which languages should have CSV files and translation support generated.
/// Apply this attribute to the LK class to control which language files are created.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LanguageSupportAttribute : Attribute
{
    /// <summary>
    /// Array of language codes (e.g., "en", "zh", "fr") that should be supported.
    /// This property contains the processed language list after expanding "all" keyword.
    /// </summary>
    public string[] Languages { get; }

    /// <summary>
    /// Gets the predefined list of all supported languages.
    /// </summary>
    public static string[] AllLanguages => new[]
    {
        "de", "en", "es", "fr", "ja", "ko", "pt", "ru", "zh-hant", "zh"
    };

    /// <summary>
    /// Creates a LanguageSupport attribute with specified language codes.
    /// </summary>
    /// <param name="languages">Language codes (e.g., "en", "zh", "fr", "de", "ja") or "all" to generate all supported languages</param>
    public LanguageSupportAttribute(params string[] languages)
    {
        if (languages == null || languages.Length == 0)
        {
            // Default to English if no languages specified
            Languages = new[] { "en" };
            return;
        }

        // Check if "all" keyword is used (case-insensitive)
        if (languages.Any(lang => string.Equals(lang, "all", StringComparison.OrdinalIgnoreCase)))
        {
            // Start with all supported languages
            var result = AllLanguages.ToList();

            // Add any additional explicit languages (excluding "all" keywords)
            var additionalLanguages = languages
                .Where(lang => !string.Equals(lang, "all", StringComparison.OrdinalIgnoreCase))
                .Where(lang => !string.IsNullOrWhiteSpace(lang))
                .Select(lang => lang.ToLowerInvariant());

            result.AddRange(additionalLanguages);

            // Remove duplicates while preserving order
            Languages = result.Distinct().ToArray();
            return;
        }

        // Process explicit language codes (original behavior)
        Languages = languages
            .Where(lang => !string.IsNullOrWhiteSpace(lang))
            .Select(lang => lang.ToLowerInvariant())
            .ToArray();
    }
}
