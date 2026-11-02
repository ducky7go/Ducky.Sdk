using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ducky.Sdk.Contracts.HttpEndpoints;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Wire codec, path and method rules, version pick and size limits, with no Unity (tasks 1.1, 1.2).</summary>
[TestFixture]
public class HttpEndpointProtocolTests
{
    [Test]
    public void HelloRoundTrips()
    {
        var body = HttpEndpointProtocol.EncodeHello("Acme.Dash", new[] { 1, 2 });

        HttpEndpointProtocol.TryDecodeHello(body, out var client, out var versions).ShouldBeTrue();
        client.ShouldBe("Acme.Dash");
        versions.ShouldBe(new[] { 1, 2 });
    }

    [Test]
    public void ReadyRoundTrips()
    {
        var body = HttpEndpointProtocol.EncodeReady(1, new[] { 1 });

        HttpEndpointProtocol.TryDecodeReady(body, out var version, out var supported).ShouldBeTrue();
        version.ShouldBe(1);
        supported.ShouldBe(new[] { 1 });
    }

    [Test]
    public void RegisterRoundTrips()
    {
        var endpoints = new[]
        {
            new RouteKey("GET", "/Acme.Dash/status"),
            new RouteKey("POST", "/Acme.Dash/echo")
        };
        var body = HttpEndpointProtocol.EncodeRegister("Acme.Dash", endpoints);

        HttpEndpointProtocol.TryDecodeRegister(body, out var mod, out var decoded).ShouldBeTrue();
        mod.ShouldBe("Acme.Dash");
        decoded.ShouldBe(endpoints.ToList());
    }

    [Test]
    public void RegisteredRoundTrips()
    {
        var body = HttpEndpointProtocol.EncodeRegistered(
            new[] { new RouteKey("GET", "/Acme.Dash/status") },
            new[] { new RejectedRoute("GET", "/other/x", "the path must start with '/Acme.Dash'") });

        HttpEndpointProtocol.TryDecodeRegistered(body, out var accepted, out var rejected).ShouldBeTrue();
        accepted.ShouldBe(new List<RouteKey> { new("GET", "/Acme.Dash/status") });
        rejected.Count.ShouldBe(1);
        rejected[0].Path.ShouldBe("/other/x");
        rejected[0].Reason.ShouldContain("/Acme.Dash");
    }

    [Test]
    public void RequestRoundTripsKeepingQueryOrderAndRepeats()
    {
        var request = new HttpEndpointRequest
        {
            Method = "GET",
            Path = "/Acme.Dash/find",
            Query = new List<KeyValuePair<string, string>> { new("tag", "a"), new("tag", "b") },
            Headers = new Dictionary<string, string> { ["accept"] = "text/plain" },
            ContentType = "text/plain",
            Body = "hello"
        };
        var body = HttpEndpointProtocol.EncodeRequest("abc", "GET", "/Acme.Dash/find", request);

        HttpEndpointProtocol.TryDecodeRequest(body, out var id, out var decoded).ShouldBeTrue();
        id.ShouldBe("abc");
        decoded.Method.ShouldBe("GET");
        decoded.Path.ShouldBe("/Acme.Dash/find");
        decoded.Query.Select(p => p.Key + "=" + p.Value).ShouldBe(new[] { "tag=a", "tag=b" });
        decoded.Headers["accept"].ShouldBe("text/plain");
        decoded.ContentType.ShouldBe("text/plain");
        decoded.Body.ShouldBe("hello");
    }

    [Test]
    public void ResponseRoundTrips()
    {
        var response = HttpEndpointResponse.Json(200, "{\"ok\":true}");
        var body = HttpEndpointProtocol.EncodeResponse("abc", response);

        HttpEndpointProtocol.TryDecodeResponse(body, out var id, out var decoded).ShouldBeTrue();
        id.ShouldBe("abc");
        decoded.Status.ShouldBe(200);
        decoded.ContentType.ShouldBe("application/json; charset=utf-8");
        decoded.Body.ShouldBe("{\"ok\":true}");
    }

    [Test]
    public void UnknownFieldsAreIgnoredAtTopLevelAndInAnEntry()
    {
        const string body = "{\"mod\":\"Acme.Dash\",\"future\":{\"x\":1},\"endpoints\":[{\"method\":\"GET\",\"path\":\"/Acme.Dash/a\",\"extra\":true}]}";

        HttpEndpointProtocol.TryDecodeRegister(body, out var mod, out var endpoints).ShouldBeTrue();
        mod.ShouldBe("Acme.Dash");
        endpoints.ShouldBe(new List<RouteKey> { new("GET", "/Acme.Dash/a") });
    }

