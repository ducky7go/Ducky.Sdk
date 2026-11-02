using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Contracts.DebugEvents;

/// <summary>
/// Wire contract of the debug event channel: the recorder id, the content types, the supported versions and the
/// tolerant codec for the hello, ready and batch bodies. Plain C#, no Unity types, so every mod copy and the tests
/// use the same rules. See docs/debug-events.md.
/// </summary>
internal static class DebugEventProtocol
{
    /// <summary>The well-known hub id of the single recorder.</summary>
    public const string RecorderModId = "Ducky.DebugRecorder";

    /// <summary>Never versioned: reporter to recorder, lists the versions the reporter speaks.</summary>
    public const string HelloContentType = "ducky.debug-event.hello";

    /// <summary>Never versioned: recorder to reporter, names the chosen version (0 = none in common).</summary>
    public const string ReadyContentType = "ducky.debug-event.ready";

    /// <summary>Prefix of the versioned batch content type, followed by the version number.</summary>
    public const string BatchContentTypePrefix = "ducky.debug-event.batch.v";

    /// <summary>The batch versions this SDK build can read and write.</summary>
    public static readonly int[] SupportedVersions = { 1 };

    /// <summary>The hub client id suffix a reporter registers under: "&lt;modId&gt;.debug-events".</summary>
    public const string ReporterClientSuffix = ".debug-events";

    /// <summary>Content type of a batch of the given version.</summary>
    public static string BatchContentType(int version) => BatchContentTypePrefix + version;

