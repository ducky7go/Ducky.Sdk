using Ducky.Sdk.Contracts;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The public facade without a game: reporting with no recorder and the static releases. StartRecorder needs the real
/// hub (a GameObject), so it is verified in the game (tasks 5.1 and 5.2).
/// </summary>
[TestFixture]
public class DebugEventsContractTests
{
    [Test]
    public void Report_EmptyKind_ReturnsNormally()
    {
        Should.NotThrow(() => Contract.DebugEvents.Report(""));
        Should.NotThrow(() => Contract.DebugEvents.Report("   "));
    }

    [Test]
    public void Report_WithNoRecorder_ReturnsNormallyAndRecordsNothing()
    {
        Should.NotThrow(() => Contract.DebugEvents.Report("hurt", "victim=player", new Dictionary<string, object?>
        {
            ["damage"] = 12
        }));

        Contract.DebugEvents.IsRecorder.ShouldBeFalse();
        Contract.DebugEvents.IsRecorderAvailable.ShouldBeFalse();
    }

    [Test]
    public void StopRecorder_WhenNotRecording_IsSafeToCallTwice()
    {
        Should.NotThrow(() => Contract.DebugEvents.StopRecorder());
        Should.NotThrow(() => Contract.DebugEvents.StopRecorder());
        Contract.DebugEvents.IsRecorder.ShouldBeFalse();
    }

    [Test]
    public void StaticReleases_WithNothingRunning_AreIdempotent()
    {
        Should.NotThrow(() =>
        {
            Ducky.Sdk.Contracts.DebugEvents.DebugEventRecorder.Release();
            Ducky.Sdk.Contracts.DebugEvents.DebugEventRecorder.Release();
            Ducky.Sdk.Contracts.DebugEvents.DebugEventReporter.Release();
            Ducky.Sdk.Contracts.DebugEvents.DebugEventReporter.Release();
        });
    }
}
