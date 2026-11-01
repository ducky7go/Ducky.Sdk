namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Development hot reload (opt-in) in the build runner: default builds stay as before, and a hot reload build
/// names its Core by stamp, cleans older Cores, deploys the shim, Harmony and the marker, and never merges Harmony.
/// </summary>
public class HotReloadStepTests
{
    private const string Stamp = "20261031210000123";
    private const string OlderStamp = "20261031205959001";
    private string _root = null!;

    [SetUp]
    public void SetUp() => _root = LibTestSupport.NewTempRoot("hot reload");

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private string ProjectDir => Path.Combine(_root, "Ducky.Probe");
    private string OutDir => Path.Combine(ProjectDir, "bin", "Debug", "netstandard2.1");
    private string ModsDir => Path.Combine(_root, "Mods");
    private string ModDir => Path.Combine(ModsDir, "Ducky.Probe");

    private BuildContext Context(bool hotReload, string? harmony = null, string? template = null) => new()
    {
        ProjectDir = ProjectDir,
        Configuration = "Debug",
        TargetFramework = "netstandard2.1",
        IntermediateOutputPath = Path.Combine(ProjectDir, "obj", "Debug", "netstandard2.1") + "/",
        BaseIntermediateOutputPath = Path.Combine(ProjectDir, "obj") + "/",
        OutputPath = OutDir + "/",
        ModName = "Ducky.Probe",
        ModsDirectory_Explicit = ModsDir,
        HotReloadEnabled = hotReload,
        HotReloadStamp = hotReload ? Stamp : null,
        HarmonyDllPath = hotReload ? harmony : null,
        ShimTemplatePath = hotReload ? template : null,
    };

    private static void Touch(string path, string content = "x")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    [Test]
    public void DefaultContext_JsonHasNoHotReloadFields()
    {
        var json = Context(hotReload: false).ToJson();

        json.ShouldNotContain("hotReloadEnabled");
        json.ShouldNotContain("hotReloadStamp");
        json.ShouldNotContain("harmonyDllPath");
        json.ShouldNotContain("shimTemplatePath");
        json.ShouldNotContain("coreAssemblyName");
        json.ShouldNotContain("mainOutputFileName");
    }

    [Test]
    public void HotReloadContext_RoundTripsItsFields()
    {
        var harmony = Path.Combine(_root, "0Harmony.dll");
        var template = Path.Combine(_root, "Shim.cs.template");
        var json = Context(hotReload: true, harmony, template).ToJson();

        json.ShouldContain("\"hotReloadEnabled\": true");
        var back = BuildContext.FromJson(json)!;
        back.HotReloadEnabled.ShouldBeTrue();
        back.HotReloadStamp.ShouldBe(Stamp);
        back.HarmonyDllPath.ShouldBe(harmony);
        back.ShimTemplatePath.ShouldBe(template);
        back.CoreAssemblyName.ShouldBe($"Ducky.Probe.Core.{Stamp}");
    }

    [Test]
    public void MainFileNames_DefaultUseProjectAndModName_HotReloadUsesCore()
    {
        var defaults = Context(hotReload: false);
        defaults.MainOutputFileName.ShouldBe("Ducky.Probe.dll");
        defaults.MainDeployFileName.ShouldBe("Ducky.Probe.dll");

        var dev = Context(hotReload: true);
        dev.MainOutputFileName.ShouldBe($"Ducky.Probe.Core.{Stamp}.dll");
        dev.MainDeployFileName.ShouldBe($"Ducky.Probe.Core.{Stamp}.dll");
    }

    [Test]
    public void GenerationStep_ReadsHotReloadLines_AndDefaultLeavesThemOff()
    {
        var lines = new List<string> { $"1={ProjectDir}", "2=Ducky.Probe", "3=Debug", "4=netstandard2.1" };
        for (var i = 5; i <= 19; i++)
        {
            lines.Add($"{i}=");
        }

        var values = BuildContextGenerationStep.ReadProperties(lines.Append("20=false").Append("21=").ToArray());
        Directory.CreateDirectory(ProjectDir);
        var defaults = BuildContextGenerationStep.CreateContext(values, out _);
        defaults.HotReloadEnabled.ShouldBeFalse();
        defaults.HotReloadStamp.ShouldBeNull();

        var devValues = BuildContextGenerationStep.ReadProperties(lines
            .Append("20=true").Append($"21={Stamp}").Append("22=/sdk/0Harmony.dll").Append("23=/sdk/Shim.cs.template").ToArray());
        var dev = BuildContextGenerationStep.CreateContext(devValues, out _);
        dev.HotReloadEnabled.ShouldBeTrue();
        dev.HotReloadStamp.ShouldBe(Stamp);
        dev.HarmonyDllPath.ShouldBe("/sdk/0Harmony.dll");
        dev.ShimTemplatePath.ShouldBe("/sdk/Shim.cs.template");
    }

