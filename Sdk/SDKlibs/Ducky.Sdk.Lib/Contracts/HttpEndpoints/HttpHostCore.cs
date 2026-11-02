using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Contracts.HttpEndpoints;

/// <summary>
/// Host side of the HTTP endpoint channel, the Unity-free core of the <c>httphost</c> role. It claims the role on the hub, keeps
/// the route table of every mod, forwards external requests to the owning mod and answers them within the limits of design
/// D10. It opens no port: the host mod owns the listener and calls <see cref="ForwardAsync"/>.
/// </summary>
internal sealed class HttpHostCore
{
    private static readonly object SharedGate = new();
    private static HttpHostCore? _shared;

    private readonly object _gate = new();
    private readonly IHttpEndpointTransport _transport;
    private readonly int[] _supportedVersions;
    private readonly Action? _routesChanged;
    private readonly Dictionary<RouteKey, HttpRouteInfo> _routes = new();
    private readonly Dictionary<string, PendingRequest> _pending = new();
    private readonly Dictionary<string, DateTime> _absentSince = new();
    private readonly HashSet<string> _warnedOnce = new();

    private bool _held;
    private bool _registered;
    private bool _released;
    private bool _loopRunning;
    private CancellationTokenSource? _loopCts;

    internal HttpHostCore(
        IHttpEndpointTransport transport,
        Action? routesChanged = null,
        IEnumerable<int>? supportedVersions = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _routesChanged = routesChanged;
        _supportedVersions = (supportedVersions ?? HttpEndpointProtocol.SupportedVersions).ToArray();
    }

    #region Per-mod shared instance

    /// <summary>The host core of this mod copy. Created again after <see cref="Release"/>.</summary>
    internal static HttpHostCore Shared
    {
        get
        {
            lock (SharedGate)
            {
                if (_shared == null || _shared.IsReleased)
                {
                    _shared = new HttpHostCore(
                        ModHttpV1HttpEndpointTransport.Instance,
                        () => HttpHostContract.Instance.RaiseRoutesChanged());
                }

                return _shared;
            }
        }
    }

    /// <summary>True while this mod copy holds the host role.</summary>
    internal static bool IsHolding
    {
        get
        {
            lock (SharedGate)
            {
                return _shared?.IsHeld == true;
            }
        }
    }

    /// <summary>
    /// Releases the shared host role: answers in-flight requests with 502, clears the route table and unregisters
    /// <c>httphost</c>. Safe to call when not holding the role. Called from <c>Stop()</c> and the shared teardown of
    /// <c>ModBehaviourBase</c>. Idempotent and never throws.
    /// </summary>
    public static void Release()
    {
        HttpHostCore? old;
        lock (SharedGate)
        {
            old = _shared;
            _shared = null;
        }

        old?.Shutdown();
    }

    #endregion

    /// <summary>True while this instance holds the role.</summary>
    internal bool IsHeld
    {
        get
        {
            lock (_gate)
            {
                return _held && !_released;
            }
        }
    }

    internal bool IsReleased
    {
        get
        {
            lock (_gate)
            {
                return _released;
            }
        }
    }

    /// <summary>The accepted routes, as a read-only snapshot.</summary>
    internal IReadOnlyList<HttpRouteInfo> Routes
    {
        get
        {
            lock (_gate)
            {
                return _routes.Values.ToList();
            }
        }
    }

    /// <summary>Requests waiting for an answer. For tests and diagnostics.</summary>
    internal int PendingCount
    {
        get
        {
            lock (_gate)
            {
                return _pending.Count;
            }
        }
    }

    /// <summary>
    /// Claims the host role. Refuses, with one warning, when <c>httphost</c> is already on the hub, and leaves that host untouched.
    /// Otherwise registers the role, verifies the registration through the id list and starts the tick. Returns true when this
    /// instance holds the role; a second call from the holder returns true without registering again.
    /// </summary>
    internal bool Start()
    {
        lock (_gate)
        {
            if (_released)
            {
                return false;
            }

            if (_held)
            {
                return true;
            }
        }

        if (_transport.GetModIds().Contains(HttpEndpointProtocol.HostModId))
        {
            Warn("An httphost is already registered on the mod hub, so this mod does not start the HTTP host role.");
            return false;
        }

        _transport.Register(HttpEndpointProtocol.HostModId, HandleMessage);
        lock (_gate)
        {
            _registered = true;
        }

        if (!_transport.GetModIds().Contains(HttpEndpointProtocol.HostModId))
        {
            lock (_gate)
            {
                _registered = false;
            }

            Warn("The httphost registration could not be verified on the mod hub, so the HTTP host role was not started.");
            return false;
        }

        lock (_gate)
        {
            _held = true;
        }

        _transport.PostToMainThread(StartTickLoop);
        return true;
    }

