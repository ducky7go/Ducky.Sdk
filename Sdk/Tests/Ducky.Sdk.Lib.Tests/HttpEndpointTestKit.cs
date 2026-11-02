using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.HttpEndpoints;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>A message the code under test sent through the fake hub.</summary>
internal sealed record HttpSentMessage(string From, string To, string ContentType, string Body);

/// <summary>One delay the code under test started. Firing it completes the delay.</summary>
internal sealed record HttpPendingDelay(TimeSpan Duration, UniTaskCompletionSource Source)
{
    public void Fire() => Source.TrySetResult();
}

/// <summary>
/// Fake hub, main-thread poster, clock and delays for the HTTP endpoint tests. Registering adds the id to <see cref="ModIds"/>, as
/// the real hub does. Posted work runs only when the test calls <see cref="Drain"/>; delays run only when the test fires them.
/// </summary>
internal sealed class FakeHttpEndpointTransport : IHttpEndpointTransport
{
    public List<string> ModIds { get; } = new();

    public Dictionary<string, Action<string, string, string>> Handlers { get; } = new();

    public List<string> Unregistered { get; } = new();

    public List<HttpSentMessage> Sent { get; } = new();

    public Queue<Action> Posted { get; } = new();

    public List<HttpPendingDelay> Delays { get; } = new();

    public List<string> Warnings { get; } = new();

    public List<string> Errors { get; } = new();

    public DateTime UtcNow { get; set; } = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);

    public Func<TimeSpan, CancellationToken, UniTask> Delay => (duration, token) =>
    {
        var source = new UniTaskCompletionSource();
        Delays.Add(new HttpPendingDelay(duration, source));
        return source.Task;
    };

    /// <summary>How many times the code under test read the hub's id list.</summary>
    public int IdListReads { get; private set; }

    /// <summary>The hub generation the fake reports. Set it to simulate a replaced hub.</summary>
    public int HubGeneration { get; set; }

    public IReadOnlyList<string> GetModIds()
    {
        IdListReads++;
        return ModIds.ToList();
    }

    /// <summary>How many times the code under test registered a hub client.</summary>
    public int RegisterCalls { get; private set; }

    public void Register(string clientId, Action<string, string, string> handler)
    {
        RegisterCalls++;
        Handlers[clientId] = handler;
        if (!ModIds.Contains(clientId))
        {
            ModIds.Add(clientId);
        }
    }

    public void Unregister(string clientId)
    {
        Handlers.Remove(clientId);
        ModIds.Remove(clientId);
        Unregistered.Add(clientId);
    }

    public void Send(string from, string to, string contentType, string body) =>
        Sent.Add(new HttpSentMessage(from, to, contentType, body));

    public void PostToMainThread(Action action) => Posted.Enqueue(action);

    public void Warn(string message) => Warnings.Add(message);

    public void Error(Exception exception, string message) => Errors.Add(message);

    /// <summary>Runs every posted action, including actions posted while draining.</summary>
    public void Drain()
    {
        while (Posted.Count > 0)
        {
            Posted.Dequeue()();
        }
    }

    /// <summary>Delivers a message to a registered hub client, as the hub would.</summary>
    public void Deliver(string clientId, string from, string contentType, string body) =>
        Handlers[clientId](from, contentType, body);

    public IEnumerable<HttpSentMessage> SentOf(string contentType) =>
        Sent.Where(m => m.ContentType == contentType);
}
