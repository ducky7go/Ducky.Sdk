namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Task 5.4: CollectFromModLib copies publishedFileId from the deployed mod's info.ini into the assets info.ini.
/// It reads the mod folder from DuckovFolder (not ModsDirectory_Explicit).
/// </summary>
public class CollectFromModLibTests
{
    private string _root = null!;
    private string _duckov = null!;
    private string _modDir = null!;
    private string _assetsDir = null!;
    private string _assetsInfo = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("collect-from-mod");
        _duckov = Path.Combine(_root, "Escape From Duckov");
        _modDir = Path.Combine(_duckov, "Duckov_Data", "Mods", "Ducky.EntranceMod");
        var projectDir = Path.Combine(_root, "Ducky EntranceMod");
        _assetsDir = Path.Combine(projectDir, "assets");
        _assetsInfo = Path.Combine(_assetsDir, "info.ini");
        Directory.CreateDirectory(_modDir);
        Directory.CreateDirectory(_assetsDir);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private BuildContext Context(bool deployMod = true, bool isModLib = false, string? modName = "Ducky.EntranceMod", string? duckov = null) =>
        LibTestSupport.NewContext(
            Path.GetDirectoryName(_assetsDir)!,
            modName: modName,
            assetsDir: _assetsDir,
            isModLib: isModLib,
            deployMod: deployMod,
            duckovFolder: duckov ?? _duckov);

    private void WriteModInfo(string content) => File.WriteAllText(Path.Combine(_modDir, "info.ini"), content);

    [Test]
    public void Execute_SkipsWhenDeployModIsFalse()
    {
        WriteModInfo("publishedFileId = 3456789\n");

        var (exit, output, _) = LibTestSupport.Capture(() => CollectFromModLib.Execute(Context(deployMod: false)));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        output.ShouldContain("Mod deployment is disabled");
        File.Exists(_assetsInfo).ShouldBeFalse();
    }

    [Test]
    public void CollectFromMod_SkipsLibraryProjects()
    {
        WriteModInfo("publishedFileId = 3456789\n");

        var result = CollectFromModLib.CollectFromMod(Context(isModLib: true));

        result.Skipped.ShouldBeTrue();
        result.Success.ShouldBeTrue();
        File.Exists(_assetsInfo).ShouldBeFalse();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void CollectFromMod_SkipsWhenModNameOrDuckovFolderIsMissing(bool modNameMissing, bool duckovMissing)
    {
        WriteModInfo("publishedFileId = 3456789\n");
        var context = Context(
            modName: modNameMissing ? null : "Ducky.EntranceMod",
            duckov: duckovMissing ? string.Empty : _duckov);

        var result = CollectFromModLib.CollectFromMod(context);

        result.Skipped.ShouldBeTrue();
        result.Success.ShouldBeTrue();
        File.Exists(_assetsInfo).ShouldBeFalse();
    }

    [Test]
    public void CollectFromMod_SkipsWhenDeployedModHasNoInfoIni()
    {
        var result = CollectFromModLib.CollectFromMod(Context());

        result.Skipped.ShouldBeTrue();
        result.Success.ShouldBeTrue();
        File.Exists(_assetsInfo).ShouldBeFalse();
    }

    [Test]
    public void CollectFromMod_SkipsWhenDeployedModInfoHasNoPublishedFileId()
    {
        WriteModInfo("name = Ducky.EntranceMod\n");

        var result = CollectFromModLib.CollectFromMod(Context());

        result.Skipped.ShouldBeTrue();
        result.PublishedFileId.ShouldBe(string.Empty);
        File.Exists(_assetsInfo).ShouldBeFalse();
    }

    [Test]
    public void Execute_CreatesAssetsInfoWithPublishedFileIdWhenMissing()
    {
        WriteModInfo("name = Ducky.EntranceMod\npublishedFileId = 3456789\n");

        var (exit, _, _) = LibTestSupport.Capture(() => CollectFromModLib.Execute(Context()));

        exit.ShouldBe(0);
        File.ReadAllLines(_assetsInfo).ShouldBe(new[] { "publishedFileId = 3456789" });
    }

    [Test]
    public void Execute_AppendsPublishedFileIdAfterExistingAssetsLines()
    {
        WriteModInfo("publishedFileId = 3456789\n");
        File.WriteAllText(_assetsInfo, "name = Ducky.EntranceMod\ndisplayName = Shown");

        var (exit, _, _) = LibTestSupport.Capture(() => CollectFromModLib.Execute(Context()));

        exit.ShouldBe(0);
        File.ReadAllLines(_assetsInfo).ShouldBe(new[]
        {
            "name = Ducky.EntranceMod",
            "displayName = Shown",
            "publishedFileId = 3456789",
        });
    }

    [Test]
    public void Execute_KeepsExistingPublishedFileIdInAssetsInfo()
    {
        WriteModInfo("publishedFileId = 3456789\n");
        File.WriteAllText(_assetsInfo, "name = Ducky.EntranceMod\npublishedFileId = 111\n");
        var before = File.ReadAllText(_assetsInfo);

        var result = CollectFromModLib.CollectFromMod(Context());

        result.Skipped.ShouldBeTrue();
        File.ReadAllText(_assetsInfo).ShouldBe(before);
    }

    [Test]
    public void Execute_FailsWhenAssetsDirectoryIsMissing()
    {
        WriteModInfo("publishedFileId = 3456789\n");
        Directory.Delete(_assetsDir);

        var (exit, _, error) = LibTestSupport.Capture(() => CollectFromModLib.Execute(Context()));

        exit.ShouldBe(1);
        error.ShouldContain("Assets directory not found");
    }
}
