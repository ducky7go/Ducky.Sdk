using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.HttpEndpoints;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// HTTP endpoint API: a mod declares endpoints under its own id with <see cref="Map"/>, and the mod that holds the host role
/// serves them to the outside world. Declaring an endpoint costs nothing while no host exists. See docs/http-endpoints.md.
/// </summary>
public sealed class HttpEndpointsContract
{
    internal static readonly HttpEndpointsContract Instance = new();

    internal HttpEndpointsContract()
    {
    }

    /// <summary>
    /// Declares one endpoint whose handler answers at once. The path must start with "/&lt;modId&gt;". Dispose the returned handle
    /// to withdraw the endpoint.
    /// </summary>
    /// <exception cref="ArgumentException">The method, path or a duplicate is not allowed.</exception>
    public IDisposable Map(string method, string path, Func<HttpEndpointRequest, HttpEndpointResponse> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return HttpEndpointClient.Shared.Map(method, path, handler);
    }

    /// <summary>
    /// Declares one endpoint whose handler may await work. It runs on the Unity main thread, so Unity APIs are allowed. Dispose the
    /// returned handle to withdraw the endpoint.
    /// </summary>
    /// <exception cref="ArgumentException">The method, path or a duplicate is not allowed.</exception>
    public IDisposable Map(string method, string path, Func<HttpEndpointRequest, UniTask<HttpEndpointResponse>> handler)
    {
        return HttpEndpointClient.Shared.Map(method, path, handler);
    }

    /// <summary>True while an <c>httphost</c> is registered on the mod hub.</summary>
    public bool IsHostAvailable =>
        ModHttpV1HttpEndpointTransport.Instance.GetModIds().Contains(HttpEndpointProtocol.HostModId);
}
