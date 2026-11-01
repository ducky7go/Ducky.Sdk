namespace Ducky.Sdk.Lib.Tests.Runner;

public class BuildRunnerStepTests
{
    private string _projectDir = null!;

    [SetUp]
    public void SetUp()
    {
        _projectDir = Path.Combine(Path.GetTempPath(), "ducky-runner-steps", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_projectDir))
        {
            Directory.Delete(_projectDir, recursive: true);
        }
    }

    private BuildContext NewContext() => new()
    {
        ProjectDir = _projectDir,
        Configuration = "Debug",
        TargetFramework = "netstandard2.1",
        IntermediateOutputPath = Path.Combine(_projectDir, "obj", "Debug", "netstandard2.1") + "/",
        BaseIntermediateOutputPath = Path.Combine(_projectDir, "obj") + "/",
        OutputPath = Path.Combine(_projectDir, "bin", "Debug", "netstandard2.1") + "/",
        ModName = "Steps"
    };

    [Test]
    public void RunSteps_UnknownStepNameFails()
    {
        var context = NewContext();
        var result = new BuildResult { ProjectDirectory = _projectDir };
        var table = new Dictionary<string, Func<BuildContext, int>>();

        var exit = BuildRunner.RunSteps(["NoSuchLib"], context, result, table);

        exit.ShouldBe(Program.ExitFailure);
        result.StepResults.ShouldBeEmpty();
    }

    [Test]
    public void RunSteps_SkipExitCodeCountsAsSuccessAndIsRecordedAsSkipped()
    {
        var context = NewContext();
        var result = new BuildResult { ProjectDirectory = _projectDir };
        var table = new Dictionary<string, Func<BuildContext, int>>
        {
            ["Skipper"] = _ => RunnerExitCodes.SkipExitCode,
        };

        var exit = BuildRunner.RunSteps(["Skipper"], context, result, table);

        exit.ShouldBe(Program.ExitSuccess);
        result.StepResults.Single().Status.ShouldBe(StepStatus.Skipped);
    }

    [Test]
    public void RunSteps_FailureStopsLaterStepsAndReturnsFailure()
    {
        var context = NewContext();
        var result = new BuildResult { ProjectDirectory = _projectDir };
        var ranAfterFailure = false;
        var table = new Dictionary<string, Func<BuildContext, int>>
        {
            ["Fails"] = _ => 3,
            ["After"] = _ =>
            {
                ranAfterFailure = true;
                return 0;
            },
        };

        var exit = BuildRunner.RunSteps(["Fails", "After"], context, result, table);

        exit.ShouldBe(Program.ExitFailure);
        ranAfterFailure.ShouldBeFalse();
        result.StepResults.Single(r => r.StepName == "Fails").Status.ShouldBe(StepStatus.Failed);
    }

    [Test]
    public void RunSteps_RunsStepsInGivenOrder()
    {
        var context = NewContext();
        var result = new BuildResult { ProjectDirectory = _projectDir };
        var order = new List<string>();
        var table = new Dictionary<string, Func<BuildContext, int>>
        {
            ["First"] = _ => Track(order, "First"),
            ["Second"] = _ => Track(order, "Second"),
        };

        BuildRunner.RunSteps(["First", "Second"], context, result, table).ShouldBe(Program.ExitSuccess);

        order.ShouldBe(["First", "Second"]);
    }

    [Test]
    public void StepTable_ContainsEveryLegacyStepInOrder()
    {
        var legacy = BuildRunner.PreSteps.Concat(BuildRunner.PostSteps).ToArray();

        legacy.ShouldAllBe(name => BuildRunner.StepTable.ContainsKey(name));
        // 14 legacy steps plus ValidateLocalizationLib, which runs after UpdateLocalizationCsvLib.
        legacy.Length.ShouldBe(15);
    }

    private static int Track(List<string> order, string name)
    {
        order.Add(name);
        return 0;
    }
}
