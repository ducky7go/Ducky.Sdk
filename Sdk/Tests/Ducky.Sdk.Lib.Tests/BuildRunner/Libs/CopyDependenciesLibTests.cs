namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>Task 5.4: CopyDependenciesLib copies project Dependency/*.dll into ModsDirectory/ModName/Dependency when ILRepack is off.</summary>
public class CopyDependenciesLibTests
{
    private string _root = null!;
    private string _projectDir = null!;
    private string _mods = null!;
    private string _sourceDeps = null!;
    private string _targetDeps = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("copy-deps");
        _projectDir = Path.Combine(_root, "Ducky EntranceMod");
        _mods = Path.Combine(_root, "Game Mods");
        _sourceDeps = Path.Combine(_projectDir, "Dependency");
        _targetDeps = Path.Combine(_mods, "Ducky.EntranceMod", "Dependency");
        Directory.CreateDirectory(_mods);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private BuildContext Context(bool enableIlRepack = false, bool isModLib = false) =>
        LibTestSupport.NewContext(_projectDir, enableIlRepack: enableIlRepack, isModLib: isModLib, modsDirectory: _mods);

    private void WriteDependencyFolder()
    {
        // Project dependencies, plus names that the filter must drop.
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "Serilog.dll"), "serilog");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "R3.dll"), "r3");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "System.Text.Json.dll"), "excluded");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "Microsoft.Bcl.AsyncInterfaces.dll"), "excluded");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "0Harmony.dll"), "excluded");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "UnityEngine.dll"), "excluded");
        LibTestSupport.WriteText(Path.Combine(_sourceDeps, "notes.txt"), "not a dll");
    }

    [Test]
    public void Execute_SkipsWhenIlRepackIsEnabled()
    {
        WriteDependencyFolder();

        var (exit, output, _) = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(enableIlRepack: true)));

        exit.ShouldBe(BuildScriptGlobals.SkipExitCode);
        output.ShouldContain("ILRepack is enabled");
        Directory.Exists(_targetDeps).ShouldBeFalse();
    }

    [Test]
    public void Execute_SkipsLibraryProjects()
    {
        WriteDependencyFolder();

        var (exit, output, _) = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context(isModLib: true)));

        exit.ShouldBe(BuildScriptGlobals.SkipExitCode);
        output.ShouldContain("IsModLib=true");
        Directory.Exists(_targetDeps).ShouldBeFalse();
    }

    [Test]
    public void CopyDependencies_CopiesOnlyNonSystemDllsIntoModDependencyFolder()
    {
        WriteDependencyFolder();

        var result = CopyDependenciesLib.CopyDependencies(Context());

        result.Success.ShouldBeTrue();
        result.DependenciesCopied.ShouldBe(2);
        Directory.GetFiles(_targetDeps).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal)
            .ShouldBe(new[] { "R3.dll", "Serilog.dll" });
    }

    [Test]
    public void Execute_ReturnsZeroAndCreatesNothingWhenProjectHasNoDependencyFolder()
    {
        var (exit, output, _) = LibTestSupport.Capture(() => CopyDependenciesLib.Execute(Context()));

        exit.ShouldBe(0);
        output.ShouldContain("No Dependency directory found");
        Directory.Exists(_targetDeps).ShouldBeFalse();
    }

    [Test]
    public void CopyDependencies_SkipsUnchangedFilesOnSecondRun()
    {
        WriteDependencyFolder();
        CopyDependenciesLib.CopyDependencies(Context());

        var result = CopyDependenciesLib.CopyDependencies(Context());

        result.DependenciesCopied.ShouldBe(0);
        result.SkippedFiles.Count.ShouldBe(2);
    }

    [Test]
    public void CopyDependencies_RecopiesWhenSourceIsNewer()
    {
        WriteDependencyFolder();
        CopyDependenciesLib.CopyDependencies(Context());
        var source = Path.Combine(_sourceDeps, "Serilog.dll");
        File.WriteAllText(source, "serilog-v2");
        File.SetLastWriteTime(source, DateTime.Now.AddHours(1));

        var result = CopyDependenciesLib.CopyDependencies(Context());

        result.DependenciesCopied.ShouldBe(1);
        File.ReadAllText(Path.Combine(_targetDeps, "Serilog.dll")).ShouldBe("serilog-v2");
    }
}
