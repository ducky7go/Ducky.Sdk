using System;

namespace Ducky.Sdk.Attributes;

/// <summary>
/// Indicates that a localization key's value should reference an external file.
/// The value in the CSV will be the filename (e.g., "Welcome.md"), and the actual
/// translated content will be loaded from files in the Locales/{lang}/ directory.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class TranslateFileAttribute : Attribute
{
    /// <summary>
    /// The file extension for the translation file.
    /// </summary>
    public string FileExtension { get; }

    /// <summary>
    /// Creates a TranslateFile attribute with default .txt extension.
    /// </summary>
    public TranslateFileAttribute()
    {
        FileExtension = "txt";
    }

    /// <summary>
    /// Creates a TranslateFile attribute with specified file extension.
    /// </summary>
    /// <param name="fileExtension">File extension without dot (e.g., "md", "txt")</param>
    public TranslateFileAttribute(string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            FileExtension = "txt";
        }
        else
        {
            // Remove leading dot if present
            FileExtension = fileExtension.TrimStart('.');
        }
    }
}
