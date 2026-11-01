namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Consecutive builds through the real runner: the pre-phase localization copy, then BuildRunner.RunPost.
/// A no-change build keeps the deployed copy outputs and skips their steps, files the last deploy wrote and this
/// build no longer plans are removed, files the SDK did not write are kept, and a changed input reruns its copy.
/// </summary>
public class DeployResetRerunTests
{
    private const string ModName = "Ducky Rerun Mod";

    private string _root = null!;
    private string _projectDir = null!;
    private string _mods = null!;
    private string _target = null!;
    private string _binDir = null!;
    private string _sourceDeps = null!;
    private string _sourceAssets = null!;
    private string _jsonPath = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("reset-rerun");
        _projectDir = Path.Combine(_root, "Samples Dir", ModName);
        _mods = Path.Combine(_root, "Game Mods");
        _target = Path.Combine(_mods, ModName);
        _binDir = Path.Combine(_projectDir, "bin", "Debug", "netstandard2.1");
        _sourceDeps = Path.Combine(_projectDir, "Dependency");
        _sourceAssets = Path.Combine(_projectDir, "assets");
        _jsonPath = Path.Combine(_projectDir, "obj", "ducky-build-context.json");
        Directory.CreateDirectory(_mods);
        Directory.CreateDirectory(Path.Combine(_projectDir, "obj"));
        LibTestSupport.WriteText(Path.Combine(_binDir, ModName + ".dll"), "dll-v1");
        LibTestSupport.WriteText(Path.Combine(_sourceAssets, "info.ini"), "name = Ducky Rerun Mod\n");
        LibTestSupport.WriteText(Path.Combine(_sourceAssets, "Locales", "en.csv"), "Key,Value\n");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "Serilog.dll"), "serilog");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "R3.dll"), "r3");
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private BuildContext Context(string? localizationDir = null) => LibTestSupport.NewContext(
        _projectDir, modName: ModName, enableIlRepack: false, modsDirectory: _mods, localizationAssetsDir: localizationDir);

    /// <summary>
    /// One build: the pre-phase localization copy (into the mod folder) and then the post phase. Returns all console output.
    /// </summary>
    private (int Exit, string Out) Build(bool withLocalization = true)
    {
        var (exit, output, _) = LibTestSupport.Capture(() =>
        {
            if (withLocalization)
            {
                CopyLocalizationAssetsLib.Execute(Context(_target));
            }

            File.WriteAllText(_jsonPath, Context(withLocalization ? _target : null).ToJson());
            return BuildRunner.RunPost(_jsonPath, (_, _) => Program.ExitSuccess);
        });

        return (exit, output);
    }

    private string ManifestPath => Path.Combine(_projectDir, "obj", "ducky-deployed-files.txt");

    [Test]
    public void FirstBuild_DeploysTheFilesAndRecordsTheManifest()
    {
        var first = Build();

        first.Exit.ShouldBe(Program.ExitSuccess);
        first.Out.ShouldContain("0 stale file(s) removed");
        File.Exists(Path.Combine(_target, ModName + ".dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "Dependency", "Serilog.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "Locales", "en.csv")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "info.ini")).ShouldBeTrue();
        File.ReadAllText(ManifestPath).ShouldContain("Dependency/Serilog.dll");
    }

    [Test]
    public void SecondBuildWithNoChanges_SkipsTheCopySteps_AndRemovesNothing()
    {
        Build();

        var second = Build();

        second.Exit.ShouldBe(Program.ExitSuccess);
        second.Out.ShouldContain("Localization assets are up to date");
        second.Out.ShouldContain("Dependencies are up to date");
        second.Out.ShouldContain("0 stale file(s) removed");
        File.Exists(Path.Combine(_target, "Dependency", "Serilog.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "Dependency", "R3.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "Locales", "en.csv")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "info.ini")).ShouldBeTrue();
    }

    [Test]
    public void ResetAfterADeploy_LeavesTheCopyOutputsInPlace_AndTheDependencyStampStillMatches()
    {
        Build();

        var (resetExit, _, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(Context()));
        // The same context as the builds, so the stamp key matches.
        var (copyExit, _, _) = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(_target)));

        resetExit.ShouldBe(0);
        File.Exists(Path.Combine(_target, "Dependency", "Serilog.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "Dependency", "R3.dll")).ShouldBeTrue();
        copyExit.ShouldBe(RunnerExitCodes.SkipExitCode);
    }

    [Test]
    public void StaleFileFromThePreviousDeploy_IsRemovedWhenItsSourceIsGone()
    {
        Build();
        File.Delete(Path.Combine(_sourceAssets, "info.ini"));
        File.Delete(Path.Combine(_sourceDeps, "R3.dll"));

        var second = Build();

        second.Exit.ShouldBe(Program.ExitSuccess);
        File.Exists(Path.Combine(_target, "info.ini")).ShouldBeFalse();
        File.Exists(Path.Combine(_target, "Dependency", "R3.dll")).ShouldBeFalse();
        File.Exists(Path.Combine(_target, "Dependency", "Serilog.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, ModName + ".dll")).ShouldBeTrue();
    }

    [Test]
    public void ChangedDependency_ReRunsItsCopyAndUpdatesTheOutput()
    {
        Build();
        var source = Path.Combine(_sourceDeps, "Serilog.dll");
        File.WriteAllText(source, "serilog-v2");
        File.SetLastWriteTime(source, DateTime.Now.AddHours(1));

        var second = Build();

        second.Exit.ShouldBe(Program.ExitSuccess);
        second.Out.ShouldContain("Dependency copying completed");
        second.Out.ShouldNotContain("Dependencies are up to date");
        File.ReadAllText(Path.Combine(_target, "Dependency", "Serilog.dll")).ShouldBe("serilog-v2");
    }

    [Test]
    public void ChangedLocalizationAsset_ReRunsItsCopy()
    {
        Build();
        var source = Path.Combine(_sourceAssets, "Locales", "en.csv");
        File.WriteAllText(source, "Key,Value\nchanged,\"changed\"\n");
        File.SetLastWriteTime(source, DateTime.Now.AddHours(1));

        var second = Build();

        second.Exit.ShouldBe(Program.ExitSuccess);
        second.Out.ShouldContain("Localization asset copying completed");
        second.Out.ShouldNotContain("Localization assets are up to date");
        File.ReadAllText(Path.Combine(_target, "Locales", "en.csv")).ShouldBe("Key,Value\nchanged,\"changed\"\n");
    }

    [Test]
    public void FileTheSdkDidNotWrite_SurvivesEveryBuild()
    {
        LibTestSupport.WriteText(Path.Combine(_target, "notes.txt"), "mine");

        Build();
        var second = Build();

        second.Exit.ShouldBe(Program.ExitSuccess);
        File.ReadAllText(Path.Combine(_target, "notes.txt")).ShouldBe("mine");
    }
}
