namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Task 5.2: CopyLocalizationAssetsLib copies assets/Locales into every LocalizationAssetsDir/Locales target.
/// Golden: the baseline Ducky.TryHarmony Locales CSVs are used as the source tree.
/// </summary>
public class CopyLocalizationAssetsLibTests
{
    private string _root = null!;
    private string _projectDir = null!;
    private string _sourceLocales = null!;
    private string _targetA = null!;
    private string _targetB = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("copy-locales");
        _projectDir = Path.Combine(_root, "Ducky TryHarmony");
        _sourceLocales = Path.Combine(_projectDir, "assets", "Locales");
        _targetA = Path.Combine(_root, "Mod A");
        _targetB = Path.Combine(_root, "Mod B");
        Directory.CreateDirectory(_sourceLocales);
        foreach (var csv in Directory.GetFiles(LibTestSupport.Golden("TryHarmony", "Locales"), "*.csv"))
        {
            File.Copy(csv, Path.Combine(_sourceLocales, Path.GetFileName(csv)));
        }
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private string TwoTargets => _targetA + ";" + _targetB;

    private BuildContext Context(string? localizationDir) => LibTestSupport.NewContext(
        _projectDir, modName: "Ducky.TryHarmony", localizationAssetsDir: localizationDir);

    private void AssertTargetMatchesGolden(string target)
    {
        var locales = Path.Combine(target, "Locales");
        foreach (var csv in Directory.GetFiles(LibTestSupport.Golden("TryHarmony", "Locales"), "*.csv"))
        {
            var name = Path.GetFileName(csv);
            var copied = LibTestSupport.NormalizeNewlines(File.ReadAllText(Path.Combine(locales, name)));
            copied.ShouldBe(LibTestSupport.NormalizeNewlines(File.ReadAllText(csv)), customMessage: name);
        }
    }

    [Test]
    public void CopyAssets_CopiesEveryLocaleFileToEachTargetOnFirstRun()
    {
        var result = CopyLocalizationAssetsLib.CopyAssets(Context(TwoTargets));

        result.Success.ShouldBeTrue();
        result.FilesCopied.ShouldBe(20);
        result.FilesSkipped.ShouldBe(0);
        AssertTargetMatchesGolden(_targetA);
        AssertTargetMatchesGolden(_targetB);
    }

    [Test]
    public void Execute_ReturnsZeroAndCopiesGoldenLocalesToTargets()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(TwoTargets)));

        exit.ShouldBe(0);
        AssertTargetMatchesGolden(_targetA);
        AssertTargetMatchesGolden(_targetB);
    }

    [Test]
    public void CopyAssets_SkipsUnchangedFilesOnSecondRun()
    {
        CopyLocalizationAssetsLib.CopyAssets(Context(TwoTargets));

        var result = CopyLocalizationAssetsLib.CopyAssets(Context(TwoTargets));

        result.FilesCopied.ShouldBe(0);
        result.FilesSkipped.ShouldBe(20);
    }

    [Test]
    public void CopyAssets_RecopiesSourceFileWhenSourceIsNewer()
    {
        CopyLocalizationAssetsLib.CopyAssets(Context(TwoTargets));
        var sourceEn = Path.Combine(_sourceLocales, "en.csv");
        File.WriteAllText(sourceEn, "Key,Value\nupdated_key,\"new\"\n");
        File.SetLastWriteTime(sourceEn, DateTime.Now.AddHours(1));

        var result = CopyLocalizationAssetsLib.CopyAssets(Context(TwoTargets));

        result.FilesCopied.ShouldBe(2);
        File.ReadAllText(Path.Combine(_targetA, "Locales", "en.csv")).ShouldBe("Key,Value\nupdated_key,\"new\"\n");
        File.ReadAllText(Path.Combine(_targetB, "Locales", "en.csv")).ShouldBe("Key,Value\nupdated_key,\"new\"\n");
    }

    [Test]
    public void Execute_ReturnsSkipCodeWhenNoLocalizationAssetsDirIsConfigured()
    {
        var (exit, output, _) = LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(null)));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        output.ShouldContain("LocalizationAssetsDir not specified");
        Directory.Exists(_targetA).ShouldBeFalse();
    }

    [Test]
    public void Execute_ReturnsZeroAndCreatesNothingWhenProjectHasNoLocalesFolder()
    {
        Directory.Delete(_sourceLocales, recursive: true);

        var (exit, _, _) = LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(TwoTargets)));

        exit.ShouldBe(0);
        Directory.Exists(_targetA).ShouldBeFalse();
        Directory.Exists(_targetB).ShouldBeFalse();
    }

    [Test]
    public void CopyAssets_CopiesNothingWhenTargetIsTheSourceLocalesFolder()
    {
        var sourceAsTarget = Path.Combine("assets", "Locales");
        var result = CopyLocalizationAssetsLib.CopyAssets(Context(sourceAsTarget));

        // The same-folder skip reports Success but leaves ExitCode at 0 (see report); only the copy count is asserted here.
        result.Success.ShouldBeTrue();
        result.FilesCopied.ShouldBe(0);
        Directory.GetFiles(_sourceLocales, "*.csv").Length.ShouldBe(10);
    }

    [Test]
    public void CopyAssets_ReportsFailureWhenTargetCannotBeCreated()
    {
        // A regular file where the target folder should be makes Directory.CreateDirectory throw.
        var blocker = Path.Combine(_root, "blocked target");
        File.WriteAllText(blocker, "x");

        var result = CopyLocalizationAssetsLib.CopyAssets(Context(blocker));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Asset copying error");
    }

    // Fixed (was a known discrepancy): CopyAssets sets ExitCode = 1 in its catch block, so Execute returns 1.
    [Test]
    public void Execute_ReturnsFailureWhenTargetCannotBeCreated()
    {
        var blocker = Path.Combine(_root, "blocked target");
        File.WriteAllText(blocker, "x");

        var (exit, _, _) = LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(blocker)));

        exit.ShouldBe(1);
    }
}
