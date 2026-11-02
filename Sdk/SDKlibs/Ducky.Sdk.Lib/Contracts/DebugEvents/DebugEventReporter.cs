using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.ModProtocols;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Threading;
using Ducky.Sdk.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Ducky.Sdk.Contracts.DebugEvents;

/// <summary>Everything the reporter and the recorder need from the outside world: the hub, the clock.</summary>
internal interface IDebugEventHost
{
    IReadOnlyList<string> GetModIds();

    void Register(string modId, Action<string, string, string> handler);

    void Unregister(string modId);

    void Send(string from, string to, string contentType, string body);

    DateTime UtcNow { get; }

    /// <summary>Seconds since game start, for the <c>t</c> field.</summary>
    double SecondsSinceStart { get; }
}

/// <summary>Reporter states of design D6.</summary>
internal enum DebugEventReporterState
{
    Idle,
    Negotiating,
    Ready,
    Incompatible,
    Released
}

/// <summary>
/// Reporter side of the debug event channel. <c>Report</c> only fills a bounded buffer, so it is safe from any thread.
/// <c>Tick</c> runs on the main thread every 200 ms: it checks for the recorder id at most every 5 seconds, negotiates
/// the version with a hello/ready exchange and sends one batch of at most 50 events per tick. While no recorder is
/// present, reports are discarded and nothing is queued in the hub.
/// </summary>
internal sealed class DebugEventReporter
{
    public const int BufferCapacity = 1000;
    public const int MaxBatchSize = 50;
    public static readonly TimeSpan AliasCheckInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(200);

    private static readonly object SharedGate = new();
    private static DebugEventReporter? _shared;

    private readonly object _gate = new();
    private readonly IDebugEventHost _host;
    private readonly Func<string> _resolveModId;
    private readonly Action<string> _warn;
    private readonly int[] _supportedVersions;
    private readonly Queue<DebugEventRecord> _buffer = new();

    private DebugEventReporterState _state = DebugEventReporterState.Idle;
    private long _dropped;
    private int _version;
    private DateTime _lastCheck = DateTime.MinValue;
    private string? _modId;
    private string _clientId = string.Empty;
    private bool _registered;
    private int _loopRequested;
    private CancellationTokenSource? _loopCts;

    internal DebugEventReporter(
        IDebugEventHost host,
        Func<string> resolveModId,
        Action<string> warn,
        IEnumerable<int>? supportedVersions = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _resolveModId = resolveModId ?? throw new ArgumentNullException(nameof(resolveModId));
        _warn = warn ?? throw new ArgumentNullException(nameof(warn));
        _supportedVersions = (supportedVersions ?? DebugEventProtocol.SupportedVersions).ToArray();
    }

    #region Per-mod shared instance (real hub)

    /// <summary>The reporter of this mod copy. Created again after <see cref="Release"/>.</summary>
    internal static DebugEventReporter Shared
    {
        get
        {
            lock (SharedGate)
            {
                if (_shared == null || _shared.IsReleased)
                {
                    _shared = new DebugEventReporter(
                        new ModHttpV1DebugEventHost(),
                        () => Helper.GetModId().Value,
                        message => Log.Warn(message));
                }

                return _shared;
            }
        }
    }

    /// <summary>
    /// Releases the shared reporter: flushes what it can, unregisters its hub client and stops its flush loop. Called
    /// from the shared teardown of <c>ModBehaviourBase</c>. Idempotent.
    /// </summary>
    public static void Release()
    {
        DebugEventReporter? old;
        lock (SharedGate)
        {
            old = _shared;
            _shared = null;
        }

        old?.Shutdown();
    }

    /// <summary>True while the shared reporter is negotiated with a compatible recorder.</summary>
    internal static bool IsRecorderReady()
    {
        lock (SharedGate)
        {
            return _shared?.State == DebugEventReporterState.Ready;
        }
    }

    #endregion

    internal DebugEventReporterState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    internal bool IsReleased => State == DebugEventReporterState.Released;

    /// <summary>Events waiting for a batch. For tests and diagnostics.</summary>
    internal int BufferedCount
    {
        get
        {
            lock (_gate)
            {
                return _buffer.Count;
            }
        }
    }

