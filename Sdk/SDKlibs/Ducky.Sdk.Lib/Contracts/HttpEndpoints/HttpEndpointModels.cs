using System;
using System.Collections.Generic;

namespace Ducky.Sdk.Contracts.HttpEndpoints;

/// <summary>One external HTTP request as an endpoint handler receives it.</summary>
public sealed class HttpEndpointRequest
{
    /// <summary>Upper-case method, one of GET, POST, PUT, PATCH or DELETE.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>The endpoint path, starting with "/&lt;modId&gt;".</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Query pairs in the order they arrived; a repeated name appears more than once.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Query { get; set; } = Array.Empty<KeyValuePair<string, string>>();

    /// <summary>Request headers with lower-case names.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>The Content-Type of the request body, or empty.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>The request body as text, at most 65536 UTF-8 bytes.</summary>
    public string Body { get; set; } = string.Empty;
}

/// <summary>The answer an endpoint handler returns.</summary>
public sealed class HttpEndpointResponse
{
    /// <summary>HTTP status, 100 to 599.</summary>
    public int Status { get; set; }

    /// <summary>Response headers. Names are lower-case by convention; values are plain text.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>The Content-Type of the body.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>The response body as text, at most 65536 UTF-8 bytes.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>A plain text response, Content-Type "text/plain; charset=utf-8".</summary>
    public static HttpEndpointResponse Text(int status, string body) => new()
    {
        Status = status,
        ContentType = "text/plain; charset=utf-8",
        Body = body ?? string.Empty
    };

    /// <summary>A JSON response, Content-Type "application/json; charset=utf-8". The text is sent as given.</summary>
    public static HttpEndpointResponse Json(int status, string json) => new()
    {
        Status = status,
        ContentType = "application/json; charset=utf-8",
        Body = json ?? string.Empty
    };
}

/// <summary>One accepted endpoint in the host's route table.</summary>
/// <param name="Method">Upper-case method.</param>
/// <param name="Path">Normalised path.</param>
/// <param name="OwnerModId">The mod that declared the endpoint.</param>
public sealed record HttpRouteInfo(string Method, string Path, string OwnerModId);
