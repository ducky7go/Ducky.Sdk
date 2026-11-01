using System.Collections.Generic;
using System.IO;
using Ducky.Sdk.Localizations;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Multiple sources, ModId scoping, game publishing, ownership and unloading.</summary>
public class LocalizationSourcesTests
{
    private LocalizationFixture _fx = null!;

    [SetUp]
    public void SetUp() => _fx = new LocalizationFixture();

    [TearDown]
    public void TearDown() => _fx.Dispose();

    private LocalizationService NewService()
    {
        // The default source is an empty Locales folder, so every text in these tests comes from a registered source.
        var empty = _fx.Locales("default");
        return _fx.Create(empty);
    }

    private static MemoryProvider Memory(string modId, string language, params (string Key, string Value)[] entries)
    {
        var map = new Dictionary<string, string>();
        foreach (var (key, value) in entries)
        {
            map[key] = value;
        }

        return new MemoryProvider(modId, new Dictionary<string, Dictionary<string, string>> { [language] = map });
    }

    [Test]
    public void DefaultSource_IsTheModLocalesFolderUnderTheDefaultModId()
    {
        var service = NewService();

        service.DefaultModId.ShouldBe(new ModId("default"));
    }

    [Test]
    public void SubModDirectory_ItsTextsAreAvailableAndTheCurrentLanguageIsApplied()
    {
        var service = NewService();
        var subDir = _fx.Locales("sub", ("en.csv", "title,Sub title\n"));
        service.Register(new DirectoryLocalizationProvider(new ModId("sub.one"), subDir), null);
        service.SetLanguage(SystemLanguage.English);

        service.GetScoped(new ModId("sub.one"), "title", null).ShouldBe("Sub title");
        _fx.Set.ShouldContain(("title", "Sub title"));
    }

    [Test]
    public void CustomProvider_IsAvailableLikeADirectorySource()
    {
        var service = NewService();
        service.Register(Memory("custom", "en", ("greet", "Hi")), null);
        service.SetLanguage(SystemLanguage.English);

        service.Get("greet").ShouldBe("Hi");
    }

    [Test]
    public void MissingDirectory_LogsWarningRegistersEmptyAndCanBePickedUpByReindex()
    {
        var service = NewService();
        var missing = Path.Combine(_fx.Root, "not-yet");
        var modId = new ModId("late.mod");
        service.Register(new DirectoryLocalizationProvider(modId, missing), null);
        _fx.Logged("Locales directory not found").ShouldBeTrue();

        service.GetScoped(modId, "later", null).ShouldBe("[later]");

        Directory.CreateDirectory(missing);
        File.WriteAllText(Path.Combine(missing, "en.csv"), "later,Now here\n");
        service.Reindex(modId);
        service.SetLanguage(SystemLanguage.English);

        service.GetScoped(modId, "later", null).ShouldBe("Now here");
    }

    [Test]
    public void RegisteringTheSameDirectoryTwice_IsIdempotent()
    {
        var service = NewService();
        var subDir = _fx.Locales("sub", ("en.csv", "a,1\n"));
        var modId = new ModId("sub.two");
        service.Register(new DirectoryLocalizationProvider(modId, subDir), null);
        service.SetLanguage(SystemLanguage.English);
        var setCount = _fx.Set.Count;

        service.Register(new DirectoryLocalizationProvider(modId, subDir), null);

        _fx.Set.Count.ShouldBe(setCount);
    }

    [Test]
    public void SameKeyInTwoSources_ScopedLookupReturnsEachSourcesOwnText()
    {
        var service = NewService();
        service.Register(Memory("a", "en", ("title", "A title")), null);
        service.Register(Memory("b", "en", ("title", "B title")), null);
        service.SetLanguage(SystemLanguage.English);

        service.GetScoped(new ModId("a"), "title", null).ShouldBe("A title");
        service.GetScoped(new ModId("b"), "title", null).ShouldBe("B title");
    }