    /// <summary>
    /// Queues one event. Callable from any thread, touches no Unity API and never throws. Discarded at once while no
    /// compatible recorder is expected (Idle, Incompatible, Released).
    /// </summary>
    internal void Report(string kind, string? detail, JObject? fields)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return;
        }

        var at = _host.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        var record = new DebugEventRecord(kind, detail, at, fields);
        lock (_gate)
        {
            if (_state == DebugEventReporterState.Idle ||
                _state == DebugEventReporterState.Incompatible ||
                _state == DebugEventReporterState.Released)
            {
                return;
            }

            if (_buffer.Count >= BufferCapacity)
            {
                _buffer.Dequeue();
                _dropped++;
            }

            _buffer.Enqueue(record);
        }
    }

    /// <summary>
    /// Starts the main-thread flush loop once per reporter. Called from <c>Report</c> on any thread; the loop itself
    /// starts on the main thread through the SDK main-thread scheduler.
    /// </summary>
    internal void RequestFlushLoop()
    {
        if (Interlocked.Exchange(ref _loopRequested, 1) == 0)
        {
            MainThread.Post(() => StartFlushLoop(DelayOnMainThread));
        }
    }

    private static UniTask DelayOnMainThread(TimeSpan delay, CancellationToken token) =>
        UniTask.Delay(delay, cancellationToken: token);

    /// <summary>
    /// Runs <see cref="Tick"/> every <see cref="FlushInterval"/> until <see cref="Shutdown"/>. The delay is injected so
    /// the loop's lifetime can be tested without Unity; the game passes a main-thread <c>UniTask.Delay</c>.
    /// </summary>
    internal void StartFlushLoop(Func<TimeSpan, CancellationToken, UniTask> delay)
    {
        CancellationToken token;
        lock (_gate)
        {
            if (_state == DebugEventReporterState.Released || _loopCts != null)
            {
                return;
            }

            _loopCts = new CancellationTokenSource();
            token = _loopCts.Token;
        }

        RunFlushLoop(token, delay).Forget();
    }

    private async UniTaskVoid RunFlushLoop(CancellationToken token, Func<TimeSpan, CancellationToken, UniTask> delay)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await delay(FlushInterval, token);
                try
                {
                    Tick();
                }
                catch (Exception e)
                {
                    Log.Error(e, "DebugEventReporter flush failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Released: the loop ends.
        }
    }

    /// <summary>One flush step on the main thread: registration, the alias check and at most one batch.</summary>
    internal void Tick()
    {
        lock (_gate)
        {
            if (_state == DebugEventReporterState.Released)
            {
                return;
            }
        }

        _modId ??= _resolveModId();
        _clientId = _modId + DebugEventProtocol.ReporterClientSuffix;
        if (!_registered)
        {
            _host.Register(_clientId, HandleIncoming);
            _registered = true;
        }

        var now = _host.UtcNow;
        if (_lastCheck == DateTime.MinValue || now - _lastCheck >= AliasCheckInterval)
        {
            _lastCheck = now;
            CheckRecorder();
        }

        var message = TakeBatch(all: false);
        if (message != null)
        {
            _host.Send(_clientId, DebugEventProtocol.RecorderModId, message.Value.ContentType, message.Value.Body);
        }
    }

    private void CheckRecorder()
    {
        var present = _host.GetModIds().Contains(DebugEventProtocol.RecorderModId);
        var sendHello = false;
        lock (_gate)
        {
            switch (_state)
            {
                case DebugEventReporterState.Idle:
                    if (present)
                    {
                        _state = DebugEventReporterState.Negotiating;
                        sendHello = true;
                    }

                    break;
                case DebugEventReporterState.Negotiating:
                    if (present)
                    {
                        // No ready yet: ask again, the hello may have been lost.
                        sendHello = true;
                    }
                    else
                    {
                        _state = DebugEventReporterState.Idle;
                        DiscardBuffer();
                    }

                    break;
                case DebugEventReporterState.Ready:
                    if (!present)
                    {
                        _state = DebugEventReporterState.Idle;
                        DiscardBuffer();
                    }

                    break;
                case DebugEventReporterState.Incompatible:
                    if (!present)
                    {
                        _state = DebugEventReporterState.Idle;
                    }

                    break;
            }
        }

        if (sendHello)
        {
            _host.Send(
                _clientId,
                DebugEventProtocol.RecorderModId,
                DebugEventProtocol.HelloContentType,
                DebugEventProtocol.EncodeHello(_modId!, _supportedVersions));
        }
    }

    /// <summary>Hub callback for messages to this reporter's client id. Runs on the main thread.</summary>
    internal void HandleIncoming(string from, string contentType, string body)
    {
        if (contentType != DebugEventProtocol.ReadyContentType || from != DebugEventProtocol.RecorderModId)
        {
            return;
        }

        if (!DebugEventProtocol.TryDecodeReady(body, out var version, out _))
        {
            return;
        }

        var compatible = version > 0 && _supportedVersions.Contains(version);
        var warnIncompatible = false;
        lock (_gate)
        {
            if (_state != DebugEventReporterState.Negotiating && _state != DebugEventReporterState.Ready)
            {
                return;
            }

            if (compatible)
            {
                _version = version;
                _state = DebugEventReporterState.Ready;
            }
            else
            {
                warnIncompatible = _state == DebugEventReporterState.Negotiating;
                _state = DebugEventReporterState.Incompatible;
            }
        }

        if (warnIncompatible)
        {
            _warn(string.Format(
                CultureInfo.InvariantCulture,
                "Debug events from {0} are not sent: the recorder answered version {1}, this SDK supports {2}.",
                _modId,
                version,
                string.Join(",", _supportedVersions)));
        }
    }

    /// <summary>
    /// Takes the next batch out of the buffer under the lock, or every remaining batch when <paramref name="all"/> is
    /// true. Returns null when there is nothing to send or the reporter is not Ready. Encoding happens outside the lock.
    /// </summary>
    private (string ContentType, string Body)? TakeBatch(bool all)
    {
        (string ContentType, string Body)? last = null;
        while (true)
        {
            List<DebugEventRecord> events;
            int dropped;
            int version;
            lock (_gate)
            {
                if (_state != DebugEventReporterState.Ready || (_buffer.Count == 0 && _dropped == 0))
                {
                    return last;
                }

                var take = Math.Min(MaxBatchSize, _buffer.Count);
                events = new List<DebugEventRecord>(take);
                for (var i = 0; i < take; i++)
                {
                    events.Add(_buffer.Dequeue());
                }

                dropped = (int)Math.Min(_dropped, int.MaxValue);
                _dropped = 0;
                version = _version;
            }

            last = (DebugEventProtocol.BatchContentType(version),
                DebugEventProtocol.EncodeBatch(_modId!, dropped, events));
            if (!all)
            {
                return last;
            }

            // Release sends everything; each iteration sends one batch.
            _host.Send(_clientId, DebugEventProtocol.RecorderModId, last.Value.ContentType, last.Value.Body);
            last = null;
        }
    }

    /// <summary>Caller holds the lock. Lost events are counted so the next batch reports them.</summary>
    private void DiscardBuffer()
    {
        _dropped += _buffer.Count;
        _buffer.Clear();
    }

    /// <summary>
    /// Final flush and teardown: while Ready, sends everything still buffered; then stops the flush loop and
    /// unregisters the hub client. Idempotent.
    /// </summary>
    internal void Shutdown()
    {
        lock (_gate)
        {
            if (_state == DebugEventReporterState.Released)
            {
                return;
            }
        }

        try
        {
            // Sends every remaining batch when Ready; does nothing otherwise.
            TakeBatch(all: true);
        }
        finally
        {
            lock (_gate)
            {
                _buffer.Clear();
                _state = DebugEventReporterState.Released;
                _loopCts?.Cancel();
            }

            if (_registered)
            {
                _registered = false;
                _host.Unregister(_clientId);
            }
        }
    }

    /// <summary>
    /// The real hub, reached through <see cref="ModHttpV1Proxy"/>. Main thread only.
    /// </summary>
    /// <remarks>
    /// The hub object is replaced whenever the mod that started it is hot reloaded: that mod's teardown destroys the hub
    /// (<c>MessageHubManager.StopOwnedHost</c>) and the next Core starts a new one. A proxy keeps the hub object it was
    /// created for, so every call here looks the current hub up and, when it changed, binds a new proxy and registers
    /// this host's clients on it again. No proxy is created while no hub object exists, so virtual proxies never pile up.
    /// </remarks>
    internal sealed class ModHttpV1DebugEventHost : IDebugEventHost
    {
        private static readonly IReadOnlyList<string> NoModIds = Array.Empty<string>();

        private readonly Dictionary<string, Action<string, string, string>> _clients = new();
        private ModHttpV1Proxy? _proxy;
        private object? _hubObject;

        private ModHttpV1Proxy? Hub()
        {
            var current = FindHubObject();
            if (current == null)
            {
                return null;
            }

            if (_proxy == null || !ReferenceEquals(current, _hubObject))
            {
                _hubObject = current;
                _proxy = ModHttpV1Proxy.CreateFromSingleton();
                foreach (var client in _clients)
                {
                    _proxy.RegisterClient(client.Key, Wrap(client.Value));
                }
            }

            return _proxy;
        }

        /// <summary>The live hub component, found by type name because the hub type may come from another Core.</summary>
        private static object? FindHubObject()
        {
            var go = GameObject.Find(ModHttpV1.HubGameObjectName);
            if (go == null)
            {
                return null;
            }

            foreach (var component in go.GetComponents<MonoBehaviour>())
            {
                if (component.GetType().Name == "ModHttpV1")
                {
                    return component;
                }
            }

            return null;
        }

        private static Func<string, string, string, UniTask> Wrap(Action<string, string, string> handler)
        {
            return (from, contentType, body) =>
            {
                try
                {
                    handler(from, contentType, body);
                }
                catch (Exception e)
                {
                    Log.Error(e, "DebugEventReporter failed to handle a message from {From}", from);
                }

                return UniTask.CompletedTask;
            };
        }

        public IReadOnlyList<string> GetModIds() => Hub()?.GetModIds() ?? NoModIds;

        public void Register(string modId, Action<string, string, string> handler)
        {
            _clients[modId] = handler;
            Hub()?.RegisterClient(modId, Wrap(handler));
        }

        public void Unregister(string modId)
        {
            _clients.Remove(modId);
            Hub()?.UnregisterClient(modId);
        }

        public void Send(string from, string to, string contentType, string body)
        {
            // No hub object: nothing can be delivered, and the reporter's next alias check will see that.
            Hub()?.Notify(from, to, contentType, body).Forget();
        }

        public DateTime UtcNow => DateTime.UtcNow;

        public double SecondsSinceStart => Time.realtimeSinceStartup;
    }
}
