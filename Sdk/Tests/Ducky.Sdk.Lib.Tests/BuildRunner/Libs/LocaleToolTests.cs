using System.IO;
using System.Linq;
using Ducky.Sdk.Build.Localization;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Tasks 7.1 to 7.5: the locales set, sync and validate commands, run through Program.Run exactly as the command line does.
/// </summary>
public class LocaleToolTests
{
    private string _root = null!;
    private string _assetsDir = null!;
    private string _locales = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("locale-tool");
        _assetsDir = Path.Combine(_root, "assets");
        _locales = Path.Combine(_assetsDir, "Locales");
        Directory.CreateDirectory(_locales);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private static (int Exit, string Out, string Err) Locales(params string[] args) =>
        LibTestSupport.Capture(() => Program.Run(new[] { "locales" }.Concat(args).ToArray()));

    private string Read(string name) => File.ReadAllText(Path.Combine(_locales, name));

    private static System.Collections.Generic.Dictionary<string, string> Rows(string content) =>
        CsvDocument.ToDictionary(content);

    // 7.1 -------------------------------------------------------------------

    [Test]
    public void NoCommand_PrintsUsageAndExitsTwo()
    {
        var (exit, _, err) = Locales();

        exit.ShouldBe(2);
        err.ShouldContain("Usage:");
    }

    [Test]
    public void SetWithoutAKey_PrintsUsageAndExitsTwo()
    {
        var (exit, _, err) = Locales("set", _locales);

        exit.ShouldBe(2);
        err.ShouldContain("locales set <Locales dir> <lang> <key> <value>");
    }

    [Test]
    public void BuildUsageIsUnchangedByTheLocalesRouting()
    {
        var (exit, _, err) = LibTestSupport.Capture(() => Program.Run(new[] { "--phase", "post" }));

        exit.ShouldBe(2);
        err.ShouldContain("Usage: dotnet Ducky.Sdk.Build.dll <build-context.json> --phase pre|post");
    }

    // 7.2 -------------------------------------------------------------------

    [Test]
    public void Set_ValueWithCommaAndQuote_IsQuotedAndReadBackUnchanged()
    {
        var (exit, _, _) = Locales("set", _locales, "en", "key", "Say \"hi\", then leave");

        exit.ShouldBe(0);
        Read("en.csv").ShouldContain("key,\"Say \"\"hi\"\", then leave\"");
        Rows(Read("en.csv"))["key"].ShouldBe("Say \"hi\", then leave");
    }

    [Test]
    public void Set_NewLanguage_CreatesTheFileWithAHeader()
    {
        Locales("set", _locales, "de", "greet", "Hallo");

        Read("de.csv").ShouldBe("Key,Value\ngreet,\"Hallo\"\n");
    }

    [Test]
    public void Set_MultilineValue_StaysOneRowAndReadsBack()
    {
        Locales("set", _locales, "en", "poem", "first line\nsecond, line");

        Rows(Read("en.csv"))["poem"].ShouldBe("first line\nsecond, line");
    }

    [Test]
    public void Set_SameValueTwice_TheSecondCallWritesNothing()
    {
        Locales("set", _locales, "en", "key", "Value");
        var (exit, output, _) = Locales("set", _locales, "en", "key", "Value");

        exit.ShouldBe(0);
        output.ShouldBeEmpty();
    }

    [Test]
    public void SetMultiForm_SetsOneKeyInSeveralLanguagesInOneCall()
    {
        var (exit, _, _) = Locales("set", _locales, "greet", "--lang", "en=Hello", "--lang", "de=Hallo");

        exit.ShouldBe(0);
        Rows(Read("en.csv"))["greet"].ShouldBe("Hello");
        Rows(Read("de.csv"))["greet"].ShouldBe("Hallo");
    }

    [Test]
    public void Set_KeepsTheRowsSortedByKey()
    {
        Locales("set", _locales, "en", "b", "B");
        Locales("set", _locales, "en", "a", "A");

        Read("en.csv").ShouldBe("Key,Value\na,\"A\"\nb,\"B\"\n");
    }

    // 7.3 -------------------------------------------------------------------

    [Test]
    public void Sync_FillsMissingKeysFromEnglish_AndKeepsExistingTranslations()
    {
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\na,A\nb,B\nc,C\n");
        File.WriteAllText(Path.Combine(_locales, "de.csv"), "Key,Value\nb,Bde\n");

        var (exit, _, _) = Locales("sync", _locales);

        exit.ShouldBe(0);
        var de = Rows(Read("de.csv"));
        de.Count.ShouldBe(3);
        de["a"].ShouldBe("A");
        de["b"].ShouldBe("Bde");
        de["c"].ShouldBe("C");
    }

