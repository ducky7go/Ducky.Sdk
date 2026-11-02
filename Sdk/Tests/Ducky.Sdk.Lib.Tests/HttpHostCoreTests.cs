using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.HttpEndpoints;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Host core: handshake, snapshots, ownership, forwarding limits, presence, release and claim, with a fake hub, poster,
/// clock and controllable delay (tasks 3.1 to 3.4).
/// </summary>
[TestFixture]
public class HttpHostCoreTests
{
    private const string A = "A.http";
    private const string B = "B.http";

    private FakeHttpEndpointTransport _transport = null!;
    private HttpHostCore _core = null!;
    private int _routesChangedCount;

    [SetUp]
    public void SetUp()
    {
        _transport = new FakeHttpEndpointTransport();
        _routesChangedCount = 0;
        _core = new HttpHostCore(_transport, () => _routesChangedCount++);
    }

    private void StartHost()
    {
        _core.Start().ShouldBeTrue();
    }

    private void Snapshot(string clientId, params (string Method, string Path)[] endpoints)
    {
        var body = HttpEndpointProtocol.EncodeRegister(
            HttpEndpointProtocol.TryGetSenderModId(clientId, out var mod) ? mod : string.Empty,
            endpoints.Select(e => new RouteKey(e.Method, e.Path)));
        _core.HandleMessage(clientId, HttpEndpointProtocol.RegisterContentType, body);
    }

    private void Hello(string clientId)
    {
        _core.HandleMessage(clientId, HttpEndpointProtocol.HelloContentType, HttpEndpointProtocol.EncodeHello("x", new[] { 1 }));
    }

    private static HttpEndpointRequest Get(string path) => new() { Method = "GET", Path = path };

    /// <summary>The id of the most recent request sent to the owner.</summary>
    private string LastRequestId()
    {
        HttpEndpointProtocol.TryDecodeRequest(
            _transport.SentOf(HttpEndpointProtocol.RequestContentType).Last().Body,
            out var id,
            out _).ShouldBeTrue();
        return id;
    }

    private void Answer(string from, string id, int status, string body = "ok")
    {
        var response = new HttpEndpointResponse { Status = status, Body = body, ContentType = "text/plain; charset=utf-8" };
        _core.HandleMessage(from, HttpEndpointProtocol.ResponseContentType, HttpEndpointProtocol.EncodeResponse(id, response));
    }

    private static HttpEndpointResponse Result(UniTask<HttpEndpointResponse> task) => task.GetAwaiter().GetResult();

    #region Handshake and registration (task 3.1)

    [Test]
    public void HelloIsAnsweredWithTheHighestCommonVersion()
    {
        StartHost();

        Hello(A);

        var ready = _transport.SentOf(HttpEndpointProtocol.ReadyContentType).Single();
        ready.To.ShouldBe(A);
        ready.From.ShouldBe("httphost");
        HttpEndpointProtocol.TryDecodeReady(ready.Body, out var version, out var supported).ShouldBeTrue();
        version.ShouldBe(1);
        supported.ShouldBe(new[] { 1 });
    }

    [Test]
    public void SnapshotWithoutHelloRegistersAndIsAnswered()
    {
        StartHost();

        Snapshot(A, ("GET", "/A/status"));

        _core.Routes.Select(r => r.Path).ShouldBe(new[] { "/A/status" });
        _transport.SentOf(HttpEndpointProtocol.RegisteredContentType).Count().ShouldBe(1);
    }

    [Test]
    public void SnapshotOfAAndBReplacedByBLeavesOnlyB()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/a"), ("GET", "/A/b"));

        Snapshot(A, ("GET", "/A/b"));

