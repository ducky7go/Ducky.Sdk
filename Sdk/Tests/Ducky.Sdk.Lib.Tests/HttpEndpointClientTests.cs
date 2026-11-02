using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.HttpEndpoints;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Endpoint client: table validation, silence without a host, negotiation, snapshots, dispatch and release, with a fake hub,
/// poster and clock (tasks 2.1 to 2.4).
/// </summary>
[TestFixture]
public class HttpEndpointClientTests
{
    private const string ModId = "Acme.Dash";
    private const string ClientId = "Acme.Dash.http";
    private const string Host = "httphost";

    private FakeHttpEndpointTransport _transport = null!;
    private HttpEndpointClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _transport = new FakeHttpEndpointTransport();
        _client = new HttpEndpointClient(_transport, ModId);
    }

    private static HttpEndpointResponse Ok(HttpEndpointRequest _) => HttpEndpointResponse.Text(200, "ok");

    private static HttpEndpointRequest Request(string method, string path, params KeyValuePair<string, string>[] query) =>
        new()
        {
            Method = method,
            Path = path,
            Query = query
        };

    /// <summary>Maps <paramref name="count"/> endpoints under the mod and runs what the posts queued.</summary>
    private void MapEndpoints(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _client.Map("GET", $"/{ModId}/e{i}", Ok);
        }

        _transport.Drain();
    }

    /// <summary>Makes the host appear and completes the negotiation with version 1.</summary>
    private void AnnounceToHost()
    {
        _transport.ModIds.Add(Host);
        _client.Tick();
        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.ReadyContentType, HttpEndpointProtocol.EncodeReady(1, new[] { 1 }));
    }

    private void AdvanceSeconds(int seconds)
    {
        _transport.UtcNow = _transport.UtcNow.AddSeconds(seconds);
    }

    #region Table (task 2.1)

    [Test]
    public void ValidMapAddsOneEndpoint()
    {
        using var handle = _client.Map("GET", $"/{ModId}/status", Ok);

        _client.EndpointCount.ShouldBe(1);
        handle.ShouldNotBeNull();
    }

    [Test]
    public void DuplicateMapThrowsAndKeepsTheFirst()
    {
        _client.Map("GET", $"/{ModId}/status", Ok);

        Should.Throw<ArgumentException>(() => _client.Map("get", $"/{ModId}/status", Ok));
        _client.EndpointCount.ShouldBe(1);
    }

    [Test]
    public void TraceMethodThrowsAndAddsNothing()
    {
        Should.Throw<ArgumentException>(() => _client.Map("TRACE", $"/{ModId}/status", Ok));
        _client.EndpointCount.ShouldBe(0);
    }

    [Test]
    public void TraversalSegmentThrows()
    {
        Should.Throw<ArgumentException>(() => _client.Map("GET", $"/{ModId}/../x", Ok));
    }

    [Test]
    public void PathUnderAnotherModPrefixThrowsNamingTheRequiredPrefix()
    {
        var error = Should.Throw<ArgumentException>(() => _client.Map("GET", "/other/stats", Ok));
        error.Message.ShouldContain("/Acme.Dash");
    }

    [Test]
    public void NullHandlerThrows()
    {
        Should.Throw<ArgumentNullException>(() => _client.Map("GET", $"/{ModId}/x", (Func<HttpEndpointRequest, HttpEndpointResponse>)null!));
    }

    [Test]
    public void DisposingTwiceRemovesOnce()
    {
        var first = _client.Map("GET", $"/{ModId}/a", Ok);
        _client.Map("GET", $"/{ModId}/b", Ok);

        first.Dispose();
        first.Dispose();

        _client.EndpointCount.ShouldBe(1);
    }

    [Test]
    public void InvalidModIdCannotMapEndpoints()
    {
        Should.Throw<ArgumentException>(() => new HttpEndpointClient(_transport, "Acme Dash"));
    }

    #endregion

    #region Silence and negotiation (task 2.2)

    [Test]
    public void TwentyMapsWithNoHostSendNothingAndPollAtMostOncePerFiveSeconds()
    {
        MapEndpoints(20);

        for (var second = 0; second < 10; second++)
        {
            _client.Tick();
            AdvanceSeconds(1);
        }

        _transport.Sent.ShouldBeEmpty();
        _transport.Warnings.ShouldBeEmpty();
        _transport.IdListReads.ShouldBeLessThanOrEqualTo(2);
    }

    [Test]
    public void EmptyTableDoesNotPollTheHub()
    {
        _client.Tick();
        _client.Tick();

        _transport.IdListReads.ShouldBe(0);
        _transport.Sent.ShouldBeEmpty();
    }

    [Test]
    public void HostAppearing60SecondsLaterGetsHelloAndOneSnapshotOfTwenty()
    {
        MapEndpoints(20);
        _client.Tick();
        AdvanceSeconds(60);

        _transport.ModIds.Add(Host);
        _client.Tick();

        _transport.SentOf(HttpEndpointProtocol.HelloContentType).Count().ShouldBe(1);
        _transport.SentOf(HttpEndpointProtocol.RegisterContentType).ShouldBeEmpty();

        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.ReadyContentType, HttpEndpointProtocol.EncodeReady(1, new[] { 1 }));

        var snapshots = _transport.SentOf(HttpEndpointProtocol.RegisterContentType).ToList();
        snapshots.Count.ShouldBe(1);
        HttpEndpointProtocol.TryDecodeRegister(snapshots[0].Body, out _, out var endpoints).ShouldBeTrue();
        endpoints.Count.ShouldBe(20);
    }

    [Test]
    public void ThirdMapAfterAnnouncementSendsASnapshotOfThree()
    {
        MapEndpoints(2);
        AnnounceToHost();
        _transport.Drain();
        var before = _transport.SentOf(HttpEndpointProtocol.RegisterContentType).Count();

        _client.Map("GET", $"/{ModId}/e2", Ok);
        _client.Tick();

        var snapshots = _transport.SentOf(HttpEndpointProtocol.RegisterContentType).ToList();
        snapshots.Count.ShouldBe(before + 1);
        HttpEndpointProtocol.TryDecodeRegister(snapshots[^1].Body, out _, out var endpoints).ShouldBeTrue();
        endpoints.Count.ShouldBe(3);
    }

    [Test]
    public void ReadyWithVersionZeroSendsNothingAndWarnsOnce()
    {
        MapEndpoints(1);
        _transport.ModIds.Add(Host);
        _client.Tick();

        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.ReadyContentType, HttpEndpointProtocol.EncodeReady(0, new[] { 1 }));
        AdvanceSeconds(5);
        _client.Tick();

        _transport.SentOf(HttpEndpointProtocol.RegisterContentType).ShouldBeEmpty();
        _transport.Warnings.Count.ShouldBe(1);
        _client.State.ShouldBe(HttpEndpointClientState.Incompatible);
    }

    [Test]
    public void HostThatDisappearsAndReturnsGetsANewHelloAndSnapshot()
    {
        MapEndpoints(2);
        AnnounceToHost();
        _transport.Drain();

        _transport.ModIds.Remove(Host);
        AdvanceSeconds(5);
        _client.Tick();
        _client.State.ShouldBe(HttpEndpointClientState.NoHost);

        _transport.ModIds.Add(Host);
        AdvanceSeconds(5);
        _client.Tick();
        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.ReadyContentType, HttpEndpointProtocol.EncodeReady(1, new[] { 1 }));

        _transport.SentOf(HttpEndpointProtocol.HelloContentType).Count().ShouldBe(2);
        _transport.SentOf(HttpEndpointProtocol.RegisterContentType).Count().ShouldBe(2);
    }

    [Test]
    public void RejectedEndpointFromRegisteredLogsOneWarning()
    {
        MapEndpoints(1);
        AnnounceToHost();
        _transport.Drain();

        var body = HttpEndpointProtocol.EncodeRegistered(
            Array.Empty<RouteKey>(),
            new[] { new RejectedRoute("GET", $"/{ModId}/e0", "already served by mod Other") });
        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.RegisteredContentType, body);

        _transport.Warnings.Count.ShouldBe(1);
        _transport.Warnings[0].ShouldContain("already served by mod Other");
    }

    [Test]
    public void UnknownContentTypeWarnsOnceForTheSenderAndType()
    {
        for (var i = 0; i < 3; i++)
        {
            _client.HandleIncoming("someone", "ducky.other.v9", "{}");
        }

        _transport.Warnings.Count.ShouldBe(1);
    }

    #endregion

    #region Dispatch (task 2.3)

    [Test]
    public void GetWithRepeatedQueryReachesTheHandlerInOrderAndAnswersWithTheSameId()
    {
        HttpEndpointRequest? seen = null;
        _client.Map("GET", $"/{ModId}/find", request =>
        {
            seen = request;
            return HttpEndpointResponse.Text(200, "found");
        });
        _transport.Drain();
        AnnounceToHost();

        var body = HttpEndpointProtocol.EncodeRequest(
            "req-1",
            "GET",
            $"/{ModId}/find",
            Request("GET", $"/{ModId}/find", new KeyValuePair<string, string>("tag", "a"), new KeyValuePair<string, string>("tag", "b")));
        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.RequestContentType, body);
        _transport.Drain();

        seen.ShouldNotBeNull();
        seen!.Query.Select(p => p.Key + "=" + p.Value).ShouldBe(new[] { "tag=a", "tag=b" });

        var responses = _transport.SentOf(HttpEndpointProtocol.ResponseContentType).ToList();
        responses.Count.ShouldBe(1);
        responses[0].From.ShouldBe(ClientId);
        responses[0].To.ShouldBe(Host);
        HttpEndpointProtocol.TryDecodeResponse(responses[0].Body, out var id, out var response).ShouldBeTrue();
        id.ShouldBe("req-1");
        response.Status.ShouldBe(200);
        response.Body.ShouldBe("found");
    }

    [Test]
    public void HandlerThatThrowsAnswers502WithoutTheExceptionTextAndLogsOnce()
    {
        Func<HttpEndpointRequest, HttpEndpointResponse> boom = _ => throw new InvalidOperationException("secret detail");
        _client.Map("GET", $"/{ModId}/boom", boom);
        _transport.Drain();
        AnnounceToHost();

        var body = HttpEndpointProtocol.EncodeRequest("r2", "GET", $"/{ModId}/boom", Request("GET", $"/{ModId}/boom"));
        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.RequestContentType, body);
        _transport.Drain();

        var responses = _transport.SentOf(HttpEndpointProtocol.ResponseContentType).ToList();
        responses.Count.ShouldBe(1);
        HttpEndpointProtocol.TryDecodeResponse(responses[0].Body, out _, out var response).ShouldBeTrue();
        response.Status.ShouldBe(502);
        response.Body.ShouldNotContain("secret detail");
        _transport.Errors.Count.ShouldBe(1);
    }

    [Test]
    public void OversizedResponseBecomes502WithOneWarning()
    {
        _client.Map("GET", $"/{ModId}/big", _ => HttpEndpointResponse.Text(200, new string('x', 70000)));
        _transport.Drain();
        AnnounceToHost();

        var body = HttpEndpointProtocol.EncodeRequest("r3", "GET", $"/{ModId}/big", Request("GET", $"/{ModId}/big"));
        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.RequestContentType, body);
        _transport.Drain();

        HttpEndpointProtocol.TryDecodeResponse(
            _transport.SentOf(HttpEndpointProtocol.ResponseContentType).Single().Body,
            out _,
            out var response).ShouldBeTrue();
        response.Status.ShouldBe(502);
        _transport.Warnings.Count.ShouldBe(1);
    }

    [Test]
    public void UnknownFieldInTheRequestIsIgnored()
    {
        var ran = false;
        _client.Map("GET", $"/{ModId}/x", _ =>
        {
            ran = true;
            return HttpEndpointResponse.Text(200, "ok");
        });
        _transport.Drain();
        AnnounceToHost();

        const string body = "{\"id\":\"r4\",\"method\":\"GET\",\"path\":\"/Acme.Dash/x\",\"future\":1}";
        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.RequestContentType, body);
        _transport.Drain();

        ran.ShouldBeTrue();
    }

    [Test]
    public void HandlerIsInvokedOnlyThroughThePosterAndNotBeforeDrain()
    {
        var ran = false;
        _client.Map("GET", $"/{ModId}/q", _ =>
        {
            ran = true;
            return HttpEndpointResponse.Text(200, "ok");
        });
        _transport.Drain();
        AnnounceToHost();

        var body = HttpEndpointProtocol.EncodeRequest("r5", "GET", $"/{ModId}/q", Request("GET", $"/{ModId}/q"));
        _transport.Deliver(ClientId, Host, HttpEndpointProtocol.RequestContentType, body);

        ran.ShouldBeFalse();
        _transport.Posted.Count.ShouldBe(1);

        _transport.Drain();
        ran.ShouldBeTrue();
    }

    [Test]
    public void UnknownRouteIsIgnoredWithOneWarning()
    {
        _client.Map("GET", $"/{ModId}/known", Ok);
        _transport.Drain();
        AnnounceToHost();

        for (var i = 0; i < 2; i++)
        {
            var body = HttpEndpointProtocol.EncodeRequest("r6", "GET", $"/{ModId}/missing", Request("GET", $"/{ModId}/missing"));
            _transport.Deliver(ClientId, Host, HttpEndpointProtocol.RequestContentType, body);
        }

        _transport.Drain();
        _transport.SentOf(HttpEndpointProtocol.ResponseContentType).ShouldBeEmpty();
        _transport.Warnings.Count.ShouldBe(1);
    }

    #endregion

    #region Release (task 2.4)

    [Test]
    public void ShutdownTwiceDoesNotThrow()
    {
        MapEndpoints(1);
        AnnounceToHost();

        Should.NotThrow(() =>
        {
            _client.Shutdown();
            _client.Shutdown();
        });
    }

    [Test]
    public void ShutdownWhenAnnouncedSendsOneEmptySnapshotAndEmptiesTheTable()
    {
        MapEndpoints(3);
        AnnounceToHost();
        _transport.Drain();
        var before = _transport.SentOf(HttpEndpointProtocol.RegisterContentType).Count();

        _client.Shutdown();

        var snapshots = _transport.SentOf(HttpEndpointProtocol.RegisterContentType).ToList();
        snapshots.Count.ShouldBe(before + 1);
        HttpEndpointProtocol.TryDecodeRegister(snapshots[^1].Body, out _, out var endpoints).ShouldBeTrue();
        endpoints.ShouldBeEmpty();
        _client.EndpointCount.ShouldBe(0);
    }

    [Test]
    public void ShutdownWithNoHostSendsNothing()
    {
        MapEndpoints(2);
        _client.Tick();

        _client.Shutdown();

        _transport.SentOf(HttpEndpointProtocol.RegisterContentType).ShouldBeEmpty();
        _transport.Unregistered.ShouldContain(ClientId);
    }

    #endregion
}