    [Test]
    public void Sync_EmptyFill_LeavesNewKeysEmptySoValidationStillSeesThem()
    {
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\na,A\n");
        File.WriteAllText(Path.Combine(_locales, "de.csv"), "Key,Value\n");

        Locales("sync", _locales, "--fill", "empty");

        Rows(Read("de.csv"))["a"].ShouldBe(string.Empty);
    }

    [Test]
    public void Sync_Prune_RemovesKeysEnglishLacksUnlessAPatternMatches()
    {
        File.WriteAllText(Path.Combine(_assetsDir, "lkeys.json"),
            "{\n  \"patterns\": [\"worldeffect.{code}.name\"],\n  \"keys\": [\"a\"]\n}");
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\na,A\n");
        File.WriteAllText(Path.Combine(_locales, "de.csv"),
            "Key,Value\na,A\nold,Old\nworldeffect.FROST.name,Frost\n");

        Locales("sync", _locales, "--prune");

        var de = Rows(Read("de.csv"));
        de.ContainsKey("old").ShouldBeFalse();
        de["worldeffect.FROST.name"].ShouldBe("Frost");
    }

    [Test]
    public void Sync_DryRun_ChangesNoFile()
    {
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\na,A\nb,B\n");
        File.WriteAllText(Path.Combine(_locales, "de.csv"), "Key,Value\na,A\n");
        var before = Read("de.csv");

        var (exit, output, _) = Locales("sync", _locales, "--dry-run");

        exit.ShouldBe(0);
        Read("de.csv").ShouldBe(before);
        output.ShouldContain("would add b to");
    }

    [Test]
    public void Sync_KeepsFileReferenceRowsAsWritten()
    {
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\nwelcome,\"@file:Welcome.md\"\n");
        File.WriteAllText(Path.Combine(_locales, "de.csv"), "Key,Value\nwelcome,\"@file:Willkommen.md\"\n");

        Locales("sync", _locales);

        Rows(Read("de.csv"))["welcome"].ShouldBe("@file:Willkommen.md");
    }

    // 7.4 -------------------------------------------------------------------

    [Test]
    public void Validate_SeveralSubModFolders_ChecksEachAndExitsZeroWithoutErrors()
    {
        var other = Path.Combine(_root, "other", "Locales");
        Directory.CreateDirectory(other);
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\na,A\n");
        File.WriteAllText(Path.Combine(_locales, "ja.csv"), "Key,Value\na,Aja\n");
        File.WriteAllText(Path.Combine(other, "en.csv"), "Key,Value\nb,B\n");
        File.WriteAllText(Path.Combine(other, "ja.csv"), "Key,Value\n");

        var (exit, output, _) = Locales("validate", _locales, other);

        exit.ShouldBe(0);
        output.ShouldContain("validated " + other);
        output.ShouldContain("warning: [LocalizationValidation] 'b' is missing in 'ja'");
        File.Exists(Path.Combine(Path.Combine(_root, "other"), "missing.csv")).ShouldBeTrue();
    }

    [Test]
    public void Validate_ErrorSeverity_ExitsOneWhenAFindingIsAnError()
    {
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\na,A\n");
        File.WriteAllText(Path.Combine(_locales, "ja.csv"), "Key,Value\n");

        var (exit, _, err) = Locales("validate", _locales, "--validation", "Error");

        exit.ShouldBe(1);
        err.ShouldContain("error: [LocalizationValidation] 'a' is missing in 'ja'");
    }

    [Test]
    public void Validate_BadOption_PrintsUsageAndExitsTwo()
    {
        var (exit, _, err) = Locales("validate", _locales, "--nope");

        exit.ShouldBe(2);
        err.ShouldContain("unknown option '--nope'");
    }

    // 7.5 -------------------------------------------------------------------

    [Test]
    public void RoundTrip_SetThenSyncKeepsCommasQuotesAndLineBreaks()
    {
        const string value = "He said \"hi\", then\nleft";
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\n");

        Locales("set", _locales, "en", "scene", value);
        Locales("sync", _locales);

        Rows(Read("en.csv"))["scene"].ShouldBe(value);
    }

    [Test]
    public void RoundTrip_SyncOfAnAlreadySyncedFileIsByteIdentical()
    {
        File.WriteAllText(Path.Combine(_locales, "en.csv"), "Key,Value\na,\"x, \"\"y\"\"\"\n");
        File.WriteAllText(Path.Combine(_locales, "de.csv"), "Key,Value\na,\"x, \"\"y\"\"\"\n");
        var before = Read("de.csv");

        var (exit, output, _) = Locales("sync", _locales);

        exit.ShouldBe(0);
        Read("de.csv").ShouldBe(before);
        output.ShouldNotContain("de.csv");
    }
}