    [Test]
    public void GenerationStep_HotReloadWithoutStamp_IsRejected()
    {
        var lines = new List<string> { $"1={ProjectDir}", "2=Ducky.Probe", "20=true", "21=" };
        var values = BuildContextGenerationStep.ReadProperties(lines);
        Directory.CreateDirectory(ProjectDir);

        Should.Throw<InvalidOperationException>(() => BuildContextGenerationStep.CreateContext(values, out _))
            .Message.ShouldContain("Core stamp");
    }

    [Test]
    public void Cleanup_DefaultBuild_Skips()
    {
        Touch(Path.Combine(OutDir, $"Ducky.Probe.Core.{OlderStamp}.dll"));

        HotReloadCleanupStep.Execute(Context(hotReload: false)).ShouldBe(RunnerExitCodes.SkipExitCode);
        File.Exists(Path.Combine(OutDir, $"Ducky.Probe.Core.{OlderStamp}.dll")).ShouldBeTrue();
    }

    [Test]
    public void Cleanup_RemovesOlderCores_KeepsCurrentAndUnrelatedFiles()
    {
        var current = $"Ducky.Probe.Core.{Stamp}.dll";
        var olderA = $"Ducky.Probe.Core.{OlderStamp}.dll";
        var olderB = "Ducky.Probe.Core.20261031200000000.dll";
        foreach (var dir in new[] { OutDir, ModDir })
        {
            Touch(Path.Combine(dir, current));
            Touch(Path.Combine(dir, olderA));
            Touch(Path.Combine(dir, olderB));
            Touch(Path.Combine(dir, Path.ChangeExtension(olderA, ".pdb")));
            Touch(Path.Combine(dir, "Ducky.Probe.Core.dll"));          // not a stamped Core
            Touch(Path.Combine(dir, "Other.Core.20261031200000000.dll")); // another mod's Core
            Touch(Path.Combine(dir, "0Harmony.dll"));
        }

        HotReloadCleanupStep.Execute(Context(hotReload: true)).ShouldBe(Program.ExitSuccess);

        foreach (var dir in new[] { OutDir, ModDir })
        {
            File.Exists(Path.Combine(dir, current)).ShouldBeTrue();
            File.Exists(Path.Combine(dir, olderA)).ShouldBeFalse();
            File.Exists(Path.Combine(dir, olderB)).ShouldBeFalse();
            File.Exists(Path.Combine(dir, Path.ChangeExtension(olderA, ".pdb"))).ShouldBeFalse();
            File.Exists(Path.Combine(dir, "Ducky.Probe.Core.dll")).ShouldBeTrue();
            File.Exists(Path.Combine(dir, "Other.Core.20261031200000000.dll")).ShouldBeTrue();
            File.Exists(Path.Combine(dir, "0Harmony.dll")).ShouldBeTrue();
        }
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void Cleanup_LockedOlderCore_WarnsAndContinues()
    {
        // A folder that cannot be written makes the delete fail the same way a game-held file does.
        var locked = Path.Combine(OutDir, $"Ducky.Probe.Core.{OlderStamp}.dll");
        Touch(locked);
        Touch(Path.Combine(ModDir, $"Ducky.Probe.Core.{OlderStamp}.dll"));
        File.SetUnixFileMode(OutDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        try
        {
            HotReloadCleanupStep.Execute(Context(hotReload: true)).ShouldBe(Program.ExitSuccess);
            File.Exists(locked).ShouldBeTrue();
            File.Exists(Path.Combine(ModDir, $"Ducky.Probe.Core.{OlderStamp}.dll")).ShouldBeFalse();
        }
        finally
        {
            File.SetUnixFileMode(OutDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Test]
    public void ShimSource_SubstitutesModName()
    {
        var template = Path.Combine(_root, "Shim.cs.template");
        File.WriteAllText(template, "namespace __MOD_NAME__ { const string C = \"__MOD_NAME__.Core.\"; }");

        HotReloadShimStep.PrepareSource(Context(hotReload: true, template: template)).ShouldBe(Program.ExitSuccess);

        var source = File.ReadAllText(HotReloadFiles.ShimSourcePath(Context(hotReload: true)));
        source.ShouldBe("namespace Ducky.Probe { const string C = \"Ducky.Probe.Core.\"; }");
        source.ShouldNotContain("__MOD_NAME__");
    }

    [Test]
    public void ShimSource_MissingTemplate_Fails()
    {
        HotReloadShimStep.PrepareSource(Context(hotReload: true, template: Path.Combine(_root, "missing.template")))
            .ShouldBe(Program.ExitFailure);
    }

    [Test]
    public void ShimDeploy_DefaultBuild_Skips()
    {
        HotReloadShimStep.Deploy(Context(hotReload: false)).ShouldBe(RunnerExitCodes.SkipExitCode);
        HotReloadShimStep.PlannedOutputs(Context(hotReload: false)).ShouldBeEmpty();
    }

    [Test]
    public void ShimDeploy_CopiesShimHarmonyAndWritesMarker()
    {
        var template = Path.Combine(_root, "Shim.cs.template");
        File.WriteAllText(template, "namespace __MOD_NAME__ { }");
        var harmony = Path.Combine(_root, "sdk", "0Harmony.dll");
        Touch(harmony, "harmony-bytes");
        var ctx = Context(hotReload: true, harmony, template);
        Touch(HotReloadFiles.ShimAssemblyPath(ctx), "shim-bytes");

        HotReloadShimStep.Deploy(ctx).ShouldBe(Program.ExitSuccess);

        File.ReadAllText(Path.Combine(ModDir, "Ducky.Probe.dll")).ShouldBe("shim-bytes");
        File.ReadAllText(Path.Combine(ModDir, "0Harmony.dll")).ShouldBe("harmony-bytes");
        new FileInfo(Path.Combine(ModDir, "hotreload.enabled")).Length.ShouldBe(0);
        HotReloadShimStep.PlannedOutputs(ctx).ShouldContain(Path.Combine(ModDir, "hotreload.enabled"));
    }

    [Test]
    public void ShimDeploy_HarmonyIsNotRewrittenWhenUnchanged()
    {
        var harmony = Path.Combine(_root, "sdk", "0Harmony.dll");
        Touch(harmony, "harmony-bytes");
        var ctx = Context(hotReload: true, harmony);
        Touch(HotReloadFiles.ShimAssemblyPath(ctx), "shim-bytes");
        HotReloadShimStep.Deploy(ctx).ShouldBe(Program.ExitSuccess);

        // Give the deployed copy an old write time; an unchanged copy must keep it.
        var deployed = Path.Combine(ModDir, "0Harmony.dll");
        var fixedTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(deployed, fixedTime);

        HotReloadShimStep.Deploy(ctx).ShouldBe(Program.ExitSuccess);

        File.GetLastWriteTimeUtc(deployed).ShouldBe(fixedTime);
    }

    [Test]
    public void HarmonyAndOtherCores_AreNotMergeInputsInHotReloadBuilds()
    {
        var dev = Context(hotReload: true);
        UpdateBuildContextAfterBuildStep.IsHotReloadOnlyFile(dev, "0Harmony.dll").ShouldBeTrue();
        UpdateBuildContextAfterBuildStep.IsHotReloadOnlyFile(dev, $"Ducky.Probe.Core.{OlderStamp}.dll").ShouldBeTrue();
        UpdateBuildContextAfterBuildStep.IsHotReloadOnlyFile(dev, "Other.Core.20261031200000000.dll").ShouldBeFalse();
        UpdateBuildContextAfterBuildStep.IsHotReloadOnlyFile(dev, "Newtonsoft.Json.dll").ShouldBeFalse();

        var defaults = Context(hotReload: false);
        UpdateBuildContextAfterBuildStep.IsHotReloadOnlyFile(defaults, "0Harmony.dll").ShouldBeFalse();
    }

    [Test]
    public void FindMainAssembly_HotReloadPicksTheStampedCore()
    {
        Touch(Path.Combine(OutDir, $"Ducky.Probe.Core.{OlderStamp}.dll"));
        Touch(Path.Combine(OutDir, $"Ducky.Probe.Core.{Stamp}.dll"));
        Touch(Path.Combine(OutDir, "0Harmony.dll"));

        UpdateBuildContextAfterBuildStep.FindMainAssembly(Context(hotReload: true))
            .ShouldBe(Path.Combine(OutDir, $"Ducky.Probe.Core.{Stamp}.dll"));
    }

    [Test]
    public void Runner_StepLists_HotReloadStepsAreInOrder_DefaultListsUnchanged()
    {
        BuildRunner.PostSteps.ShouldBe(new[] { "ResetDeployLib", "ILRepackAssembliesLib", "CopyDependenciesLib", "DeployModLib" });
        BuildRunner.PreSteps.ShouldNotContain("HotReloadShimSourceStep");

        var post = BuildRunner.HotReloadPostSteps;
        Array.IndexOf(post, "HotReloadCleanupStep").ShouldBeLessThan(Array.IndexOf(post, "ILRepackAssembliesLib"));
        Array.IndexOf(post, "ResetDeployLib").ShouldBeLessThan(Array.IndexOf(post, "HotReloadCleanupStep"));
        post.Last().ShouldBe("HotReloadShimDeployStep");
        BuildRunner.HotReloadPreSteps.Last().ShouldBe("HotReloadShimSourceStep");

        foreach (var name in post.Concat(BuildRunner.HotReloadPreSteps))
        {
            BuildRunner.StepTable.ContainsKey(name).ShouldBeTrue(name);
        }
    }

    [Test]
    public void DeployManifest_HotReloadBuildPlansShimAndMarker()
    {
        var ctx = Context(hotReload: true, Path.Combine(_root, "sdk", "0Harmony.dll"));
        Touch(ctx.HarmonyDllPath!, "h");
        var planned = DeployManifest.PlannedRelativeFiles(ctx, ModDir);

        planned.ShouldContain("Ducky.Probe.dll");
        planned.ShouldContain("hotreload.enabled");
        planned.ShouldContain("0Harmony.dll");
    }
}
