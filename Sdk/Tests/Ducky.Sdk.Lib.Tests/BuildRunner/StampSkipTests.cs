namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Up-to-date stamps for CopyLocalizationAssetsLib: a no-change second run skips, a changed asset, a deleted output
/// or a changed destination reruns, and a failed or unwritable stamp never changes the step's result.
/// </summary>
public class CopyLocalizationAssetsStampTests
{
    private string _root = null!;
    private string _projectDir = null!;
    private string _sourceLocales = null!;
    private string _targetA = null!;
    private string _targetB = null!;
    private string _targetC = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("stamp-locales");
        _projectDir = Path.Combine(_root, "Ducky TryHarmony");
        _sourceLocales = Path.Combine(_projectDir, "assets", "Locales");
        _targetA = Path.Combine(_root, "Mod A");
        _targetB = Path.Combine(_root, "Mod B");
        _targetC = Path.Combine(_root, "Mod C");
        Directory.CreateDirectory(_sourceLocales);
        foreach (var csv in Directory.GetFiles(LibTestSupport.Golden("TryHarmony", "Locales"), "*.csv"))
        {
            File.Copy(csv, Path.Combine(_sourceLocales, Path.GetFileName(csv)));
        }
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private string StampPath => Path.Combine(_projectDir, "obj", "ducky-stamps", "CopyLocalizationAssetsLib.stamp");

    private BuildContext Context(string localizationDir) => LibTestSupport.NewContext(
        _projectDir, modName: "Ducky.TryHarmony", localizationAssetsDir: localizationDir);

    [Test]
    public void Execute_SecondRunWithNoChangesSkipsTheCopy()
    {
        var targets = _targetA + ";" + _targetB;
        var first = LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(targets)));
        var second = LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(targets)));

        first.Exit.ShouldBe(0);
        second.Exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        second.Out.ShouldContain("up to date");
        File.Exists(StampPath).ShouldBeTrue();
    }

    [Test]
    public void Execute_ChangedAssetReRunsAndUpdatesTheOutput()
    {
        var targets = _targetA + ";" + _targetB;
        LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(targets)));
        var sourceEn = Path.Combine(_sourceLocales, "en.csv");
        File.WriteAllText(sourceEn, "Key,Value\nchanged_key,\"changed\"\n");
        File.SetLastWriteTime(sourceEn, DateTime.Now.AddHours(1));

        var (exit, _, _) = LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(targets)));

        exit.ShouldBe(0);
        File.ReadAllText(Path.Combine(_targetA, "Locales", "en.csv")).ShouldBe("Key,Value\nchanged_key,\"changed\"\n");
        File.ReadAllText(Path.Combine(_targetB, "Locales", "en.csv")).ShouldBe("Key,Value\nchanged_key,\"changed\"\n");
    }

    [Test]
    public void Execute_DeletedOutputReRunsAndRestoresIt()
    {
        var targets = _targetA + ";" + _targetB;
        LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(targets)));
        var deleted = Path.Combine(_targetA, "Locales", "en.csv");
        File.Delete(deleted);

        var (exit, _, _) = LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(targets)));

        exit.ShouldBe(0);
        File.Exists(deleted).ShouldBeTrue();
    }

    [Test]
    public void Execute_ChangedDestinationFolderReRunsAndFillsTheNewTarget()
    {
        LibTestSupport.Capture(() => CopyLocalizationAssetsLib.Execute(Context(_targetA + ";" + _targetB)));

        var (exit, _, _) = LibTestSupport.Capture(() =>
            CopyLocalizationAssetsLib.Execute(Context(_targetA + ";" + _targetC)));

        exit.ShouldBe(0);
        Directory.GetFiles(Path.Combine(_targetC, "Locales"), "*.csv").Length.ShouldBe(10);
    }

    [Test]
    public void Execute_UnwritableStampDoesNotFailTheStep()
    {
        // A regular file where the obj/ducky-stamps folder should be makes the stamp write fail.
        Directory.CreateDirectory(Path.Combine(_projectDir, "obj"));
        File.WriteAllText(Path.Combine(_projectDir, "obj", "ducky-stamps"), "not a folder");

        var (exit, output, _) = LibTestSupport.Capture(() =>
            CopyLocalizationAssetsLib.Execute(Context(_targetA)));

        exit.ShouldBe(0);
        output.ShouldContain("Could not write the up-to-date stamp");
        Directory.GetFiles(Path.Combine(_targetA, "Locales"), "*.csv").Length.ShouldBe(10);
    }

    [Test]
    public void CopyAssets_FailedRunWritesNoStamp()
    {
        // A regular file where the target folder should be makes Directory.CreateDirectory throw.
        var blocker = Path.Combine(_root, "blocked target");
        File.WriteAllText(blocker, "x");

        var result = CopyLocalizationAssetsLib.CopyAssets(Context(blocker));

        result.Success.ShouldBeFalse();
        File.Exists(StampPath).ShouldBeFalse();
    }
}

