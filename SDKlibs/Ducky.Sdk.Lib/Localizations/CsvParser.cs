// CsvParser.cs

using System;
using System.Collections.Generic;
using System.Text;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// CSV parsing helpers used by LocalizationManager.
/// Supports simple two-column CSVs (key,value) and quoted fields with commas.
/// </summary>
internal static class CsvParser
{
    /// <summary>
    /// Parse simple CSV content into key->value dictionary. Supports quoted fields.
    /// Expects each meaningful line to contain at least two columns: key,value
    /// </summary>
    public static Dictionary<string, string> ParseCsvToDictionary(string csvContent)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(csvContent))
        {
            return result;
        }

        var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Allow header rows; skip if first cell is 'key' (case-insensitive)
            var fields = ParseCsvLine(line);
            if (fields.Count < 2) continue;
            var key = fields[0].Trim();
            if (string.Equals(key, "key", StringComparison.OrdinalIgnoreCase)) continue;
            var value = fields[1].Trim();
            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// CSV line parser handling quoted fields and commas inside quotes.
    /// Handles escaped quotes ("") inside quoted fields.
    /// </summary>
    public static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                // Handle escaped quotes ""
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        fields.Add(sb.ToString());
        return fields;
    }
}