namespace Ducky.Sdk.Lib.Tests.Runner;

public class BuildRunnerArgumentsTests
{
    [Test]
    public void TryParse_AcceptsPathAndPrePhase()
    {
        var ok = BuildRunnerArguments.TryParse(["/proj/obj/ducky-build-context.json", "--phase", "pre"],
            out var parsed, out var error);

        ok.ShouldBeTrue(error);
        parsed.Phase.ShouldBe("pre");
        parsed.BuildContextPath.ShouldBe(Path.GetFullPath("/proj/obj/ducky-build-context.json"));
    }

    [Test]
    public void TryParse_AcceptsPostPhaseForPathWithSpaces()
    {
        var ok = BuildRunnerArguments.TryParse(["/games/Escape from Duckov/obj/ducky-build-context.json", "--phase", "post"],
            out var parsed, out _);

        ok.ShouldBeTrue();
        parsed.Phase.ShouldBe("post");
        parsed.BuildContextPath.ShouldEndWith("Escape from Duckov/obj/ducky-build-context.json");
    }

    [Test]
    public void TryParse_RejectsNoArguments()
    {
        BuildRunnerArguments.TryParse([], out _, out var error).ShouldBeFalse();
        error.ShouldContain("build-context.json");
    }

    [Test]
    public void TryParse_RejectsFlagInPlaceOfPath()
    {
        BuildRunnerArguments.TryParse(["--phase", "pre"], out _, out var error).ShouldBeFalse();
        error.ShouldContain("path");
    }

    [Test]
    public void TryParse_RejectsMissingPhase()
    {
        BuildRunnerArguments.TryParse(["/proj/obj/ducky-build-context.json"], out _, out var error).ShouldBeFalse();
        error.ShouldContain("--phase");
    }

    [Test]
    public void TryParse_RejectsUnknownPhase()
    {
        BuildRunnerArguments.TryParse(["/proj/obj/ducky-build-context.json", "--phase", "middle"], out _, out var error)
            .ShouldBeFalse();
        error.ShouldContain("middle");
    }

    [Test]
    public void TryParse_RejectsExtraArguments()
    {
        BuildRunnerArguments.TryParse(["/proj/obj/ducky-build-context.json", "--phase", "pre", "extra"], out _, out var error)
            .ShouldBeFalse();
        error.ShouldContain("extra");
    }

    [Test]
    public void Run_ReturnsUsageExitCodeForInvalidArguments()
    {
        Program.Run(["/proj/obj/ducky-build-context.json"]).ShouldBe(Program.ExitUsage);
    }
}
