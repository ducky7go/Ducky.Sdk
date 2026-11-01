namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Task 5.2: UpdateLocalizationCsvLib turns lkeys.json into Locales/*.csv and keys.hash.txt.
/// Golden: the baseline Ducky.TryHarmony Locales folder and keys.hash.txt (CRC32 9365c9eb).
/// </summary>
public class UpdateLocalizationCsvLibTests
{
    private string _root = null!;
    private string _assetsDir = null!;
    private string _localesDir = null!;
    private BuildContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("locales-csv");
        var projectDir = Path.Combine(_root, "Ducky TryHarmony");
        _assetsDir = Path.Combine(projectDir, "assets");
        _localesDir = Path.Combine(_assetsDir, "Locales");
        Directory.CreateDirectory(_assetsDir);
        File.Copy(LibTestSupport.Golden("TryHarmony", "lkeys.json"), Path.Combine(_assetsDir, "lkeys.json"));
        _context = LibTestSupport.NewContext(projectDir, modName: "Ducky.TryHarmony", assetsDir: _assetsDir);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private static string GoldenLocalesDir => LibTestSupport.Golden("TryHarmony", "Locales");

    private static string GoldenHash => File.ReadAllText(LibTestSupport.Golden("TryHarmony", "keys.hash.txt"));

    private void ReplaceLkeys(string json) => File.WriteAllText(Path.Combine(_assetsDir, "lkeys.json"), json);

    private void AssertLocalesMatchGolden()
    {
        var goldenNames = Directory.GetFiles(GoldenLocalesDir, "*.csv").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var actualNames = Directory.GetFiles(_localesDir, "*.csv").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        actualNames.ShouldBe(goldenNames);

        foreach (var name in goldenNames)
        {
            var expected = LibTestSupport.NormalizeNewlines(File.ReadAllText(Path.Combine(GoldenLocalesDir, name!)));
            var actual = LibTestSupport.NormalizeNewlines(File.ReadAllText(Path.Combine(_localesDir, name!)));
            actual.ShouldBe(expected, customMessage: $"{name} differs from the baseline golden");
        }
    }

    [Test]
    public void Execute_GeneratesLocaleCsvsAndHashMatchingBaselineGolden()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(0);
        AssertLocalesMatchGolden();
        File.ReadAllText(Path.Combine(_assetsDir, "keys.hash.txt")).ShouldBe(GoldenHash);
    }

    [Test]
    public void Execute_SkipsOnSecondRunWhenHashAndCsvsAreCurrent()
    {
        LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));
        var csvBefore = Directory.GetFiles(_localesDir, "*.csv").ToDictionary(f => f, File.GetLastWriteTimeUtc);

        var (exit, output, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        output.ShouldContain("skipping CSV updates");
        foreach (var (path, time) in csvBefore)
        {
            File.GetLastWriteTimeUtc(path).ShouldBe(time);
        }
    }

    [Test]
    public void Execute_RewritesEverythingWhenStoredHashIsStale()
    {
        LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));
        File.WriteAllText(Path.Combine(_assetsDir, "keys.hash.txt"), "00000000");

        var (exit, _, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(0);
        File.ReadAllText(Path.Combine(_assetsDir, "keys.hash.txt")).ShouldBe(GoldenHash);
        AssertLocalesMatchGolden();
    }

    [Test]
    public void Execute_RecreatesMissingLocaleCsv()
    {
        LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));
        File.Delete(Path.Combine(_localesDir, "fr.csv"));

        var (exit, _, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(0);
        AssertLocalesMatchGolden();
    }

    [Test]
    public void Execute_DropsObsoleteRowsFromExistingCsv()
    {
        LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));
        File.AppendAllText(Path.Combine(_localesDir, "de.csv"), "removed_key,\"stale\"\n");

        var (exit, _, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(0);
        AssertLocalesMatchGolden();
    }

    [Test]
    public void Execute_SkipsWhenLkeysJsonIsMissing()
    {
        File.Delete(Path.Combine(_assetsDir, "lkeys.json"));

        var (exit, output, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        output.ShouldContain("Skipping CSV generation");
        Directory.Exists(_localesDir).ShouldBeFalse();
    }

    [Test]
    public void Execute_SkipsWhenLkeysHasNoKeys()
    {
        ReplaceLkeys("{ \"keys\": [] }");

        var (exit, output, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        output.ShouldContain("No keys found in JSON file");
    }

    [Test]
    public void Execute_FailsWhenLkeysHasNoKeysProperty()
    {
        ReplaceLkeys("{ \"namespace\": \"Ducky.TryHarmony\" }");

        var (exit, _, error) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(1);
        error.ShouldContain("'keys' property not found in JSON file");
    }

    [Test]
    public void Execute_WritesFileReferenceKeysAsStandaloneFilesWithDefaultLanguages()
    {
        // No supportedLanguages: the lib defaults to zh and en. Object keys with FileExtension become files.
        ReplaceLkeys("{ \"keys\": { \"ducky_intro\": { \"FileExtension\": \"md\" } } }");

        var (exit, _, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(0);
        File.Exists(Path.Combine(_localesDir, "en", "ducky_intro.md")).ShouldBeTrue();
        File.Exists(Path.Combine(_localesDir, "zh", "ducky_intro.md")).ShouldBeTrue();
        File.ReadAllText(Path.Combine(_localesDir, "en.csv")).ShouldBe("Key,Value\nducky_intro,\"@file:ducky_intro.md\"\n");
    }

    [Test]
    public void Execute_UsesLegacyArrayKeysWithDefaultLanguages()
    {
        ReplaceLkeys("{ \"keys\": [\"b_key\", \"a_key\"] }");

        var (exit, _, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(0);
        File.ReadAllText(Path.Combine(_localesDir, "en.csv")).ShouldBe("Key,Value\na_key,\"\"\nb_key,\"\"\n");
        File.Exists(Path.Combine(_localesDir, "zh.csv")).ShouldBeTrue();
    }

    [Test]
    public void Execute_KeepsRowsMatchedByAKeyPatternAndStillRemovesObsoleteRows()
    {
        // Task 3.5: a pattern row is data-driven and kept; a row that matches no key or pattern is obsolete.
        var golden = File.ReadAllText(Path.Combine(_assetsDir, "lkeys.json"));
        ReplaceLkeys(golden.Replace("  \"keys\": [", "  \"patterns\": [\"worldeffect.{code}.{name|summary}\"],\n  \"keys\": ["));
        Directory.CreateDirectory(_localesDir);
        File.WriteAllText(Path.Combine(_localesDir, "en.csv"),
            "Key,Value\nworldeffect.FROST.name,\"Frost\"\nold.gone,\"Obsolete\"\n");

        var (exit, _, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(0);
        var rows = Ducky.Sdk.Build.Localization.CsvDocument.ToDictionary(File.ReadAllText(Path.Combine(_localesDir, "en.csv")));
        rows["worldeffect.FROST.name"].ShouldBe("Frost");
        rows.ContainsKey("old.gone").ShouldBeFalse();
    }

    [Test]
    public void Execute_RewriteKeepsMultilineAndQuotedValuesExactly()
    {
        Directory.CreateDirectory(_localesDir);
        var value = "Line one\nLine \"two\", still two";
        File.WriteAllText(Path.Combine(_localesDir, "en.csv"),
            "Key,Value\nducky_doNothing_buff_name,\"" + value.Replace("\"", "\"\"") + "\"\n");

        var (exit, _, _) = LibTestSupport.Capture(() => UpdateLocalizationCsvLib.Execute(_context));

        exit.ShouldBe(0);
        var rows = Ducky.Sdk.Build.Localization.CsvDocument.ToDictionary(File.ReadAllText(Path.Combine(_localesDir, "en.csv")));
        rows["ducky_doNothing_buff_name"].ShouldBe(value);
    }
}
