using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Ducky.Sdk.Contracts.DebugEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>A message the code under test sent through the fake hub.</summary>
internal sealed record SentMessage(string From, string To, string ContentType, string Body);

/// <summary>
/// Fake hub and clock for the debug event tests. Registering adds the id to <see cref="ModIds"/>, as the real hub does,
/// so the claim-then-verify step in the recorder sees its own registration.
/// </summary>
internal sealed class FakeDebugEventHost : IDebugEventHost
{
    public List<string> ModIds { get; } = new();

    public List<SentMessage> Sent { get; } = new();

    public Dictionary<string, Action<string, string, string>> Handlers { get; } = new();

    public List<string> Unregistered { get; } = new();

    public int GetModIdsCalls { get; private set; }

    public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);

    public double SecondsSinceStart { get; set; }

    public IReadOnlyList<string> GetModIds()
    {
        GetModIdsCalls++;
        return ModIds.ToList();
    }

    public void Register(string modId, Action<string, string, string> handler)
    {
        Handlers[modId] = handler;
        if (!ModIds.Contains(modId))
        {
            ModIds.Add(modId);
        }
    }

    public void Unregister(string modId)
    {
        Handlers.Remove(modId);
        ModIds.Remove(modId);
        Unregistered.Add(modId);
    }

    public void Send(string from, string to, string contentType, string body) =>
        Sent.Add(new SentMessage(from, to, contentType, body));
}

/// <summary>Parses one JSON Lines line the way the recorder file is meant to be read: timestamps stay strings.</summary>
internal static class JsonLine
{
    public static JObject Parse(string line)
    {
        using var reader = new JsonTextReader(new StringReader(line)) { DateParseHandling = DateParseHandling.None };
        return (JObject)JToken.ReadFrom(reader);
    }
}