    [Test]
    public void BadMiddleEntryIsSkippedAndTheOthersKept()
    {
        const string body = "{\"mod\":\"Acme.Dash\",\"endpoints\":[" +
                            "{\"method\":\"GET\",\"path\":\"/Acme.Dash/a\"}," +
                            "{\"method\":\"GET\"}," +
                            "{\"method\":\"GET\",\"path\":\"/Acme.Dash/c\"}]}";

        HttpEndpointProtocol.TryDecodeRegister(body, out _, out var endpoints).ShouldBeTrue();
        endpoints.ShouldBe(new List<RouteKey> { new("GET", "/Acme.Dash/a"), new("GET", "/Acme.Dash/c") });
    }

    [Test]
    public void NonObjectBodyIsRejected()
    {
        HttpEndpointProtocol.TryDecodeRegister("not json", out _, out _).ShouldBeFalse();
        HttpEndpointProtocol.TryDecodeRegister("[1,2]", out _, out _).ShouldBeFalse();
    }

    [Test]
    public void ResponseWithoutIdIsUnreadableAndMissingStatusIsInvalid()
    {
        HttpEndpointProtocol.TryDecodeResponse("{\"status\":200}", out _, out _).ShouldBeFalse();

        HttpEndpointProtocol.TryDecodeResponse("{\"id\":\"x\"}", out var id, out var response).ShouldBeTrue();
        id.ShouldBe("x");
        HttpEndpointProtocol.IsValidStatus(response.Status).ShouldBeFalse();
    }

    [TestCase(42)]
    [TestCase(99)]
    [TestCase(600)]
    public void StatusOutsideRangeIsInvalid(int status)
    {
        HttpEndpointProtocol.IsValidStatus(status).ShouldBeFalse();
    }

    [TestCase(100)]
    [TestCase(200)]
    [TestCase(599)]
    public void StatusInsideRangeIsValid(int status)
    {
        HttpEndpointProtocol.IsValidStatus(status).ShouldBeTrue();
    }

    [Test]
    public void PickVersionGivesTheHighestCommonVersion()
    {
        HttpEndpointProtocol.PickVersion(new[] { 2 }, new[] { 1 }).ShouldBe(0);
        HttpEndpointProtocol.PickVersion(new[] { 1 }, new[] { 1, 2 }).ShouldBe(1);
        HttpEndpointProtocol.PickVersion(new[] { 1, 2 }, new[] { 1, 2 }).ShouldBe(2);
    }

    [TestCase("/Acme.Dash/api/stats", true)]
    [TestCase("/Acme.Dash", true)]
    [TestCase("/Acme.Dash/", true)]
    [TestCase("/other/x", false)]
    [TestCase("/Acme.Dashboard/x", false)]
    [TestCase("/Acme.Dash/../x", false)]
    [TestCase("/Acme.Dash/a?b=1", false)]
    [TestCase("/Acme.Dash//x", false)]
    [TestCase("/Acme.Dash/a b", false)]
    [TestCase("/Acme.Dash/a/./b", false)]
    public void PathRulesAcceptAndRejectAsDesigned(string path, bool accepted)
    {
        HttpEndpointProtocol.TryNormalizeEndpointPath("Acme.Dash", path, out _, out _).ShouldBe(accepted);
    }

    [Test]
    public void TrailingSlashIsIgnoredInTheNormalisedPath()
    {
        HttpEndpointProtocol.TryNormalizeEndpointPath("Acme.Dash", "/Acme.Dash/api/", out var normalized, out _);
        normalized.ShouldBe("/Acme.Dash/api");
    }

    [Test]
    public void PrefixReasonNamesTheRequiredPrefix()
    {
        HttpEndpointProtocol.TryNormalizeEndpointPath("Acme.Dash", "/other/stats", out _, out var reason);
        reason.ShouldContain("/Acme.Dash");
    }

    [Test]
    public void ModIdWithASpaceIsRefused()
    {
        HttpEndpointProtocol.IsValidModId("Acme Dash").ShouldBeFalse();
        HttpEndpointProtocol.IsValidModId("Acme.Dash").ShouldBeTrue();
        HttpEndpointProtocol.TryNormalizeEndpointPath("Acme Dash", "/Acme Dash/x", out _, out _).ShouldBeFalse();
    }

    [Test]
    public void SenderModIdIsTheClientIdWithoutTheSuffix()
    {
        HttpEndpointProtocol.TryGetSenderModId("Acme.Dash.http", out var modId).ShouldBeTrue();
        modId.ShouldBe("Acme.Dash");
        HttpEndpointProtocol.TryGetSenderModId("Acme.Dash", out _).ShouldBeFalse();
        HttpEndpointProtocol.TryGetSenderModId("httphost", out _).ShouldBeFalse();
    }

    [TestCase("get", "GET", true)]
    [TestCase("Patch", "PATCH", true)]
    [TestCase("DELETE", "DELETE", true)]
    [TestCase("TRACE", "TRACE", false)]
    [TestCase("OPTIONS", "OPTIONS", false)]
    public void MethodRulesAreCaseInsensitiveAndLimitedToFive(string method, string normalized, bool valid)
    {
        HttpEndpointProtocol.TryNormalizeMethod(method, out var result).ShouldBe(valid);
        result.ShouldBe(normalized);
    }

