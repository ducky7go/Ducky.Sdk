using Ducky.Sdk.Localizations;

namespace Ducky.Sdk.Lib.Tests;

public class KeyPatternSyntaxTests
{
    [Test]
    public void Identifier_IsAParameterInsideOneSegment()
    {
        var info = KeyPatternSyntax.Parse("worldeffect.{code}.name");

        info.IsValid.ShouldBeTrue();
        info.Parameters.ShouldBe(new[] { "code" });
        KeyPatternSyntax.Matches("worldeffect.{code}.name", "worldeffect.FROST.name").ShouldBeTrue();
        KeyPatternSyntax.Matches("worldeffect.{code}.name", "worldeffect.FROST.extra.name").ShouldBeFalse();
    }

    [Test]
    public void ChoiceList_IsTheOnlyChoiceAndMatchesOneOfItsChoices()
    {
        var info = KeyPatternSyntax.Parse("worldeffect.{code}.{name|summary|description}");

        info.IsValid.ShouldBeTrue();
        info.Choice!.Choices.ShouldBe(new[] { "name", "summary", "description" });
        KeyPatternSyntax.Matches("worldeffect.{code}.{name|summary|description}", "worldeffect.FROST.summary").ShouldBeTrue();
        KeyPatternSyntax.Matches("worldeffect.{code}.{name|summary|description}", "worldeffect.FROST.other").ShouldBeFalse();
    }

    [Test]
    public void LastSegmentRule_ParameterInTheLastSegmentMayContainDots()
    {
        var pattern = "effect.{id}";

        KeyPatternSyntax.Matches(pattern, "effect.a.b.c").ShouldBeTrue();
        KeyPatternSyntax.Parse("effect.{id}.name").Tokens.Count.ShouldBe(3);
        KeyPatternSyntax.Matches("effect.{id}.name", "effect.a.b.name").ShouldBeFalse();
        KeyPatternSyntax.Matches("effect.{id}.name", "effect.a.name").ShouldBeTrue();
    }

    [Test]
    public void Matching_IsCaseInsensitiveAndAnchored()
    {
        KeyPatternSyntax.Matches("worldeffect.{code}.name", "WORLDEFFECT.frost.NAME").ShouldBeTrue();
        KeyPatternSyntax.Matches("worldeffect.{code}.name", "xworldeffect.frost.name").ShouldBeFalse();
    }

    [Test]
    public void TwoChoiceLists_AreInvalid()
    {
        var info = KeyPatternSyntax.Parse("{a|b}.{c|d}");

        info.IsValid.ShouldBeFalse();
        info.Error!.ShouldContain("only one choice list");
        info.ToRegex().ShouldBeNull();
    }

    [Test]
    public void EmptyPattern_IsInvalid()
    {
        KeyPatternSyntax.Parse("").IsValid.ShouldBeFalse();
        KeyPatternSyntax.Parse("   ").Error!.ShouldContain("empty");
    }

    [Test]
    public void UnclosedBrace_AndEmptyChoice_AreInvalid()
    {
        KeyPatternSyntax.Parse("worldeffect.{code").Error!.ShouldContain("not closed");
        KeyPatternSyntax.Parse("worldeffect.{a||b}").Error!.ShouldContain("empty choice");
        KeyPatternSyntax.Parse("worldeffect.{}").Error!.ShouldContain("empty");
    }

    [Test]
    public void FormatTemplate_NumbersParametersAndFillsTheChoice()
    {
        var info = KeyPatternSyntax.Parse("worldeffect.{code}.{name|summary}");

        info.FormatTemplate("summary").ShouldBe("worldeffect.{0}.summary");
    }
}
