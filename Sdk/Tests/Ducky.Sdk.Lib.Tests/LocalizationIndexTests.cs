using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ducky.Sdk.Localizations;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Language index, file order, fallback chain and chain lookups.</summary>
public class LocalizationIndexTests
{
    private LocalizationFixture _fx = null!;

    [SetUp]
    public void SetUp() => _fx = new LocalizationFixture();

    [TearDown]
    public void TearDown() => _fx.Dispose();

    [Test]
    public void IndexBuiltOnRegistration_LoadingALanguageDoesNotEnumerateAgain()
    {
        var dir = _fx.Locales("idx", ("en.csv", "a,A\n"), ("fr/x.csv", "b,B\n"));
        var provider = new DirectoryLocalizationProvider(new ModId("idx"), dir);
        var service = _fx.Create(_fx.Locales("default"));
        service.Register(provider, null);

        provider.EnumerationCount.ShouldBe(1);
        service.SetLanguage(SystemLanguage.English);
        service.SetLanguage(SystemLanguage.French);
        provider.EnumerationCount.ShouldBe(1);
    }

    [Test]
    public void ReindexFindsANewFileWithoutRestarting()
    {
        var dir = _fx.Locales("re", ("en.csv", "a,A\n"));
        var provider = new DirectoryLocalizationProvider(new ModId("re"), dir);
        var service = _fx.Create(_fx.Locales("default"));
        service.Register(provider, null);
        provider.Languages.ShouldNotContain("de");

        File.WriteAllText(Path.Combine(dir, "de.csv"), "a,Ah\n");
        service.Reindex(new ModId("re"));

        provider.Languages.ShouldContain("de");
    }

    [Test]
    public void LanguageSetOfASource_IsEnFrAndZhHantFromCanonicalNames()
    {
        var dir = _fx.Locales("set",
            ("en.csv", "a,A\n"),
            ("zh-Hant.csv", "a,甲\n"),
            ("fr/extra.csv", "b,B\n"));
        var provider = new DirectoryLocalizationProvider(new ModId("set"), dir);

        provider.Languages.OrderBy(code => code, System.StringComparer.Ordinal).ToArray()
            .ShouldBe(new[] { "en", "fr", "zh-hant" });
    }

    [Test]
    public void FilesThatAreNotLanguages_AreIgnoredIncludingTheReportFile()
    {
        var dir = _fx.Locales("ign",
            ("en.csv", "a,A\n"),
            ("missing.csv", "Language,Key,English\n"),
            ("notes.csv", "a,b\n"));
        var provider = new DirectoryLocalizationProvider(new ModId("ign"), dir);

        provider.Languages.ShouldBe(new[] { "en" });
    }

    [Test]
    public void TwoFilesDefiningOneKey_LaterFileInTheDeterministicOrderWins()
    {
        var dir = _fx.Locales("order",
            ("en.csv", "title,First\n"),
            ("en/extra.csv", "title,Second\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.Get("title").ShouldBe("Second");
    }

    [Test]
    public void SameCodeFromTwoNames_ZhHantFileOrderIsDeterministic()
    {
        var dir = _fx.Locales("same",
            ("zh-Hant.csv", "k,upper name\n"),
            ("zh-hant.csv", "k,lower name\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.ChineseTraditional);

        service.Get("k").ShouldBe("lower name");
    }

    [TestCase("zh-hant", new[] { "zh-hant", "zh", "en" })]
    [TestCase("pt-br", new[] { "pt-br", "pt", "en" })]
    [TestCase("en", new[] { "en" })]
    public void DefaultChain_IsTheLanguageShorterFormsThenEnglish(string language, string[] expected) =>
        FallbackChain.For(language, new Dictionary<string, IReadOnlyList<string>>()).ShouldBe(expected);

    [Test]
    public void CustomChain_ReplacesTheDefaultAndEndsWithEnglish()
    {
        var chain = FallbackChain.Build("ko", new[] { "ja" }, _ => { });

        chain.ShouldBe(new[] { "ko", "ja", "en" });
    }

    [Test]
    public void RepeatedCode_IsDroppedWithAWarning()
    {
        var warnings = new List<string>();

        var chain = FallbackChain.Build("zh-hant", new[] { "zh", "zh" }, warnings.Add);

        chain.ShouldBe(new[] { "zh-hant", "zh", "en" });
        warnings.Count.ShouldBe(1);
    }

    [Test]
    public void ConfiguredChain_KoreanMissingTextFallsBackToJapanese()
    {
        var dir = _fx.Locales("cfg", ("ja.csv", "greet,こんにちは\n"), ("en.csv", "greet,Hello\n"));
        var service = _fx.Create(dir);
        service.ConfigureFallback("ko", new[] { "ja" });
        service.SetLanguage(SystemLanguage.Korean);

        service.Get("greet").ShouldBe("こんにちは");
    }

    [Test]
    public void ZhHantUser_ReceivesZhTextBeforeEnglish()
    {
        var dir = _fx.Locales("chain", ("en.csv", "hello,Hello\n"), ("zh.csv", "hello,你好\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.ChineseTraditional);

        service.Get("hello").ShouldBe("你好");
    }

    [Test]
    public void EmptyValue_FallsThroughToTheNextLanguage()
    {
        var dir = _fx.Locales("empty", ("en.csv", "hello,Hello\n"), ("zh.csv", "hello,\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.Chinese);

        service.Get("hello").ShouldBe("Hello");
    }

    [Test]
    public void PartialTranslation_MissingKeysArePublishedFromTheChain()
    {
        var dir = _fx.Locales("partial",
            ("en.csv", "k1,E1\nk2,E2\nk3,E3\nk4,E4\nk5,E5\n"),
            ("zh-hant.csv", "k1,T1\nk2,T2\nk3,T3\n"),
            ("zh.csv", "k4,S4\nk5,S5\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.ChineseTraditional);

        _fx.Set.Select(entry => entry.Key).OrderBy(key => key, System.StringComparer.Ordinal).ToArray()
            .ShouldBe(new[] { "k1", "k2", "k3", "k4", "k5" });
        _fx.Set.ShouldContain(("k4", "S4"));
        _fx.Set.ShouldContain(("k1", "T1"));
    }

    [Test]
    public void LazyLoad_OnlyTheLanguagesInTheChainAreLoaded()
    {
        var provider = new MemoryProvider("lazy", new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new() { ["a"] = "A" },
            ["zh"] = new() { ["a"] = "甲" },
            ["ja"] = new() { ["a"] = "ア" },
        });
        var service = _fx.Create(_fx.Locales("default"));
        service.Register(provider, null);

        // Registration applies the current language (English), so only its chain is loaded: en.
        provider.LoadCount.ShouldBe(1);

        service.SetLanguage(SystemLanguage.English);
        provider.LoadCount.ShouldBe(1);

        service.SetLanguage(SystemLanguage.Japanese);
        provider.LoadCount.ShouldBe(2);
    }
}
