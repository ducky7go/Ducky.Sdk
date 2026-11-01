namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>Task 5.1: ResolveSdkPropertiesLib resolves paths, localization dirs, game dependencies and configuration validity.</summary>
public class ResolveSdkPropertiesLibTests
{
    private string _root = null!;
    private string _projectDir = null!;
    private string _duckov = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("resolve-sdk");
        _projectDir = Path.Combine(_root, "Ducky Project");
        _duckov = Path.Combine(_root, "Escape From Duckov");
        Directory.CreateDirectory(_projectDir);
        Directory.CreateDirectory(_duckov);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    [Test]
    public void Resolve_ModLibWithoutLocalizationDirSkipsLocalizationAndValidatesAsValid()
    {
        var context = LibTestSupport.NewContext(_projectDir, modName: null, isModLib: true, localizationAssetsDir: null);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.ShouldProcessLocalization.ShouldBeFalse();
        props.HasValidConfiguration.ShouldBeTrue();
        props.ValidationErrors.ShouldBeEmpty();
        LibTestSupport.Capture(() => ResolveSdkPropertiesLib.Execute(context)).Exit.ShouldBe(0);
    }

    [Test]
    public void Resolve_SemicolonSeparatedLocalizationDirsUsesFirstAsPrimary()
    {
        var first = Path.Combine(_root, "first loc");
        var second = Path.Combine(_root, "second loc");
        var context = LibTestSupport.NewContext(
            _projectDir, isModLib: true, localizationAssetsDir: $"{first} ; {second}");

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.ShouldProcessLocalization.ShouldBeTrue();
        props.HasMultipleLocalizationDirs.ShouldBeTrue();
        props.PrimaryLocalizationDir.ShouldBe(first);
        props.EffectiveLocalizationAssetsDir.ShouldBe($"{first} ; {second}");
    }

    [Test]
    public void Resolve_FallsBackToAssetsDirWhenNoLocalizationDirIsGiven()
    {
        var assets = Path.Combine(_projectDir, "assets");
        var context = LibTestSupport.NewContext(_projectDir, deployMod: false, assetsDir: assets);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.ShouldProcessLocalization.ShouldBeTrue();
        props.EffectiveLocalizationAssetsDir.ShouldBe(assets);
        props.PrimaryLocalizationDir.ShouldBe(assets);
        props.HasMultipleLocalizationDirs.ShouldBeFalse();
    }

    [Test]
    public void Resolve_DerivesManagedAndModsDirectoriesFromDuckovFolder()
    {
        var context = LibTestSupport.NewContext(_projectDir, duckovFolder: _duckov);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.ManagedDirectory.ShouldBe(Path.Combine(_duckov, "Duckov_Data", "Managed"));
        props.ModsDirectory.ShouldBe(Path.Combine(_duckov, "Duckov_Data", "Mods"));
        props.HasExplicitManagedDirectory.ShouldBeFalse();
        props.HasExplicitModsDirectory.ShouldBeFalse();
        props.HasValidConfiguration.ShouldBeTrue();
        LibTestSupport.Capture(() => ResolveSdkPropertiesLib.Execute(context)).Exit.ShouldBe(0);
    }

    [Test]
    public void Resolve_ExplicitManagedAndModsDirectoriesMakeDuckovFolderOptional()
    {
        var managed = Path.Combine(_root, "ci managed");
        var mods = Path.Combine(_root, "ci mods");
        var context = LibTestSupport.NewContext(
            _projectDir, isCi: true, managedDirectory: managed, modsDirectory: mods);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.HasExplicitManagedDirectory.ShouldBeTrue();
        props.HasExplicitModsDirectory.ShouldBeTrue();
        props.ManagedDirectory.ShouldBe(managed);
        props.ModsDirectory.ShouldBe(mods);
        props.DuckovFolder.ShouldBe(string.Empty);
        props.HasValidConfiguration.ShouldBeTrue();
        LibTestSupport.Capture(() => ResolveSdkPropertiesLib.Execute(context)).Exit.ShouldBe(0);
    }

