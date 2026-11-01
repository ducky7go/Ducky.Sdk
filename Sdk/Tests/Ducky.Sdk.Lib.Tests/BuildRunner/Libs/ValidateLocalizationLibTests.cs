using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ducky.Sdk.Build.Localization;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Tasks 6.1 to 6.4: ValidateLocalizationLib compares every language with the English baseline, checks placeholders,
/// writes missing.csv beside the Locales folder and maps findings to warning and error output with exit codes.
/// </summary>
public class ValidateLocalizationLibTests
{
    private string _root = null!;
    private string _assetsDir = null!;
    private string _localesDir = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("validate-locales");
        _assetsDir = Path.Combine(_root, "assets");
        _localesDir = Path.Combine(_assetsDir, "Locales");
        Directory.CreateDirectory(_localesDir);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private void Csv(string name, string content) => LibTestSupport.WriteText(Path.Combine(_localesDir, name), content);

    private static Regex? Pattern(string pattern) => KeyPatternSyntax.Parse(pattern).ToRegex();

    private ValidateLocalizationLib.ValidationResult Validate(IReadOnlyList<Regex>? patterns = null) =>
        ValidateLocalizationLib.Validate(_localesDir, null, patterns?.ToList() ?? new List<Regex>());

    private BuildContext Context(string validation = null, string extra = null)
    {
        var context = LibTestSupport.NewContext(_root, modName: "Ducky.Validate", assetsDir: _assetsDir);
        context.LocalizationValidation = validation;
        context.LocalizationExtraKeys = extra;
        return context;
    }

    // 6.1 -------------------------------------------------------------------

    [Test]
    public void DirectoryWithoutLkeys_ComparesTheOtherLanguageWithEnglish()
    {
        Csv("en.csv", "Key,Value\na,A\nb,B\n");
        Csv("ja.csv", "Key,Value\na,A-ja\n");

        var result = Validate();

        result.Missing.Select(row => (row.Language, row.Key)).ShouldBe(new[] { ("ja", "b") });
    }

    [Test]
    public void NoEnglishFile_ReportsTheBaselineAndComparesNothing()
    {
        Csv("ja.csv", "Key,Value\na,A\n");

        var result = Validate();

        result.Findings.Select(f => f.Kind).ShouldBe(new[] { ValidateLocalizationLib.FindingKind.BaselineMissing });
        result.Missing.ShouldBeEmpty();
    }

    [Test]
    public void AbsentKey_IsReportedAsMissingForThatLanguage()
    {
        Csv("en.csv", "Key,Value\nquit,Quit\n");
        Csv("de.csv", "Key,Value\n");

        Validate().Missing.Select(row => (row.Language, row.Key)).ShouldBe(new[] { ("de", "quit") });
    }

    [Test]
    public void EmptyValue_IsReportedAsMissing()
    {
        Csv("en.csv", "Key,Value\nquit,Quit\n");
        Csv("fr.csv", "Key,Value\nquit,\"\"\n");

        Validate().Missing.Select(row => (row.Language, row.Key)).ShouldBe(new[] { ("fr", "quit") });
    }

    [Test]
    public void DifferentCasing_CountsAsPresent()
    {
        Csv("en.csv", "Key,Value\nMenu.Quit,Quit\n");
        Csv("de.csv", "Key,Value\nmenu.quit,Beenden\n");

        Validate().Missing.ShouldBeEmpty();
    }

    [Test]
    public void ObsoleteKey_IsReportedAsExtraOfThatLanguage()
    {
        Csv("en.csv", "Key,Value\na,A\n");
        Csv("ja.csv", "Key,Value\na,A\nold.key,Old\n");

        var extras = Validate().Findings.Where(f => f.Kind == ValidateLocalizationLib.FindingKind.Extra).ToList();

        extras.Select(f => (f.Language, f.Key)).ShouldBe(new[] { ("ja", "old.key") });
    }

    [Test]
    public void PatternKey_IsNeverReportedAsExtra()
    {
        Csv("en.csv", "Key,Value\na,A\n");
        Csv("ja.csv", "Key,Value\na,A\nworldeffect.FROST.name,Frost\n");

        Validate(new[] { Pattern("worldeffect.{code}.{name|summary}")! })
            .Findings.Where(f => f.Kind == ValidateLocalizationLib.FindingKind.Extra).ShouldBeEmpty();
    }

    [Test]
    public void StaticKeysFromLkeys_AreTheBaselineWhenGiven()
    {
        Csv("ja.csv", "Key,Value\na,A\n");

        var result = ValidateLocalizationLib.Validate(_localesDir, new[] { "a", "b" }, new List<Regex>());

        result.Missing.Select(row => row.Key).ShouldBe(new[] { "b" });
    }

    // 6.2 -------------------------------------------------------------------

    [Test]
    public void MissingPlaceholder_IsReported()
    {
        Csv("en.csv", "Key,Value\ngreet,\"Hello {0}, you have {1} items\"\n");
        Csv("de.csv", "Key,Value\ngreet,\"Hallo {0}\"\n");

        Validate().Findings.Any(f => f.Kind == ValidateLocalizationLib.FindingKind.Placeholder && f.Key == "greet")
            .ShouldBeTrue();
    }

