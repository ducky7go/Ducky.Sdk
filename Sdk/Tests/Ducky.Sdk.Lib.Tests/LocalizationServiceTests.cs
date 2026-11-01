using System;
using System.Collections.Generic;
using System.IO;
using Ducky.Sdk.Localizations;
using UnityEngine;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Lookup, fallback, language selection and override release of <see cref="LocalizationService"/>, with the locales
/// folder and the game's override calls injected through its internal seam.
/// </summary>
public class LocalizationServiceTests
{
    private string _dir = string.Empty;
    private List<(string Key, string Value)> _set = new();
    private List<string> _removed = new();

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(AppContext.BaseDirectory, "locales-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "en.csv"), "key,value\nhello,Hello\nonlyEnglish,English text\n");
        File.WriteAllText(Path.Combine(_dir, "zh.csv"), "key,value\nhello,你好\n");
        _set = new List<(string Key, string Value)>();
        _removed = new List<string>();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private LocalizationService Create() =>
        new(_dir, (key, value) => _set.Add((key, value)), key => _removed.Add(key));

    [Test]
    public void Lookup_ReturnsTheTextOfTheSelectedLanguage()
    {
        var service = Create();
        service.SetLanguage(SystemLanguage.English);

        service.Get("hello").ShouldBe("Hello");
    }

    [Test]
    public void Fallback_MissingKeyUsesTheFallbackOrABracketedKey()
    {
        var service = Create();
        service.SetLanguage(SystemLanguage.English);

        service.Get("missing", "fallback text").ShouldBe("fallback text");
        service.Get("missing").ShouldBe("[missing]");
    }

    [Test]
    public void LanguageSelection_UsesTheLanguageFileAndFallsBackToEnglishForMissingKeys()
    {
        var service = Create();
        service.SetLanguage(SystemLanguage.Chinese);

        service.Get("hello").ShouldBe("你好");
        service.Get("onlyEnglish").ShouldBe("English text");
    }

    [Test]
    public void Get_ForLanguageWithoutAFile_FallsBackToEnglish()
    {
        var service = Create();
        service.SetLanguage(SystemLanguage.English);

        service.Get(SystemLanguage.French, "hello").ShouldBe("Hello");
    }

    [TestCase(SystemLanguage.Chinese, "zh")]
    [TestCase(SystemLanguage.ChineseSimplified, "zh")]
    [TestCase(SystemLanguage.ChineseTraditional, "zh-hant")]
    [TestCase(SystemLanguage.Japanese, "ja")]
    [TestCase(SystemLanguage.Unknown, "en")]
    public void LanguageCode_MapsEachLanguageToItsFileCode(SystemLanguage language, string expected) =>
        LocalizationService.GetLanguageCode(language).ShouldBe(expected);

    [Test]
    public void ReleaseOverrideTexts_RemovesExactlyTheKeysThisServiceSet()
    {
        var service = Create();
        service.SetLanguage(SystemLanguage.English);

        _set.Count.ShouldBe(2);
        service.ReleaseOverrideTexts();
        _removed.Sort(StringComparer.Ordinal);
        _removed.ShouldBe(new[] { "hello", "onlyEnglish" });

        service.ReleaseOverrideTexts();
        _removed.Count.ShouldBe(2);
    }
}
