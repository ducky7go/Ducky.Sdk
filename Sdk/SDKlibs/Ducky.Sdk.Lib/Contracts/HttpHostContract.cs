using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.HttpEndpoints;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// HTTP host role: exactly one mod at a time claims it with <see cref="Start"/>, forwards external requests with
/// <see cref="ForwardAsync"/> and releases it with <see cref="Stop"/>. The SDK opens no port; the host mod owns the listener.
/// See docs/http-endpoints.md.
/// </summary>
public sealed class HttpHostContract
{
    internal static readonly HttpHostContract Instance = new();

    private Action? _routesChanged;

    internal HttpHostContract()
    {
    }

    /// <summary>
    /// Claims the host role. Returns false, with one warning, when another mod already holds it. Call on the Unity main thread, for
    /// example in <c>ModEnabled()</c>.
    /// </summary>
    public bool Start() => HttpHostCore.Shared.Start();

    /// <summary>Releases the host role and answers in-flight requests with 502. Safe to call when the role is not held.</summary>
    public void Stop() => HttpHostCore.Release();

    /// <summary>True while this mod holds the host role.</summary>
    public bool IsHost => HttpHostCore.IsHolding;

    /// <summary>
    /// Forwards one external request to the endpoint's owner and returns its answer. Never throws and callable from any thread. Returns
    /// 503 when the role is not held, 404 for a route miss, 413 for an oversized body, 504 after 15 seconds and 502 for a broken answer.
    /// </summary>
    public UniTask<HttpEndpointResponse> ForwardAsync(HttpEndpointRequest request) =>
        HttpHostCore.Shared.ForwardAsync(request);

    /// <summary>The accepted routes as a read-only snapshot of method, path and owning mod id.</summary>
    public IReadOnlyList<HttpRouteInfo> Routes => HttpHostCore.Shared.Routes;

    /// <summary>Raised when the route table changes. Raised on the Unity main thread.</summary>
    public event Action? RoutesChanged
    {
        add => _routesChanged += value;
        remove => _routesChanged -= value;
    }

    internal void RaiseRoutesChanged() => _routesChanged?.Invoke();
}
