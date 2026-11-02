using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Ducky.Sdk.Contracts.ModProtocols;

/// <summary>
/// The part of the hub proxy that <see cref="HubBinding"/> uses. <see cref="ModHttpV1Proxy"/> implements it; the tests
/// replace it with a fake.
/// </summary>
internal interface IHubProxy
{
    /// <summary>True while the proxy waits for a real hub and holds its registrations and messages itself.</summary>
    bool IsVirtual { get; }

    void RegisterClient(string modId, Func<string, string, string, UniTask> callback);

    /// <summary>Unregisters the registration this proxy made for <paramref name="modId"/>.</summary>
    void UnregisterClient(string modId);

    UniTask Notify(string fromModId, string toModId, string contentType, string body);

    IReadOnlyList<string> GetModIds();

    /// <summary>Stops background work of a virtual proxy. Idempotent.</summary>
    void Release();
}

/// <summary>
/// The one place that decides which hub object is alive and whether this Core's hub clients are registered on it.
/// Every SDK-internal hub client (the HTTP endpoint transport, the debug event host, the client contracts) goes through
/// one binding. The binding keeps the last hub object it saw and asks the locator again only when that object is no
/// longer alive. When the hub object has changed, it binds one new proxy and registers every tracked client on it, so a
/// hot reload leaves each client registered once on the new hub. No Unity API is called in this class; the game-facing
/// seams are supplied by <see cref="ForSingleton"/>.
/// </summary>
internal sealed class HubBinding
{
    private static readonly IReadOnlyList<string> NoModIds = Array.Empty<string>();

    private readonly object _gate = new();
    private readonly Func<object?> _locate;
    private readonly Func<object, bool> _isAlive;
    private readonly Func<object, IHubProxy> _bindProxy;
    private readonly Func<IHubProxy>? _createVirtualProxy;
    private readonly Dictionary<string, Func<string, string, string, UniTask>> _clients = new();

    private object? _hubObject;
    private IHubProxy? _proxy;
    private int _generation;

    /// <param name="locate">Returns the live hub object, or null when none exists.</param>
    /// <param name="isAlive">Whether a previously returned hub object is still usable.</param>
    /// <param name="bindProxy">Creates a proxy bound to a hub object.</param>
    /// <param name="createVirtualProxy">
    /// When not null, the binding keeps one virtual proxy while no hub exists, so messages are held until a hub appears.
    /// When null, nothing is sent or queued while no hub exists.
    /// </param>
    internal HubBinding(
        Func<object?> locate,
        Func<object, bool> isAlive,
        Func<object, IHubProxy> bindProxy,
        Func<IHubProxy>? createVirtualProxy = null)
    {
        _locate = locate ?? throw new ArgumentNullException(nameof(locate));
        _isAlive = isAlive ?? throw new ArgumentNullException(nameof(isAlive));
        _bindProxy = bindProxy ?? throw new ArgumentNullException(nameof(bindProxy));
        _createVirtualProxy = createVirtualProxy;
    }

    /// <summary>
    /// Incremented each time a proxy is bound to a new hub object. A client that keeps state about the hub can compare
    /// its last value with this one. Reading it first refreshes the binding.
    /// </summary>
    internal int Generation
    {
        get
        {
            lock (_gate)
            {
                Current(out _);
                return _generation;
            }
        }
    }

    /// <summary>The hub's registered ids. Empty while no hub exists.</summary>
    internal IReadOnlyList<string> GetModIds()
    {
        lock (_gate)
        {
            return Current(out _)?.GetModIds() ?? NoModIds;
        }
    }

    /// <summary>Tracks the client and registers it on the live hub. Registers it once even when the hub was just bound.</summary>
    internal void Register(string modId, Func<string, string, string, UniTask> callback)
    {
        lock (_gate)
        {
            _clients[modId] = callback;
            var proxy = Current(out var bound);
            if (proxy != null && !bound)
            {
                proxy.RegisterClient(modId, callback);
            }
        }
    }

    /// <summary>
    /// Stops tracking the client and removes its registration from the hub it is bound to. A registration on a replaced
    /// hub is gone already, so nothing is done there; a newer registration under the same id is never removed here.
    /// </summary>
    internal void Unregister(string modId)
    {
        lock (_gate)
        {
            _clients.Remove(modId);
            if (_proxy == null)
            {
                return;
            }

            if (_proxy.IsVirtual || (_hubObject != null && _isAlive(_hubObject)))
            {
                _proxy.UnregisterClient(modId);
            }
        }
    }

    /// <summary>Sends one message through the live hub. Nothing is sent or queued while no hub exists and no fallback is set.</summary>
    internal UniTask Notify(string fromModId, string toModId, string contentType, string body)
    {
        lock (_gate)
        {
            var proxy = Current(out _);
            return proxy == null
                ? UniTask.CompletedTask
                : proxy.Notify(fromModId, toModId, contentType, body);
        }
    }

    /// <summary>Drops every tracked client, unregisters them and releases the proxy. Idempotent.</summary>
    internal void Release()
    {
        lock (_gate)
        {
            if (_proxy != null)
            {
                var stillLive = _proxy.IsVirtual || (_hubObject != null && _isAlive(_hubObject));
                foreach (var modId in _clients.Keys)
                {
                    if (stillLive)
                    {
                        _proxy.UnregisterClient(modId);
                    }
                }

                _proxy.Release();
            }

            _clients.Clear();
            _proxy = null;
            _hubObject = null;
        }
    }

    /// <summary>
    /// Returns the proxy to use now, or null when no hub exists and no fallback is set. Caller holds the gate.
    /// <paramref name="bound"/> is true when this call created the proxy and registered every tracked client on it.
    /// </summary>
    private IHubProxy? Current(out bool bound)
    {
        bound = false;
        if (_hubObject != null && _isAlive(_hubObject))
        {
            return _proxy;
        }

        // The last hub object is gone, or none was seen yet: look again.
        var found = _locate();
        if (found != null)
        {
            _hubObject = found;
            if (_proxy != null && _proxy.IsVirtual)
            {
                // The virtual proxy finds this hub through its own check, moves its clients and replays what it holds.
                return _proxy;
            }

            _proxy = _bindProxy(found);
            _generation++;
            RegisterAll(_proxy);
            bound = true;
            return _proxy;
        }

        _hubObject = null;
        if (_createVirtualProxy == null)
        {
            _proxy = null;
            return null;
        }

        if (_proxy != null && _proxy.IsVirtual)
        {
            return _proxy;
        }

        _proxy = _createVirtualProxy();
        RegisterAll(_proxy);
        bound = true;
        return _proxy;
    }

    private void RegisterAll(IHubProxy proxy)
    {
        foreach (var client in _clients)
        {
            proxy.RegisterClient(client.Key, client.Value);
        }
    }

    /// <summary>
    /// The game-facing binding: the hub is the <c>ModHttpV1</c> component found by name, and liveness is Unity's
    /// null check. Main thread only, like every Unity call it makes.
    /// </summary>
    internal static HubBinding ForSingleton(bool virtualFallback) => new(
        FindLiveHubObject,
        hub => hub is not UnityEngine.Object unityObject || unityObject != null,
        hub => ModHttpV1Proxy.BindToHub(hub),
        virtualFallback ? () => ModHttpV1Proxy.CreateVirtual() : null);

    /// <summary>The live hub component, found by type name because the hub type may come from another Core.</summary>
    private static object? FindLiveHubObject()
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
}
