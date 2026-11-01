// KeyPatternSyntax.cs
//
// Shared source file: linked into Ducky.Sdk.Lib (netstandard2.1), Ducky.Sdk.Analyser (netstandard2.0) and
// Ducky.Sdk.Build (net10.0). Keep to netstandard2.0 APIs only. It logs nothing itself.

#if DUCKY_SDK_BUILD_SHARED
namespace Ducky.Sdk.Build.Localization;
#else
namespace Ducky.Sdk.Localizations;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>Kinds of piece a key pattern is made of.</summary>
internal enum KeyPatternTokenKind
{
    /// <summary>Fixed text, such as <c>worldeffect.</c>.</summary>
    Literal,

    /// <summary>A runtime parameter, written <c>{identifier}</c>.</summary>
    Parameter,

    /// <summary>A choice list, written <c>{a|b|c}</c>. One choice is picked per generated accessor.</summary>
    Choice,
}

/// <summary>One piece of a parsed key pattern.</summary>
internal sealed class KeyPatternToken
{
    public KeyPatternToken(KeyPatternTokenKind kind, string text, IReadOnlyList<string> choices, bool lastSegment)
    {
        Kind = kind;
        Text = text;
        Choices = choices;
        LastSegment = lastSegment;
    }

    public KeyPatternTokenKind Kind { get; }

    /// <summary>The literal text, or the parameter name. Empty for a choice list.</summary>
    public string Text { get; }

    /// <summary>The choices of a choice list, in declaration order. Empty for other kinds.</summary>
    public IReadOnlyList<string> Choices { get; }

    /// <summary>True when no literal dot follows this token, so a parameter may contain dots.</summary>
    public bool LastSegment { get; }
}

/// <summary>A parsed key pattern. <see cref="Error"/> is null when the pattern is valid.</summary>
internal sealed class KeyPatternInfo
{
    public KeyPatternInfo(string pattern, IReadOnlyList<KeyPatternToken> tokens, string? error)
    {
        Pattern = pattern;
        Tokens = tokens;
        Error = error;
    }

    public string Pattern { get; }

    public IReadOnlyList<KeyPatternToken> Tokens { get; }

    /// <summary>Why the pattern is invalid, or null when it is valid.</summary>
    public string? Error { get; }

    public bool IsValid => Error == null;

    /// <summary>The parameter names, in the order they appear.</summary>
    public IReadOnlyList<string> Parameters => Tokens
        .Where(token => token.Kind == KeyPatternTokenKind.Parameter)
        .Select(token => token.Text)
        .ToList();

    /// <summary>The single choice list of the pattern, or null when there is none.</summary>
    public KeyPatternToken? Choice => Tokens.FirstOrDefault(token => token.Kind == KeyPatternTokenKind.Choice);