        _core.Routes.Select(r => r.Path).ShouldBe(new[] { "/A/b" });
    }

    [Test]
    public void ForeignPrefixIsRejectedWithAReason()
    {
        StartHost();

        Snapshot(A, ("GET", "/B/status"));

        _core.Routes.ShouldBeEmpty();
        HttpEndpointProtocol.TryDecodeRegistered(
            _transport.SentOf(HttpEndpointProtocol.RegisteredContentType).Single().Body,
            out var accepted,
            out var rejected).ShouldBeTrue();
        accepted.ShouldBeEmpty();
        rejected.Count.ShouldBe(1);
        rejected[0].Reason.ShouldContain("/A");
    }

    [Test]
    public void SameOwnerSnapshotReplacesItsRoutesInsteadOfRejectingThem()
    {
        // The path rule makes a route owned by one mod unreachable from any other sender, so a second snapshot from the
        // same hub client id is the same owner: it replaces the routes rather than duplicating them. A cross-owner duplicate
        // can only come from a spoofed hub id, which the hub does not authenticate (design D3, risks).
        StartHost();
        Snapshot(A, ("GET", "/A/x"));

        Snapshot(A, ("GET", "/A/x"));

        _core.Routes.Count.ShouldBe(1);
        _core.Routes[0].OwnerModId.ShouldBe("A");
    }

    [Test]
    public void SeventyEndpointsGive64AcceptedAnd6Rejected()
    {
        StartHost();
        var endpoints = Enumerable.Range(0, 70).Select(i => ("GET", $"/A/e{i}")).ToArray();

        Snapshot(A, endpoints);

        _core.Routes.Count.ShouldBe(64);
        HttpEndpointProtocol.TryDecodeRegistered(
            _transport.SentOf(HttpEndpointProtocol.RegisteredContentType).Single().Body,
            out var accepted,
            out var rejected).ShouldBeTrue();
        accepted.Count.ShouldBe(64);
        rejected.Count.ShouldBe(6);
        rejected.All(r => r.Reason.Length > 0).ShouldBeTrue();
    }

    [Test]
    public void ThreeMessagesOfAnUnknownContentTypeLogOneWarning()
    {
        StartHost();

        for (var i = 0; i < 3; i++)
        {
            _core.HandleMessage(A, "ducky.other.v9", "{}");
        }

        _transport.Warnings.Count(w => w.Contains("ducky.other.v9")).ShouldBe(1);
    }

    [Test]
    public void ChangeNotificationFiresOncePerAcceptedChange()
    {
        StartHost();

        Snapshot(A, ("GET", "/A/a"), ("GET", "/A/b"));
        _routesChangedCount.ShouldBe(1);

        Snapshot(A, ("GET", "/A/a"), ("GET", "/A/b"));
        _routesChangedCount.ShouldBe(1);

        Snapshot(A, ("GET", "/A/a"));
        _routesChangedCount.ShouldBe(2);
    }

    #endregion

    #region Forwarding (task 3.2)

    [Test]
    public void FullRoundTripReturnsTheHandlersAnswer()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/status"));

        var task = _core.ForwardAsync(Get("/A/status"));
        Answer(A, LastRequestId(), 200, "ok");

        var response = Result(task);
        response.Status.ShouldBe(200);
        response.Body.ShouldBe("ok");
    }

    [Test]
    public void MissingRouteReturns404AndSendsNothing()
    {
        StartHost();

        var response = Result(_core.ForwardAsync(Get("/A/missing")));

        response.Status.ShouldBe(404);
        _transport.SentOf(HttpEndpointProtocol.RequestContentType).ShouldBeEmpty();
    }

    [Test]
    public void OversizedRequestReturns413AndSendsNothing()
    {
        StartHost();
        Snapshot(A, ("POST", "/A/upload"));
        var request = new HttpEndpointRequest { Method = "POST", Path = "/A/upload", Body = new string('x', 70000) };

        var response = Result(_core.ForwardAsync(request));

        response.Status.ShouldBe(413);
        _transport.SentOf(HttpEndpointProtocol.RequestContentType).ShouldBeEmpty();
    }

    [Test]
    public void SixtyFourPendingRequestsMake65thReturn503AndACompletedOneFreesASlot()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/x"));
        var tasks = new List<UniTask<HttpEndpointResponse>>();
        for (var i = 0; i < 64; i++)
        {
            tasks.Add(_core.ForwardAsync(Get("/A/x")));
        }

        Result(_core.ForwardAsync(Get("/A/x"))).Status.ShouldBe(503);
        _transport.SentOf(HttpEndpointProtocol.RequestContentType).Count().ShouldBe(64);

        var firstId = HttpEndpointProtocol.TryDecodeRequest(
            _transport.SentOf(HttpEndpointProtocol.RequestContentType).First().Body,
            out var id,
            out _)
            ? id
            : string.Empty;
        Answer(A, firstId, 200);
        Result(tasks[0]).Status.ShouldBe(200);

        _core.ForwardAsync(Get("/A/x"));
        _transport.SentOf(HttpEndpointProtocol.RequestContentType).Count().ShouldBe(65);
    }

    [Test]
    public void FiringTheDelayReturns504AndRemovesTheEntry()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/slow"));
        var task = _core.ForwardAsync(Get("/A/slow"));

        _transport.Delays.Single().Duration.ShouldBe(TimeSpan.FromSeconds(15));
        _transport.Delays.Single().Fire();

        Result(task).Status.ShouldBe(504);
        _core.PendingCount.ShouldBe(0);
    }

    [Test]
    public void ResponseAfter504IsIgnoredWithoutThrowing()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/slow"));
        var task = _core.ForwardAsync(Get("/A/slow"));
        var id = LastRequestId();
        _transport.Delays.Single().Fire();
        Result(task).Status.ShouldBe(504);

        Should.NotThrow(() => Answer(A, id, 200));
        _core.PendingCount.ShouldBe(0);
    }

    [Test]
    public void DuplicateResponseIsIgnored()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/x"));
        var task = _core.ForwardAsync(Get("/A/x"));
        var id = LastRequestId();

        Answer(A, id, 200, "first");
        Answer(A, id, 200, "second");

        Result(task).Body.ShouldBe("first");
    }

    [Test]
    public void ResponseFromANonOwnerLeavesTheRequestPending()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/x"));
        var task = _core.ForwardAsync(Get("/A/x"));
        var id = LastRequestId();

        Answer(B, id, 200);

        _core.PendingCount.ShouldBe(1);
        Answer(A, id, 200);
        Result(task).Status.ShouldBe(200);
    }

    [Test]
    public void StatusOutsideRangeReturns502()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/x"));
        var task = _core.ForwardAsync(Get("/A/x"));

        Answer(A, LastRequestId(), 42);

        Result(task).Status.ShouldBe(502);
        _core.PendingCount.ShouldBe(0);
    }

    [Test]
    public void ForwardingFromAThreadPoolThreadCompletesWithoutTouchingUnity()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/pool"));

        var forwarded = Task.Run(() => _core.ForwardAsync(Get("/A/pool"))).GetAwaiter().GetResult();
        Answer(A, LastRequestId(), 200);

        Result(forwarded).Status.ShouldBe(200);
    }

    [Test]
    public void ForwardBeforeStartReturns503()
    {
        var response = Result(_core.ForwardAsync(Get("/A/x")));

        response.Status.ShouldBe(503);
        _transport.Sent.ShouldBeEmpty();
    }

    #endregion

    #region Presence and release (task 3.3)

    [Test]
    public void EmptySnapshotRemovesRoutesAndALaterRequestGets404()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/x"));

        Snapshot(A);

        _core.Routes.ShouldBeEmpty();
        Result(_core.ForwardAsync(Get("/A/x"))).Status.ShouldBe(404);
    }

    [Test]
    public void ClientIdGoneFor5SecondsDropsRoutesAndFailsItsInFlightRequestWith502()
    {
        StartHost();
        _transport.ModIds.Add(A);
        Snapshot(A, ("GET", "/A/x"));
        var task = _core.ForwardAsync(Get("/A/x"));
        _transport.ModIds.Remove(A);

        _core.Tick();
        _transport.UtcNow = _transport.UtcNow.AddSeconds(5);
        _core.Tick();

        Result(task).Status.ShouldBe(502);
        _core.Routes.ShouldBeEmpty();
        _core.PendingCount.ShouldBe(0);
        _transport.Delays.Single().Fire();
        _routesChangedCount.ShouldBe(2);
    }

    [Test]
    public void ClientIdGoneFor4SecondsKeepsItsRoutes()
    {
        StartHost();
        _transport.ModIds.Add(A);
        Snapshot(A, ("GET", "/A/x"));
        _transport.ModIds.Remove(A);

        _core.Tick();
        _transport.UtcNow = _transport.UtcNow.AddSeconds(4);
        _core.Tick();

        _core.Routes.Count.ShouldBe(1);
    }

    [Test]
    public void ReturningModNeedsANewSnapshotToBeServedAgain()
    {
        StartHost();
        _transport.ModIds.Add(A);
        Snapshot(A, ("GET", "/A/x"));
        _transport.ModIds.Remove(A);
        _core.Tick();
        _transport.UtcNow = _transport.UtcNow.AddSeconds(5);
        _core.Tick();

        _transport.ModIds.Add(A);
        _core.Tick();
        Result(_core.ForwardAsync(Get("/A/x"))).Status.ShouldBe(404);

        Snapshot(A, ("GET", "/A/x"));
        _core.Routes.Count.ShouldBe(1);
    }

    [Test]
    public void ReleaseWithOneRequestInFlightAnswers502EmptiesTheTableAndSecondReleaseIsHarmless()
    {
        StartHost();
        Snapshot(A, ("GET", "/A/x"));
        var task = _core.ForwardAsync(Get("/A/x"));

        _core.Shutdown();

        Result(task).Status.ShouldBe(502);
        _core.Routes.ShouldBeEmpty();
        _transport.Unregistered.ShouldContain("httphost");
        Should.NotThrow(() => _core.Shutdown());
        _core.IsHeld.ShouldBeFalse();
    }

    #endregion

    #region Claim (task 3.4)

    [Test]
    public void StartRefusesWhenHttpHostIsAlreadyRegisteredAndSendsNothing()
    {
        _transport.ModIds.Add("httphost");

        _core.Start().ShouldBeFalse();

        _transport.Warnings.Count.ShouldBe(1);
        _transport.Sent.ShouldBeEmpty();
        _transport.RegisterCalls.ShouldBe(0);
        _core.IsHeld.ShouldBeFalse();
    }

    [Test]
    public void SuccessfulStartRegistersOnceAndASecondStartFromTheHolderReturnsTrue()
    {
        _core.Start().ShouldBeTrue();
        _core.Start().ShouldBeTrue();

        _transport.RegisterCalls.ShouldBe(1);
        _core.IsHeld.ShouldBeTrue();
        _transport.ModIds.Count(id => id == "httphost").ShouldBe(1);
    }

    [Test]
    public void StartStartsTheTickThatChecksPresence()
    {
        StartHost();

        _transport.Posted.Count.ShouldBe(1);
        _transport.Drain();
        _transport.Delays.Count.ShouldBe(1);
    }

    #endregion
}
