namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>Task 5.4: ResetDeployLib removes the target mod folder (ModsDirectory first, DuckovFolder as fallback).</summary>
public class ResetDeployLibTests
{
    private string _root = null!;
    private string _mods = null!;
    private string _target = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("reset-deploy");
        _mods = Path.Combine(_root, "Game Mods");
        _target = Path.Combine(_mods, "Ducky.EntranceMod");
        Directory.CreateDirectory(_mods);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private BuildContext Context(bool isModLib = false, string? modName = "Ducky.EntranceMod", string? mods = null, string? duckov = null) =>
        LibTestSupport.NewContext(
            Path.Combine(_root, "Ducky Project"),
            modName: modName,
            isModLib: isModLib,
            modsDirectory: mods ?? _mods,
            duckovFolder: duckov);

    private void CreateOldDeployment(string folder)
    {
        LibTestSupport.WriteText(Path.Combine(folder, "Ducky.EntranceMod.dll"), "old");
        LibTestSupport.WriteText(Path.Combine(folder, "Locales", "en.csv"), "Key,Value\n");
    }

    [Test]
    public void Reset_DeletesExistingModFolderIncludingNestedFiles()
    {
        CreateOldDeployment(_target);

        var result = ResetDeployLib.Reset(Context());

        result.Success.ShouldBeTrue();
        result.DirectoryExisted.ShouldBeTrue();
        result.DirectoryDeleted.ShouldBeTrue();
        result.TargetModDir.ShouldBe(_target);
        Directory.Exists(_target).ShouldBeFalse();
        Directory.Exists(_mods).ShouldBeTrue();
    }

    [Test]
    public void Execute_ReturnsZeroWhenModFolderDoesNotExist()
    {
        var (exit, output, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(Context()));

        exit.ShouldBe(0);
        output.ShouldContain("Target deployment directory does not exist");
        Directory.Exists(_target).ShouldBeFalse();
    }

    [Test]
    public void Execute_UsesDuckovFolderModsWhenModsDirectoryIsNotSet()
    {
        var duckov = Path.Combine(_root, "Escape From Duckov");
        var target = Path.Combine(duckov, "Duckov_Data", "Mods", "Ducky.EntranceMod");
        CreateOldDeployment(target);

        var (exit, _, _) = LibTestSupport.Capture(() =>
            ResetDeployLib.Execute(Context(mods: string.Empty, duckov: duckov)));

        exit.ShouldBe(0);
        Directory.Exists(target).ShouldBeFalse();
    }

    [Test]
    public void Execute_SkipsLibraryProjectsAndKeepsDeployment()
    {
        CreateOldDeployment(_target);

        var (exit, _, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(Context(isModLib: true)));

        exit.ShouldBe(BuildScriptGlobals.SkipExitCode);
        File.Exists(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBeTrue();
    }

    [Test]
    public void Execute_SkipsWhenModNameIsMissingAndKeepsSiblingFolders()
    {
        CreateOldDeployment(_target);
        var sibling = Path.Combine(_mods, "Other Mod");
        LibTestSupport.WriteText(Path.Combine(sibling, "Other.dll"), "keep");

        var (exit, output, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(Context(modName: null)));

        exit.ShouldBe(BuildScriptGlobals.SkipExitCode);
        output.ShouldContain("ModName is not specified");
        File.Exists(Path.Combine(sibling, "Other.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBeTrue();
    }

    [Test]
    public void Execute_FailsWhenNeitherModsDirectoryNorDuckovFolderIsSet()
    {
        var (exit, _, error) = LibTestSupport.Capture(() =>
            ResetDeployLib.Execute(Context(mods: string.Empty, duckov: null)));

        exit.ShouldBe(1);
        error.ShouldContain("Reset operation failed");
    }

    [Test]
    public void Reset_ReportsDeleteErrorWhenFileInsideModFolderCannotBeRemoved()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Read-only directories are emulated with Unix file modes.");
        }

        var locked = CreateLockedFileInsideTarget();
        try
        {
            var result = ResetDeployLib.Reset(Context());

            result.Success.ShouldBeFalse();
            result.DirectoryDeleted.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.StartsWith("Failed to delete deployment directory"));
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    // Known discrepancy (see report): on a failed delete, Reset returns early (ResetDeployLib.cs:108-112) without
    // setting ExitCode, so Execute returns 0 (line 64). Remove the Ignore once ExitCode is set on that path.
    [Test]
    [Ignore("Known production discrepancy: ResetDeployLib.cs:108-112 returns with ExitCode 0 after a failed delete, so Execute returns 0.")]
    public void Execute_ReturnsFailureWhenModFolderCannotBeDeleted()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Read-only directories are emulated with Unix file modes.");
        }

        var locked = CreateLockedFileInsideTarget();
        try
        {
            var (exit, _, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(Context()));

            exit.ShouldBe(1);
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    /// <summary>
    /// Puts a file in a read-only subfolder of the target, so recursive delete fails with UnauthorizedAccessException.
    /// Returns the read-only folder so the caller can restore its mode. Assumes (not asserts) when the test runs as root.
    /// </summary>
    private string CreateLockedFileInsideTarget()
    {
        var locked = Path.Combine(_target, "locked");
        LibTestSupport.WriteText(Path.Combine(locked, "pinned.dll"), "pinned");
        File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        var stillWritable = false;
        try
        {
            File.WriteAllText(Path.Combine(locked, "probe.txt"), "probe");
            File.Delete(Path.Combine(locked, "probe.txt"));
            stillWritable = true;
        }
        catch (UnauthorizedAccessException)
        {
        }

        Assume.That(!stillWritable, "Read-only folder is still writable (running as root?).");
        return locked;
    }
}
