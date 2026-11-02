using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Contracts.HttpEndpoints;

/// <summary>A method and path pair, the identity of one endpoint.</summary>
internal readonly record struct RouteKey(string Method, string Path);

/// <summary>One endpoint a host rejected in a <c>registered</c> answer, with the reason.</summary>
internal readonly record struct RejectedRoute(string Method, string Path, string Reason);

/// <summary>
/// Wire contract of the HTTP endpoint channel: ids, content types, versions, limits, path and method rules and the
/// tolerant codec for the six message bodies. Plain C#, no Unity types, so every mod copy and the tests share one set
/// of rules. See docs/http-endpoints.md.
/// </summary>
internal static class HttpEndpointProtocol
{
    /// <summary>The well-known hub id of the single HTTP host.</summary>
    public const string HostModId = "httphost";

    /// <summary>Suffix of the hub client id a mod registers under: "&lt;modId&gt;.http". It is also the sender identity.</summary>
    public const string ClientSuffix = ".http";

    /// <summary>Never versioned: client to host, lists the versions the client speaks.</summary>
    public const string HelloContentType = "ducky.http.hello";

    /// <summary>Never versioned: host to client, names the chosen version (0 = none in common).</summary>
    public const string ReadyContentType = "ducky.http.ready";

    /// <summary>Client to host: the full snapshot of the client's endpoints.</summary>
    public const string RegisterContentType = "ducky.http.register.v1";

    /// <summary>Host to client: which endpoints of a snapshot were accepted and which were rejected.</summary>
    public const string RegisteredContentType = "ducky.http.registered.v1";

    /// <summary>Host to client: one external request to forward to the endpoint's owner.</summary>
    public const string RequestContentType = "ducky.http.request.v1";

    /// <summary>Client to host: the answer to one forwarded request.</summary>
    public const string ResponseContentType = "ducky.http.response.v1";

    /// <summary>The protocol versions this SDK build can read and write.</summary>
    public static readonly int[] SupportedVersions = { 1 };

    /// <summary>Largest request or response body, in UTF-8 bytes (design D10).</summary>
    public const int MaxBodyBytes = 65536;

    /// <summary>Most endpoints one mod may have accepted by the host (design D10).</summary>
    public const int MaxEndpointsPerMod = 64;

    /// <summary>Most requests the host keeps in flight at once (design D10).</summary>
    public const int MaxInFlightRequests = 64;

    /// <summary>Time the host waits for an answer before it replies 504 (design D10).</summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    /// <summary>How long a mod's hub client may be absent before the host drops its routes.</summary>
    public static readonly TimeSpan PresenceGrace = TimeSpan.FromSeconds(5);

    /// <summary>Minimum time between two looks at the hub's id list for the host (design D10).</summary>
    public static readonly TimeSpan HostCheckInterval = TimeSpan.FromSeconds(5);

    /// <summary>Period of the main-thread tick that both the client and the host run.</summary>
    public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private static readonly string[] Methods = { "GET", "POST", "PUT", "PATCH", "DELETE" };

    /// <summary>The hub client id of a mod: "&lt;modId&gt;.http".</summary>
    public static string ClientId(string modId) => modId + ClientSuffix;