/// <summary>
/// Up-to-date stamps for CopyDependenciesLib. Mirrors the localization tests for Dependency/*.dll.
/// </summary>
public class CopyDependenciesStampTests
{
    private string _root = null!;
    private string _projectDir = null!;
    private string _mods = null!;
    private string _sourceDeps = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("stamp-deps");
        _projectDir = Path.Combine(_root, "Ducky EntranceMod");
        _mods = Path.Combine(_root, "Game Mods");
        _sourceDeps = Path.Combine(_projectDir, "Dependency");
        Directory.CreateDirectory(_mods);
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "Serilog.dll"), "serilog");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "R3.dll"), "r3");
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private string StampPath => Path.Combine(_projectDir, "obj", "ducky-stamps", "CopyDependenciesLib.stamp");

    private string TargetDeps(string mods) => Path.Combine(mods, "Ducky.EntranceMod", "Dependency");

    private BuildContext Context(string mods) =>
        LibTestSupport.NewContext(_projectDir, modsDirectory: mods);

    [Test]
    public void Execute_SecondRunWithNoChangesSkipsTheCopy()
    {
        var first = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(_mods)));
        var second = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(_mods)));

        first.Exit.ShouldBe(0);
        second.Exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        second.Out.ShouldContain("up to date");
        Directory.GetFiles(TargetDeps(_mods)).Length.ShouldBe(2);
    }

    [Test]
    public void Execute_ChangedDependencyDllReRunsAndUpdatesTheOutput()
    {
        LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(_mods)));
        var source = Path.Combine(_sourceDeps, "Serilog.dll");
        File.WriteAllText(source, "serilog-v2");
        File.SetLastWriteTime(source, DateTime.Now.AddHours(1));

        var (exit, _, _) = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(_mods)));

        exit.ShouldBe(0);
        File.ReadAllText(Path.Combine(TargetDeps(_mods), "Serilog.dll")).ShouldBe("serilog-v2");
    }

    [Test]
    public void Execute_DeletedOutputReRunsAndRestoresIt()
    {
        LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(_mods)));
        var deleted = Path.Combine(TargetDeps(_mods), "R3.dll");
        File.Delete(deleted);

        var (exit, _, _) = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(_mods)));

        exit.ShouldBe(0);
        File.Exists(deleted).ShouldBeTrue();
    }

    [Test]
    public void Execute_ChangedDestinationReRunsAndFillsTheNewFolder()
    {
        LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(_mods)));
        var otherMods = Path.Combine(_root, "Other Mods");
        Directory.CreateDirectory(otherMods);

        var (exit, _, _) = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(otherMods)));

        exit.ShouldBe(0);
        Directory.GetFiles(TargetDeps(otherMods)).Length.ShouldBe(2);
    }

    [Test]
    public void CopyDependencies_FailedRunWritesNoStamp()
    {
        // A regular file where the mods folder should be makes the destination folder creation throw.
        var blocker = Path.Combine(_root, "blocked mods");
        File.WriteAllText(blocker, "x");

        var (exit, _, _) = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(blocker)));

        exit.ShouldBe(1);
        File.Exists(StampPath).ShouldBeFalse();
    }
}
