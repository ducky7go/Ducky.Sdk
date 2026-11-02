using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.DebugEvents;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Reporter state machine, batching, bounds and release, with a fake hub and a manual clock (tasks 2.1, 4.1).</summary>
[TestFixture]
public class DebugEventReporterTests
{
    private const string ModId = "local.tester";
    private const string ClientId = "local.tester.debug-events";
    private static readonly TimeSpan Tick200 = TimeSpan.FromMilliseconds(200);

    private FakeDebugEventHost _host = null!;
    private List<string> _warnings = null!;
    private DebugEventReporter _reporter = null!;

    [SetUp]
    public void SetUp()
    {
        _host = new FakeDebugEventHost();
        _warnings = new List<string>();
        _reporter = new DebugEventReporter(_host, () => ModId, message => _warnings.Add(message));
    }

    private void Advance(TimeSpan by) => _host.UtcNow += by;

    private void Ready(int version = 1) =>
        _reporter.HandleIncoming(DebugEventProtocol.RecorderModId, DebugEventProtocol.ReadyContentType,
            DebugEventProtocol.EncodeReady(version, new[] { 1 }));

    /// <summary>Recorder appears, the reporter sends hello and the recorder answers ready.</summary>
    private void NegotiateReady()
    {
        _host.ModIds.Add(DebugEventProtocol.RecorderModId);
        _reporter.Tick();
        Ready();
    }

