using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Contracts.HttpEndpoints;

/// <summary>Client states of design D9.</summary>
internal enum HttpEndpointClientState
{
    NoHost,
    Negotiating,
    Announced,
    Incompatible,
    Released
}

/// <summary>
/// Client side of the HTTP endpoint channel. <c>Map</c> only records the endpoint in a local table and never sends
/// anything by itself. A main-thread tick checks for the <c>httphost</c> id at most every 5 seconds while the table is not
/// empty, negotiates the version with hello and ready, announces the whole table as one snapshot and dispatches
/// forwarded requests to the handlers. Without a host, nothing is queued and nothing is logged per call.
/// </summary>
internal sealed class HttpEndpointClient
{
    private static readonly object SharedGate = new();
    private static HttpEndpointClient? _shared;

    private readonly object _gate = new();
    private readonly IHttpEndpointTransport _transport;
    private readonly string _modId;
    private readonly string _clientId;
    private readonly int[] _supportedVersions;
    private readonly Dictionary<RouteKey, Registration> _table = new();
    private readonly HashSet<string> _warnedOnce = new();

    private HttpEndpointClientState _state = HttpEndpointClientState.NoHost;
    private int _version;
    private DateTime _lastCheck = DateTime.MinValue;
    private bool _dirty;
    private bool _registered;
    private bool _loopRunning;
    private CancellationTokenSource? _loopCts;

    internal HttpEndpointClient(
        IHttpEndpointTransport transport,
        string modId,
        IEnumerable<int>? supportedVersions = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        if (!HttpEndpointProtocol.IsValidModId(modId))
        {
            throw new ArgumentException(
                "The mod id may only contain letters, digits, '.', '_' and '-', so this mod cannot map HTTP endpoints.",
                nameof(modId));
        }

        _modId = modId;
        _clientId = HttpEndpointProtocol.ClientId(modId);
        _supportedVersions = (supportedVersions ?? HttpEndpointProtocol.SupportedVersions).ToArray();
    }

    #region Per-mod shared instance

    /// <summary>The client of this mod copy. Created again after <see cref="Release"/>.</summary>
    internal static HttpEndpointClient Shared
    {
        get
        {
            lock (SharedGate)
            {
                if (_shared == null || _shared.IsReleased)
                {
                    _shared = new HttpEndpointClient(
                        ModHttpV1HttpEndpointTransport.Instance,
                        Helper.GetModId().Value);
                }

                return _shared;
            }
        }
    }

    /// <summary>
    /// Releases the shared client: withdraws its endpoints from an announced host, unregisters its hub client and stops the
    /// tick. Called from the shared teardown of <c>ModBehaviourBase</c>. Idempotent and never throws.
    /// </summary>
    public static void Release()
    {
        HttpEndpointClient? old;
        lock (SharedGate)
        {
            old = _shared;
            _shared = null;
        }

        old?.Shutdown();
    }

    #endregion

    /// <summary>The client's state. For tests and diagnostics.</summary>
    internal HttpEndpointClientState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    internal bool IsReleased => State == HttpEndpointClientState.Released;

    /// <summary>Endpoints in the local table. For tests and diagnostics.</summary>
    internal int EndpointCount
    {
        get
        {
            lock (_gate)
            {
                return _table.Count;
            }
        }
    }

    /// <summary>
    /// Declares one endpoint. Validates the method, the path and the handler and throws an argument error for a bad value or a
    /// duplicate. Returns a handle that removes the endpoint once when disposed. Sends nothing and never throws for a missing host.
    /// </summary>
    internal IDisposable Map(string method, string path, Func<HttpEndpointRequest, UniTask<HttpEndpointResponse>> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (!HttpEndpointProtocol.TryNormalizeMethod(method, out var normalizedMethod))
        {
            throw new ArgumentException(
                $"Unsupported HTTP method '{method}'. Use GET, POST, PUT, PATCH or DELETE.",
                nameof(method));
        }

        if (!HttpEndpointProtocol.TryNormalizeEndpointPath(_modId, path, out var normalizedPath, out var reason))
        {
            throw new ArgumentException($"Endpoint path '{path}' is not allowed: {reason}.", nameof(path));
        }

        var key = new RouteKey(normalizedMethod, normalizedPath);
        var registration = new Registration(key, handler);
        lock (_gate)
        {
            if (_state == HttpEndpointClientState.Released)
            {
                return new Handle(this, registration);
            }

            if (_table.ContainsKey(key))
            {
                throw new ArgumentException(
                    $"Endpoint {normalizedMethod} {normalizedPath} is already mapped by this mod.",
                    nameof(path));
            }

            _table[key] = registration;
            _dirty = true;
        }

        RequestTickLoop();
        return new Handle(this, registration);
    }

