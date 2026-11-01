// CsvParser.cs

using System.Collections.Generic;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// Compatibility wrapper over the shared <see cref="CsvDocument"/> reader. Kept so existing callers and tests keep
/// their entry points; all parsing rules live in <see cref="CsvDocument"/>.
/// </summary>
internal static class CsvParser
{
    /// <summary>
    /// Parses CSV content into a key-to-value dictionary (case-insensitive keys, later duplicates win).
    /// </summary>
    public static Dictionary<string, string> ParseCsvToDictionary(string csvContent)
        => CsvDocument.ToDictionary(csvContent);

    /// <summary>
    /// Parses one CSV line into its fields. Quoted fields, commas inside quotes and <c>""</c> escapes are supported.
    /// An empty line gives one empty field.
    /// </summary>
    public static List<string> ParseCsvLine(string line)
    {
        var rows = CsvDocument.Parse(line);
        if (rows.Count == 0)
        {
            return new List<string> { string.Empty };
        }

        return rows[0];
    }
}
