using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Ducky.Sdk.Attributes;
using Ducky.Sdk.Localizations;

namespace Ducky.Sdk.Lib.Tests;

public class LanguageCodesTests
{
    [TestCase("zh-Hant", "zh-hant")]
    [TestCase("zh_TW", "zh-hant")]
    [TestCase("ZH-CN", "zh")]
    [TestCase("  DE  ", "de")]
    [TestCase("zh_hant", "zh-hant")]
    [TestCase("zh-hans", "zh")]
    [TestCase("zh-sg", "zh")]
    [TestCase("zh-hk", "zh-hant")]
    [TestCase("zh-mo", "zh-hant")]
    public void Normalize_GivesTheCanonicalCode(string input, string expected) =>
        LanguageCodes.Normalize(input).ShouldBe(expected);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Normalize_BlankInputGivesEmpty(string? input) =>
        LanguageCodes.Normalize(input).ShouldBe(string.Empty);

    [TestCase("zh-hant", true)]
    [TestCase("pt-br", true)]
    [TestCase("en", true)]
    [TestCase("missing", false)]
    [TestCase("notes", false)]
    [TestCase("", false)]
    public void IsLanguageCode_AcceptsOnlyLanguageShapedNames(string input, bool expected) =>
        LanguageCodes.IsLanguageCode(input).ShouldBe(expected);

    [Test]
    public void AllLanguages_IsTheOneOrderedList()
    {
        LanguageCodes.AllLanguages.ShouldBe(new[] { "de", "en", "es", "fr", "ja", "ko", "pt", "ru", "zh-hant", "zh" });
        LanguageSupportAttribute.AllLanguages.ShouldBe(LanguageCodes.AllLanguages);
    }

    [Test]
    public void LanguageSupportAll_ExpandsToTheSharedListAndNormalizesExtras()
    {
        var attribute = new LanguageSupportAttribute("all", "zh_Hant");

        attribute.Languages.ShouldBe(LanguageCodes.AllLanguages);
    }

    [Test]
    public void DefaultChain_ZhHantFallsBackToZhThenEn() =>
        LanguageCodes.DefaultChain("zh-Hant").ShouldBe(new[] { "zh-hant", "zh", "en" });

    [Test]
    public void DefaultChain_RegionalPortugueseFallsBackToPtThenEn() =>
        LanguageCodes.DefaultChain("pt-br").ShouldBe(new[] { "pt-br", "pt", "en" });

    [Test]
    public void DefaultChain_EnglishIsJustEnglish() =>
        LanguageCodes.DefaultChain("en").ShouldBe(new[] { "en" });

    [Test]
    public void DefaultChain_NeverRepeatsACode()
    {
        var chain = LanguageCodes.DefaultChain("en-gb").ToList();

        chain.Distinct().Count().ShouldBe(chain.Count);
        chain.Last().ShouldBe("en");
    }

    [Test]
    public void Generator_ReadsTheSharedLanguageListInsteadOfKeepingACopy()
    {
        var source = ReadGeneratorSource();

        source.ShouldContain("LanguageCodes.AllLanguages");
        source.ShouldNotContain("\"de\", \"en\", \"es\"");
    }

    private static string ReadGeneratorSource([CallerFilePath] string thisFile = "")
    {
        var generatorPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!,
            "..", "..", "Ducky.Sdk.Analyser", "DuckyLocalizationGenerator.cs"));
        return File.ReadAllText(generatorPath);
    }

    [Test]
    public void Package_ShipsTheSharedLocalizationSourcesWithLib()
    {
        // Lib is shipped as source inside the package; the shared files it links must ship too, or mods do not compile.
        var nuspec = File.ReadAllText(Path.GetFullPath(Path.Combine(ThisDirectory(), "..", "..", "SDKlibs", "Ducky.Sdk", "Ducky.Sdk.nuspec")));

        nuspec.ShouldContain("../../Shared/Localization/*.cs");
        nuspec.ShouldContain("contentFiles/cs/netstandard2.1/Sdk/Shared/Localization/");
    }

    private static string ThisDirectory([CallerFilePath] string file = "") => Path.GetDirectoryName(file)!;
}
