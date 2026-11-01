using Ducky.Sdk.HotReload;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The hot reload shim's decisions (HotReloadSchedule, compiled from the same file the shim uses): which Core is newest,
/// when a file has been stable long enough, and when a same-name Core is refused.
/// </summary>
public class HotReloadScheduleTests
{
    private static readonly DateTime WriteA = new(2026, 10, 31, 21, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime WriteB = WriteA.AddSeconds(5);

    [Test]
    public void Newest_PicksTheGreatestStamp_NotTheLatestWriteTime()
    {
        var cores = new[]
        {
            ("Ducky.Probe.Core.20261031210000002", WriteB),
            ("Ducky.Probe.Core.20261031210000010", WriteA),
        };

        HotReloadSchedule.Newest(cores).ShouldBe("Ducky.Probe.Core.20261031210000010");
        HotReloadSchedule.Newest(Array.Empty<(string, DateTime)>()).ShouldBeNull();
    }

    [Test]
    public void Loaded_CoreAndUnchangedFile_DoNothing()
    {
        var watch = new HotReloadSchedule.CoreWatch(1.0);
        var loaded = new HashSet<string> { "Ducky.Probe.Core.1" };

        watch.Observe("Ducky.Probe.Core.1", WriteA, 0, "Ducky.Probe.Core.1", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.None);
    }

    [Test]
    public void NewerCore_WaitsForTheStabilityWindow_ThenReloads()
    {
        var watch = new HotReloadSchedule.CoreWatch(1.0);
        var loaded = new HashSet<string> { "Ducky.Probe.Core.1" };

        watch.Observe("Ducky.Probe.Core.2", WriteA, 10.0, "Ducky.Probe.Core.1", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Wait);
        watch.Observe("Ducky.Probe.Core.2", WriteA, 10.5, "Ducky.Probe.Core.1", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Wait);
        watch.Observe("Ducky.Probe.Core.2", WriteA, 11.0, "Ducky.Probe.Core.1", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Reload);
    }

    [Test]
    public void FileStillBeingWritten_RestartsTheWindow()
    {
        var watch = new HotReloadSchedule.CoreWatch(1.0);
        var loaded = new HashSet<string> { "Ducky.Probe.Core.1" };

        watch.Observe("Ducky.Probe.Core.2", WriteA, 10.0, "Ducky.Probe.Core.1", WriteA, loaded);
        // the write time moved (the copy is still going): the window starts again from this poll
        watch.Observe("Ducky.Probe.Core.2", WriteB, 10.8, "Ducky.Probe.Core.1", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Wait);
        watch.Observe("Ducky.Probe.Core.2", WriteB, 11.5, "Ducky.Probe.Core.1", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Wait);
        watch.Observe("Ducky.Probe.Core.2", WriteB, 11.9, "Ducky.Probe.Core.1", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Reload);
    }

    [Test]
    public void SameNameAsALoadedAssembly_IsRejectedOnceAndNeverReloaded()
    {
        var watch = new HotReloadSchedule.CoreWatch(1.0);
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ducky.Probe.Core", "Ducky.Probe.Core.2" };

        // the running Core is "Ducky.Probe.Core.2"; the same file name was rewritten later
        watch.Observe("Ducky.Probe.Core.2", WriteB, 20.0, "Ducky.Probe.Core.2", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Reject);
        watch.Observe("Ducky.Probe.Core.2", WriteB, 25.0, "Ducky.Probe.Core.2", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.None);
        watch.Observe("Ducky.Probe.Core.2", WriteB, 30.0, "Ducky.Probe.Core.2", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.None);
    }

    [Test]
    public void SameNameOfAnEarlierLoad_IsRejectedWhileAnotherCoreRuns()
    {
        var watch = new HotReloadSchedule.CoreWatch(1.0);
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ducky.Probe.Core.1", "Ducky.Probe.Core.2" };

        watch.Observe("Ducky.Probe.Core.1", WriteB, 40.0, "Ducky.Probe.Core.2", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Reject);
    }

    [Test]
    public void AfterARejection_AUniqueNewerCoreReloadsNormally()
    {
        var watch = new HotReloadSchedule.CoreWatch(1.0);
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Ducky.Probe.Core.2" };

        watch.Observe("Ducky.Probe.Core.2", WriteB, 20.0, "Ducky.Probe.Core.2", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Reject);
        watch.Observe("Ducky.Probe.Core.3", WriteB, 21.0, "Ducky.Probe.Core.2", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Wait);
        watch.Observe("Ducky.Probe.Core.3", WriteB, 22.0, "Ducky.Probe.Core.2", WriteA, loaded)
            .ShouldBe(HotReloadSchedule.Step.Reload);
    }
}
