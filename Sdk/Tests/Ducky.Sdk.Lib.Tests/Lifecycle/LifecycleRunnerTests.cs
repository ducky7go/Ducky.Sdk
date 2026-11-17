using Ducky.Sdk.Lifecycle;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <see cref="LifecycleRunner"/> on fake steps that record their calls (the context is the log): the early return, a failing step,
/// and a step list that is not the plan.
/// </summary>
public class LifecycleRunnerTests
{
    private enum Start
    {
        Logging,
        ExistingCheck,
        CreateHost,
        Report,
    }

    private enum Stop
    {
        Release,
        Destroy,
        Caches,
    }

    private static readonly Start[] StartPlan = { Start.Logging, Start.ExistingCheck, Start.CreateHost, Start.Report };

    private static readonly Stop[] StopPlan = { Stop.Release, Stop.Destroy, Stop.Caches };

    [Test]
    public void Start_RunsEveryStepOnceInPlanOrder_AndContinues()
    {
        var log = new List<string>();

        var outcome = LifecycleRunner.RunStartup(StartPlan, FakeStart(controllerExists: false), log);

        outcome.ShouldBe(StepOutcome.Continue);
        log.ShouldBe(new[] { "Logging", "ExistingCheck", "CreateHost", "Report" });
    }

    [Test]
    public void Start_HaltFromAStep_EndsTheStartThere()
    {
        var log = new List<string>();

        var outcome = LifecycleRunner.RunStartup(StartPlan, FakeStart(controllerExists: true), log);

        outcome.ShouldBe(StepOutcome.Halt);
        log.ShouldBe(new[] { "Logging", "ExistingCheck" });
    }

    [Test]
    public void Start_FailingStep_EndsTheStartAndLaterStepsDoNotRun()
    {
        var log = new List<string>();

        var error = Should.Throw<InvalidOperationException>(() =>
            LifecycleRunner.RunStartup(StartPlan, FakeStart(controllerExists: false, failAt: Start.CreateHost), log));

        error.Message.ShouldContain("CreateHost");
        log.ShouldBe(new[] { "Logging", "ExistingCheck", "CreateHost" });
    }

    [Test]
    public void Stop_RunsEveryStepOnceInPlanOrder()
    {
        var log = new List<string>();

        LifecycleRunner.RunStop(StopPlan, FakeStop(), log);

        log.ShouldBe(new[] { "Release", "Destroy", "Caches" });
    }

    [Test]
    public void Stop_FailingStep_EndsTheStopAndSkipsTheRest()
    {
        var log = new List<string>();

        Should.Throw<InvalidOperationException>(() => LifecycleRunner.RunStop(StopPlan, FakeStop(failAt: Stop.Destroy), log));

        log.ShouldBe(new[] { "Release", "Destroy" });
    }

    [Test]
    public void Start_ListOutOfPlanOrder_IsRefusedBeforeAnyStepRuns()
    {
        var log = new List<string>();
        var steps = FakeStart(controllerExists: false);
        (steps[0], steps[1]) = (steps[1], steps[0]);

        var error = Should.Throw<InvalidOperationException>(() => LifecycleRunner.RunStartup(StartPlan, steps, log));

        error.Message.ShouldContain("start steps are not the planned order");
        log.ShouldBeEmpty();
    }

    [Test]
    public void Start_ListMissingAStep_IsRefusedBeforeAnyStepRuns()
    {
        var log = new List<string>();
        var steps = FakeStart(controllerExists: false).Where(step => step.Id != Start.Report).ToArray();

        Should.Throw<InvalidOperationException>(() => LifecycleRunner.RunStartup(StartPlan, steps, log));

        log.ShouldBeEmpty();
    }

    [Test]
    public void Stop_ListOutOfPlanOrder_IsRefusedBeforeAnyStepRuns()
    {
        var log = new List<string>();
        var steps = FakeStop();
        (steps[1], steps[2]) = (steps[2], steps[1]);

        Should.Throw<InvalidOperationException>(() => LifecycleRunner.RunStop(StopPlan, steps, log));

        log.ShouldBeEmpty();
    }

    [Test]
    public void MissingStepListOrPlan_IsRefused()
    {
        Should.Throw<ArgumentNullException>(() => LifecycleRunner.RunStartup<Start, List<string>>(StartPlan, null!, new List<string>()));
        Should.Throw<ArgumentNullException>(() => LifecycleRunner.RunStop<Stop, List<string>>(StopPlan, null!, new List<string>()));
        Should.Throw<ArgumentNullException>(() => LifecycleRunner.RunStartup(null!, FakeStart(false), new List<string>()));
    }

    [Test]
    public void ActionStep_AlwaysLetsTheSequenceGoOn()
    {
        var step = new DelegateStartupStep<Start, int>(Start.Logging, _ => { });

        step.Run(0).ShouldBe(StepOutcome.Continue);
        step.Id.ShouldBe(Start.Logging);
    }

    [Test]
    public void NullWork_IsRefused()
    {
        Should.Throw<ArgumentNullException>(() => new DelegateStartupStep<Start, int>(Start.Logging, (Action<int>)null!));
        Should.Throw<ArgumentNullException>(() => new DelegateStartupStep<Start, int>(Start.Logging, (Func<int, StepOutcome>)null!));
        Should.Throw<ArgumentNullException>(() => new DelegateStopStep<Stop, int>(Stop.Release, null!));
    }

    // the start steps of the plan; each records its name, the check halts when a controller exists, and failAt throws
    private static IStartupStep<Start, List<string>>[] FakeStart(bool controllerExists, Start? failAt = null) =>
        StartPlan
            .Select(id => (IStartupStep<Start, List<string>>)new DelegateStartupStep<Start, List<string>>(id, log =>
            {
                log.Add(id.ToString());
                if (id == failAt)
                {
                    throw new InvalidOperationException("step " + id + " failed");
                }

                return id == Start.ExistingCheck && controllerExists ? StepOutcome.Halt : StepOutcome.Continue;
            }))
            .ToArray();

    private static IStopStep<Stop, List<string>>[] FakeStop(Stop? failAt = null) =>
        StopPlan
            .Select(id => (IStopStep<Stop, List<string>>)new DelegateStopStep<Stop, List<string>>(id, log =>
            {
                log.Add(id.ToString());
                if (id == failAt)
                {
                    throw new InvalidOperationException("step " + id + " failed");
                }
            }))
            .ToArray();
}