    #region Tick

    private void StartTickLoop()
    {
        CancellationToken token;
        lock (_gate)
        {
            if (_released || _loopRunning)
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
                    _transport.Error(e, "HttpHost tick failed");
                }

                lock (_gate)
                {
                    if (_released)
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
    /// One step on the main thread: drops the routes of a mod whose hub client has been absent for 5 seconds and answers its
    /// in-flight requests with 502 at once.
    /// </summary>
    internal void Tick()
    {
        var now = _transport.UtcNow;
        var ids = _transport.GetModIds();
        var orphaned = new List<string>();
        var changed = false;
        lock (_gate)
        {
            if (_released)
            {
                return;
            }

            var owners = _routes.Values.Select(r => r.OwnerModId).Distinct().ToList();
            foreach (var owner in owners)
            {
                if (ids.Contains(HttpEndpointProtocol.ClientId(owner)))
                {
                    _absentSince.Remove(owner);
                    continue;
                }

                if (!_absentSince.TryGetValue(owner, out var since))
                {
                    _absentSince[owner] = now;
                }
                else if (now - since >= HttpEndpointProtocol.PresenceGrace)
                {
                    orphaned.Add(owner);
                }
            }

            foreach (var owner in orphaned)
            {
                changed |= RemoveRoutesOf(owner);
                _absentSince.Remove(owner);
            }
        }

        foreach (var owner in orphaned)
        {
            FailPendingOf(owner, "The endpoint owner left the mod hub.");
        }

        if (changed)
        {
            RaiseRoutesChanged();
        }
    }

    #endregion

    #region Forwarding

    /// <summary>
    /// Forwards one external request to the owning mod and returns its answer. Never throws, callable from any thread. Returns
    /// 503 when the role is not held or 64 requests are in flight, 404 for a route miss, 413 for an oversized body, 504 after 15
    /// seconds and 502 for an invalid or missing answer. Every path leaves no pending entry behind.
    /// </summary>
    internal UniTask<HttpEndpointResponse> ForwardAsync(HttpEndpointRequest? request)
    {
        try
        {
            return ForwardCore(request);
        }
        catch (Exception e)
        {
            _transport.Error(e, "HttpHost failed to forward a request");
            return UniTask.FromResult(HttpEndpointResponse.Text(502, "The endpoint could not be reached."));
        }
    }

    private UniTask<HttpEndpointResponse> ForwardCore(HttpEndpointRequest? request)
    {
        if (request == null || !HttpEndpointProtocol.TryNormalizeMethod(request.Method, out var method) ||
            !HttpEndpointProtocol.TryNormalizeRoutePath(request.Path, out var path))
        {
            return UniTask.FromResult(HttpEndpointResponse.Text(404, "No endpoint serves this request."));
        }

        var key = new RouteKey(method, path);
        var id = Guid.NewGuid().ToString("N");
        string ownerClientId;
        PendingRequest pending;
        lock (_gate)
        {
            if (!_held || _released)
            {
                return UniTask.FromResult(HttpEndpointResponse.Text(503, "The HTTP host role is not held."));
            }

            if (!_routes.TryGetValue(key, out var route))
            {
                return UniTask.FromResult(HttpEndpointResponse.Text(404, "No endpoint serves this request."));
            }

            if (!HttpEndpointProtocol.IsWithinBodyLimit(request.Body))
            {
                return UniTask.FromResult(HttpEndpointResponse.Text(413, "The request body is too large."));
            }

            if (_pending.Count >= HttpEndpointProtocol.MaxInFlightRequests)
            {
                return UniTask.FromResult(HttpEndpointResponse.Text(503, "Too many requests are in flight."));
            }

            ownerClientId = HttpEndpointProtocol.ClientId(route.OwnerModId);
            pending = new PendingRequest(id, route.OwnerModId, ownerClientId);
            _pending[id] = pending;
        }

        try
        {
            _transport.Send(
                HttpEndpointProtocol.HostModId,
                ownerClientId,
                HttpEndpointProtocol.RequestContentType,
                HttpEndpointProtocol.EncodeRequest(id, key.Method, key.Path, request));
        }
        catch (Exception e)
        {
            _transport.Error(e, "HttpHost failed to send a request to its endpoint owner");
            Complete(id, HttpEndpointResponse.Text(502, "The endpoint could not be reached."));
            return pending.Completion.Task;
        }

        AwaitTimeout(id, pending.Timeout.Token).Forget();
        return pending.Completion.Task;
    }

    private async UniTaskVoid AwaitTimeout(string id, CancellationToken token)
    {
        try
        {
            await _transport.Delay(HttpEndpointProtocol.RequestTimeout, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Complete(id, HttpEndpointResponse.Text(504, "The endpoint did not answer in time."));
    }

    /// <summary>Completes one pending request once and removes it. A later completion for the same id does nothing.</summary>
    private void Complete(string id, HttpEndpointResponse response)
    {
        PendingRequest? pending = null;
        lock (_gate)
        {
            if (_pending.TryGetValue(id, out var found))
            {
                _pending.Remove(id);
                pending = found;
            }
        }

        if (pending == null)
        {
            return;
        }

        pending.Timeout.Cancel();
        pending.Completion.TrySetResult(response);
    }

    private void FailPendingOf(string owner, string reason)
    {
        List<string> ids;
        lock (_gate)
        {
            ids = _pending.Values.Where(p => p.Owner == owner).Select(p => p.Id).ToList();
        }

        foreach (var id in ids)
        {
            Complete(id, HttpEndpointResponse.Text(502, reason));
        }
    }

    /// <summary>Handles <c>response.v1</c>: accepted only for a pending id and only from the owner's hub client.</summary>
    private void HandleResponse(string from, string body)
    {
        if (!HttpEndpointProtocol.TryDecodeResponse(body, out var id, out var response))
        {
            return;
        }

        lock (_gate)
        {
            if (!_pending.TryGetValue(id, out var pending) || pending.OwnerClientId != from)
            {
                return;
            }
        }

        if (!HttpEndpointProtocol.IsValidStatus(response.Status))
        {
            Complete(id, HttpEndpointResponse.Text(502, "The endpoint returned an invalid response."));
            return;
        }

        if (!HttpEndpointProtocol.IsWithinBodyLimit(response.Body))
        {
            Complete(id, HttpEndpointResponse.Text(502, "The endpoint response is too large."));
            return;
        }

        Complete(id, response);
    }

    #endregion

    #region Messages

    /// <summary>Hub callback for <c>httphost</c>. Runs on the main thread.</summary>
    internal void HandleMessage(string from, string contentType, string body)
    {
        switch (contentType)
        {
            case HttpEndpointProtocol.HelloContentType:
                HandleHello(from, body);
                break;
            case HttpEndpointProtocol.RegisterContentType:
                HandleRegister(from, body);
                break;
            case HttpEndpointProtocol.ResponseContentType:
                HandleResponse(from, body);
                break;
            default:
                WarnOnce(
                    $"content:{from}:{contentType}",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "httphost ignored a message of unknown content type '{0}' from {1}.",
                        contentType,
                        from));
                break;
        }
    }

    private void HandleHello(string from, string body)
    {
        if (!HttpEndpointProtocol.TryGetSenderModId(from, out _) ||
            !HttpEndpointProtocol.TryDecodeHello(body, out _, out var clientVersions))
        {
            return;
        }

        var version = HttpEndpointProtocol.PickVersion(clientVersions, _supportedVersions);
        _transport.Send(
            HttpEndpointProtocol.HostModId,
            from,
            HttpEndpointProtocol.ReadyContentType,
            HttpEndpointProtocol.EncodeReady(version, _supportedVersions));
    }

    /// <summary>
    /// Replaces every route of the sender with the accepted entries of the snapshot and answers with the accepted and rejected
    /// entries. The owner is the sender's hub client id without ".http"; the body's <c>mod</c> member is not trusted.
    /// </summary>
    private void HandleRegister(string from, string body)
    {
        if (!HttpEndpointProtocol.TryGetSenderModId(from, out var owner) ||
            !HttpEndpointProtocol.TryDecodeRegister(body, out _, out var endpoints))
        {
            return;
        }

        var accepted = new List<RouteKey>();
        var rejected = new List<RejectedRoute>();
        var next = new Dictionary<RouteKey, HttpRouteInfo>();
        bool changed;
        lock (_gate)
        {
            if (_released)
            {
                return;
            }

            foreach (var endpoint in endpoints)
            {
                if (!HttpEndpointProtocol.TryNormalizeMethod(endpoint.Method, out var method))
                {
                    rejected.Add(new RejectedRoute(endpoint.Method, endpoint.Path, "the method is not supported"));
                    continue;
                }

                if (!HttpEndpointProtocol.TryNormalizeEndpointPath(owner, endpoint.Path, out var path, out var reason))
                {
                    rejected.Add(new RejectedRoute(method, endpoint.Path, reason));
                    continue;
                }

                var key = new RouteKey(method, path);
                if (next.ContainsKey(key))
                {
                    rejected.Add(new RejectedRoute(method, path, "the same endpoint appears twice in this snapshot"));
                    continue;
                }

                if (_routes.TryGetValue(key, out var existing) && existing.OwnerModId != owner)
                {
                    rejected.Add(new RejectedRoute(
                        method,
                        path,
                        "already served by mod " + existing.OwnerModId));
                    continue;
                }

                if (next.Count >= HttpEndpointProtocol.MaxEndpointsPerMod)
                {
                    rejected.Add(new RejectedRoute(
                        method,
                        path,
                        $"the limit of {HttpEndpointProtocol.MaxEndpointsPerMod} endpoints per mod is reached"));
                    continue;
                }

                next[key] = new HttpRouteInfo(method, path, owner);
                accepted.Add(key);
            }

            var previous = _routes.Where(kv => kv.Value.OwnerModId == owner).Select(kv => kv.Key).ToList();
            changed = !new HashSet<RouteKey>(previous).SetEquals(next.Keys);
            foreach (var key in previous)
            {
                _routes.Remove(key);
            }

            foreach (var pair in next)
            {
                _routes[pair.Key] = pair.Value;
            }
        }

        _transport.Send(
            HttpEndpointProtocol.HostModId,
            from,
            HttpEndpointProtocol.RegisteredContentType,
            HttpEndpointProtocol.EncodeRegistered(accepted, rejected));

        if (changed)
        {
            RaiseRoutesChanged();
        }
    }

    #endregion

    #region Release

    /// <summary>
    /// Answers in-flight requests with 502, clears the route table, unregisters <c>httphost</c> and stops the tick. Idempotent and
    /// never throws.
    /// </summary>
    internal void Shutdown()
    {
        List<PendingRequest> inFlight;
        bool unregister;
        bool hadRoutes;
        CancellationTokenSource? loop;
        lock (_gate)
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _held = false;
            inFlight = _pending.Values.ToList();
            _pending.Clear();
            hadRoutes = _routes.Count > 0;
            _routes.Clear();
            _absentSince.Clear();
            unregister = _registered;
            _registered = false;
            loop = _loopCts;
            _loopCts = null;
        }

        foreach (var pending in inFlight)
        {
            try
            {
                pending.Timeout.Cancel();
                pending.Completion.TrySetResult(HttpEndpointResponse.Text(502, "The HTTP host was released."));
            }
            catch (Exception e)
            {
                _transport.Error(e, "HttpHost failed to answer a request during release");
            }
        }

        try
        {
            loop?.Cancel();
        }
        catch (Exception e)
        {
            _transport.Error(e, "HttpHost failed to stop the tick");
        }

        if (unregister)
        {
            try
            {
                _transport.Unregister(HttpEndpointProtocol.HostModId);
            }
            catch (Exception e)
            {
                _transport.Error(e, "HttpHost failed to unregister httphost");
            }
        }

        if (hadRoutes)
        {
            RaiseRoutesChanged();
        }
    }

    #endregion

    /// <summary>Removes every route of one owner. Caller holds the lock. Returns true when a route was removed.</summary>
    private bool RemoveRoutesOf(string owner)
    {
        var keys = _routes.Where(kv => kv.Value.OwnerModId == owner).Select(kv => kv.Key).ToList();
        foreach (var key in keys)
        {
            _routes.Remove(key);
        }

        return keys.Count > 0;
    }

    private void RaiseRoutesChanged()
    {
        try
        {
            _routesChanged?.Invoke();
        }
        catch (Exception e)
        {
            _transport.Error(e, "HttpHost routes listener failed");
        }
    }

    private void Warn(string message) => _transport.Warn(message);

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

    /// <summary>One forwarded request waiting for its answer.</summary>
    private sealed class PendingRequest
    {
        public PendingRequest(string id, string owner, string ownerClientId)
        {
            Id = id;
            Owner = owner;
            OwnerClientId = ownerClientId;
            Completion = new UniTaskCompletionSource<HttpEndpointResponse>();
            Timeout = new CancellationTokenSource();
        }

        public string Id { get; }

        public string Owner { get; }

        public string OwnerClientId { get; }

        public UniTaskCompletionSource<HttpEndpointResponse> Completion { get; }

        public CancellationTokenSource Timeout { get; }
    }
}