    [Test]
    public void BodyLimitCountsUtf8BytesNotCharacters()
    {
        HttpEndpointProtocol.IsWithinBodyLimit(new string('a', 65536)).ShouldBeTrue();
        HttpEndpointProtocol.IsWithinBodyLimit(new string('a', 65537)).ShouldBeFalse();

        // "é" is two UTF-8 bytes: 32768 of them fill the limit exactly, 32769 do not.
        HttpEndpointProtocol.IsWithinBodyLimit(new string('é', 32768)).ShouldBeTrue();
        HttpEndpointProtocol.IsWithinBodyLimit(new string('é', 32769)).ShouldBeFalse();
        Encoding.UTF8.GetByteCount(new string('é', 32768)).ShouldBe(65536);
    }

    [Test]
    public void UnpairedSurrogateIsNotValidText()
    {
        HttpEndpointProtocol.IsValidText("ok").ShouldBeTrue();
        HttpEndpointProtocol.IsValidText("😀").ShouldBeTrue();
        HttpEndpointProtocol.IsValidText("\uD83D").ShouldBeFalse();
    }

    [Test]
    public void HelperResponsesSetTheirContentTypes()
    {
        HttpEndpointResponse.Text(200, "ok").ContentType.ShouldBe("text/plain; charset=utf-8");
        HttpEndpointResponse.Json(200, "{}").ContentType.ShouldBe("application/json; charset=utf-8");
    }

    [Test]
    public void DocumentedExampleBodiesDecodeVerbatim()
    {
        // The same texts as the example block in docs/http-endpoints.md.
        const string hello = "{\"client\":\"Acme.Dash\",\"versions\":[1]}";
        const string ready = "{\"version\":1,\"supported\":[1]}";
        const string register = "{\"mod\":\"Acme.Dash\",\"endpoints\":[{\"method\":\"GET\",\"path\":\"/Acme.Dash/status\"}]}";
        const string registered = "{\"accepted\":[{\"method\":\"GET\",\"path\":\"/Acme.Dash/status\"}],\"rejected\":[{\"method\":\"GET\",\"path\":\"/other/x\",\"reason\":\"the path must start with '/Acme.Dash'\"}]}";
        const string request = "{\"id\":\"6f1c2a\",\"method\":\"GET\",\"path\":\"/Acme.Dash/find\",\"query\":[[\"tag\",\"a\"],[\"tag\",\"b\"]],\"headers\":{\"accept\":\"text/plain\"},\"contentType\":\"\",\"body\":\"\"}";
        const string response = "{\"id\":\"6f1c2a\",\"status\":200,\"headers\":{\"content-type\":\"text/plain; charset=utf-8\"},\"contentType\":\"text/plain; charset=utf-8\",\"body\":\"ok\"}";

        HttpEndpointProtocol.TryDecodeHello(hello, out var client, out var versions).ShouldBeTrue();
        client.ShouldBe("Acme.Dash");
        versions.ShouldBe(new[] { 1 });

        HttpEndpointProtocol.TryDecodeReady(ready, out var version, out _).ShouldBeTrue();
        version.ShouldBe(1);

        HttpEndpointProtocol.TryDecodeRegister(register, out _, out var endpoints).ShouldBeTrue();
        endpoints.ShouldBe(new List<RouteKey> { new("GET", "/Acme.Dash/status") });

        HttpEndpointProtocol.TryDecodeRegistered(registered, out var accepted, out var rejected).ShouldBeTrue();
        accepted.Count.ShouldBe(1);
        rejected[0].Reason.ShouldBe("the path must start with '/Acme.Dash'");

        HttpEndpointProtocol.TryDecodeRequest(request, out var id, out var decoded).ShouldBeTrue();
        id.ShouldBe("6f1c2a");
        decoded.Query.Select(p => p.Value).ShouldBe(new[] { "a", "b" });

        HttpEndpointProtocol.TryDecodeResponse(response, out _, out var answer).ShouldBeTrue();
        answer.Status.ShouldBe(200);
        answer.Body.ShouldBe("ok");

        // The documented version example: a client on [1, 2] meets a host on [1].
        HttpEndpointProtocol.PickVersion(new[] { 1, 2 }, new[] { 1 }).ShouldBe(1);
    }

    [Test]
    public void InvalidHeaderTextIsDroppedWhenEncoding()
    {
        var request = new HttpEndpointRequest
        {
            Method = "GET",
            Path = "/Acme.Dash/x",
            Headers = new Dictionary<string, string> { ["good"] = "v", ["bad"] = "\uD83D" }
        };
        var body = HttpEndpointProtocol.EncodeRequest("id", "GET", "/Acme.Dash/x", request);

        HttpEndpointProtocol.TryDecodeRequest(body, out _, out var decoded).ShouldBeTrue();
        decoded.Headers.Keys.ShouldBe(new[] { "good" });
    }
}