    [Test]
    public void UnscopedLookup_DefaultSourceWinsThenTheEarliestRegistered()
    {
        var service = NewService();
        var defaultDir = _fx.Locales("default-with-title", ("en.csv", "title,Default title\n"));
        service = _fx.Create(defaultDir);
        service.Register(Memory("a", "en", ("title", "A title"), ("only.a", "A only")), null);
        service.Register(Memory("b", "en", ("title", "B title")), null);
        service.SetLanguage(SystemLanguage.English);

        service.Get("title").ShouldBe("Default title");
        service.Get("only.a").ShouldBe("A only");

        var withoutDefault = NewService();
        withoutDefault.Register(Memory("a", "en", ("title", "A title")), null);
        withoutDefault.Register(Memory("b", "en", ("title", "B title")), null);
        withoutDefault.SetLanguage(SystemLanguage.English);

        withoutDefault.Get("title").ShouldBe("A title");
    }

    [Test]
    public void PrefixedPublishing_PublishesTheKeyWithThePrefix()
    {
        var service = NewService();
        service.Register(Memory("dod", "en", ("title", "Hello")), new LocalizationSourceOptions { KeyPrefix = "dod." });
        service.SetLanguage(SystemLanguage.English);

        _fx.Set.ShouldContain(("dod.title", "Hello"));
        _fx.Set.ShouldNotContain(("title", "Hello"));
    }

    [Test]
    public void Collision_OwnerTextIsPublishedAndOneWarningIsLogged()
    {
        var service = NewService();
        service.Register(Memory("first", "en", ("shared.key", "First text")), null);
        service.Register(Memory("second", "en", ("shared.key", "Second text")), null);
        service.SetLanguage(SystemLanguage.English);
        service.SetLanguage(SystemLanguage.English);

        _fx.Set.FindAll(entry => entry.Key == "shared.key").ShouldBe(new[] { ("shared.key", "First text") });
        _fx.Messages.FindAll(message => message.Contains("shared.key")).Count.ShouldBe(1);
    }

    [Test]
    public void UnloadOneOfTwo_RemovesOnlyItsOverrides()
    {
        var service = NewService();
        var a = service.Register(Memory("a", "en", ("only.a", "A")), null);
        service.Register(Memory("b", "en", ("only.b", "B")), null);
        service.SetLanguage(SystemLanguage.English);

        a.Dispose();

        _fx.Removed.ShouldBe(new[] { "only.a" });
    }

    [Test]
    public void ShadowedKey_ReturnsWithTheOtherSourcesTextWhenTheOwnerIsUnloaded()
    {
        var service = NewService();
        var a = service.Register(Memory("a", "en", ("shared.key", "Owner text")), null);
        service.Register(Memory("b", "en", ("shared.key", "Shadowed text")), null);
        service.SetLanguage(SystemLanguage.English);

        a.Dispose();

        _fx.Set[^1].ShouldBe(("shared.key", "Shadowed text"));
        _fx.Removed.ShouldNotContain("shared.key");
    }

    [Test]
    public void ReleaseOverrideTexts_RemovesEverySourceOverride()
    {
        var service = NewService();
        service.Register(Memory("a", "en", ("k1", "1")), new LocalizationSourceOptions { KeyPrefix = "p." });
        service.Register(Memory("b", "en", ("k2", "2")), null);
        service.SetLanguage(SystemLanguage.English);

        service.ReleaseOverrideTexts();

        _fx.Removed.Sort(System.StringComparer.Ordinal);
        _fx.Removed.ShouldBe(new[] { "k2", "p.k1" });
    }

    [Test]
    public void LanguageSwitch_AppliesToEverySource()
    {
        var service = NewService();
        var zh = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new() { ["a"] = "A en" },
            ["zh"] = new() { ["a"] = "A zh" },
        };
        var zhB = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new() { ["b"] = "B en" },
            ["zh"] = new() { ["b"] = "B zh", ["zhOnly"] = "B only zh" },
        };
        service.Register(new MemoryProvider("two.a", zh), null);
        service.Register(new MemoryProvider("two.b", zhB), null);

        service.SetLanguage(SystemLanguage.Chinese);
        _fx.Set.ShouldContain(("a", "A zh"));
        _fx.Set.ShouldContain(("b", "B zh"));

        service.SetLanguage(SystemLanguage.English);
        _fx.Set.ShouldContain(("b", "B en"));
        _fx.Removed.ShouldContain("zhOnly");
    }
}
