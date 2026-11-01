using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System;
using Ducky.Sdk.Localizations;

namespace Ducky.Sdk.Analyser;

/// <summary>
/// Incremental source generator that mirrors LK key constants into a strongly-typed
/// facade on L with properties that return localized strings via L.Get(key).
///
/// Convention:
/// - Reads nested static classes and const string fields.
/// - Emits partial class L with matching nested static classes and properties.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DuckyLocalizationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover LK class declarations by syntax, then enrich with semantic model
        var lkProvider = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax cds && cds.Identifier.Text == "LK",
                static (ctx, ct) => TransformLK(ctx, ct))
            .Where(static info => info is not null)!
            .Select((info, _) => info!);

        // Collect all (usually one) LK descriptors and generate once
        var collected = lkProvider.Collect();
        context.RegisterSourceOutput(collected, static (spc, list) =>
        {
            if (!list.Any())
            {
                return;
            }

            // Now generate for every discovered LK in the compilation.
            foreach (var lk in list)
            {
                if (lk is null) continue;
                Emit(spc, lk);
                EmitKeyListAndHash(spc, lk);
            }
        });
    }

    private static LKDescriptor? TransformLK(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var cds = (ClassDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(cds, ct) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var ns = symbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty) ?? string.Empty;

        // Only consider static classes named LK
        if (symbol.Name != "LK" || symbol.TypeKind != TypeKind.Class || !symbol.IsStatic)
        {
            return null;
        }

        // Check for LanguageSupport attribute on LK class
        List<string>? supportedLanguages = null;
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "LanguageSupportAttribute")
            {
                // Extract language codes from attribute
                if (attr.ConstructorArguments.Length > 0)
                {
                    var arg = attr.ConstructorArguments[0];
                    if (arg.Kind == TypedConstantKind.Array && arg.Values.Length > 0)
                    {
                        supportedLanguages = new List<string>();
                        var hasAllKeyword = false;

                        foreach (var langValue in arg.Values)
                        {
                            if (langValue.Value is string langCode && !string.IsNullOrWhiteSpace(langCode))
                            {
                                var normalizedLang = LanguageCodes.Normalize(langCode);
                                if (normalizedLang == "all")
                                {
                                    hasAllKeyword = true;
                                }
                                else
                                {
                                    supportedLanguages.Add(normalizedLang);
                                }
                            }
                        }

                        // If "all" keyword was found, expand to all supported languages
                        if (hasAllKeyword)
                        {
                            // Start with all supported languages (shared with LanguageSupportAttribute)
                            var result = LanguageCodes.AllLanguages.ToList();

                            // Add any additional explicit languages (removing duplicates)
                            foreach (var lang in supportedLanguages)
                            {
                                if (!result.Contains(lang))
                                {
                                    result.Add(lang);
                                }
                            }

                            supportedLanguages = result;
                        }
                    }
                }

                break;
            }
        }

        // If no LanguageSupport attribute found, default to zh and en
        if (supportedLanguages == null || supportedLanguages.Count == 0)
        {
            supportedLanguages = new List<string> { "zh", "en" };
        }

        var groups = new List<GroupDescriptor>();
        foreach (var nested in symbol.GetTypeMembers())
        {
            // Only nested static classes
            if (!nested.IsStatic || nested.TypeKind != TypeKind.Class)
            {
                continue;
            }

            var entries = new List<EntryDescriptor>();
            foreach (var member in nested.GetMembers())
            {
                if (member is IFieldSymbol field && field.IsConst &&
                    field.Type.SpecialType == SpecialType.System_String)
                {
                    var name = field.Name;
                    var value = field.ConstantValue as string ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    // Check for TranslateFile attribute
                    string? fileExtension = null;
                    foreach (var attr in field.GetAttributes())
                    {
                        if (attr.AttributeClass?.Name == "TranslateFileAttribute")
                        {
                            // Extract file extension from attribute
                            if (attr.ConstructorArguments.Length > 0)
                            {
                                var ext = attr.ConstructorArguments[0].Value as string;
                                fileExtension = string.IsNullOrWhiteSpace(ext) ? "txt" : ext!.TrimStart('.');
                            }
                            else
                            {
                                fileExtension = "txt"; // default
                            }

                            break;
                        }
                    }

                    entries.Add(new EntryDescriptor(name, value, fileExtension));
                }
            }

            if (entries.Count > 0)
            {
                groups.Add(new GroupDescriptor(nested.Name, entries));
            }
        }

        if (groups.Count == 0)
        {
            return null;
        }

        return new LKDescriptor(ns, groups, supportedLanguages, ReadKeyPatterns(symbol));
    }

    /// <summary>
    /// 生成 L 的部分类代码
    /// </summary>
    private static void Emit(SourceProductionContext spc, LKDescriptor lk)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/> DO NOT EDIT");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("namespace Ducky.Sdk.Localizations;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated strongly-typed accessors for BetterModLoader localizations.");
        sb.AppendLine("/// Mirrors LK constants as properties that call L.Get(…)");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public partial class L");
        sb.AppendLine("{");

        // Typed accessors for [KeyPattern] declarations, keyed by accessor class name.
        var patternMembers = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var descriptor in lk.Patterns)
        {
            var info = KeyPatternSyntax.Parse(descriptor.Pattern);
            if (!info.IsValid)
            {
                spc.ReportDiagnostic(Diagnostic.Create(InvalidKeyPatternDescriptor, Location.None, descriptor.Pattern, info.Error));
                continue;
            }

            var groupName = AccessorGroupName(info, descriptor);
            if (groupName == null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(InvalidKeyPatternDescriptor, Location.None, descriptor.Pattern,
                    "the pattern must start with a literal name, or set Group"));
                continue;
            }

            if (info.Choice == null)
            {
                // No choice list: nothing to name an accessor after. The pattern still works with L.Dynamic.
                continue;
            }

            if (!patternMembers.TryGetValue(groupName, out var members))
            {
                members = new List<string>();
                patternMembers[groupName] = members;
            }

            var parameters = info.Parameters.Select(name => "string " + EscapeIdentifier(name)).ToList();
            var arguments = info.Parameters.Select(EscapeIdentifier).ToList();
            foreach (var option in info.Choice.Choices)
            {
                var template = info.FormatTemplate(option).Replace("\\", "\\\\").Replace("\"", "\\\"");
                var call = arguments.Count == 0
                    ? $"Dynamic(\"{template}\")"
                    : $"Dynamic(\"{template}\", {string.Join(", ", arguments)})";
                members.Add($"        /// <summary>Typed accessor for <c>{EscapeXml(descriptor.Pattern)}</c>, choice <c>{EscapeXml(option)}</c>.</summary>");
                members.Add($"        public static string {PascalCase(option)}({string.Join(", ", parameters)}) => {call};");
            }
        }

        var emittedPatternGroups = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in lk.Groups.OrderBy(g => g.Name))
        {
            sb.AppendLine($"    /// <summary>Localized strings for {EscapeXml(group.Name)}</summary>");
            sb.AppendLine($"    public static class {EscapeIdentifier(group.Name)}");
            sb.AppendLine("    {");

            foreach (var entry in group.Entries.OrderBy(e => e.Name))
            {
                // Property that returns L.Get(LK.Group.Name)
                sb.AppendLine($"        /// <summary>Localized text for key: {EscapeXml(entry.Value)}</summary>");
                sb.AppendLine(
                    $"        public static string {EscapeIdentifier(entry.Name)} => Get(global::{lk.Namespace}.LK.{group.Name}.{entry.Name});");
            }

            if (patternMembers.TryGetValue(group.Name, out var extra))
            {
                foreach (var line in extra)
                {
                    sb.AppendLine(line);
                }

                emittedPatternGroups.Add(group.Name);
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        foreach (var pair in patternMembers.Where(pair => !emittedPatternGroups.Contains(pair.Key)).OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"    /// <summary>Typed accessors for the key patterns of {EscapeXml(pair.Key)}</summary>");
            sb.AppendLine($"    public static class {EscapeIdentifier(pair.Key)}");
            sb.AppendLine("    {");
            foreach (var line in pair.Value)
            {
                sb.AppendLine(line);
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        // Use a file-name that includes the LK namespace so multiple LK from different namespaces
        // do not collide when generating in the same compilation.
        var fileName = $"L.GeneratedKeys.{MakeSafeFileName(lk.Namespace)}.g.cs";
        spc.AddSource(fileName, sb.ToString());
    }

    /// <summary>
    /// 生成所有 key 的列表和 hash 文件，供 MSBuild Task 使用
    /// 方案：生成一个包含 JSON 字面量的 .cs 文件，MSBuild Target 可以读取生成的源文件并提取 JSON
    /// </summary>
    private static void EmitKeyListAndHash(Microsoft.CodeAnalysis.SourceProductionContext spc, LKDescriptor lk)
    {
        // 收集所有 key 和它们的元数据
        var allEntries = lk.Groups
            .SelectMany(g => g.Entries)
            .GroupBy(e => e.Value)
            .Select(g => g.First()) // 去重，保留第一个
            .OrderBy(e => e.Value)
            .ToList();

        var allKeys = allEntries.Select(e => e.Value).ToList();

        // 生成 hash
        var hash = ComputeSha256(string.Join("|", allKeys));

        // 构建 JSON 字符串
        var json = new StringBuilder();
        json.AppendLine("{");
        json.AppendLine($"  \"namespace\": \"{EscapeJson(lk.Namespace)}\",");
        json.AppendLine($"  \"hash\": \"{EscapeJson(hash)}\",");
        json.AppendLine($"  \"keyCount\": {allKeys.Count},");

        // Add supported languages if specified
        if (lk.SupportedLanguages != null && lk.SupportedLanguages.Count > 0)
        {
            json.AppendLine("  \"supportedLanguages\": [");
            for (int i = 0; i < lk.SupportedLanguages.Count; i++)
            {
                var comma = i < lk.SupportedLanguages.Count - 1 ? "," : "";
                json.AppendLine($"    \"{EscapeJson(lk.SupportedLanguages[i])}\"{comma}");
            }

            json.AppendLine("  ],");
        }

        var validPatterns = lk.Patterns
            .Where(descriptor => KeyPatternSyntax.Parse(descriptor.Pattern).IsValid)
            .Select(descriptor => descriptor.Pattern)
            .ToList();
        if (validPatterns.Count > 0)
        {
            json.AppendLine("  \"patterns\": [");
            for (int i = 0; i < validPatterns.Count; i++)
            {
                var comma = i < validPatterns.Count - 1 ? "," : "";
                json.AppendLine($"    \"{EscapeJson(validPatterns[i])}\"{comma}");
            }

            json.AppendLine("  ],");
        }

        json.AppendLine("  \"keys\": [");
        for (int i = 0; i < allEntries.Count; i++)
        {
            var entry = allEntries[i];
            var comma = i < allEntries.Count - 1 ? "," : "";

            // All keys should be simple strings in the standard format
            json.AppendLine($"    \"{EscapeJson(entry.Value)}\"{comma}");
        }

        json.AppendLine("  ]");
        json.Append("}");

        var jsonContent = json.ToString();

        // 生成一个 .cs 文件，包含 JSON 作为字符串常量
        // MSBuild Target 可以通过读取这个生成的源文件来获取 JSON
        var code = new StringBuilder();
        code.AppendLine("// <auto-generated/> DO NOT EDIT");
        code.AppendLine("// This file contains localization keys metadata as a JSON string constant.");
        code.AppendLine("// MSBuild targets can read this source file to extract the JSON.");
        code.AppendLine("#nullable enable");
        code.AppendLine($"namespace {lk.Namespace};");
        code.AppendLine();
        code.AppendLine("/// <summary>");
        code.AppendLine("/// Contains localization keys metadata in JSON format.");
        code.AppendLine("/// This class exists solely to make the JSON data available to build-time tools.");
        code.AppendLine("/// </summary>");
        code.AppendLine("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]");
        code.AppendLine("internal static class LKeysMetadata");
        code.AppendLine("{");
        code.AppendLine("    /// <summary>JSON metadata - DO NOT MODIFY</summary>");
        code.AppendLine("    internal const string JsonData = @\"");

        // 将 JSON 转义后嵌入到 C# 字符串字面量中（使用逐字字符串 @"..."）
        // 在逐字字符串中，只需要将 " 转义为 ""
        var escapedJson = jsonContent.Replace("\"", "\"\"");
        code.Append(escapedJson);

        code.AppendLine("\";");
        code.AppendLine();
        code.AppendLine($"    internal const string Hash = \"{EscapeString(hash)}\";");
        code.AppendLine($"    internal const int KeyCount = {allKeys.Count};");
        code.AppendLine("}");

        var metadataFile = $"LKeys.{MakeSafeFileName(lk.Namespace)}.metadata.g.cs";
        spc.AddSource(metadataFile, code.ToString());
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                default:
                    if (c < 32)
                        sb.Append($"\\u{(int)c:x4}");
                    else
                        sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static string ComputeSha256(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static string EscapeIdentifier(string name)
    {
        // Escape keywords or invalid identifiers minimally
        return Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(name) !=
               Microsoft.CodeAnalysis.CSharp.SyntaxKind.None
            ? "@" + name
            : name;
    }

    private static string EscapeXml(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static string MakeSafeFileName(string ns)
    {
        if (string.IsNullOrWhiteSpace(ns))
        {
            return "global";
        }

        // Replace dots and invalid filename chars with '_' to create a predictable file name.
        var sb = new StringBuilder();
        foreach (var c in ns)
        {
            if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
            else sb.Append('_');
        }

        return sb.ToString();
    }

    private sealed record LKDescriptor(
        string Namespace,
        IReadOnlyList<GroupDescriptor> Groups,
        IReadOnlyList<string>? SupportedLanguages,
        IReadOnlyList<KeyPatternDescriptor> Patterns)
    {
        public string Namespace { get; } = Namespace;
        public IReadOnlyList<GroupDescriptor> Groups { get; } = Groups;
        public IReadOnlyList<string>? SupportedLanguages { get; } = SupportedLanguages;
        public IReadOnlyList<KeyPatternDescriptor> Patterns { get; } = Patterns;
    }

    private sealed class KeyPatternDescriptor
    {
        public KeyPatternDescriptor(string pattern, string? group)
        {
            Pattern = pattern;
            Group = group;
        }

        public string Pattern { get; }

        public string? Group { get; }
    }

    /// <summary>Invalid [KeyPattern] declaration.</summary>
    private static readonly DiagnosticDescriptor InvalidKeyPatternDescriptor = new(
        id: "DUCKYL001",
        title: "Invalid key pattern",
        messageFormat: "Invalid [KeyPattern] \"{0}\": {1}",
        category: "Ducky.Localization",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Reads the [KeyPattern] attributes declared on an LK class.</summary>
    private static List<KeyPatternDescriptor> ReadKeyPatterns(INamedTypeSymbol symbol)
    {
        var patterns = new List<KeyPatternDescriptor>();
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "KeyPatternAttribute" || attr.ConstructorArguments.Length == 0)
            {
                continue;
            }

            var pattern = attr.ConstructorArguments[0].Value as string ?? string.Empty;
            string? group = null;
            foreach (var named in attr.NamedArguments)
            {
                if (named.Key == "Group")
                {
                    group = named.Value.Value as string;
                }
            }

            patterns.Add(new KeyPatternDescriptor(pattern, group));
        }

        return patterns;
    }

    /// <summary>The accessor class name: the Group if given, else the first literal segment, upper-cased.</summary>
    private static string? AccessorGroupName(KeyPatternInfo info, KeyPatternDescriptor descriptor)
    {
        if (!string.IsNullOrWhiteSpace(descriptor.Group))
        {
            return PascalCase(descriptor.Group!);
        }

        var first = info.Tokens.FirstOrDefault();
        if (first == null || first.Kind != KeyPatternTokenKind.Literal)
        {
            return null;
        }

        var dot = first.Text.IndexOf('.');
        var segment = dot >= 0 ? first.Text.Substring(0, dot) : first.Text;
        return segment.Length == 0 ? null : PascalCase(segment);
    }

    private static string PascalCase(string text)
    {
        var sb = new StringBuilder();
        var upper = true;
        foreach (var c in text)
        {
            if (!char.IsLetterOrDigit(c))
            {
                upper = true;
                continue;
            }

            sb.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }

        return sb.ToString();
    }

    private sealed record GroupDescriptor(string Name, IReadOnlyList<EntryDescriptor> Entries)
    {
        public string Name { get; } = Name;
        public IReadOnlyList<EntryDescriptor> Entries { get; } = Entries;
    }

    private sealed record EntryDescriptor(string Name, string Value, string? FileExtension)
    {
        public string Name { get; } = Name;
        public string Value { get; } = Value;

        /// <summary>
        /// If not null, indicates this key should use a file reference with the specified extension.
        /// </summary>
        public string? FileExtension { get; } = FileExtension;
    }
}