    [Test]
    public void ReorderedPlaceholders_AreNotAMismatch()
    {
        Csv("en.csv", "Key,Value\ngreet,\"{0} and {1}\"\n");
        Csv("de.csv", "Key,Value\ngreet,\"{1} und {0}\"\n");

        Validate().Findings.ShouldBeEmpty();
    }

    [Test]
    public void EscapedBraces_AreNotPlaceholders()
    {
        Csv("en.csv", "Key,Value\nhint,\"Use {{0}} literally, then {0}\"\n");
        Csv("de.csv", "Key,Value\nhint,\"{0} wörtlich {{0}}\"\n");

        Validate().Findings.ShouldBeEmpty();
        ValidateLocalizationLib.PlaceholderIndices("a {{1}} b {2:N2} c {0,5}").OrderBy(i => i).ShouldBe(new[] { 0, 2 });
    }

    // 6.3 -------------------------------------------------------------------

    [Test]
    public void MissingReport_IsWrittenToAssetsNotLocales()
    {
        Csv("en.csv", "Key,Value\nquit,\"Quit, now\"\n");
        Csv("de.csv", "Key,Value\n");

        var (exit, _, _) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context()));

        exit.ShouldBe(0);
        File.ReadAllText(Path.Combine(_assetsDir, "missing.csv")).ShouldBe("Language,Key,English\nde,quit,\"Quit, now\"\n");
        File.Exists(Path.Combine(_localesDir, "missing.csv")).ShouldBeFalse();
    }

    [Test]
    public void AllTranslated_WritesNoReportAndRemovesAnOldOne()
    {
        Csv("en.csv", "Key,Value\nquit,Quit\n");
        Csv("de.csv", "Key,Value\nquit,Beenden\n");
        LibTestSupport.WriteText(Path.Combine(_assetsDir, "missing.csv"), "old");

        var (exit, _, _) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context()));

        exit.ShouldBe(0);
        File.Exists(Path.Combine(_assetsDir, "missing.csv")).ShouldBeFalse();
    }

    // 6.4 -------------------------------------------------------------------

    [Test]
    public void DefaultSeverity_ReportsWarningsAndExitsZero()
    {
        Csv("en.csv", "Key,Value\nquit,Quit\n");
        Csv("de.csv", "Key,Value\n");

        var (exit, output, error) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context()));

        exit.ShouldBe(0);
        output.ShouldContain("warning: [LocalizationValidation] 'quit' is missing in 'de'");
        error.ShouldNotContain("error:");
    }

    [Test]
    public void ErrorMode_FailsTheBuildWithTheKeyAndLanguage()
    {
        Csv("en.csv", "Key,Value\ngreet,\"Hello {0}\"\n");
        Csv("de.csv", "Key,Value\ngreet,\"Hallo\"\n");

        var (exit, _, error) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context("Error")));

        exit.ShouldBe(1);
        error.ShouldContain("error: [LocalizationValidation] placeholders differ");
    }

    [Test]
    public void ExtraKeys_StayASeparateSeverity()
    {
        Csv("en.csv", "Key,Value\na,A\n");
        Csv("ja.csv", "Key,Value\na,A\nold.key,Old\n");

        var (exit, output, error) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context("Error", "Warn")));

        exit.ShouldBe(0);
        output.ShouldContain("warning: [LocalizationValidation] 'old.key' is not in the English baseline");
        error.ShouldNotContain("error:");
    }

    [Test]
    public void ExtraKeysIgnore_ReportsNothingForExtras()
    {
        Csv("en.csv", "Key,Value\na,A\n");
        Csv("ja.csv", "Key,Value\na,A\nold.key,Old\n");

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context("Warn", "Ignore")));

        exit.ShouldBe(0);
        output.ShouldNotContain("old.key");
    }

    [Test]
    public void Off_SkipsValidationAndWritesNoReport()
    {
        Csv("en.csv", "Key,Value\nquit,Quit\n");
        Csv("de.csv", "Key,Value\n");

        var (exit, _, _) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context("Off")));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        File.Exists(Path.Combine(_assetsDir, "missing.csv")).ShouldBeFalse();
    }

    [Test]
    public void SeverityChange_TakesEffectOnTheNextRun()
    {
        Csv("en.csv", "Key,Value\nquit,Quit\n");
        Csv("de.csv", "Key,Value\n");

        var (offExit, _, _) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context("Off")));
        var (warnExit, warnOutput, _) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context("Warn")));

        offExit.ShouldBe(RunnerExitCodes.SkipExitCode);
        warnExit.ShouldBe(0);
        warnOutput.ShouldContain("warning: [LocalizationValidation]");
    }

    [Test]
    public void UnknownSeverity_FallsBackToWarnWithAWarning()
    {
        Csv("en.csv", "Key,Value\nquit,Quit\n");
        Csv("de.csv", "Key,Value\n");

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(Context("Loud")));

        exit.ShouldBe(0);
        output.ShouldContain("unknown severity 'Loud'");
    }
}