    private void TickTimes(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _reporter.Tick();
            Advance(Tick200);
        }
    }

    private static DebugEventBatch DecodeBatch(SentMessage message)
    {
        DebugEventProtocol.TryDecodeBatch(message.Body, out var batch).ShouldBeTrue();
        return batch;
    }

    private IEnumerable<SentMessage> Batches() =>
        _host.Sent.Where(m => m.ContentType == DebugEventProtocol.BatchContentType(1));

    [Test]
    public void NoRecorder_ThousandReports_SendNothingAndCheckTheIdListAtMostEvery5Seconds()
    {
        for (var i = 0; i < 1000; i++)
        {
            _reporter.Report("hurt", $"n={i}", null);
        }

        TickTimes(51); // 10 seconds of 200 ms ticks

        _host.Sent.ShouldBeEmpty();
        _reporter.BufferedCount.ShouldBe(0);
        _reporter.State.ShouldBe(DebugEventReporterState.Idle);
        _host.GetModIdsCalls.ShouldBeLessThanOrEqualTo(3);
    }

    [Test]
    public void RecorderAppears_SendsHelloThenBatches()
    {
        _host.ModIds.Add(DebugEventProtocol.RecorderModId);
        _reporter.Tick();

        _host.Sent.Count.ShouldBe(1);
        var hello = _host.Sent[0];
        hello.From.ShouldBe(ClientId);
        hello.To.ShouldBe(DebugEventProtocol.RecorderModId);
        hello.ContentType.ShouldBe(DebugEventProtocol.HelloContentType);
        DebugEventProtocol.TryDecodeHello(hello.Body, out var reporter, out var versions).ShouldBeTrue();
        reporter.ShouldBe(ModId);
        versions.ShouldBe(new[] { 1 });

        _reporter.Report("hurt", "victim=player", null); // buffered while negotiating
        Ready();
        Advance(Tick200);
        _reporter.Tick();

        _host.Sent.Count.ShouldBe(2);
        var batch = _host.Sent[1];
        batch.ContentType.ShouldBe("ducky.debug-event.batch.v1");
        batch.To.ShouldBe(DebugEventProtocol.RecorderModId);
        var decoded = DecodeBatch(batch);
        decoded.Reporter.ShouldBe(ModId);
        decoded.Events.Count.ShouldBe(1);
        decoded.Events[0].Kind.ShouldBe("hurt");
        decoded.Events[0].Detail.ShouldBe("victim=player");
    }

    [Test]
    public void ClientIsRegisteredUnderTheModDebugEventsId()
    {
        NegotiateReady();

        _host.Handlers.ContainsKey(ClientId).ShouldBeTrue();
        _host.Handlers[ClientId].ShouldNotBeNull();
    }

    [Test]
    public void WaitingEvents_SendInBatchesOf50Then50Then20InOrder()
    {
        NegotiateReady();
        for (var i = 0; i < 120; i++)
        {
            _reporter.Report("e", i.ToString(), null);
        }

        TickTimes(3);

        var batches = Batches().Select(DecodeBatch).ToList();
        batches.Select(b => b.Events.Count).ToArray().ShouldBe(new[] { 50, 50, 20 });
        batches.SelectMany(b => b.Events).Select(e => e.Detail).ToArray()
            .ShouldBe(Enumerable.Range(0, 120).Select(i => i.ToString()).ToArray());
        _reporter.Tick();
        Batches().Count().ShouldBe(3);
    }

    [Test]
    public void BurstOf1500_SendsThe1000Newest_WithDroppedCountOf500OnTheFirstBatch()
    {
        NegotiateReady();
        for (var i = 0; i < 1500; i++)
        {
            _reporter.Report("burst", i.ToString(), null);
        }

        TickTimes(25);

        var batches = Batches().Select(DecodeBatch).ToList();
        batches.Sum(b => b.Events.Count).ShouldBe(1000);
        batches[0].Dropped.ShouldBe(500);
        batches.Skip(1).All(b => b.Dropped == 0).ShouldBeTrue();
        batches[0].Events[0].Detail.ShouldBe("500");
    }

    [Test]
    public void VersionZero_SendsNothing_AndWarnsOnce()
    {
        _host.ModIds.Add(DebugEventProtocol.RecorderModId);
        _reporter.Tick();

        Ready(version: 0);
        Ready(version: 0);
        _reporter.Report("hurt", null, null);
        TickTimes(3);

        _reporter.State.ShouldBe(DebugEventReporterState.Incompatible);
        _warnings.Count.ShouldBe(1);
        _host.Sent.Count.ShouldBe(1); // the hello only
        _reporter.BufferedCount.ShouldBe(0);
    }

    [Test]
    public void ReporterOnlySpeaksV2_AgainstAV1Recorder_IsIncompatibleWithOneWarning()
    {
        var reporter = new DebugEventReporter(_host, () => ModId, message => _warnings.Add(message), new[] { 2 });
        _host.ModIds.Add(DebugEventProtocol.RecorderModId);
        reporter.Tick();

        DebugEventProtocol.TryDecodeHello(_host.Sent[0].Body, out _, out var versions);
        versions.ShouldBe(new[] { 2 });

        reporter.HandleIncoming(DebugEventProtocol.RecorderModId, DebugEventProtocol.ReadyContentType,
            DebugEventProtocol.EncodeReady(0, new[] { 1 }));

        reporter.State.ShouldBe(DebugEventReporterState.Incompatible);
        _warnings.Count.ShouldBe(1);
    }

    [Test]
    public void RecorderDisappearsAndReturns_ReporterSendsANewHello()
    {
        NegotiateReady();
        _reporter.Report("hurt", "a", null);
        _reporter.Report("hurt", "b", null);

        _host.ModIds.Remove(DebugEventProtocol.RecorderModId);
        Advance(TimeSpan.FromSeconds(5));
        _reporter.Tick();
        _reporter.State.ShouldBe(DebugEventReporterState.Idle);
        _reporter.BufferedCount.ShouldBe(0);

        _host.ModIds.Add(DebugEventProtocol.RecorderModId);
        Advance(TimeSpan.FromSeconds(5));
        _reporter.Tick();

        _reporter.State.ShouldBe(DebugEventReporterState.Negotiating);
        _host.Sent.Last().ContentType.ShouldBe(DebugEventProtocol.HelloContentType);
    }

    [Test]
    public void ReadyFromAnotherSender_IsIgnored()
    {
        _host.ModIds.Add(DebugEventProtocol.RecorderModId);
        _reporter.Tick();

        _reporter.HandleIncoming("someone.else", DebugEventProtocol.ReadyContentType,
            DebugEventProtocol.EncodeReady(1, new[] { 1 }));

        _reporter.State.ShouldBe(DebugEventReporterState.Negotiating);
    }

    [Test]
    public void EmptyKind_IsDiscarded()
    {
        NegotiateReady();

        _reporter.Report("", null, null);
        _reporter.Report("   ", null, null);

        _reporter.BufferedCount.ShouldBe(0);
    }

    [Test]
    public void ReportFromAThreadPoolThread_OnlyFillsTheBuffer()
    {
        NegotiateReady();
        var sentBefore = _host.Sent.Count;
        var registeredBefore = _host.Handlers.Count;

        Task.Run(() => _reporter.Report("hurt", "from worker", null)).Wait();

        _reporter.BufferedCount.ShouldBe(1);
        _host.Sent.Count.ShouldBe(sentBefore);
        _host.Handlers.Count.ShouldBe(registeredBefore);
    }

    [Test]
    public void Shutdown_FlushesBufferedEvents_UnregistersAndStops()
    {
        NegotiateReady();
        _reporter.Report("hurt", "last", null);
        _reporter.Report("hurt", "later", null);

        _reporter.Shutdown();

        var last = Batches().Last();
        DecodeBatch(last).Events.Select(e => e.Detail).ToArray().ShouldBe(new[] { "last", "later" });
        _host.Unregistered.ShouldContain(ClientId);
        _reporter.IsReleased.ShouldBeTrue();

        var count = _host.Sent.Count;
        _reporter.Report("hurt", "after", null);
        TickTimes(3);
        _reporter.Shutdown();
        _host.Sent.Count.ShouldBe(count);
        _host.Unregistered.Count(id => id == ClientId).ShouldBe(1);
    }

    [Test]
    public void FlushLoop_TicksUntilShutdown_ThenStops()
    {
        NegotiateReady();
        _reporter.Report("hurt", "first", null);
        var delays = 0;

        _reporter.StartFlushLoop((_, _) =>
        {
            delays++;
            if (delays == 2)
            {
                _reporter.Shutdown();
            }

            return UniTask.CompletedTask;
        });

        // First pass flushed the buffered event; the second pass released the reporter and the loop exited.
        delays.ShouldBe(2);
        Batches().Count().ShouldBe(1);
        DecodeBatch(Batches().Single()).Events[0].Detail.ShouldBe("first");
        _reporter.IsReleased.ShouldBeTrue();
        _host.Unregistered.ShouldContain(ClientId);
    }

    [Test]
    public void Shutdown_WithoutRecorder_SendsNothing()
    {
        _reporter.Report("hurt", "lost", null);

        _reporter.Shutdown();

        _host.Sent.ShouldBeEmpty();
        _reporter.IsReleased.ShouldBeTrue();
    }
}
