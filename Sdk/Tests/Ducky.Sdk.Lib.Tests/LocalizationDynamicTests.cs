using System.Collections.Generic;
using Ducky.Sdk.Localizations;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>L.Dynamic: templates formatted with invariant culture, same lookup as L.Get, no exceptions.</summary>
public class LocalizationDynamicTests
{
    private LocalizationFixture _fx = null!;
    private LocalizationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _fx = new LocalizationFixture();
        var dir = _fx.Locales("dyn", ("en.csv", "worldeffect.FROST.name,Frost\nhello.{0},Hello\n"));
        _service = _fx.Create(dir);
        _service.SetLanguage(SystemLanguage.English);
    }

    [TearDown]
    public void TearDown() => _fx.Dispose();

    [Test]
    public void EffectCodeKey_ReturnsTheTextOfTheFormattedKey()
    {
        L.DynamicCore(_service, null, "worldeffect.{0}.name", null, new object?[] { "FROST" }).ShouldBe("Frost");
    }

    [Test]
    public void UnknownDynamicKey_GivesTheBracketedKeyOrTheFallback()
    {
        L.DynamicCore(_service, null, "worldeffect.{0}.name", null, new object?[] { "NOPE" })
            .ShouldBe("[worldeffect.NOPE.name]");
        L.DynamicCore(_service, null, "worldeffect.{0}.name", "fallback", new object?[] { "NOPE" })
            .ShouldBe("fallback");
    }

    [Test]
    public void CodeCase_DoesNotMatter()
    {
        L.DynamicCore(_service, null, "worldeffect.{0}.name", null, new object?[] { "frost" }).ShouldBe("Frost");
    }

    [Test]
    public void MissingArgument_LogsAndGivesTheTemplateResultWithoutThrowing()
    {
        var result = L.DynamicCore(_service, null, "hello.{1}", null, new object?[] { "x" });

        result.ShouldBe("[hello.{1}]");
        _fx.Logged("invalid key template 'hello.{1}'").ShouldBeTrue();
    }

    [Test]
    public void TryDynamic_ReportsAbsenceWithoutAPlaceholder()
    {
        L.TryDynamicCore(_service, null, "worldeffect.{0}.name", out var found, new object?[] { "FROST" }).ShouldBeTrue();
        found.ShouldBe("Frost");

        L.TryDynamicCore(_service, null, "worldeffect.{0}.name", out var missing, new object?[] { "NOPE" }).ShouldBeFalse();
        missing.ShouldBe(string.Empty);
    }

    [Test]
    public void ScopedDynamic_SeesOnlyItsOwnSource()
    {
        _service.Register(new MemoryProvider("other", new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new() { ["worldeffect.ICE.name"] = "Ice" },
        }), null);

        L.DynamicCore(_service, new ModId("other"), "worldeffect.{0}.name", null, new object?[] { "ICE" }).ShouldBe("Ice");
        L.DynamicCore(_service, new ModId("other"), "worldeffect.{0}.name", null, new object?[] { "FROST" })
            .ShouldBe("[worldeffect.FROST.name]");
    }
}
