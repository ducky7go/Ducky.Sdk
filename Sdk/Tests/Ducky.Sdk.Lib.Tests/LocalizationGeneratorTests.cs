using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ducky.Sdk.Analyser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Runs <see cref="DuckyLocalizationGenerator"/> in-process on a small LK declaration: typed accessors for
/// <c>[KeyPattern]</c>, the DUCKYL001 diagnostic, and the <c>patterns</c> metadata entry.
/// </summary>
public class LocalizationGeneratorTests
{
    private const string Support = """
        using System;
        namespace Ducky.Sdk.Attributes {
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
            public class LanguageSupportAttribute : Attribute { public LanguageSupportAttribute(params string[] languages) { } }
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
            public sealed class KeyPatternAttribute : Attribute {
                public KeyPatternAttribute(string pattern) { }
                public string? Group { get; set; }
            }
            [AttributeUsage(AttributeTargets.Field)]
            public class TranslateFileAttribute : Attribute { public TranslateFileAttribute(string ext = "txt") { } }
        }
        namespace Ducky.Sdk.Localizations {
            public partial class L {
                public static string Get(string key) => key;
                public static string Dynamic(string template, params object?[] args) => template;
            }
        }
        """;

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(() =>
    {
        var trusted = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty;
        return trusted.Split(Path.PathSeparator)
            .Where(path => path.Length > 0)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    });

    /// <summary>Runs the generator over an LK class (with the attributes above) and returns its result.</summary>
    private static GeneratorDriverRunResult Run(string lkClass)
    {
        var source = Support + "\nnamespace Game {\n" + lkClass + "\n}\n";
        var compilation = CSharpCompilation.Create("game",
            new[] { CSharpSyntaxTree.ParseText(source) },
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create(new DuckyLocalizationGenerator());
        return driver.RunGenerators(compilation).GetRunResult();
    }

    private static string GeneratedText(GeneratorDriverRunResult result, string fileFragment) =>
        string.Join("\n", result.GeneratedTrees
            .Where(tree => tree.FilePath.Contains(fileFragment, StringComparison.Ordinal))
            .Select(tree => tree.GetText().ToString()));

    private const string WorldEffectLk = """
        [Ducky.Sdk.Attributes.LanguageSupport("en")]
        [Ducky.Sdk.Attributes.KeyPattern("worldeffect.{code}.{name|summary|description}", Group = "WorldEffect")]
        public static class LK
        {
            public static class Common
            {
                public const string Ok = "common.ok";
            }
        }
        """;

    [Test]
    public void KeyPattern_EmitsOneTypedAccessorPerChoiceOverDynamic()
    {
        var result = Run(WorldEffectLk);

        var code = GeneratedText(result, "L.GeneratedKeys");
        code.ShouldContain("public static class WorldEffect");
        code.ShouldContain("public static string Name(string code) => Dynamic(\"worldeffect.{0}.name\", code);");
        code.ShouldContain("public static string Summary(string code) => Dynamic(\"worldeffect.{0}.summary\", code);");
        code.ShouldContain("public static string Description(string code) => Dynamic(\"worldeffect.{0}.description\", code);");
    }

    [Test]
    public void KeyPattern_WithoutGroup_UsesTheFirstLiteralSegmentUpperCased()
    {
        var result = Run("""
            [Ducky.Sdk.Attributes.LanguageSupport("en")]
            [Ducky.Sdk.Attributes.KeyPattern("effecttoggle.{code}.{label|hint}")]
            public static class LK
            {
                public static class Common { public const string Ok = "common.ok"; }
            }
            """);

        GeneratedText(result, "L.GeneratedKeys").ShouldContain("public static class Effecttoggle");
    }

    [Test]
    public void InvalidKeyPattern_ReportsDucky001AndEmitsNoAccessor()
    {
        var result = Run("""
            [Ducky.Sdk.Attributes.LanguageSupport("en")]
            [Ducky.Sdk.Attributes.KeyPattern("worldeffect.{code.{name|summary}", Group = "WorldEffect")]
            public static class LK
            {
                public static class Common { public const string Ok = "common.ok"; }
            }
            """);

        var diagnostic = result.Diagnostics.Single(d => d.Id == "DUCKYL001");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.GetMessage().ShouldContain("worldeffect.{code.{name|summary}");
        GeneratedText(result, "L.GeneratedKeys").ShouldNotContain("WorldEffect");
    }

    [Test]
    public void ValidPatterns_AreListedInTheMetadataJson()
    {
        var result = Run(WorldEffectLk);

        // The JSON sits in a verbatim string literal, so its quotes are doubled in the generated source.
        var metadata = GeneratedText(result, "metadata").Replace("\"\"", "\"");
        metadata.ShouldContain("\"patterns\": [");
        metadata.ShouldContain("\"worldeffect.{code}.{name|summary|description}\"");
    }

    [Test]
    public void NoPattern_EmitsNoDynamicAccessorsAndNoPatternsKey()
    {
        var result = Run("""
            [Ducky.Sdk.Attributes.LanguageSupport("en")]
            public static class LK
            {
                public static class Common { public const string Ok = "common.ok"; }
            }
            """);

        GeneratedText(result, "L.GeneratedKeys").ShouldNotContain("Dynamic(");
        GeneratedText(result, "metadata").Replace("\"\"", "\"").ShouldNotContain("\"patterns\"");
        result.Diagnostics.ShouldNotContain(d => d.Id == "DUCKYL001");
    }
}