    /// <summary>Declares one endpoint whose handler answers at once. See the asynchronous overload.</summary>
    internal IDisposable Map(string method, string path, Func<HttpEndpointRequest, HttpEndpointResponse> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return Map(method, path, request => UniTask.FromResult(handler(request)));
    }

    private void Remove(Registration registration)
    {
        lock (_gate)
        {
            if (_table.TryGetValue(registration.Key, out var current) && ReferenceEquals(current, registration))
            {
                _table.Remove(registration.Key);
                _dirty = true;
            }
        }

        RequestTickLoop();
    }

    /// <summary>Starts the main-thread tick once. Called from any thread; the loop starts on the main thread.</summary>
    private void RequestTickLoop()
    {
        _transport.PostToMainThread(StartTickLoop);
    }

    private void StartTickLoop()
    {
        CancellationToken token;
        lock (_gate)
        {
            if (_state == HttpEndpointClientState.Released || _loopRunning)
            {
                return;
            }

            _loopRunning = true;
            _loopCts = new CancellationTokenSource();
            token = _loopCts.Token;
        }

        RunTickLoop(token).Forget();
    }

    private async UniTaskVoid RunTickLoop(CancellationToken token)
    {
        try
        {
            while (true)
            {
                await _transport.Delay(HttpEndpointProtocol.TickInterval, token);
                try
                {
                    Tick();
                }
                catch (Exception e)
                {
                    _transport.Error(e, "HttpEndpoints client tick failed");
                }

                lock (_gate)
                {
                    // Stops when the table is empty and every withdrawal has been sent, or when the client is released.
                    if (_state == HttpEndpointClientState.Released || (_table.Count == 0 && !_dirty))
                    {
                        _loopRunning = false;
                        return;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Released: the loop ends.
        }
        finally
        {
            lock (_gate)
            {
                _loopRunning = false;
            }
        }
    }

    /// <summary>
    /// One step on the main thread: registers the hub client while endpoints exist, checks for the host at most every 5
    /// seconds while the table is not empty, and sends a pending snapshot when announced.
    /// </summary>
    internal void Tick()
    {
        bool hasEndpoints;
        lock (_gate)
        {
            if (_state == HttpEndpointClientState.Released)
            {
                return;
            }

            hasEndpoints = _table.Count > 0;
        }

        if (hasEndpoints && !_registered)
        {
            _transport.Register(_clientId, HandleIncoming);
            _registered = true;
        }

        if (hasEndpoints)
        {
            var now = _transport.UtcNow;
            if (_lastCheck == DateTime.MinValue || now - _lastCheck >= HttpEndpointProtocol.HostCheckInterval)
            {
                _lastCheck = now;
                CheckHost();
            }
        }

        FlushSnapshot();
    }

    private void CheckHost()
    {
        var present = _transport.GetModIds().Contains(HttpEndpointProtocol.HostModId);
        var sendHello = false;
        lock (_gate)
        {
            switch (_state)
            {
                case HttpEndpointClientState.NoHost:
                    if (present)
                    {
                        _state = HttpEndpointClientState.Negotiating;
                        sendHello = true;
                    }

                    break;
                case HttpEndpointClientState.Negotiating:
                    if (present)
                    {
                        // No ready yet: ask again, the hello may have been lost.
                        sendHello = true;
                    }
                    else
                    {
                        _state = HttpEndpointClientState.NoHost;
                    }

                    break;
                case HttpEndpointClientState.Announced:
                case HttpEndpointClientState.Incompatible:
                    if (!present)
                    {
                        _state = HttpEndpointClientState.NoHost;
                    }

                    break;
            }
        }

        if (sendHello)
        {
            _transport.Send(
                _clientId,
                HttpEndpointProtocol.HostModId,
                HttpEndpointProtocol.HelloContentType,
                HttpEndpointProtocol.EncodeHello(_modId, _supportedVersions));
        }
    }

    /// <summary>Sends the whole table as one snapshot when it changed and the host is announced.</summary>
    private void FlushSnapshot()
    {
        List<RouteKey> endpoints;
        lock (_gate)
        {
            if (_state != HttpEndpointClientState.Announced || !_dirty)
            {
                return;
            }

            _dirty = false;
            endpoints = _table.Keys.OrderBy(k => k.Path, StringComparer.Ordinal)
                .ThenBy(k => k.Method, StringComparer.Ordinal)
                .ToList();
        }

        _transport.Send(
            _clientId,
            HttpEndpointProtocol.HostModId,
            HttpEndpointProtocol.RegisterContentType,
            HttpEndpointProtocol.EncodeRegister(_modId, endpoints));
    }

    /// <summary>Hub callback for messages to this client's id. Runs on the main thread.</summary>
    internal void HandleIncoming(string from, string contentType, string body)
    {
        switch (contentType)
        {
            case HttpEndpointProtocol.ReadyContentType:
                HandleReady(from, body);
                break;
            case HttpEndpointProtocol.RegisteredContentType:
                HandleRegistered(from, body);
                break;
            case HttpEndpointProtocol.RequestContentType:
                HandleRequest(from, body);
                break;
            default:
                WarnOnce(
                    $"content:{from}:{contentType}",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "HttpEndpoints ignored a message of unknown content type '{0}' from {1}.",
                        contentType,
                        from));
                break;
        }
    }

    private void HandleReady(string from, string body)
    {
        if (from != HttpEndpointProtocol.HostModId || !HttpEndpointProtocol.TryDecodeReady(body, out var version, out _))
        {
            return;
        }

        var compatible = version > 0 && _supportedVersions.Contains(version);
        var warnIncompatible = false;
        lock (_gate)
        {
            if (_state != HttpEndpointClientState.Negotiating)
            {
                return;
            }

            if (compatible)
            {
                _version = version;
                _state = HttpEndpointClientState.Announced;
                _dirty = true;
            }
            else
            {
                _state = HttpEndpointClientState.Incompatible;
                warnIncompatible = true;
            }
        }

        if (warnIncompatible)
        {
            _transport.Warn(string.Format(
                CultureInfo.InvariantCulture,
                "HttpEndpoints of {0} are not announced: the httphost answered version {1}, this SDK supports {2}.",
                _modId,
                version,
                string.Join(",", _supportedVersions)));
            return;
        }

        FlushSnapshot();
    }

    private void HandleRegistered(string from, string body)
    {
        if (from != HttpEndpointProtocol.HostModId ||
            !HttpEndpointProtocol.TryDecodeRegistered(body, out _, out var rejected))
        {
            return;
        }

        lock (_gate)
        {
            if (_state != HttpEndpointClientState.Announced)
            {
                return;
            }
        }

        foreach (var route in rejected)
        {
            _transport.Warn(string.Format(
                CultureInfo.InvariantCulture,
                "HttpEndpoints of {0}: endpoint {1} {2} was rejected by the httphost: {3}.",
                _modId,
                route.Method,
                route.Path,
                route.Reason));
        }
    }

    private void HandleRequest(string from, string body)
    {
        if (from != HttpEndpointProtocol.HostModId ||
            !HttpEndpointProtocol.TryDecodeRequest(body, out var id, out var request))
        {
            return;
        }

        Registration? registration;
        lock (_gate)
        {
            if (_state == HttpEndpointClientState.Released)
            {
                return;
            }

            _table.TryGetValue(new RouteKey(request.Method, request.Path), out registration);
        }

        if (registration == null)
        {
            WarnOnce(
                $"route:{request.Method} {request.Path}",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "HttpEndpoints ignored a request for {0} {1}: no such endpoint in this mod.",
                    request.Method,
                    request.Path));
            return;
        }

        _transport.PostToMainThread(() => RunHandler(registration, id, request).Forget());
    }

    /// <summary>Runs one handler on the main thread, then sends its answer under the request's id.</summary>
    private async UniTaskVoid RunHandler(Registration registration, string id, HttpEndpointRequest request)
    {
        HttpEndpointResponse response;
        try
        {
            response = await registration.Handler(request) ?? HttpEndpointResponse.Text(502, "Endpoint returned no response.");
        }
        catch (Exception e)
        {
            // Logged once here; the external client only sees the status, never the exception text.
            _transport.Error(
                e,
                $"HttpEndpoints handler for {registration.Key.Method} {registration.Key.Path} threw an exception");
            response = HttpEndpointResponse.Text(502, "Endpoint handler failed.");
        }

        if (!HttpEndpointProtocol.IsWithinBodyLimit(response.Body))
        {
            _transport.Warn(string.Format(
                CultureInfo.InvariantCulture,
                "HttpEndpoints response for {0} {1} is larger than {2} bytes and was replaced with 502.",
                registration.Key.Method,
                registration.Key.Path,
                HttpEndpointProtocol.MaxBodyBytes));
            response = HttpEndpointResponse.Text(502, "Endpoint response too large.");
        }

        bool released;
        lock (_gate)
        {
            released = _state == HttpEndpointClientState.Released;
        }

        if (released)
        {
            return;
        }

        _transport.Send(
            _clientId,
            HttpEndpointProtocol.HostModId,
            HttpEndpointProtocol.ResponseContentType,
            HttpEndpointProtocol.EncodeResponse(id, response));
    }

    /// <summary>
    /// Withdraws everything: when announced, sends an empty snapshot; then unregisters the hub client and clears the table.
    /// Idempotent and never throws.
    /// </summary>
    internal void Shutdown()
    {
        bool sendEmpty;
        bool unregister;
        CancellationTokenSource? loop;
        lock (_gate)
        {
            if (_state == HttpEndpointClientState.Released)
            {
                return;
            }

            sendEmpty = _state == HttpEndpointClientState.Announced;
            _state = HttpEndpointClientState.Released;
            _table.Clear();
            _dirty = false;
            unregister = _registered;
            _registered = false;
            loop = _loopCts;
            _loopCts = null;
        }

        try
        {
            loop?.Cancel();
        }
        catch (Exception e)
        {
            _transport.Error(e, "HttpEndpoints failed to stop the tick");
        }

        if (sendEmpty)
        {
            try
            {
                _transport.Send(
                    _clientId,
                    HttpEndpointProtocol.HostModId,
                    HttpEndpointProtocol.RegisterContentType,
                    HttpEndpointProtocol.EncodeRegister(_modId, Array.Empty<RouteKey>()));
            }
            catch (Exception e)
            {
                _transport.Error(e, "HttpEndpoints failed to withdraw its endpoints");
            }
        }

        if (unregister)
        {
            try
            {
                _transport.Unregister(_clientId);
            }
            catch (Exception e)
            {
                _transport.Error(e, "HttpEndpoints failed to unregister its hub client");
            }
        }
    }

    private void WarnOnce(string key, string message)
    {
        bool first;
        lock (_gate)
        {
            first = _warnedOnce.Add(key);
        }

        if (first)
        {
            _transport.Warn(message);
        }
    }

    private sealed class Registration
    {
        public Registration(RouteKey key, Func<HttpEndpointRequest, UniTask<HttpEndpointResponse>> handler)
        {
            Key = key;
            Handler = handler;
        }

        public RouteKey Key { get; }

        public Func<HttpEndpointRequest, UniTask<HttpEndpointResponse>> Handler { get; }
    }

    /// <summary>The handle <c>Map</c> returns. Disposing it removes its endpoint once; later calls do nothing.</summary>
    private sealed class Handle : IDisposable
    {
        private readonly HttpEndpointClient _owner;
        private readonly Registration _registration;
        private int _disposed;

        public Handle(HttpEndpointClient owner, Registration registration)
        {
            _owner = owner;
            _registration = registration;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _owner.Remove(_registration);
            }
        }
    }
}