    /// <summary>Reads the sender mod id from a hub client id by removing the ".http" suffix. False for other ids.</summary>
    public static bool TryGetSenderModId(string? clientId, out string modId)
    {
        modId = string.Empty;
        if (clientId == null || !clientId.EndsWith(ClientSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        var candidate = clientId.Substring(0, clientId.Length - ClientSuffix.Length);
        if (!IsValidModId(candidate))
        {
            return false;
        }

        modId = candidate;
        return true;
    }

    /// <summary>A mod id uses only ASCII letters, digits, '.', '_' and '-' and is not empty.</summary>
    public static bool IsValidModId(string? modId)
    {
        if (string.IsNullOrEmpty(modId))
        {
            return false;
        }

        foreach (var c in modId!)
        {
            if (!IsAsciiLetterOrDigit(c) && c != '.' && c != '_' && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Upper-cases a method and checks it is one of GET, POST, PUT, PATCH or DELETE.</summary>
    public static bool TryNormalizeMethod(string? method, out string normalized)
    {
        normalized = (method ?? string.Empty).Trim().ToUpperInvariant();
        return Array.IndexOf(Methods, normalized) >= 0;
    }

    /// <summary>
    /// Checks an endpoint path for mod <paramref name="modId"/> (design D6). The path must be "/&lt;modId&gt;" or start with
    /// "/&lt;modId&gt;/"; one trailing slash is ignored. It may contain only ASCII letters, digits and '.', '_', '-', '~',
    /// '/', and no empty, "." or ".." segment. Returns the normalised path, or false with a reason.
    /// </summary>
    public static bool TryNormalizeEndpointPath(string modId, string? path, out string normalized, out string reason)
    {
        normalized = string.Empty;
        reason = string.Empty;
        if (!IsValidModId(modId))
        {
            reason = "the mod id may only contain letters, digits, '.', '_' and '-'";
            return false;
        }

        if (string.IsNullOrEmpty(path))
        {
            reason = "the path is empty";
            return false;
        }

        var value = path!;
        if (value.EndsWith("/", StringComparison.Ordinal))
        {
            value = value.Substring(0, value.Length - 1);
        }

        var prefix = "/" + modId;
        if (value != prefix && !value.StartsWith(prefix + "/", StringComparison.Ordinal))
        {
            reason = "the path must start with '" + prefix + "'";
            return false;
        }

        foreach (var c in value)
        {
            if (!IsAsciiLetterOrDigit(c) && c != '.' && c != '_' && c != '-' && c != '~' && c != '/')
            {
                reason = "the path contains a character that is not allowed";
                return false;
            }
        }

        if (value != prefix)
        {
            foreach (var segment in value.Substring(prefix.Length + 1).Split('/'))
            {
                if (segment.Length == 0 || segment == "." || segment == "..")
                {
                    reason = "the path contains an empty, '.' or '..' segment";
                    return false;
                }
            }
        }

        normalized = value;
        return true;
    }

    /// <summary>
    /// Normalises the path of an external request for a route lookup: one trailing slash is ignored and the path must start with '/'.
    /// Matching stays exact, so nothing else is changed.
    /// </summary>
    public static bool TryNormalizeRoutePath(string? path, out string normalized)
    {
        normalized = path ?? string.Empty;
        if (normalized.EndsWith("/", StringComparison.Ordinal) && normalized.Length > 1)
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }

        return normalized.StartsWith("/", StringComparison.Ordinal);
    }

    /// <summary>
    /// The highest version both sides list, or 0 when they have none in common. The client's versions come first,
    /// so <c>PickVersion([2], [1])</c> is 0 and <c>PickVersion([1], [1,2])</c> is 1.
    /// </summary>
    public static int PickVersion(IEnumerable<int> clientVersions, IEnumerable<int> hostVersions)
    {
        var host = new HashSet<int>(hostVersions);
        var common = clientVersions.Where(host.Contains).ToList();
        return common.Count == 0 ? 0 : common.Max();
    }

    /// <summary>True when the status is a valid HTTP status, 100 to 599.</summary>
    public static bool IsValidStatus(int status) => status >= 100 && status <= 599;

    /// <summary>True when the text is at most <see cref="MaxBodyBytes"/> UTF-8 bytes. Null counts as empty.</summary>
    public static bool IsWithinBodyLimit(string? text) =>
        text == null || Encoding.UTF8.GetByteCount(text) <= MaxBodyBytes;

    /// <summary>True when the text has no unpaired surrogate, so it can be sent as valid UTF-8.</summary>
    public static bool IsValidText(string? text)
    {
        if (text == null)
        {
            return true;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                {
                    return false;
                }

                i++;
            }
            else if (char.IsLowSurrogate(c))
            {
                return false;
            }
        }

        return true;
    }

    #region Hello and ready

    /// <summary>Encodes the hello body: <c>{"client":"&lt;modId&gt;","versions":[...]}</c>.</summary>
    public static string EncodeHello(string client, IEnumerable<int> versions)
    {
        return new JObject
        {
            ["client"] = client,
            ["versions"] = new JArray(versions)
        }.ToString(Formatting.None);
    }

    /// <summary>Decodes a hello body. False when the body is not a JSON object.</summary>
    public static bool TryDecodeHello(string body, out string client, out int[] versions)
    {
        client = string.Empty;
        versions = Array.Empty<int>();
        if (!TryParseObject(body, out var root))
        {
            return false;
        }

        client = ReadString(root, "client") ?? string.Empty;
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
        }.ToString(Formatting.None);
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

    #endregion

    #region Registration

    /// <summary>Encodes a full snapshot: <c>{"mod":"&lt;modId&gt;","endpoints":[{"method","path"}]}</c>.</summary>
    public static string EncodeRegister(string mod, IEnumerable<RouteKey> endpoints)
    {
        var array = new JArray();
        foreach (var endpoint in endpoints)
        {
            array.Add(new JObject
            {
                ["method"] = endpoint.Method,
                ["path"] = endpoint.Path
            });
        }

        return new JObject
        {
            ["mod"] = mod,
            ["endpoints"] = array
        }.ToString(Formatting.None);
    }

    /// <summary>
    /// Decodes a snapshot leniently. Unknown members are ignored. An entry whose <c>method</c> or <c>path</c> is missing or
    /// not text is skipped without discarding the rest. The host checks the values that remain. False when the body is
    /// not a JSON object.
    /// </summary>
    public static bool TryDecodeRegister(string body, out string mod, out List<RouteKey> endpoints)
    {
        mod = string.Empty;
        endpoints = new List<RouteKey>();
        if (!TryParseObject(body, out var root))
        {
            return false;
        }

        mod = ReadString(root, "mod") ?? string.Empty;
        if (root["endpoints"] is JArray array)
        {
            foreach (var item in array)
            {
                if (item is not JObject entry)
                {
                    continue;
                }

                var method = ReadString(entry, "method");
                var path = ReadString(entry, "path");
                if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(path))
                {
                    continue;
                }

                endpoints.Add(new RouteKey(method!, path!));
            }
        }

        return true;
    }

    /// <summary>
    /// Encodes the answer to a snapshot: <c>{"accepted":[{"method","path"}],"rejected":[{"method","path","reason"}]}</c>.
    /// </summary>
    public static string EncodeRegistered(IEnumerable<RouteKey> accepted, IEnumerable<RejectedRoute> rejected)
    {
        var acceptedArray = new JArray();
        foreach (var route in accepted)
        {
            acceptedArray.Add(new JObject
            {
                ["method"] = route.Method,
                ["path"] = route.Path
            });
        }

        var rejectedArray = new JArray();
        foreach (var route in rejected)
        {
            rejectedArray.Add(new JObject
            {
                ["method"] = route.Method,
                ["path"] = route.Path,
                ["reason"] = route.Reason
            });
        }

        return new JObject
        {
            ["accepted"] = acceptedArray,
            ["rejected"] = rejectedArray
        }.ToString(Formatting.None);
    }

    /// <summary>Decodes the answer to a snapshot leniently. False when the body is not a JSON object.</summary>
    public static bool TryDecodeRegistered(string body, out List<RouteKey> accepted, out List<RejectedRoute> rejected)
    {
        accepted = new List<RouteKey>();
        rejected = new List<RejectedRoute>();
        if (!TryParseObject(body, out var root))
        {
            return false;
        }

        if (root["accepted"] is JArray acceptedArray)
        {
            foreach (var item in acceptedArray.OfType<JObject>())
            {
                var method = ReadString(item, "method");
                var path = ReadString(item, "path");
                if (!string.IsNullOrEmpty(method) && !string.IsNullOrEmpty(path))
                {
                    accepted.Add(new RouteKey(method!, path!));
                }
            }
        }

        if (root["rejected"] is JArray rejectedArray)
        {
            foreach (var item in rejectedArray.OfType<JObject>())
            {
                var method = ReadString(item, "method") ?? string.Empty;
                var path = ReadString(item, "path") ?? string.Empty;
                var reason = ReadString(item, "reason") ?? string.Empty;
                rejected.Add(new RejectedRoute(method, path, reason));
            }
        }

        return true;
    }

    #endregion

    #region Requests and responses

    /// <summary>
    /// Encodes a forwarded request. <paramref name="method"/> and <paramref name="path"/> are the normalised values the host
    /// routed on. Header names are lower-case; a header whose name or value is not valid text is dropped.
    /// </summary>
    public static string EncodeRequest(string id, string method, string path, HttpEndpointRequest request)
    {
        var query = new JArray();
        foreach (var pair in request.Query ?? Array.Empty<KeyValuePair<string, string>>())
        {
            query.Add(new JArray(pair.Key ?? string.Empty, pair.Value ?? string.Empty));
        }

        return new JObject
        {
            ["id"] = id,
            ["method"] = method,
            ["path"] = path,
            ["query"] = query,
            ["headers"] = EncodeHeaders(request.Headers),
            ["contentType"] = request.ContentType ?? string.Empty,
            ["body"] = request.Body ?? string.Empty
        }.ToString(Formatting.None);
    }

    /// <summary>Decodes a forwarded request leniently. False when the body is not an object or lacks id, method or path.</summary>
    public static bool TryDecodeRequest(string body, out string id, out HttpEndpointRequest request)
    {
        id = string.Empty;
        request = new HttpEndpointRequest();
        if (!TryParseObject(body, out var root))
        {
            return false;
        }

        var readId = ReadString(root, "id");
        var method = ReadString(root, "method");
        var path = ReadString(root, "path");
        if (string.IsNullOrEmpty(readId) || string.IsNullOrEmpty(method) || string.IsNullOrEmpty(path))
        {
            return false;
        }

        id = readId!;
        var query = new List<KeyValuePair<string, string>>();
        if (root["query"] is JArray pairs)
        {
            foreach (var pair in pairs.OfType<JArray>())
            {
                if (pair.Count == 2 && pair[0].Type == JTokenType.String && pair[1].Type == JTokenType.String)
                {
                    query.Add(new KeyValuePair<string, string>(
                        (string?)pair[0] ?? string.Empty,
                        (string?)pair[1] ?? string.Empty));
                }
            }
        }

        request = new HttpEndpointRequest
        {
            Method = method!,
            Path = path!,
            Query = query,
            Headers = DecodeHeaders(root["headers"]),
            ContentType = ReadString(root, "contentType") ?? string.Empty,
            Body = ReadString(root, "body") ?? string.Empty
        };
        return true;
    }

    /// <summary>Encodes the answer to one forwarded request, with the request's correlation id.</summary>
    public static string EncodeResponse(string id, HttpEndpointResponse response)
    {
        return new JObject
        {
            ["id"] = id,
            ["status"] = response.Status,
            ["headers"] = EncodeHeaders(response.Headers),
            ["contentType"] = response.ContentType ?? string.Empty,
            ["body"] = response.Body ?? string.Empty
        }.ToString(Formatting.None);
    }

    /// <summary>
    /// Decodes an answer. False when the body is not an object or lacks <c>id</c>, so it cannot be matched to a request.
    /// A missing or non-numeric status is read as 0; the caller checks it with <see cref="IsValidStatus"/>.
    /// </summary>
    public static bool TryDecodeResponse(string body, out string id, out HttpEndpointResponse response)
    {
        id = string.Empty;
        response = new HttpEndpointResponse();
        if (!TryParseObject(body, out var root))
        {
            return false;
        }

        var readId = ReadString(root, "id");
        if (string.IsNullOrEmpty(readId))
        {
            return false;
        }

        id = readId!;
        response = new HttpEndpointResponse
        {
            Status = ReadInt(root["status"]) ?? 0,
            Headers = DecodeHeaders(root["headers"]),
            ContentType = ReadString(root, "contentType") ?? string.Empty,
            Body = ReadString(root, "body") ?? string.Empty
        };
        return true;
    }

    #endregion

    private static JObject EncodeHeaders(IReadOnlyDictionary<string, string>? headers)
    {
        var result = new JObject();
        if (headers == null)
        {
            return result;
        }

        foreach (var header in headers)
        {
            if (string.IsNullOrEmpty(header.Key) || !IsValidText(header.Key) || !IsValidText(header.Value))
            {
                continue;
            }

            result[header.Key.ToLowerInvariant()] = header.Value ?? string.Empty;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string> DecodeHeaders(JToken? token)
    {
        var headers = new Dictionary<string, string>();
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties())
            {
                if (property.Value.Type == JTokenType.String)
                {
                    headers[property.Name.ToLowerInvariant()] = (string)property.Value!;
                }
            }
        }

        return headers;
    }

    private static bool IsAsciiLetterOrDigit(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');

    private static bool TryParseObject(string body, out JObject root)
    {
        root = null!;
        if (string.IsNullOrEmpty(body))
        {
            return false;
        }

        try
        {
            if (JToken.Parse(body) is JObject parsed)
            {
                root = parsed;
                return true;
            }
        }
        catch (JsonException)
        {
            // Not JSON: the caller treats the message as unreadable.
        }

        return false;
    }

    private static string? ReadString(JObject root, string name)
    {
        return root[name]?.Type == JTokenType.String ? (string?)root[name] : null;
    }

    private static int? ReadInt(JToken? token)
    {
        if (token == null || token.Type != JTokenType.Integer)
        {
            return null;
        }

        return (int)token;
    }

    private static int[] ReadInts(JToken? token)
    {
        if (token is not JArray array)
        {
            return Array.Empty<int>();
        }

        return array.Where(t => t.Type == JTokenType.Integer).Select(t => (int)t).ToArray();
    }
}
