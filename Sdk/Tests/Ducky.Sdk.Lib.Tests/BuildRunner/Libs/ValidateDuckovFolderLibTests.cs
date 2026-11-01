namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>Task 5.1: ValidateDuckovFolderLib covers skip, explicit-CI directories, missing folders and install warnings.</summary>
public class ValidateDuckovFolderLibTests
{
    private string _root = null!;
    private string _duckov = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("duckov-folder");
        _duckov = Path.Combine(_root, "Escape From Duckov");
        Directory.CreateDirectory(_duckov);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private string ExplicitManaged => Path.Combine(_root, "ci managed");
    private string ExplicitMods => Path.Combine(_root, "ci mods");

    [Test]
    public void Execute_SkipsLibraryProjects()
    {
        var context = LibTestSupport.NewContext(_root, modName: null, isModLib: true);

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(BuildScriptGlobals.SkipExitCode);
        output.ShouldContain("Duckov folder validation skipped (library project)");
    }

    [Test]
    public void Execute_CiEnvironmentWithExplicitDirectoriesPassesWithoutDuckovFolder()
    {
        var context = LibTestSupport.NewContext(
            _root, isCi: true, managedDirectory: ExplicitManaged, modsDirectory: ExplicitMods);

        var (exit, output, error) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(0);
        output.ShouldContain("CI Environment: True");
        output.ShouldContain("DuckovFolder not required when paths are explicitly set");
        output.ShouldContain("Duckov folder validation passed");
        error.ShouldBeEmpty();
    }

    [Test]
    public void Execute_ExplicitDirectoriesPassWithCiFlagFalseToo()
    {
        var context = LibTestSupport.NewContext(
            _root, isCi: false, managedDirectory: ExplicitManaged, modsDirectory: ExplicitMods);

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(0);
        output.ShouldContain("CI Environment: False");
        output.ShouldContain("DuckovFolder not required when paths are explicitly set");
    }

    [Test]
    public void Execute_FailsWhenNoDuckovFolderAndNoExplicitDirectories()
    {
        var context = LibTestSupport.NewContext(_root, duckovFolder: null);

        var (exit, _, error) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(1);
        error.ShouldContain("DuckovFolder is not set");
        error.ShouldContain("Duckov folder validation failed");
    }

    [Test]
    public void Execute_FailsWhenOnlyManagedDirectoryIsExplicit()
    {
        var context = LibTestSupport.NewContext(_root, duckovFolder: null, managedDirectory: ExplicitManaged);

        var (exit, _, error) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(1);
        error.ShouldContain("DuckovFolder is not set");
    }

    [Test]
    public void Execute_FailsWhenDuckovFolderDoesNotExist()
    {
        var missing = Path.Combine(_root, "not installed");
        var context = LibTestSupport.NewContext(_root, duckovFolder: missing);

        var (exit, _, error) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(1);
        error.ShouldContain($"DuckovFolder does not exist: {missing}");
    }

    [Test]
    public void Execute_PartialInstallPassesWithWarnings()
    {
        var context = LibTestSupport.NewContext(_root, duckovFolder: _duckov);

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(0);
        output.ShouldContain("Valid: True");
        output.ShouldContain("Managed directory not found");
        output.ShouldContain("Mods directory not found");
        output.ShouldContain("Steam API not found");
    }

    [Test]
    public void Execute_CompleteInstallPassesWithoutWarnings()
    {
        Directory.CreateDirectory(Path.Combine(_duckov, "Duckov_Data", "Managed"));
        Directory.CreateDirectory(Path.Combine(_duckov, "Duckov_Data", "Mods"));
        File.WriteAllText(Path.Combine(_duckov, "steam_api.dll"), "x");
        var context = LibTestSupport.NewContext(_root, duckovFolder: _duckov);

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(0);
        output.ShouldNotContain("Warnings:");
        output.ShouldContain("Duckov folder validation passed");
    }

    [Test]
    public void Execute_MissingSteamFolderIsOnlyAWarning()
    {
        var missingSteam = Path.Combine(_root, "no steam");
        var context = LibTestSupport.NewContext(_root, duckovFolder: _duckov, steamFolder: missingSteam);

        var (exit, output, _) = LibTestSupport.Capture(() => ValidateDuckovFolderLib.Execute(context));

        exit.ShouldBe(0);
        output.ShouldContain($"Steam folder does not exist: {missingSteam}");
    }
}
