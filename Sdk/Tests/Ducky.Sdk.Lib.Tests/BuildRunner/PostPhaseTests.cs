namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Post phase: step order, the BuildContext update, the missing-output error and the non-fatal print.
/// </summary>
public class PostPhaseTests
{
    private string _root = null!;
    private string _projectDir = null!;
    private string _outputDir = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "ducky runner post", Guid.NewGuid().ToString("N"));
        _projectDir = Path.Combine(_root, "Samples", "Ducky Post Mod");
        _outputDir = Path.Combine(_projectDir, "bin", "Debug", "netstandard2.1");
        Directory.CreateDirectory(_outputDir);
        Directory.CreateDirectory(Path.Combine(_projectDir, "obj"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string JsonPath => Path.Combine(_projectDir, "obj", "ducky-build-context.json");

    private BuildContext NewContext(bool deployMod = false, string? managedDirectory = null) => new()
    {
        ProjectDir = _projectDir,
        Configuration = "Debug",
        TargetFramework = "netstandard2.1",
        IntermediateOutputPath = Path.Combine(_projectDir, "obj", "Debug", "netstandard2.1") + "/",
        BaseIntermediateOutputPath = Path.Combine(_projectDir, "obj") + "/",
        OutputPath = _outputDir + "/",
        ModName = "Ducky Post Mod",
        ManagedDirectory_Explicit = managedDirectory,
        ModsDirectory_Explicit = Path.Combine(_root, "artifacts", "Mods") + "/",
        DeployMod = deployMod,
    };

    private void WriteContext(BuildContext context) => File.WriteAllText(JsonPath, context.ToJson());

    [Test]
    public void PostSteps_MatchLegacyOrderAfterTheUpdate()
    {
        // Legacy order (see artifacts baseline 1.2): UpdateBuildContextAfterBuildLib.csx, ResetDeployLib,
        // ILRepackAssembliesLib, CopyDependenciesLib, DeployModLib, PrintResultLib (non-fatal).
        BuildRunner.PostSteps.ShouldBe(["ResetDeployLib", "ILRepackAssembliesLib", "CopyDependenciesLib", "DeployModLib"]);
    }

    [Test]
    public void PreSteps_MatchLegacyOrderPlusTheLocalizationValidation()
    {
        BuildRunner.PreSteps.ShouldBe([
            "InitializeBuildLoggingLib",
            "ValidateProjectPathLib",
            "ResolveSdkPropertiesLib",
            "ExtractLocalizationKeysLib",
            "UpdateLocalizationCsvLib",
            "ValidateLocalizationLib",
            "CopyLocalizationAssetsLib",
            "EnsureInfoIniLib",
            "GeneratePreviewLib",
            "ValidateDuckovFolderLib",
            "CollectFromModLib",
        ]);
    }

    [Test]
    public void UpdateStep_SetsMainAssemblyDependenciesAndFlagsAndSavesJson()
    {
        var managed = Path.Combine(_root, "Managed");
        Directory.CreateDirectory(managed);
        File.WriteAllText(Path.Combine(managed, "Newtonsoft.Json.dll"), "managed");
        File.WriteAllText(Path.Combine(_outputDir, "Ducky Post Mod.dll"), "main");
        File.WriteAllText(Path.Combine(_outputDir, "Dep One.dll"), "dep");
        File.WriteAllText(Path.Combine(_outputDir, "Newtonsoft.Json.dll"), "managed copy");
        var context = NewContext(managedDirectory: managed + "/");
        WriteContext(context);

        var exit = UpdateBuildContextAfterBuildStep.Execute(context, JsonPath);

        exit.ShouldBe(0);
        context.MainAssemblyPath.ShouldBe(Path.Combine(_outputDir, "Ducky Post Mod.dll"));
        context.DependencyAssemblies.ShouldNotBeNull();
        context.DependencyAssemblies!.Select(Path.GetFileName).ShouldBe(["Dep One.dll"]);
        context.HasDependencies.ShouldBeTrue();
        context.ShouldUseILRepack.ShouldBeTrue();

        var saved = BuildContext.FromJson(File.ReadAllText(JsonPath));
        saved!.MainAssemblyPath.ShouldBe(context.MainAssemblyPath);
        saved.DependencyAssemblies!.Count.ShouldBe(1);
        File.Exists(JsonPath + ".tmp").ShouldBeFalse();
    }

    [Test]
    public void RunPost_FailingPrintStepDoesNotFailThePhase()
    {
        WriteContext(NewContext(deployMod: false));

        var exit = BuildRunner.RunPost(JsonPath, (_, _) => 1);

        exit.ShouldBe(Program.ExitSuccess);
    }

    [Test]
    public void RunPost_ThrowingPrintStepDoesNotFailThePhase()
    {
        WriteContext(NewContext(deployMod: false));

        var exit = BuildRunner.RunPost(JsonPath, (_, _) => throw new InvalidOperationException("print broke"));

        exit.ShouldBe(Program.ExitSuccess);
    }

    [Test]
    public void RunPost_FailsWithActionableMessageWhenContextJsonIsMissing()
    {
        var (exit, output) = CaptureConsoleError(() => BuildRunner.RunPost(JsonPath));

        exit.ShouldBe(Program.ExitFailure);
        output.ShouldContain(JsonPath);
        output.ShouldContain("pre phase");
    }

    [Test]
    public void RunPost_FailsBeforeResetDeployWhenTheDeployedDllIsMissing()
    {
        var modsDir = Path.Combine(_root, "artifacts", "Mods");
        var oldModFolder = Path.Combine(modsDir, "Ducky Post Mod");
        Directory.CreateDirectory(oldModFolder);
        File.WriteAllText(Path.Combine(oldModFolder, "previous.dll"), "keep me");
        WriteContext(NewContext(deployMod: true));

        var (exit, output) = CaptureConsoleError(() => BuildRunner.RunPost(JsonPath));

        exit.ShouldBe(Program.ExitFailure);
        output.ShouldContain(Path.Combine(_projectDir, "bin", "Debug", "netstandard2.1", "Ducky Post Mod.dll"));
        File.Exists(Path.Combine(oldModFolder, "previous.dll")).ShouldBeTrue("ResetDeployLib must not run");
    }

    private static (int exit, string output) CaptureConsoleError(Func<int> action)
    {
        var original = Console.Error;
        using var writer = new StringWriter();
        Console.SetError(writer);
        try
        {
            var exit = action();
            return (exit, writer.ToString());
        }
        finally
        {
            Console.SetError(original);
        }
    }
}
