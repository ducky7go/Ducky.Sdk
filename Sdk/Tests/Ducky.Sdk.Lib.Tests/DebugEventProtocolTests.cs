using System.IO;
using Ducky.Sdk.Contracts.DebugEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Codec, version pick and tolerant reads of the debug event wire contract (tasks 1.1 and 3.4).</summary>
[TestFixture]
public class DebugEventProtocolTests
{
    [Test]
    public void Hello_RoundTrips()
    {
        var body = DebugEventProtocol.EncodeHello("local.tester", new[] { 1, 2 });

        DebugEventProtocol.TryDecodeHello(body, out var reporter, out var versions).ShouldBeTrue();
        reporter.ShouldBe("local.tester");
        versions.ShouldBe(new[] { 1, 2 });
    }

    [Test]
    public void Ready_RoundTrips()
    {
        var body = DebugEventProtocol.EncodeReady(1, new[] { 1, 2 });

        DebugEventProtocol.TryDecodeReady(body, out var version, out var supported).ShouldBeTrue();
        version.ShouldBe(1);
        supported.ShouldBe(new[] { 1, 2 });
    }

    [Test]
    public void Batch_RoundTripsEventsWithOptionalMembers()
    {
        var fields = JObject.Parse("{\"count\":3,\"tag\":\"a\"}");
        var events = new[]
        {
            new DebugEventRecord("hurt", "victim=player", "2026-11-02T01:02:03.456Z", fields),
            new DebugEventRecord("tick", null, "2026-11-02T01:02:03.700Z", null)
        };
        var body = DebugEventProtocol.EncodeBatch("local.tester", 7, events);

        DebugEventProtocol.TryDecodeBatch(body, out var batch).ShouldBeTrue();
        batch.Reporter.ShouldBe("local.tester");
        batch.Dropped.ShouldBe(7);
        batch.Events.Count.ShouldBe(2);
        batch.Events[0].Kind.ShouldBe("hurt");
        batch.Events[0].Detail.ShouldBe("victim=player");
        batch.Events[0].At.ShouldBe("2026-11-02T01:02:03.456Z");
        JToken.DeepEquals(batch.Events[0].Fields!, fields).ShouldBeTrue();
        batch.Events[1].Detail.ShouldBeNull();
        batch.Events[1].Fields.ShouldBeNull();
    }

    [Test]
    public void Batch_IgnoresUnknownFields()
    {
        const string body = "{\"reporter\":\"local.a\",\"future\":{\"x\":1},\"dropped\":0," +
                            "\"events\":[{\"kind\":\"hurt\",\"at\":\"t\",\"extra\":true}]}";

        DebugEventProtocol.TryDecodeBatch(body, out var batch).ShouldBeTrue();
        batch.Events.Count.ShouldBe(1);
        batch.Events[0].Kind.ShouldBe("hurt");
    }

    [Test]
    public void Batch_SkipsOnlyTheEventWithoutKind()
    {
        const string body = "{\"reporter\":\"local.a\",\"events\":[" +
                            "{\"kind\":\"a\",\"at\":\"t\"}," +
                            "{\"detail\":\"no kind here\",\"at\":\"t\"}," +
                            "{\"kind\":\"c\",\"at\":\"t\"}]}";

        DebugEventProtocol.TryDecodeBatch(body, out var batch).ShouldBeTrue();
        batch.Events.Select(e => e.Kind).ToArray().ShouldBe(new[] { "a", "c" });
    }

    [Test]
    public void Batch_NotAnObject_IsRejected()
    {
        DebugEventProtocol.TryDecodeBatch("not json", out _).ShouldBeFalse();
        DebugEventProtocol.TryDecodeBatch("[1,2]", out _).ShouldBeFalse();
        DebugEventProtocol.TryDecodeBatch("", out _).ShouldBeFalse();
    }

    [TestCase(new[] { 2 }, new[] { 1 }, 0)]
    [TestCase(new[] { 1, 2 }, new[] { 1 }, 1)]
    [TestCase(new[] { 1 }, new[] { 1, 2 }, 1)]
    [TestCase(new[] { 1, 2, 3 }, new[] { 2, 3 }, 3)]
    public void PickVersion_ReturnsHighestCommonOrZero(int[] reporter, int[] recorder, int expected)
    {
        DebugEventProtocol.PickVersion(reporter, recorder).ShouldBe(expected);
    }

    [Test]
    public void TryParseBatchVersion_ReadsOnlyBatchTypes()
    {
        DebugEventProtocol.TryParseBatchVersion("ducky.debug-event.batch.v9", out var version).ShouldBeTrue();
        version.ShouldBe(9);
        DebugEventProtocol.TryParseBatchVersion(DebugEventProtocol.HelloContentType, out _).ShouldBeFalse();
        DebugEventProtocol.TryParseBatchVersion(DebugEventProtocol.ReadyContentType, out _).ShouldBeFalse();
        DebugEventProtocol.TryParseBatchVersion("ducky.debug-event.batch.vX", out _).ShouldBeFalse();
        DebugEventProtocol.TryParseBatchVersion(null, out _).ShouldBeFalse();
    }

    [Test]
    public void DocumentedExamples_AreAcceptedByTheDecoders()
    {
        // The example bodies from docs/debug-events.md, repeated verbatim.
        const string hello = "{\"reporter\":\"Ducky.Terminals\",\"versions\":[1]}";
        const string ready = "{\"version\":1,\"supported\":[1]}";
        const string batch =
            "{\"reporter\":\"Ducky.Terminals\",\"dropped\":500,\"events\":[" +
            "{\"kind\":\"hurt\",\"detail\":\"victim=player\",\"at\":\"2026-11-02T01:02:03.456Z\",\"fields\":{\"damage\":12}}," +
            "{\"kind\":\"state\",\"at\":\"2026-11-02T01:02:03.500Z\"}]}";

        DebugEventProtocol.TryDecodeHello(hello, out var reporter, out var helloVersions).ShouldBeTrue();
        reporter.ShouldBe("Ducky.Terminals");
        helloVersions.ShouldBe(new[] { 1 });

        DebugEventProtocol.TryDecodeReady(ready, out var version, out var supported).ShouldBeTrue();
        version.ShouldBe(1);
        supported.ShouldBe(new[] { 1 });

        DebugEventProtocol.TryDecodeBatch(batch, out var decoded).ShouldBeTrue();
        decoded.Dropped.ShouldBe(500);
        decoded.Events.Count.ShouldBe(2);
        decoded.Events[0].Detail.ShouldBe("victim=player");
        decoded.Events[1].Kind.ShouldBe("state");
    }
}
