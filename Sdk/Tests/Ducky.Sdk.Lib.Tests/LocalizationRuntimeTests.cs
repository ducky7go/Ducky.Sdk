using System.IO;
using Ducky.Sdk.Localizations;
using UnityEngine;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Runtime contract: key comparison, file markers, missing text, TryGet and language-switch cleanup.</summary>
public class LocalizationRuntimeTests
{
    private LocalizationFixture _fx = null!;

    [SetUp]
    public void SetUp() => _fx = new LocalizationFixture();

    [TearDown]
    public void TearDown() => _fx.Dispose();

    [Test]
    public void MixedCaseCsvKey_MatchesLookupInAnyCase()
    {
        var dir = _fx.Locales("a", ("en.csv", "Key,Value\nMenu.Title,Menu\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.Get("menu.title").ShouldBe("Menu");
        service.Get("MENU.TITLE").ShouldBe("Menu");
    }

    [Test]
    public void SameFileDuplicateKey_LaterRowWinsAndWarns()
    {
        var dir = _fx.Locales("a", ("en.csv", "k,first\nK,second\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.Get("k").ShouldBe("second");
        _fx.Logged("duplicate key").ShouldBeTrue();
    }

    [Test]
    public void FileMarker_ReadsTheReferencedFileFromTheLanguageFolder()
    {
        var dir = _fx.Locales("a", ("en.csv", "welcome,@file:Welcome.md\n"), ("en/Welcome.md", "Hello there"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.Get("welcome").ShouldBe("Hello there");
    }

    [Test]
    public void PlainTextThatLooksLikeAFileName_StaysText()
    {
        var dir = _fx.Locales("a", ("en.csv", "label,notes.txt\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.Get("label").ShouldBe("notes.txt");
    }

    [Test]
    public void FileMarker_PathEscapeIsRejectedWithWarning()
    {
        var dir = _fx.Locales("a", ("en.csv", "x,@file:../secret.txt\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.Get("x").ShouldBe("[x]");
        _fx.Logged("rejected file reference").ShouldBeTrue();
    }

    [Test]
    public void LegacyExactRow_ReadsTheFileAndLogsDeprecation()
    {
        var dir = _fx.Locales("a", ("en.csv", "intro,intro.md\n"), ("en/intro.md", "Intro body"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.Get("intro").ShouldBe("Intro body");
        _fx.Logged("deprecated").ShouldBeTrue();
    }

    [Test]
    public void MissingReferencedFile_WarnsAndFallsThroughToEnglish()
    {
        var dir = _fx.Locales("a", ("en.csv", "welcome,English\n"), ("zh.csv", "welcome,@file:Welcome.md\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.Chinese);

        service.Get("welcome").ShouldBe("English");
        _fx.Logged("missing file 'Welcome.md' for key 'welcome'").ShouldBeTrue();
    }

    [Test]
    public void MixedCaseLanguageFileName_ResolvesToTheCanonicalCode()
    {
        var dir = _fx.Locales("a", ("zh-Hant.csv", "hello,你好\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.ChineseTraditional);

        service.Get("hello").ShouldBe("你好");
    }

    [Test]
    public void UnknownKey_GivesBracketedKeyAndTryGetReportsAbsenceWithoutPlaceholder()
    {
        var dir = _fx.Locales("a", ("en.csv", "hello,Hello\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.Get("nope").ShouldBe("[nope]");
        service.TryGet(null, "nope", out var missing).ShouldBeFalse();
        missing.ShouldBe(string.Empty);
        service.TryGet(null, "hello", out var present).ShouldBeTrue();
        present.ShouldBe("Hello");
    }

    [Test]
    public void LanguageSwitch_RemovesOverridesOnlyTheOldLanguageHad()
    {
        var dir = _fx.Locales("a",
            ("en.csv", "hello,Hello\nonlyEnglish,English text\n"),
            ("zh.csv", "hello,你好\nzhOnly,仅中文\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.Chinese);

        service.SetLanguage(SystemLanguage.English);

        _fx.Removed.ShouldContain("zhOnly");
        _fx.Removed.ShouldNotContain("onlyEnglish");
    }

    [Test]
    public void LanguageSwitch_UnchangedTextIsNotRemovedAndSetAgain()
    {
        var dir = _fx.Locales("a",
            ("en.csv", "hello,Hello\nonlyEnglish,English text\n"),
            ("zh.csv", "hello,你好\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.Chinese);
        var setBefore = _fx.Set.FindAll(entry => entry.Key == "onlyEnglish").Count;

        service.SetLanguage(SystemLanguage.English);

        _fx.Set.FindAll(entry => entry.Key == "onlyEnglish").Count.ShouldBe(setBefore);
        _fx.Removed.ShouldNotContain("onlyEnglish");
    }

    [Test]
    public void LanguageSwitch_NeverRemovesAKeyTheSdkDidNotSet()
    {
        var dir = _fx.Locales("a", ("en.csv", "hello,Hello\n"));
        var service = _fx.Create(dir);
        service.SetLanguage(SystemLanguage.English);

        service.SetLanguage(SystemLanguage.Japanese);

        _fx.Removed.ShouldNotContain("foreign.key");
    }
}
