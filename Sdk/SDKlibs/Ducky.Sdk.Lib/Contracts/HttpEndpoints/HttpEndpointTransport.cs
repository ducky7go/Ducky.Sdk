using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.ModProtocols;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Threading;

namespace Ducky.Sdk.Contracts.HttpEndpoints;

/// <summary>
/// Everything the endpoint client and the host core need from the outside world: the hub, the main thread, the clock,
/// the delay and the log. The tests replace it with a fake; the game uses <see cref="ModHttpV1HttpEndpointTransport"/>.
/// </summary>
internal interface IHttpEndpointTransport
{
    /// <summary>The hub's registered ids.</summary>
    IReadOnlyList<string> GetModIds();

    /// <summary>
    /// Changes each time the hub object this client is bound to is replaced (a hot reload of the mod that started the hub).
    /// Reading it refreshes the binding; the client resets its protocol state when the value changes.
    /// </summary>
    int HubGeneration { get; }

    /// <summary>Registers a hub client. The handler is called on the main thread.</summary>
    void Register(string clientId, Action<string, string, string> handler);

    /// <summary>Unregisters a hub client. Unknown ids are ignored.</summary>
    void Unregister(string clientId);

    /// <summary>Sends one message through the hub. Never blocks.</summary>
    void Send(string from, string to, string contentType, string body);

    /// <summary>Runs the action on the main thread, later and never inline.</summary>
    void PostToMainThread(Action action);

    /// <summary>A delay that completes on the main thread, or fails with <see cref="OperationCanceledException"/>.</summary>
    Func<TimeSpan, CancellationToken, UniTask> Delay { get; }

    /// <summary>The current UTC time.</summary>
    DateTime UtcNow { get; }

    /// <summary>Logs one warning.</summary>
    void Warn(string message);

    /// <summary>Logs one error with its exception.</summary>
    void Error(Exception exception, string message);
}

/// <summary>
/// The real transport over the shared <see cref="ModHttpV1Proxy"/> hub. The only file of the endpoint channel that touches
/// Unity or the hub. A hub callback only posts its work to the main thread (design D7).
/// </summary>
internal sealed class ModHttpV1HttpEndpointTransport : IHttpEndpointTransport
{
    public static readonly ModHttpV1HttpEndpointTransport Instance = new();

    // One binding per mod copy. It follows the live hub object (see HubBinding), so a hot reload of the hub's mod is
    // picked up without a game restart. Created on first use, on the main thread, never in a static constructor.
    private static readonly object BindingGate = new();
    private static HubBinding? _binding;

    private static HubBinding Binding
    {
        get
        {
            lock (BindingGate)
            {
                return _binding ??= HubBinding.ForSingleton(virtualFallback: false);
            }
        }
    }

    public int HubGeneration => Binding.Generation;

    public IReadOnlyList<string> GetModIds() => Binding.GetModIds();

    public void Register(string clientId, Action<string, string, string> handler)
    {
        Binding.Register(clientId, (from, contentType, body) =>
        {
            MainThread.Post(() =>
            {
                try
                {
                    handler(from, contentType, body);
                }
                catch (Exception e)
                {
                    Log.Error(e, "HttpEndpoints failed to handle a message from {From}", from);
                }
            });

            return UniTask.CompletedTask;
        });
    }

    public void Unregister(string clientId) => Binding.Unregister(clientId);

    public void Send(string from, string to, string contentType, string body)
    {
        Binding.Notify(from, to, contentType, body).Forget();
    }

    public void PostToMainThread(Action action) => MainThread.Post(action);

    public Func<TimeSpan, CancellationToken, UniTask> Delay => (delay, token) =>
        UniTask.Delay(delay, cancellationToken: token);

    public DateTime UtcNow => DateTime.UtcNow;

    public void Warn(string message) => Log.Warn(message);

    public void Error(Exception exception, string message) => Log.Error(exception, "{Message}", message);
}
