namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Task 5.4: DeployModLib copies the main DLL and assets into ModsDirectory/ModName. The golden is the deployed
/// file-name list of the baseline Ducky.EntranceMod run (names only; file bytes are not compared).
/// </summary>
public class DeployModLibTests
{
    private string _root = null!;
    private string _projectDir = null!;
    private string _mods = null!;
    private string _target = null!;
    private string _binDir = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("deploy-mod");
        // The project folder name is the DLL base name, so it must be Ducky.EntranceMod.
        _projectDir = Path.Combine(_root, "Samples Dir", "Ducky.EntranceMod");
        _mods = Path.Combine(_root, "Game Mods");
        _target = Path.Combine(_mods, "Ducky.EntranceMod");
        _binDir = Path.Combine(_projectDir, "bin", "Debug", "netstandard2.1");
        Directory.CreateDirectory(_mods);
        LibTestSupport.WriteText(Path.Combine(_binDir, "Ducky.EntranceMod.dll"), "dll-v1");
        LibTestSupport.WriteText(Path.Combine(_projectDir, "assets", "info.ini"), "name = Ducky.EntranceMod\n");
        LibTestSupport.WriteText(Path.Combine(_projectDir, "assets", "preview.png"), "png-bytes");
        LibTestSupport.WriteText(Path.Combine(_projectDir, "assets", "Locales", "en.csv"), "Key,Value\n");
        LibTestSupport.WriteText(Path.Combine(_projectDir, "assets", "Locales", "zh.csv"), "Key,Value\n");
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private BuildContext Context(bool deployMod = true, bool isModLib = false, bool enableIlRepack = true,
        string? mods = null, string? duckov = null, string? modName = "Ducky.EntranceMod") =>
        LibTestSupport.NewContext(
            _projectDir,
            modName: modName,
            deployMod: deployMod,
            isModLib: isModLib,
            enableIlRepack: enableIlRepack,
            modsDirectory: mods ?? _mods,
            duckovFolder: duckov);

    private static string[] GoldenDeployedNames()
    {
        return File.ReadAllLines(LibTestSupport.Golden("EntranceMod", "deployed-files.txt"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line => line.TrimStart('.', '/'))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private string[] DeployedNames() => Directory.GetFiles(_target, "*", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(_target, path).Replace('\\', '/'))
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();

    [Test]
    public void Execute_DeployedFileNamesMatchBaselineEntranceModGolden()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => DeployModLib.Execute(Context()));

        exit.ShouldBe(0);
        DeployedNames().ShouldBe(GoldenDeployedNames());
    }

    [Test]
    public void Deploy_CopiesMainDllAsModNameAndReportsAssetCount()
    {
        var result = DeployModLib.Deploy(Context());

        result.Success.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.TargetModDir.ShouldBe(_target);
        result.AssetsCopied.ShouldBe(4);
        File.ReadAllText(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBe("dll-v1");
        File.Exists(Path.Combine(_target, "Locales", "zh.csv")).ShouldBeTrue();
    }

    [Test]
    public void Execute_ReturnsSkipCodeWhenDeployModIsFalse()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => DeployModLib.Execute(Context(deployMod: false)));

