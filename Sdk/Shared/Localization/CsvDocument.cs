// CsvDocument.cs
//
// Shared source file. The Build copy compiles into its own namespace (DUCKY_SDK_BUILD_SHARED) so the test
// assembly, which sees both Lib and Build internals, never gets two types with one name. Linked into Ducky.Sdk.Lib (netstandard2.1), Ducky.Sdk.Analyser (netstandard2.0) and
// Ducky.Sdk.Build (net10.0). Keep to netstandard2.0 APIs only. It logs nothing itself; problems are reported through
// the optional diagnostic callback so each host can route them to its own logger.

using System;
using System.Collections.Generic;
using System.Text;

#if DUCKY_SDK_BUILD_SHARED
namespace Ducky.Sdk.Build.Localization;
#else
namespace Ducky.Sdk.Localizations;
#endif

/// <summary>
/// One RFC 4180 reader and writer for translation CSV files, shared by the runtime, the build steps and the CLI.
/// The reader is a character state machine over the whole text, so quoted values may span lines.
/// </summary>
internal static class CsvDocument
{
    /// <summary>
    /// Splits CSV text into rows of fields. Handles a UTF-8 byte order mark, CRLF and LF line endings, quoted fields,
    /// commas inside quotes, <c>""</c> escapes and line breaks inside quotes. Whitespace outside quotes is trimmed;
    /// whitespace inside quotes is kept. Blank lines are skipped.
    /// </summary>
    public static List<List<string>> Parse(string? text)
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrEmpty(text))
        {
            return rows;
        }

        var i = text![0] == '﻿' ? 1 : 0;
        var n = text.Length;
        while (i < n)
        {
            var fields = new List<string>();
            var lastQuoted = false;
            while (true)
            {
                var field = ReadField(text, i);
                fields.Add(field.Value);
                lastQuoted = field.Quoted;
                i = field.Next;
                if (i >= n)
                {
                    break;
                }

                var c = text[i];
                if (c == ',')
                {
                    i++;
                    continue;
                }

                if (c == '\r')
                {
                    i++;
                    if (i < n && text[i] == '\n')
                    {
                        i++;
                    }
                }
                else if (c == '\n')
                {
                    i++;
                }

                break;
            }

            var blank = fields.Count == 1 && fields[0].Length == 0 && !lastQuoted;
            if (!blank)
            {
                rows.Add(fields);
            }
        }

        return rows;
    }

    /// <summary>
    /// Reads a two-column key,value table. The first column is trimmed. A row named <c>key</c> (any case) is treated
    /// as a header and skipped. Rows with fewer than two fields or an empty key are skipped. For a key that appears
    /// more than once (compared ignoring case) the later row wins and <paramref name="diagnostic"/> is called.
    /// </summary>
    public static Dictionary<string, string> ToDictionary(string? text, Action<string>? diagnostic = null)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in Parse(text))
        {
            if (row.Count < 2)
            {
                diagnostic?.Invoke($"skipped row with {row.Count} field(s): {row[0]}");
                continue;
            }

            var key = row[0].Trim();
            if (key.Length == 0 || string.Equals(key, "key", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (result.ContainsKey(key))
            {
                diagnostic?.Invoke($"duplicate key '{key}': the later row wins");
            }

            result[key] = row[1];
        }

        return result;
    }

    /// <summary>Quotes a value: wraps it in double quotes and doubles every inner quote.</summary>
    public static string Quote(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

    /// <summary>
    /// One <c>key,"value"</c> line without a line break. The key is written bare unless it needs quoting (comma,
    /// quote, line break or surrounding whitespace).
    /// </summary>
    public static string FormatRow(string key, string value) => EscapeKey(key) + "," + Quote(value);

    /// <summary>
    /// A full file: the <c>Key,Value</c> header followed by one row per entry, each ending with <c>\n</c>.
    /// </summary>
    public static string Serialize(IEnumerable<KeyValuePair<string, string>> rows)
    {
        var sb = new StringBuilder();
        sb.Append("Key,Value\n");
        foreach (var row in rows)
        {
            sb.Append(FormatRow(row.Key, row.Value)).Append('\n');
        }

        return sb.ToString();
    }

    private static string EscapeKey(string key)
    {
        if (key.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 || (key.Length > 0 && (char.IsWhiteSpace(key[0]) || char.IsWhiteSpace(key[key.Length - 1]))))
        {
            return Quote(key);
        }

        return key;
    }

    private static (string Value, int Next, bool Quoted) ReadField(string text, int start)
    {
        var n = text.Length;
        var j = start;
        while (j < n && (text[j] == ' ' || text[j] == '\t'))
        {
            j++;
        }

        if (j < n && text[j] == '"')
        {
            var sb = new StringBuilder();
            j++;
            while (j < n)
            {
                var c = text[j];
                if (c == '"')
                {
                    if (j + 1 < n && text[j + 1] == '"')
                    {
                        sb.Append('"');
                        j += 2;
                        continue;
                    }

                    j++;
                    break;
                }

                sb.Append(c);
                j++;
            }

            while (j < n && (text[j] == ' ' || text[j] == '\t'))
            {
                j++;
            }

            // Malformed text after the closing quote is kept rather than dropped.
            var extraStart = j;
            while (j < n && text[j] != ',' && text[j] != '\r' && text[j] != '\n')
            {
                j++;
            }

            if (j > extraStart)
            {
                sb.Append(text, extraStart, j - extraStart);
            }

            return (sb.ToString(), j, true);
        }

        var end = start;
        while (end < n && text[end] != ',' && text[end] != '\r' && text[end] != '\n')
        {
            end++;
        }

        return (text.Substring(start, end - start).Trim(), end, false);
    }
}
