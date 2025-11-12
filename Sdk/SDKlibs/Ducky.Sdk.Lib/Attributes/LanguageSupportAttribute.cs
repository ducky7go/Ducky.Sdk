using System;

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
    /// </summary>
    public string[] Languages { get; }

    /// <summary>
    /// Creates a LanguageSupport attribute with specified language codes.
    /// </summary>
    /// <param name="languages">Language codes (e.g., "en", "zh", "fr", "de", "ja")</param>
    public LanguageSupportAttribute(params string[] languages)
    {
        if (languages == null || languages.Length == 0)
        {
            // Default to English if no languages specified
            Languages = new[] { "en" };
        }
        else
        {
            Languages = languages;
        }
    }
}