    [Test]
    public void Resolve_DeploymentWithoutDuckovFolderOrExplicitDirectoriesIsInvalid()
    {
        var context = LibTestSupport.NewContext(_projectDir, duckovFolder: null);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.HasValidConfiguration.ShouldBeFalse();
        props.ValidationErrors.ShouldContain(e => e.StartsWith("DuckovFolder is required for mod deployment"));
        var (exit, _, error) = LibTestSupport.Capture(() => ResolveSdkPropertiesLib.Execute(context));
        exit.ShouldBe(1);
        error.ShouldContain("Configuration validation failed");
    }

    [Test]
    public void Resolve_NonexistentDuckovFolderIsInvalid()
    {
        var missing = Path.Combine(_root, "no such game");
        var context = LibTestSupport.NewContext(_projectDir, duckovFolder: missing);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.HasValidConfiguration.ShouldBeFalse();
        props.ValidationErrors.ShouldContain($"DuckovFolder does not exist: {missing}");
        LibTestSupport.Capture(() => ResolveSdkPropertiesLib.Execute(context)).Exit.ShouldBe(1);
    }

    [Test]
    public void Resolve_DeploymentRequiresModName()
    {
        var context = LibTestSupport.NewContext(_projectDir, modName: null, duckovFolder: _duckov);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.HasValidConfiguration.ShouldBeFalse();
        props.ValidationErrors.ShouldContain("ModName is required for mod deployment but not specified");
        LibTestSupport.Capture(() => ResolveSdkPropertiesLib.Execute(context)).Exit.ShouldBe(1);
    }

    [Test]
    public void Resolve_DeployModFalseSkipsGameFolderAndModNameValidation()
    {
        var context = LibTestSupport.NewContext(_projectDir, modName: null, deployMod: false, duckovFolder: null);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.HasValidConfiguration.ShouldBeTrue();
        props.ValidationErrors.ShouldBeEmpty();
        LibTestSupport.Capture(() => ResolveSdkPropertiesLib.Execute(context)).Exit.ShouldBe(0);
    }

    [Test]
    public void Resolve_MissingLocalizationDirectoryIsOnlyAWarning()
    {
        var missingLoc = Path.Combine(_root, "missing loc");
        var context = LibTestSupport.NewContext(
            _projectDir, isModLib: true, localizationAssetsDir: missingLoc);

        var (exit, output, _) = LibTestSupport.Capture(() => ResolveSdkPropertiesLib.Execute(context));

        exit.ShouldBe(0);
        output.ShouldContain($"[BuildContext][WARN] Localization assets directory does not exist: {missingLoc}");
    }

    [Test]
    public void Resolve_NonexistentProjectDirectoryIsReportedAsResolutionError()
    {
        var missingProject = Path.Combine(_root, "no project");
        var context = LibTestSupport.NewContext(missingProject, duckovFolder: _duckov);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.HasValidConfiguration.ShouldBeFalse();
        props.ValidationErrors.ShouldContain(e => e.StartsWith("Property resolution error:"));
    }

    [Test]
    public void Resolve_DetectsGameDependencyReferencesInProjectSources()
    {
        var sub = Path.Combine(_projectDir, "Code Folder");
        LibTestSupport.WriteText(Path.Combine(sub, "A.cs"), "using TeamSoda.Core;");
        LibTestSupport.WriteText(Path.Combine(sub, "B.cs"), "var x = UnityEngine.Vector3.zero;");
        LibTestSupport.WriteText(Path.Combine(sub, "C.cs"), "FOW.Map.Init(); SodaLocalization.Load();");
        var context = LibTestSupport.NewContext(_projectDir, isModLib: true);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.HasTeamSoda.ShouldBeTrue();
        props.HasUnity.ShouldBeTrue();
        props.HasFOW.ShouldBeTrue();
        props.HasSodaLocalization.ShouldBeTrue();
    }

    [Test]
    public void Resolve_PlainProjectReportsNoGameDependencies()
    {
        LibTestSupport.WriteText(Path.Combine(_projectDir, "Plain.cs"), "class Plain { }");
        var context = LibTestSupport.NewContext(_projectDir, isModLib: true);

        var props = ResolveSdkPropertiesLib.Resolve(context);

        props.HasTeamSoda.ShouldBeFalse();
        props.HasUnity.ShouldBeFalse();
        props.HasFOW.ShouldBeFalse();
        props.HasSodaLocalization.ShouldBeFalse();
    }
}
