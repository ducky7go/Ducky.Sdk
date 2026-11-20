using System;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <see cref="PatchHealth"/>: a patch that could not be installed is recorded where a mod can read it, and the record goes
/// away when the same patch installs later. `HarmonyPatches.Install` feeds it (not exercised here: the test project has no reference to 0Harmony).
/// </summary>
[NonParallelizable]
public class PatchHealthTests
{
    [SetUp]
    public void Clean() => PatchHealth.Reset();

    [TearDown]
    public void CleanUp() => PatchHealth.Reset();

    [Test]
    public void Fail_RecordsTheKeyAndTheExceptionText()
    {
        PatchHealth.Fail("id-1", new InvalidOperationException("no such method"), "[X] could not install its prefix");

        PatchHealth.HasFailures.ShouldBeTrue();
        PatchHealth.IsFailed("id-1").ShouldBeTrue();
        var failure = PatchHealth.Failures.ShouldHaveSingleItem();
        failure.Key.ShouldBe("id-1");
        failure.Message.ShouldContain("could not install its prefix");
        failure.Message.ShouldContain("no such method");
    }

    [Test]
    public void FailWithoutAKey_UsesTheMessageAsTheKey()
    {
        PatchHealth.Fail(new Exception("boom"), "[Y] could not install its patch");

        PatchHealth.IsFailed("[Y] could not install its patch").ShouldBeTrue();
    }

    [Test]
    public void Succeeded_RemovesOnlyThatKey()
    {
        PatchHealth.Fail("a", new Exception("x"), "a failed");
        PatchHealth.Fail("b", new Exception("x"), "b failed");

        PatchHealth.Succeeded("a");

        PatchHealth.IsFailed("a").ShouldBeFalse();
        PatchHealth.IsFailed("b").ShouldBeTrue();
    }
}
