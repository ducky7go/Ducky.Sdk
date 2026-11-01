namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// ResetDeployLib removes only the stale files of the last successful deploy (listed in the deployed-files manifest).
/// It never deletes the mod folder, files the SDK did not write, or files this build still plans to write.
/// </summary>
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

    /// <summary>Writes files of a previous deploy and the manifest that lists them.</summary>
    private static void CreateOldDeployment(BuildContext context, string folder, params string[] relativeFiles)
    {
        foreach (var relative in relativeFiles)
        {
            LibTestSupport.WriteText(Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar)), "old");
        }

        DeployManifest.WriteEntries(context, folder, relativeFiles);
    }

    [Test]
    public void Reset_KeepsFilesTheSdkDidNotWriteWhenNoManifestExists()
    {
        LibTestSupport.WriteText(Path.Combine(_target, "Ducky.EntranceMod.dll"), "old");
        LibTestSupport.WriteText(Path.Combine(_target, "notes.txt"), "mine");

        var result = ResetDeployLib.Reset(Context());

        result.Success.ShouldBeTrue();
        result.DirectoryExisted.ShouldBeTrue();
        result.RemovedFiles.ShouldBeEmpty();
        result.TargetModDir.ShouldBe(_target);
        File.Exists(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(_target, "notes.txt")).ShouldBeTrue();
    }

    [Test]
    public void Reset_RemovesOnlyTheStaleFilesOfTheLastDeploy()
    {
        var context = Context();
        CreateOldDeployment(context, _target, "Ducky.EntranceMod.dll", "Locales/en.csv", "Dependency/Old.dll");
        LibTestSupport.WriteText(Path.Combine(_target, "notes.txt"), "mine");

        var result = ResetDeployLib.Reset(context);

        result.Success.ShouldBeTrue();
        result.RemovedFiles.Count.ShouldBe(3);
        File.Exists(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBeFalse();
        File.Exists(Path.Combine(_target, "Locales", "en.csv")).ShouldBeFalse();
        Directory.Exists(Path.Combine(_target, "Locales")).ShouldBeFalse("an emptied folder is removed");
        Directory.Exists(Path.Combine(_target, "Dependency")).ShouldBeFalse("an emptied folder is removed");
        Directory.Exists(_target).ShouldBeTrue("the mod folder itself is never removed");
        File.Exists(Path.Combine(_target, "notes.txt")).ShouldBeTrue();
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
        var context = Context(mods: string.Empty, duckov: duckov);
        CreateOldDeployment(context, target, "Ducky.EntranceMod.dll");

        var (exit, _, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(context));

        exit.ShouldBe(0);
        File.Exists(Path.Combine(target, "Ducky.EntranceMod.dll")).ShouldBeFalse();
        Directory.Exists(target).ShouldBeTrue();
    }

    [Test]
    public void Execute_SkipsLibraryProjectsAndKeepsDeployment()
    {
        LibTestSupport.WriteText(Path.Combine(_target, "Ducky.EntranceMod.dll"), "old");

        var (exit, _, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(Context(isModLib: true)));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        File.Exists(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBeTrue();
    }

    [Test]
    public void Execute_SkipsWhenModNameIsMissingAndKeepsSiblingFolders()
    {
        LibTestSupport.WriteText(Path.Combine(_target, "Ducky.EntranceMod.dll"), "old");
        var sibling = Path.Combine(_mods, "Other Mod");
        LibTestSupport.WriteText(Path.Combine(sibling, "Other.dll"), "keep");

        var (exit, output, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(Context(modName: null)));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
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
    public void Reset_IgnoresAManifestThatBelongsToAnotherFolder()
    {
        var context = Context();
        CreateOldDeployment(context, Path.Combine(_mods, "Other Mod"), "Ducky.EntranceMod.dll");
        LibTestSupport.WriteText(Path.Combine(_target, "Ducky.EntranceMod.dll"), "unrelated");

        var result = ResetDeployLib.Reset(context);

        result.Success.ShouldBeTrue();
        result.RemovedFiles.ShouldBeEmpty();
        File.Exists(Path.Combine(_target, "Ducky.EntranceMod.dll")).ShouldBeTrue();
    }

    [Test]
    public void Reset_ReportsDeleteErrorWhenStaleFileCannotBeRemoved()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Read-only directories are emulated with Unix file modes.");
        }

        var context = Context();
        var locked = CreateLockedStaleFile(context);
        try
        {
            var result = ResetDeployLib.Reset(context);

            result.Success.ShouldBeFalse();
            result.RemovedFiles.ShouldBeEmpty();
            result.Errors.ShouldContain(e => e.StartsWith("Failed to delete stale file"));
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    // Fixed (was a known discrepancy): a failed delete now sets ExitCode 1 (ResetDeployLib.Reset), so Execute returns 1.
    [Test]
    public void Execute_ReturnsFailureWhenStaleFileCannotBeDeleted()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Read-only directories are emulated with Unix file modes.");
        }

        var context = Context();
        var locked = CreateLockedStaleFile(context);
        try
        {
            var (exit, _, _) = LibTestSupport.Capture(() => ResetDeployLib.Execute(context));

            exit.ShouldBe(1);
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    /// <summary>
    /// Lists a file in a read-only subfolder of the target as a stale entry of the last deploy, so recursive
    /// delete of that file fails with UnauthorizedAccessException. Returns the read-only folder so the caller can
    /// restore its mode. Assumes (not asserts) when the test runs as root.
    /// </summary>
    private string CreateLockedStaleFile(BuildContext context)
    {
        CreateOldDeployment(context, _target, "locked/pinned.dll");
        var locked = Path.Combine(_target, "locked");
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
