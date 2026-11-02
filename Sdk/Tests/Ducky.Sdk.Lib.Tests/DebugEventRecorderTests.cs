using System.IO;
using System.Text;
using Ducky.Sdk.Contracts.DebugEvents;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Recorder decisions with a fake hub and an in-memory writer, and the file and claim glue (tasks 3.1, 3.2).</summary>
[TestFixture]
public class DebugEventRecorderTests
{
    private const string Sender = "local.tester.debug-events";
    private const string Batch1 = "ducky.debug-event.batch.v1";
    private const string Batch9 = "ducky.debug-event.batch.v9";

    private sealed class ThrowingTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value) => throw new IOException("disk full");

        public override void Write(string? value) => throw new IOException("disk full");

        public override void Flush()
        {
        }
    }

    private sealed class Harness
    {
        public Harness(TextWriter? sink = null, long capBytes = DebugEventLineWriter.DefaultCapBytes)
        {
            Sink = sink ?? new StringWriter();
            Writer = new DebugEventLineWriter(Sink, () => 2.0, capBytes: capBytes);
            Recorder = new DebugEventRecorder(
                Host,
                Writer,
                message => Warnings.Add(message),
                message => Errors.Add(message),
                () => CloseCalls++);
        }

        public FakeDebugEventHost Host { get; } = new();

        public TextWriter Sink { get; }

        public DebugEventLineWriter Writer { get; }

        public DebugEventRecorder Recorder { get; }

        public List<string> Warnings { get; } = new();

        public List<string> Errors { get; } = new();

        public int CloseCalls { get; set; }

        public void Send(string contentType, string body) =>
            Recorder.HandleMessage(Sender, contentType, body);
    }

    private static string BatchBody(params string[] kinds)
    {
        var events = kinds.Select(k => new DebugEventRecord(k, "victim=player", "2026-11-02T01:02:03.456Z", null))
            .ToArray();
        return DebugEventProtocol.EncodeBatch("local.tester", 0, events);
    }

    private static List<string> Lines(TextWriter sink) =>
        sink.ToString()!.Split('\n').Where(line => line.Length > 0).ToList();

    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "DuckyDebugEventTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        DebugEventRecorder.Release();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort: the temp folder is the OS's to clean up.
        }
    }

    [Test]
    public void Hello_IsAnsweredWithReadyNamingTheHighestCommonVersion()
    {
        var harness = new Harness();

        harness.Send(DebugEventProtocol.HelloContentType, DebugEventProtocol.EncodeHello("local.tester", new[] { 1, 2 }));

        var ready = harness.Host.Sent.Single();
        ready.From.ShouldBe(DebugEventProtocol.RecorderModId);
        ready.To.ShouldBe(Sender);
        ready.ContentType.ShouldBe(DebugEventProtocol.ReadyContentType);
        DebugEventProtocol.TryDecodeReady(ready.Body, out var version, out var supported).ShouldBeTrue();
        version.ShouldBe(1);
        supported.ShouldBe(new[] { 1 });
    }

    [Test]
    public void Hello_WithNoCommonVersion_IsAnsweredWithZero()
    {
        var harness = new Harness();

        harness.Send(DebugEventProtocol.HelloContentType, DebugEventProtocol.EncodeHello("local.tester", new[] { 2 }));

        DebugEventProtocol.TryDecodeReady(harness.Host.Sent.Single().Body, out var version, out _);
        version.ShouldBe(0);
    }

    [Test]
    public void VersionOneBatch_WithoutHello_IsWritten()
    {
        var harness = new Harness();

        harness.Send(Batch1, BatchBody("hurt", "heal"));

        Lines(harness.Sink).Count.ShouldBe(2);
        harness.Warnings.ShouldBeEmpty();
    }

    [Test]
    public void UnknownVersion_IsWrittenNowhere_AndWarnsOnceForThreeMessagesFromOneSender()
    {
        var harness = new Harness();

        harness.Send(Batch9, BatchBody("a"));
        harness.Send(Batch9, BatchBody("b"));
        harness.Send(Batch9, BatchBody("c"));

        harness.Sink.ToString().ShouldBeEmpty();
        harness.Warnings.Count.ShouldBe(1);
        harness.Warnings[0].ShouldContain(Batch9);
        harness.Host.Sent.ShouldBeEmpty();
    }

    [Test]
    public void FileAndPathFields_InTheBody_HaveNoEffect()
    {
        var harness = new Harness();
        const string body = "{\"reporter\":\"local.tester\",\"file\":\"x\",\"path\":\"C:/evil.txt\",\"dropped\":0," +
                            "\"events\":[{\"kind\":\"hurt\",\"at\":\"t\",\"file\":\"y\"}]}";

        harness.Send(Batch1, body);

        var lines = Lines(harness.Sink).Select(JObject.Parse).ToList();
        lines.Count.ShouldBe(1);
        lines[0].ContainsKey("file").ShouldBeFalse();
        lines[0].ContainsKey("path").ShouldBeFalse();
        harness.Sink.ToString().ShouldNotContain("evil");
    }

    [Test]
    public void WriteFailure_ReleasesTheRole_WithOneError_AndLaterMessagesAreIgnored()
    {
        var harness = new Harness(sink: new ThrowingTextWriter());

        harness.Send(Batch1, BatchBody("hurt"));
        harness.Send(Batch1, BatchBody("hurt"));

        harness.Recorder.IsActive.ShouldBeFalse();
        harness.Errors.Count.ShouldBe(1);
        harness.CloseCalls.ShouldBe(1);
        harness.Host.Unregistered.ShouldContain(DebugEventProtocol.RecorderModId);
    }

    [Test]
    public void SizeCap_ReleasesTheRole_AfterACompleteLine_WithOneWarning()
    {
        var harness = new Harness(capBytes: 50);

        harness.Send(Batch1, BatchBody("a-long-enough-kind", "another"));
        harness.Send(Batch1, BatchBody("ignored-after-cap"));

        harness.Recorder.IsActive.ShouldBeFalse();
        harness.Warnings.Count.ShouldBe(1);
        var lines = Lines(harness.Sink);
        lines.Count.ShouldBeGreaterThan(0);
        lines.All(line => line.StartsWith("{", StringComparison.Ordinal) && line.EndsWith("}", StringComparison.Ordinal))
            .ShouldBeTrue();
        harness.Sink.ToString().ShouldNotContain("ignored-after-cap");
    }

    [Test]
    public void Stop_TwiceDoesNothingTheSecondTime()
    {
        var harness = new Harness();

        harness.Recorder.Stop();
        harness.Recorder.Stop();

        harness.CloseCalls.ShouldBe(1);
        harness.Host.Unregistered.Count(id => id == DebugEventProtocol.RecorderModId).ShouldBe(1);
        harness.Recorder.IsActive.ShouldBeFalse();
    }

    [Test]
    public void Start_WhenTheAliasIsAlreadyTaken_ReturnsFalse_AndDoesNotTouchTheFile()
    {
        var host = new FakeDebugEventHost();
        host.ModIds.Add(DebugEventProtocol.RecorderModId);
        var warnings = new List<string>();
        var path = Path.Combine(_directory, "run.jsonl");

        var started = DebugEventRecorder.Start(path, false, host, warnings.Add, _ => { });

        started.ShouldBeFalse();
        File.Exists(path).ShouldBeFalse();
        warnings.Count.ShouldBe(1);
        DebugEventRecorder.IsRecorderActive.ShouldBeFalse();
    }

    [Test]
    public void Start_WritesJsonLines_AndStopTwiceDoesNotThrow()
    {
        var host = new FakeDebugEventHost();
        var path = Path.Combine(_directory, "run.jsonl");

        DebugEventRecorder.Start(path, false, host, _ => { }, _ => { }).ShouldBeTrue();
        DebugEventRecorder.IsRecorderActive.ShouldBeTrue();
        host.ModIds.ShouldContain(DebugEventProtocol.RecorderModId);

        host.Handlers[DebugEventProtocol.RecorderModId](Sender, Batch1, BatchBody("hurt", "heal"));
        DebugEventRecorder.Release();
        DebugEventRecorder.Release();

        DebugEventRecorder.IsRecorderActive.ShouldBeFalse();
        host.ModIds.ShouldNotContain(DebugEventProtocol.RecorderModId);
        var lines = File.ReadAllLines(path).Where(l => l.Length > 0).Select(JObject.Parse).ToList();
        lines.Count.ShouldBe(2);
        lines[1]["n"]!.Value<long>().ShouldBe(2);
        lines[0]["source"]!.Value<string>().ShouldBe("sdk");
    }

    [Test]
    public void SecondStart_IsRefused_AndTheFirstRecorderKeepsRunning()
    {
        var host = new FakeDebugEventHost();
        var first = Path.Combine(_directory, "first.jsonl");
        var second = Path.Combine(_directory, "second.jsonl");
        var warnings = new List<string>();
        DebugEventRecorder.Start(first, false, host, _ => { }, _ => { }).ShouldBeTrue();

        DebugEventRecorder.Start(second, false, host, warnings.Add, _ => { }).ShouldBeFalse();

        warnings.Count.ShouldBe(1);
        File.Exists(second).ShouldBeFalse();
        DebugEventRecorder.IsRecorderActive.ShouldBeTrue();
        host.Handlers[DebugEventProtocol.RecorderModId](Sender, Batch1, BatchBody("still-writing"));
        File.ReadAllText(first).ShouldContain("still-writing");
    }

    [Test]
    public void Append_ContinuesLineNumbers()
    {
        var path = Path.Combine(_directory, "append.jsonl");
        File.WriteAllText(path, "{\"n\":1}\n{\"n\":2}\n");
        var host = new FakeDebugEventHost();

        DebugEventRecorder.Start(path, true, host, _ => { }, _ => { }).ShouldBeTrue();
        host.Handlers[DebugEventProtocol.RecorderModId](Sender, Batch1, BatchBody("third"));
        DebugEventRecorder.Release();

        var numbers = File.ReadAllLines(path).Where(l => l.Length > 0).Select(l => JObject.Parse(l)["n"]!.Value<long>());
        numbers.ShouldBe(new long[] { 1, 2, 3 });
    }

    [Test]
    public void Append_AfterAPartialLine_StartsANewLine()
    {
        var path = Path.Combine(_directory, "partial.jsonl");
        File.WriteAllText(path, "{\"n\":1}");
        var host = new FakeDebugEventHost();

        DebugEventRecorder.Start(path, true, host, _ => { }, _ => { }).ShouldBeTrue();
        host.Handlers[DebugEventProtocol.RecorderModId](Sender, Batch1, BatchBody("next"));
        DebugEventRecorder.Release();

        var lines = File.ReadAllLines(path).Where(l => l.Length > 0).ToList();
        lines.Count.ShouldBe(2);
        Should.NotThrow(() => JObject.Parse(lines[1]));
    }
}