    /// <summary>Reads the version from a batch content type. False for any other content type.</summary>
    public static bool TryParseBatchVersion(string? contentType, out int version)
    {
        version = 0;
        if (string.IsNullOrEmpty(contentType) || !contentType!.StartsWith(BatchContentTypePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(contentType.Substring(BatchContentTypePrefix.Length), out version) && version > 0;
    }

    /// <summary>
    /// The highest version both sides list, or 0 when they have none in common. Pure; used by the recorder when it
    /// answers a hello and by tests.
    /// </summary>
    public static int PickVersion(IEnumerable<int> reporterVersions, IEnumerable<int> recorderVersions)
    {
        var recorder = new HashSet<int>(recorderVersions);
        var common = reporterVersions.Where(recorder.Contains).ToList();
        return common.Count == 0 ? 0 : common.Max();
    }

    /// <summary>Encodes the hello body: <c>{"reporter":"&lt;modId&gt;","versions":[...]}</c>.</summary>
    public static string EncodeHello(string reporter, IEnumerable<int> versions)
    {
        return new JObject
        {
            ["reporter"] = reporter,
            ["versions"] = new JArray(versions)
        }.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>Decodes a hello body. False when the body is not a JSON object.</summary>
    public static bool TryDecodeHello(string body, out string reporter, out int[] versions)
    {
        reporter = string.Empty;
        versions = Array.Empty<int>();
        if (!TryParseObject(body, out var root))
        {
            return false;
        }

        reporter = ReadString(root, "reporter") ?? string.Empty;
        versions = ReadInts(root["versions"]);
        return true;
    }

    /// <summary>Encodes the ready body: <c>{"version":1,"supported":[1]}</c>. Version 0 means no common version.</summary>
    public static string EncodeReady(int version, IEnumerable<int> supported)
    {
        return new JObject
        {
            ["version"] = version,
            ["supported"] = new JArray(supported)
        }.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>Decodes a ready body. False when the body is not a JSON object.</summary>
    public static bool TryDecodeReady(string body, out int version, out int[] supported)
    {
        version = 0;
        supported = Array.Empty<int>();
        if (!TryParseObject(body, out var root))
        {
            return false;
        }

        version = ReadInt(root["version"]) ?? 0;
        supported = ReadInts(root["supported"]);
        return true;
    }

    /// <summary>
    /// Encodes a batch body: <c>{"reporter":"...","dropped":0,"events":[...]}</c>. Each event is
    /// <c>{"kind","detail"?,"at","fields"?}</c>; absent optional members are left out.
    /// </summary>
    public static string EncodeBatch(string reporter, int dropped, IEnumerable<DebugEventRecord> events)
    {
        var array = new JArray();
        foreach (var record in events)
        {
            var item = new JObject
            {
                ["kind"] = record.Kind,
                ["at"] = record.At
            };
            if (record.Detail != null)
            {
                item["detail"] = record.Detail;
            }

            if (record.Fields != null)
            {
                item["fields"] = record.Fields;
            }

            array.Add(item);
        }

        return new JObject
        {
            ["reporter"] = reporter,
            ["dropped"] = dropped,
            ["events"] = array
        }.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>
    /// Decodes a batch body leniently. Unknown members are ignored, a missing optional member is absent, and an event
    /// without a non-empty string <c>kind</c> is skipped without discarding the rest. False only when the body is not
    /// a JSON object.
    /// </summary>
    public static bool TryDecodeBatch(string body, out DebugEventBatch batch)
    {
        batch = new DebugEventBatch(string.Empty, 0, Array.Empty<DebugEventRecord>());
        if (!TryParseObject(body, out var root))
        {
            return false;
        }

        var reporter = ReadString(root, "reporter") ?? string.Empty;
        var dropped = ReadInt(root["dropped"]) ?? 0;
        var events = new List<DebugEventRecord>();
        if (root["events"] is JArray array)
        {
            foreach (var token in array)
            {
                if (token is JObject item && TryReadEvent(item, out var record))
                {
                    events.Add(record);
                }
            }
        }

        batch = new DebugEventBatch(reporter, Math.Max(0, dropped), events);
        return true;
    }

    private static bool TryReadEvent(JObject item, out DebugEventRecord record)
    {
        record = null!;
        var kind = ReadString(item, "kind");
        if (string.IsNullOrEmpty(kind))
        {
            return false;
        }

        record = new DebugEventRecord(
            kind!,
            ReadString(item, "detail"),
            ReadString(item, "at") ?? string.Empty,
            item["fields"] as JObject);
        return true;
    }

    private static bool TryParseObject(string? body, out JObject root)
    {
        root = null!;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            // Timestamps stay strings: DateParseHandling would turn "at" into a DateTime and change its text.
            using var reader = new JsonTextReader(new StringReader(body!)) { DateParseHandling = DateParseHandling.None };
            if (JToken.ReadFrom(reader) is JObject obj)
            {
                root = obj;
                return true;
            }
        }
        catch (Newtonsoft.Json.JsonException)
        {
            // Not JSON: the caller decides what to do with the message.
        }

        return false;
    }

    private static string? ReadString(JObject obj, string name)
    {
        return obj[name] is JValue value && value.Type == JTokenType.String ? (string?)value.Value : null;
    }

    private static int? ReadInt(JToken? token)
    {
        if (token is JValue value && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float))
        {
            try
            {
                return Convert.ToInt32(value.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (OverflowException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        return null;
    }

    private static int[] ReadInts(JToken? token)
    {
        if (token is not JArray array)
        {
            return Array.Empty<int>();
        }

        return array.Select(ReadInt).Where(v => v.HasValue).Select(v => v!.Value).ToArray();
    }
}

/// <summary>One event as carried on the wire and written to the recorder file.</summary>
internal sealed class DebugEventRecord
{
    public DebugEventRecord(string kind, string? detail, string at, JObject? fields)
    {
        Kind = kind;
        Detail = detail;
        At = at;
        Fields = fields;
    }

    /// <summary>Non-empty event kind.</summary>
    public string Kind { get; }

    /// <summary>Optional free text. Escaped by the writer, so it may contain line breaks.</summary>
    public string? Detail { get; }

    /// <summary>When the reporter created the event, ISO 8601 UTC. Empty when the sender did not say.</summary>
    public string At { get; }

    /// <summary>Optional structured fields.</summary>
    public JObject? Fields { get; }
}

/// <summary>A decoded batch: the reporting mod, the number of events dropped before it and its events.</summary>
internal sealed class DebugEventBatch
{
    public DebugEventBatch(string reporter, int dropped, IReadOnlyList<DebugEventRecord> events)
    {
        Reporter = reporter;
        Dropped = dropped;
        Events = events;
    }

    public string Reporter { get; }

    public int Dropped { get; }

    public IReadOnlyList<DebugEventRecord> Events { get; }
}
