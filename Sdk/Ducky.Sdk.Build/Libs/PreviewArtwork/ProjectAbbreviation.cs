using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Derives the up-to-three-letter abbreviation drawn on a preview from the project name.
/// Words are split at ASCII punctuation and spaces (for example '.', '-', '_'), and at case changes:
/// "EntranceMod" gives "Entrance" + "Mod", and "HTTPServer" gives "HTTP" + "Server".
/// Each word that starts with an ASCII letter or digit contributes its upper-case first character.
/// </summary>
internal static class ProjectAbbreviation
{
    public const int MaxLength = 3;

    public static string From(string name)
    {
        var initials = new StringBuilder();
        foreach (var word in SplitWords(name ?? ""))
        {
            if (initials.Length == MaxLength) break;

            var first = word[0];
            if (!char.IsAsciiLetterOrDigit(first)) continue;

            initials.Append(char.ToUpperInvariant(first));
        }

        if (initials.Length > 0) return initials.ToString();

        // No ASCII letter or digit at all (for example a Chinese-only name): take three letters from the hash.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(name ?? ""));
        var fallback = new char[MaxLength];
        for (var i = 0; i < MaxLength; i++)
        {
            fallback[i] = (char)('A' + hash[i] % 26);
        }

        return new string(fallback);
    }

    private static List<string> SplitWords(string name)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c < 128 && !char.IsAsciiLetterOrDigit(c))
            {
                Flush();
                continue;
            }

            if (current.Length > 0 && StartsNewWord(name, i)) Flush();

            current.Append(c);
        }

        Flush();
        return words;
    }

    /// <summary>
    /// A new word starts at an upper-case letter that follows a lower-case letter or digit ("Ducky|Mod", "2|Fast"),
    /// or at the last capital of a run that is followed by a lower-case letter ("HTTP|Server").
    /// </summary>
    private static bool StartsNewWord(string name, int i)
    {
        if (!char.IsAsciiLetterUpper(name[i])) return false;

        var previous = name[i - 1];
        if (char.IsAsciiLetterLower(previous) || char.IsAsciiDigit(previous)) return true;

        return char.IsAsciiLetterUpper(previous) && i + 1 < name.Length && char.IsAsciiLetterLower(name[i + 1]);
    }
}
