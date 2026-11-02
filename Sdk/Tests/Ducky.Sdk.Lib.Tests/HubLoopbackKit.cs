using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.DebugEvents;
using Ducky.Sdk.Contracts.HttpEndpoints;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// An in-memory message hub for the hot reload scenarios. One <see cref="HubLoopback"/> is the hub object; each Core
/// (client, host, reporter, recorder) gets its own <see cref="LoopbackTransport"/>, which stands for that Core's
/// <c>HubBinding</c>. The hub keeps one table of registrations and one FIFO queue of messages. Messages are delivered only
/// when the test calls <see cref="HubLoopback.Pump"/>, so every step of a scenario is deterministic.
/// </summary>
/// <remarks>
/// Simplifications against the real <c>ModHttpV1</c>: a message to an id with no handler is dropped (the real hub keeps it
/// for 60 seconds), and <see cref="HubLoopback.ReplaceHub"/> models a hub replacement with what <c>HubBinding</c> does
/// on the Core side: the table starts empty and every owner registers its own clients again, once.
/// </remarks>
internal sealed class HubLoopback
{
    private readonly Dictionary<string, Action<string, string, string>> _table = new();
    private readonly Queue<LoopbackMessage> _queue = new();
    private readonly Queue<Action> _posted = new();
    private readonly List<LoopbackTransport> _owners = new();

    public HubLoopback()
    {
        UtcNow = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>The hub generation. Each <see cref="ReplaceHub"/> increments it.</summary>
    public int HubGeneration { get; private set; }

    /// <summary>The shared clock of every Core on this hub. Tests advance it.</summary>
    public DateTime UtcNow { get; set; }

    public double SecondsSinceStart { get; set; }

    public List<string> Warnings { get; } = new();

    public List<string> Errors { get; } = new();

    /// <summary>Messages dropped because their target had no handler.</summary>
    public List<LoopbackMessage> Dropped { get; } = new();

    /// <summary>Ids that currently have a handler on this hub.</summary>
    public IReadOnlyList<string> ModIds => _table.Keys.ToList();

    /// <summary>Creates the transport of one Core on this hub.</summary>
    public LoopbackTransport CreateOwner()
    {
        var owner = new LoopbackTransport(this);
        _owners.Add(owner);
        return owner;
    }

    /// <summary>Delivers queued work in order until nothing is left. Posted actions and messages may queue more.</summary>
    public void Pump()
    {
        while (_posted.Count > 0 || _queue.Count > 0)
        {
            while (_posted.Count > 0)
            {
                _posted.Dequeue()();
            }

            if (_queue.Count == 0)
            {
                continue;
            }

            var message = _queue.Dequeue();
            if (_table.TryGetValue(message.To, out var handler))
            {
                handler(message.From, message.ContentType, message.Body);
            }
            else
            {
                Dropped.Add(message);
            }
        }
    }

    /// <summary>
    /// Replaces the hub object: the table and the queued messages are lost, the generation increments, and every Core
    /// registers its clients again on the new table.
    /// </summary>
    public void ReplaceHub()
    {
        _queue.Clear();
        _table.Clear();
        HubGeneration++;
        foreach (var owner in _owners)
        {
            owner.RegisterAgain();
        }
    }

    internal void Put(string modId, Action<string, string, string> handler) => _table[modId] = handler;

    /// <summary>Removes the entry only when it is still the handler the caller registered.</summary>
    internal void RemoveIfSame(string modId, Action<string, string, string> handler)
    {
        if (_table.TryGetValue(modId, out var current) && ReferenceEquals(current, handler))
        {
            _table.Remove(modId);
        }
    }

    internal void Enqueue(LoopbackMessage message) => _queue.Enqueue(message);

    internal void Post(Action action) => _posted.Enqueue(action);
}

/// <summary>One message on the loopback hub.</summary>
internal sealed record LoopbackMessage(string From, string To, string ContentType, string Body);

/// <summary>
/// One Core's view of the loopback hub. It serves as both the HTTP endpoint transport and the debug event host, because
/// both channels talk through the same hub. Unregister is owner-aware: it removes only the handler this Core registered,
/// so a late teardown of an older Core cannot remove a newer registration under the same id.
/// </summary>
internal sealed class LoopbackTransport : IHttpEndpointTransport, IDebugEventHost
{
    private readonly HubLoopback _hub;
    private readonly Dictionary<string, Action<string, string, string>> _mine = new();

    internal LoopbackTransport(HubLoopback hub)
    {
        _hub = hub;
    }

    public IReadOnlyList<string> GetModIds() => _hub.ModIds;

    /// <summary>The hub generation, as the endpoint client reads it.</summary>
    public int HubGeneration => _hub.HubGeneration;

    public void Register(string clientId, Action<string, string, string> handler)
    {
        _mine[clientId] = handler;
        _hub.Put(clientId, handler);
    }

    public void Unregister(string clientId)
    {
        if (_mine.Remove(clientId, out var mine))
        {
            _hub.RemoveIfSame(clientId, mine);
        }
    }

    public void Send(string from, string to, string contentType, string body) =>
        _hub.Enqueue(new LoopbackMessage(from, to, contentType, body));

    public void PostToMainThread(Action action) => _hub.Post(action);

    /// <summary>Never completes on its own. The tests call <c>Tick</c> directly and cancel through the token.</summary>
    public Func<TimeSpan, CancellationToken, UniTask> Delay => (_, token) => UniTask.Never(token);

    public DateTime UtcNow => _hub.UtcNow;

    public double SecondsSinceStart => _hub.SecondsSinceStart;

    public void Warn(string message) => _hub.Warnings.Add(message);

    public void Error(Exception exception, string message) => _hub.Errors.Add(message);

    /// <summary>Registers every client of this Core again on the current table, as a binding does after a hub change.</summary>
    internal void RegisterAgain()
    {
        foreach (var (clientId, handler) in _mine)
        {
            _hub.Put(clientId, handler);
        }
    }
}
