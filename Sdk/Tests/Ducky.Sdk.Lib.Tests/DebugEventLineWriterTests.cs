using System.IO;
using System.Text;
using Ducky.Sdk.Contracts.DebugEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>JSON Lines output of the recorder file writer (task 1.2, design D7).</summary>
[TestFixture]
public class DebugEventLineWriterTests
{
    private const string At = "2026-11-02T01:02:03.456Z";

    private static DebugEventRecord Event(string kind, string? detail = null, JObject? fields = null) =>
        new(kind, detail, At, fields);

    private static List<string> NonEmptyLines(string text) =>
        text.Split('\n').Where(line => line.Length > 0).ToList();

    [Test]
    public void EachEventIsOneJsonObjectOnItsOwnLine_AndTheWholeIsNotOneDocument()
    {
        var sink = new StringWriter();
        var writer = new DebugEventLineWriter(sink, () => 1.5);

        writer.Write("local.a", new DebugEventBatch("local.a", 0,
            new[] { Event("hurt", "victim=player"), Event("heal") }));

        var text = sink.ToString();
        var lines = NonEmptyLines(text);
        lines.Count.ShouldBe(2);
        foreach (var line in lines)
        {
            Should.NotThrow(() => JsonLine.Parse(line));
        }

        text.EndsWith("\n").ShouldBeTrue();
        Should.Throw<JsonReaderException>(() => JToken.Parse(text));
    }

    [Test]
    public void Envelope_CarriesSdkSourceReporterKindAndRisingN()
    {
        var sink = new StringWriter();
        var writer = new DebugEventLineWriter(sink, () => 1.5);

        writer.Write("local.a", new DebugEventBatch("local.a", 0,
            new[] { Event("hurt", "victim=player"), Event("heal") }));

        var lines = NonEmptyLines(sink.ToString()).Select(JsonLine.Parse).ToList();
        lines[0]["n"]!.Value<long>().ShouldBe(1);
        lines[1]["n"]!.Value<long>().ShouldBe(2);
        lines[0]["t"]!.Value<double>().ShouldBe(1.5);
        lines[0]["at"]!.Value<string>().ShouldBe(At);
        lines[0]["source"]!.Value<string>().ShouldBe("sdk");
        lines[0]["reporter"]!.Value<string>().ShouldBe("local.a");
        lines[0]["kind"]!.Value<string>().ShouldBe("hurt");
        lines[0]["detail"]!.Value<string>().ShouldBe("victim=player");
        lines[1]["kind"]!.Value<string>().ShouldBe("heal");
    }

    [Test]
    public void AppendedRun_KeepsEveryLineParseableAndNRising()
    {
        var first = new StringWriter();
        new DebugEventLineWriter(first, () => 0)
            .Write("local.a", new DebugEventBatch("local.a", 0, new[] { Event("one"), Event("two") }));
        var firstText = first.ToString();
        var firstBytes = Encoding.UTF8.GetByteCount(firstText);

        var second = new StringWriter();
        new DebugEventLineWriter(second, () => 0, existingLines: 2, existingBytes: firstBytes)
            .Write("local.a", new DebugEventBatch("local.a", 0, new[] { Event("three") }));

        var all = NonEmptyLines(firstText + second.ToString()).Select(JsonLine.Parse).ToList();
        all.Count.ShouldBe(3);
        all.Select(l => l["n"]!.Value<long>()).ShouldBe(new long[] { 1, 2, 3 });
    }

    [Test]
    public void DetailWithNewlineAndQuote_StaysOnOneLineAndRoundTrips()
    {
        const string detail = "first line\nsecond \"quoted\" line\r\nthird";
        var sink = new StringWriter();

        new DebugEventLineWriter(sink, () => 0)
            .Write("local.a", new DebugEventBatch("local.a", 0, new[] { Event("text", detail) }));

        var lines = NonEmptyLines(sink.ToString());
        lines.Count.ShouldBe(1);
        JsonLine.Parse(lines[0])["detail"]!.Value<string>().ShouldBe(detail);
    }

    [Test]
    public void DroppedBatch_WritesOneDroppedLineWithCount()
    {
        var sink = new StringWriter();

        new DebugEventLineWriter(sink, () => 0)
            .Write("local.a", new DebugEventBatch("local.a", 500, Array.Empty<DebugEventRecord>()));

        var lines = NonEmptyLines(sink.ToString()).Select(JsonLine.Parse).ToList();
        lines.Count.ShouldBe(1);
        lines[0]["kind"]!.Value<string>().ShouldBe("debug-events-dropped");
        lines[0]["fields"]!["count"]!.Value<int>().ShouldBe(500);
        lines[0]["source"]!.Value<string>().ShouldBe("sdk");
    }

    [Test]
    public void DetailAndFields_AreOmittedWhenAbsent()
    {
        var sink = new StringWriter();

        new DebugEventLineWriter(sink, () => 0)
            .Write("local.a", new DebugEventBatch("local.a", 0, new[] { Event("plain") }));

        var line = JsonLine.Parse(NonEmptyLines(sink.ToString())[0]);
        line.ContainsKey("detail").ShouldBeFalse();
        line.ContainsKey("fields").ShouldBeFalse();
    }

    [Test]
    public void Cap_IsSetAfterAWholeLine_AndNothingFollows()
    {
        var sink = new StringWriter();
        var writer = new DebugEventLineWriter(sink, () => 0, capBytes: 200);
        var events = Enumerable.Range(0, 10).Select(i => Event("tick", $"step={i}")).ToArray();

        writer.Write("local.a", new DebugEventBatch("local.a", 0, events));

        writer.CapReached.ShouldBeTrue();
        writer.Bytes.ShouldBeGreaterThanOrEqualTo(200);
        var text = sink.ToString();
        Encoding.UTF8.GetByteCount(text).ShouldBe((int)writer.Bytes);
        foreach (var line in NonEmptyLines(text))
        {
            Should.NotThrow(() => JsonLine.Parse(line));
        }

        var before = text;
        writer.Write("local.a", new DebugEventBatch("local.a", 0, events));
        sink.ToString().ShouldBe(before);
    }
}
