using System.Text;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>Task 5.3: EnsureInfoIniLib creates info.ini and keeps its name line in sync with ModName.</summary>
public class EnsureInfoIniLibTests
{
    private string _root = null!;
    private string _assetsDir = null!;
    private string _infoPath = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("info-ini");
        var projectDir = Path.Combine(_root, "Ducky TryHarmony");
        _assetsDir = Path.Combine(projectDir, "assets");
        _infoPath = Path.Combine(_assetsDir, "info.ini");
        Directory.CreateDirectory(projectDir);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private BuildContext Context(string? modName = "Ducky.TryHarmony", bool isModLib = false) =>
        LibTestSupport.NewContext(Path.GetDirectoryName(_assetsDir)!, modName: modName, assetsDir: _assetsDir, isModLib: isModLib);

    [Test]
    public void Execute_SkipsWhenModNameIsMissing()
    {
        var (exit, output, _) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context(modName: null)));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        output.ShouldContain("ModName not specified");
        Directory.Exists(_assetsDir).ShouldBeFalse();
    }

    [Test]
    public void Execute_SkipsLibraryProjects()
    {
        var (exit, output, _) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context(isModLib: true)));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        output.ShouldContain("IsModLib is true");
        File.Exists(_infoPath).ShouldBeFalse();
    }

    [Test]
    public void Execute_CreatesMissingAssetsFolderAndInfoIniWithBasicMetadata()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context()));

        exit.ShouldBe(0);
        File.ReadAllLines(_infoPath).ShouldBe(new[]
        {
            "name = Ducky.TryHarmony",
            "displayName = Ducky.TryHarmony",
            "description = A mod for Escape from Duckov",
        });
    }

    [Test]
    public void Execute_CreatedInfoIniMatchesBaselineGoldenBytes()
    {
        LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context()));

        // Compare text including the UTF-8 BOM the baseline file carries; newlines normalized for Windows runs.
        var created = LibTestSupport.NormalizeNewlines(Encoding.UTF8.GetString(File.ReadAllBytes(_infoPath)));
        var golden = LibTestSupport.NormalizeNewlines(Encoding.UTF8.GetString(
            File.ReadAllBytes(LibTestSupport.Golden("TryHarmony", "info.ini"))));
        created.ShouldBe(golden);
    }

    [Test]
    public void Execute_LeavesUnchangedFileWithItsTimestampWhenNameAlreadyMatches()
    {
        LibTestSupport.WriteText(_infoPath, "name = Ducky.TryHarmony\ndisplayName = Ducky.TryHarmony\n");
        var fixedTime = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(_infoPath, fixedTime);

        var (exit, output, _) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context()));

        exit.ShouldBe(0);
        output.ShouldContain("No changes needed");
        File.GetLastWriteTimeUtc(_infoPath).ShouldBe(fixedTime);
    }

    [Test]
    public void Execute_BaselineInfoIniIsUnchangedWhenNameMatches()
    {
        LibTestSupport.WriteText(_infoPath, File.ReadAllText(LibTestSupport.Golden("TryHarmony", "info.ini")));
        var before = File.ReadAllBytes(_infoPath);
        var fixedTime = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(_infoPath, fixedTime);

        var (exit, _, _) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context()));

        exit.ShouldBe(0);
        File.ReadAllBytes(_infoPath).ShouldBe(before);
        File.GetLastWriteTimeUtc(_infoPath).ShouldBe(fixedTime);
    }

    [Test]
    public void Execute_RewritesNameLineWhenItDiffersFromModName()
    {
        LibTestSupport.WriteText(_infoPath, "name = Old Name\ndisplayName = Old Name\n");

        var (exit, output, _) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context()));

        exit.ShouldBe(0);
        output.ShouldContain("Name mismatch found");
        File.ReadAllLines(_infoPath).ShouldBe(new[] { "name = Ducky.TryHarmony", "displayName = Old Name" });
    }

    [Test]
    public void Execute_AppendsNameLineWhenInfoIniHasNone()
    {
        LibTestSupport.WriteText(_infoPath, "displayName = Shown\ndescription = Text\n");

        var (exit, _, _) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context()));

        exit.ShouldBe(0);
        File.ReadAllLines(_infoPath).ShouldBe(new[]
        {
            "displayName = Shown",
            "description = Text",
            "name = Ducky.TryHarmony",
        });
    }

    [Test]
    public void Execute_LeavesMalformedNameLineAndAppendsValidNameLine()
    {
        // "name Ducky.TryHarmony" has no '=', so the name regex does not match it.
        LibTestSupport.WriteText(_infoPath, "name Ducky.TryHarmony\n");

        var (exit, _, _) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context()));

        exit.ShouldBe(0);
        File.ReadAllLines(_infoPath).ShouldBe(new[] { "name Ducky.TryHarmony", "name = Ducky.TryHarmony" });
    }

    [Test]
    public void Execute_FailsWhenInfoIniPathCannotBeWritten()
    {
        // A directory named info.ini: File.Exists is false, so the create path tries to write over a folder.
        Directory.CreateDirectory(_infoPath);

        var (exit, _, error) = LibTestSupport.Capture(() => EnsureInfoIniLib.Execute(Context()));

        exit.ShouldBe(1);
        error.ShouldContain("Failed to create new info.ini");
    }
}