        exit.ShouldBe(BuildScriptGlobals.SkipExitCode);
        Directory.Exists(_target).ShouldBeFalse();
    }

    [Test]
    public void Execute_ReturnsSkipCodeForLibraryProjects()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => DeployModLib.Execute(Context(isModLib: true)));

        exit.ShouldBe(BuildScriptGlobals.SkipExitCode);
        Directory.Exists(_target).ShouldBeFalse();
    }

    [Test]
    public void Deploy_ReportsMissingTargetDllWithoutCopyingIt()
    {
        File.Delete(Path.Combine(_binDir, "Ducky.EntranceMod.dll"));

        var result = DeployModLib.Deploy(Context());

        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.StartsWith("Target DLL not found"));
    }

    [Test]
    public void Deploy_ReportsMissingModNameAsAnErrorAndDeploysNothing()
    {
        var result = DeployModLib.Deploy(Context(modName: null));

        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain("ModName is required for deployment");
        Directory.Exists(_target).ShouldBeFalse();
    }

    // Known discrepancy (see report): the missing-ModName branch sets Success=false and an error, but returns
    // SkipExitCode (DeployModLib.cs:163-166), so the runner counts it as skipped. Expected: failure (1).
    [Test]
    [Ignore("Known production discrepancy: DeployModLib.cs:165 returns SkipExitCode although the deployment failed.")]
    public void Execute_ReturnsFailureWhenModNameIsMissing()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => DeployModLib.Execute(Context(modName: null)));

        exit.ShouldBe(1);
    }

    // Known discrepancy (see report): Deploy records errors but never sets ExitCode (DeployModLib.cs:91 and 123),
    // so Execute (line 62) returns 0 for a failed deployment. Remove the Ignore once failures set ExitCode = 1.
    [Test]
    [Ignore("Known production discrepancy: DeployModLib.cs:91/123 never set ExitCode on failure, so Execute returns 0 (line 62).")]
    public void Execute_ReturnsFailureWhenTargetDllIsMissing()
    {
        File.Delete(Path.Combine(_binDir, "Ducky.EntranceMod.dll"));

        var (exit, _, _) = LibTestSupport.Capture(() => DeployModLib.Execute(Context()));

        exit.ShouldBe(1);
    }

    [Test]
    public void Deploy_RejectsMissingModsDirectoryAndDuckovFolder()
    {
        var result = DeployModLib.Deploy(Context(mods: string.Empty, duckov: null));

        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain("Neither ModsDirectory nor DuckovFolder is specified");
        Directory.Exists(_target).ShouldBeFalse();
    }

    [Test]
    public void Deploy_RejectsDuckovFolderThatDoesNotExist()
    {
        var missing = Path.Combine(_root, "not installed");

        var result = DeployModLib.Deploy(Context(mods: string.Empty, duckov: missing));

        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain($"DuckovFolder does not exist: {missing}");
    }

    [Test]
    public void Deploy_RedeployOverwritesPreviousFiles()
    {
        DeployModLib.Deploy(Context());
        LibTestSupport.WriteText(Path.Combine(_binDir, "Ducky.EntranceMod.dll"), "dll-v2");
        LibTestSupport.WriteText(Path.Combine(_projectDir, "assets", "info.ini"), "name = Ducky.EntranceMod\nversion = 2\n");

        var result = DeployModLib.Deploy(Context());

        result.Success.ShouldBeTrue();
        File.ReadAllText(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBe("dll-v2");
        File.ReadAllText(Path.Combine(_target, "info.ini")).ShouldContain("version = 2");
    }

    [Test]
    public void Deploy_CopiesPdbBesideMainDllWhenPresent()
    {
        LibTestSupport.WriteText(Path.Combine(_binDir, "Ducky.EntranceMod.pdb"), "pdb");

        var result = DeployModLib.Deploy(Context());

        result.Success.ShouldBeTrue();
        File.Exists(Path.Combine(_target, "Ducky.EntranceMod.pdb")).ShouldBeTrue();
    }

    [Test]
    public void Execute_WithIlRepackDisabledCopiesMainDllAndAssetsWithoutDependencies()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => DeployModLib.Execute(Context(enableIlRepack: false)));

        exit.ShouldBe(0);
        File.Exists(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "Locales", "en.csv")).ShouldBeTrue();
    }

    // Known discrepancy (see report): CopyDependencies reads targetDir/Dependency (the mods folder) instead of the
    // build output's Dependency folder (DeployModLib.cs:319-320; targetDirName is computed but unused). Nothing gets
    // deployed from bin, and a pre-existing target Dependency folder would be copied onto itself.
    [Test]
    [Ignore("Known production discrepancy: DeployModLib.cs:319-320 copies from the mod folder, not from bin/.../Dependency.")]
    public void Execute_WithIlRepackDisabledCopiesBinDependencies()
    {
        LibTestSupport.WriteText(Path.Combine(_binDir, "Dependency", "Serilog.dll"), "serilog");

        var (exit, _, _) = LibTestSupport.Capture(() => DeployModLib.Execute(Context(enableIlRepack: false)));

        exit.ShouldBe(0);
        File.Exists(Path.Combine(_target, "Dependency", "Serilog.dll")).ShouldBeTrue();
    }
}
