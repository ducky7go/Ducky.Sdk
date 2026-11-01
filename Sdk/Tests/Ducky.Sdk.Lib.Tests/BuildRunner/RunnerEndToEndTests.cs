using System.Reflection;
using System.Reflection.Emit;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Runs the runner on a minimal fake mod: pre phase, then the compiled DLL appears, then the post phase.
/// Checks the deployed file list and the recorded build steps.
/// </summary>
public class RunnerEndToEndTests
{
    private const string ModName = "Fake Mod";

    private string _root = null!;
    private string _projectDir = null!;
    private string _modsDir = null!;
    private string _managedDir = null!;

    [SetUp]
    public void SetUp()
    {
        // Spaces in the game and project folders, as in "Escape from Duckov".
        _root = Path.Combine(Path.GetTempPath(), "ducky e2e", Guid.NewGuid().ToString("N"));
        _projectDir = Path.Combine(_root, "Escape from Duckov", "Samples", ModName);
        _modsDir = Path.Combine(_root, "Escape from Duckov", "Duckov_Data", "Mods");
        Directory.CreateDirectory(Path.Combine(_projectDir, "obj"));
        Directory.CreateDirectory(_modsDir);
        _managedDir = Path.Combine(_root, "Escape from Duckov", "Duckov_Data", "Managed");
        Directory.CreateDirectory(_managedDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string OutputDir => Path.Combine(_projectDir, "bin", "Debug", "netstandard2.1");
    private string JsonPath => Path.Combine(_projectDir, "obj", "ducky-build-context.json");

    /// <summary>The 19 property values GenerateBuildContext writes for this fake mod.</summary>
    private void WriteProperties()
    {
        var values = new[]
        {
            _projectDir, ModName, "Debug", "netstandard2.1", "", "",
            Path.Combine(_projectDir, "assets"), "", "false", "true", "false", "true", "false", "false",
            Path.Combine(_projectDir, "obj", "Debug", "netstandard2.1") + "/",
            Path.Combine(_projectDir, "obj") + "/",
            OutputDir + "/",
            _managedDir + "/",
            _modsDir + "/",
        };
        File.WriteAllLines(Path.Combine(_projectDir, "obj", BuildContextGenerationStep.PropertiesFileName),
            values.Select((value, index) => $"{index + 1}={value}"));
    }

    /// <summary>Stands in for the compiler: writes the main assembly where the post phase expects it.</summary>
    private void WriteCompiledMainAssembly()
    {
        Directory.CreateDirectory(OutputDir);
        var path = Path.Combine(OutputDir, $"{ModName}.dll");
        var builder = new PersistedAssemblyBuilder(new AssemblyName(ModName), typeof(object).Assembly);
        var module = builder.DefineDynamicModule(ModName);
        var type = module.DefineType("Fake.Mod.Entry", TypeAttributes.Public | TypeAttributes.Class);
        var method = type.DefineMethod("Run", MethodAttributes.Public, typeof(void), Type.EmptyTypes);
        method.GetILGenerator().Emit(OpCodes.Ret);
        type.CreateType();
        builder.Save(path);
    }

    [Test]
    public void PrePhaseThenPostPhase_DeploysTheModAndRecordsEveryStep()
    {
        WriteProperties();

        Program.Run([JsonPath, "--phase", "pre"]).ShouldBe(Program.ExitSuccess);
        File.Exists(JsonPath).ShouldBeTrue("the pre phase writes the BuildContext JSON");

        WriteCompiledMainAssembly();

        Program.Run([JsonPath, "--phase", "post"]).ShouldBe(Program.ExitSuccess);

        var modFolder = Path.Combine(_modsDir, ModName);
        var deployed = Directory.GetFiles(modFolder, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(modFolder, f).Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();
        deployed.ShouldContain($"{ModName}.dll");
        deployed.ShouldContain("info.ini");
        deployed.ShouldContain("preview.png");
    }

    [Test]
    public void PrePhaseThenPostPhase_BuildResultRecordsPreAndPostLibs()
    {
        WriteProperties();
        Program.Run([JsonPath, "--phase", "pre"]).ShouldBe(Program.ExitSuccess);
        WriteCompiledMainAssembly();
        Program.Run([JsonPath, "--phase", "post"]).ShouldBe(Program.ExitSuccess);

        // The runner's result file is obj/ducky-build-result.json (BuildResult.GetResultFilePath).
        var resultPath = BuildResult.GetResultFilePath(_projectDir);
        File.Exists(resultPath).ShouldBeTrue();
        var result = BuildResult.FromJson(File.ReadAllText(resultPath));

        result.ShouldNotBeNull();
        result!.FailedSteps.ShouldBe(0);
        var names = result.StepResults.Select(r => r.StepName).ToArray();
        names.ShouldContain("InitializeBuildLoggingLib");
        names.ShouldContain("CollectFromModLib");
        names.ShouldContain("DeployModLib");
        result.StepResults.Single(r => r.StepName == "DeployModLib").Status.ShouldBe(StepStatus.Success);
    }

    [Test]
    public void PostPhaseFailsBeforeDeployWhenTheCompiledDllIsMissing()
    {
        WriteProperties();
        Program.Run([JsonPath, "--phase", "pre"]).ShouldBe(Program.ExitSuccess);

        Program.Run([JsonPath, "--phase", "post"]).ShouldBe(Program.ExitFailure);

        Directory.Exists(Path.Combine(_modsDir, ModName)).ShouldBeFalse("nothing is deployed without the DLL");
    }
}