    /// <summary>
    /// The format template for one choice: each parameter becomes <c>{0}</c>, <c>{1}</c>, ... and the choice becomes its text.
    /// For a pattern without a choice list, pass null.
    /// </summary>
    public string FormatTemplate(string? choice)
    {
        var sb = new StringBuilder();
        var index = 0;
        foreach (var token in Tokens)
        {
            switch (token.Kind)
            {
                case KeyPatternTokenKind.Literal:
                    sb.Append(token.Text);
                    break;
                case KeyPatternTokenKind.Parameter:
                    sb.Append('{').Append(index++).Append('}');
                    break;
                case KeyPatternTokenKind.Choice:
                    sb.Append(choice ?? string.Empty);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// The anchored, case-insensitive regex that matches keys of this pattern. A parameter matches one segment (no dots)
    /// unless it is in the last segment, where it may contain dots. A choice matches one of its choices.
    /// Returns null for an invalid pattern.
    /// </summary>
    public Regex? ToRegex()
    {
        if (!IsValid)
        {
            return null;
        }

        var sb = new StringBuilder("^");
        foreach (var token in Tokens)
        {
            switch (token.Kind)
            {
                case KeyPatternTokenKind.Literal:
                    sb.Append(Regex.Escape(token.Text));
                    break;
                case KeyPatternTokenKind.Parameter:
                    sb.Append(token.LastSegment ? ".+" : "[^.]+");
                    break;
                case KeyPatternTokenKind.Choice:
                    sb.Append("(?:").Append(string.Join("|", token.Choices.Select(Regex.Escape))).Append(')');
                    break;
            }
        }

        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

/// <summary>Parses <c>[KeyPattern]</c> strings such as <c>worldeffect.{code}.{name|summary|description}</c>.</summary>
internal static class KeyPatternSyntax
{
    /// <summary>Parses a pattern. An invalid pattern keeps its tokens so the caller can report the error.</summary>
    public static KeyPatternInfo Parse(string? pattern)
    {
        var text = pattern ?? string.Empty;
        if (text.Trim().Length == 0)
        {
            return Invalid(text, "the pattern is empty");
        }

        var tokens = new List<KeyPatternToken>();
        var literal = new StringBuilder();
        var choiceLists = 0;
        var seenParameters = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '}')
            {
                return Invalid(text, "'}' has no matching '{'");
            }

            if (c != '{')
            {
                literal.Append(c);
                continue;
            }

            var close = text.IndexOf('}', i + 1);
            if (close < 0)
            {
                return Invalid(text, "'{' is not closed");
            }

            var inner = text.Substring(i + 1, close - i - 1);
            if (inner.IndexOf('{') >= 0)
            {
                return Invalid(text, "'{' cannot be nested");
            }

            if (literal.Length > 0)
            {
                tokens.Add(new KeyPatternToken(KeyPatternTokenKind.Literal, literal.ToString(), Array.Empty<string>(), false));
                literal.Clear();
            }

            if (inner.IndexOf('|') >= 0)
            {
                var choices = inner.Split('|').Select(choice => choice.Trim()).ToList();
                if (choices.Any(choice => choice.Length == 0))
                {
                    return Invalid(text, $"the choice list '{{{inner}}}' has an empty choice");
                }

                if (choices.Any(choice => !IsChoiceName(choice)))
                {
                    return Invalid(text, $"the choice list '{{{inner}}}' has a choice that is not a plain name");
                }

                choiceLists++;
                if (choiceLists > 1)
                {
                    return Invalid(text, "only one choice list is allowed per pattern");
                }

                tokens.Add(new KeyPatternToken(KeyPatternTokenKind.Choice, string.Empty, choices, false));
            }
            else
            {
                var name = inner.Trim();
                if (name.Length == 0)
                {
                    return Invalid(text, "'{}' is empty");
                }

                if (!IsIdentifier(name))
                {
                    return Invalid(text, $"'{{{name}}}' is not a valid parameter name");
                }

                if (!seenParameters.Add(name))
                {
                    return Invalid(text, $"parameter '{{{name}}}' appears more than once");
                }

                tokens.Add(new KeyPatternToken(KeyPatternTokenKind.Parameter, name, Array.Empty<string>(), false));
            }

            i = close;
        }

        if (literal.Length > 0)
        {
            tokens.Add(new KeyPatternToken(KeyPatternTokenKind.Literal, literal.ToString(), Array.Empty<string>(), false));
        }

        return new KeyPatternInfo(text, MarkLastSegments(tokens), null);
    }

    /// <summary>True when the key matches the pattern. An invalid pattern matches nothing.</summary>
    public static bool Matches(string? pattern, string key)
    {
        var regex = Parse(pattern).ToRegex();
        return regex != null && regex.IsMatch(key ?? string.Empty);
    }

    private static IReadOnlyList<KeyPatternToken> MarkLastSegments(List<KeyPatternToken> tokens)
    {
        var marked = new KeyPatternToken[tokens.Count];
        var dotAfter = false;
        for (var i = tokens.Count - 1; i >= 0; i--)
        {
            var token = tokens[i];
            if (token.Kind == KeyPatternTokenKind.Literal)
            {
                if (token.Text.IndexOf('.') >= 0)
                {
                    dotAfter = true;
                }

                marked[i] = token;
            }
            else
            {
                marked[i] = new KeyPatternToken(token.Kind, token.Text, token.Choices, !dotAfter);
            }
        }

        return marked;
    }

    private static KeyPatternInfo Invalid(string pattern, string error) =>
        new(pattern, Array.Empty<KeyPatternToken>(), error);

    private static bool IsIdentifier(string name)
    {
        if (name.Length == 0 || !(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static bool IsChoiceName(string name)
    {
        return name.Length > 0 && name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }
}
