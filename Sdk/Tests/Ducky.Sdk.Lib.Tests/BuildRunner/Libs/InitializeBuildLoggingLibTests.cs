namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>Task 5.1: InitializeBuildLoggingLib creates the build log under obj/ducky-build.</summary>
public class InitializeBuildLoggingLibTests
{
    private string _root = null!;
    private string _projectDir = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("init-logging");
        _projectDir = Path.Combine(_root, "Ducky Project");
        Directory.CreateDirectory(_projectDir);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private string LogDir => Path.Combine(_projectDir, "obj", "ducky-build");

    [Test]
    public void Execute_CreatesBuildLogUnderObjDirForProjectPathWithSpaces()
    {
        var context = LibTestSupport.NewContext(_projectDir, modName: "Ducky.EntranceMod", deployMod: true);

        var (exit, output, _) = LibTestSupport.Capture(() => InitializeBuildLoggingLib.Execute(context));

        exit.ShouldBe(0);
        var logs = Directory.GetFiles(LogDir, "build-*.log");
        logs.Length.ShouldBe(1);
        var log = File.ReadAllText(logs[0]);
        log.ShouldContain($"[INFO] [Build] Project: {_projectDir}");
        log.ShouldContain("[INFO] [Build] Configuration: Debug");
        log.ShouldContain("[INFO] [Build] Target Framework: netstandard2.1");
        log.ShouldContain("[INFO] [Build] ModName: Ducky.EntranceMod");
        log.ShouldContain("[INFO] [Build] DeployMod: True");
        log.ShouldContain("[INFO] [Build] IsModLib: False");
        output.ShouldContain($"Log file created at: {logs[0]}");
    }

    [Test]
    public void Execute_RecordsLibraryFlagsInLog()
    {
        var context = LibTestSupport.NewContext(_projectDir, modName: null, isModLib: true, deployMod: false);

        var (exit, _, _) = LibTestSupport.Capture(() => InitializeBuildLoggingLib.Execute(context));

        exit.ShouldBe(0);
        var log = File.ReadAllText(Directory.GetFiles(LogDir, "build-*.log").Single());
        log.ShouldContain("[INFO] [Build] ModName: ");
        log.ShouldContain("[INFO] [Build] IsModLib: True");
        log.ShouldContain("[INFO] [Build] DeployMod: False");
    }

    [Test]
    public void Execute_ReturnsFailureWhenObjDirectoryCannotBeCreated()
    {
        // A regular file sitting where the project folder should be makes Directory.CreateDirectory("<file>/obj") throw.
        var blocker = Path.Combine(_root, "not-a-folder");
        File.WriteAllText(blocker, "x");
        var context = LibTestSupport.NewContext(blocker);

        var (exit, output, _) = LibTestSupport.Capture(() => InitializeBuildLoggingLib.Execute(context));

        exit.ShouldBe(1);
        output.ShouldContain("[InitializeBuildLoggingLib][ERROR]");
    }
}
