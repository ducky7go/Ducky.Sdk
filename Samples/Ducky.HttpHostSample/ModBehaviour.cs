using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.HttpEndpoints;

namespace Ducky.HttpHostSample;

/// <summary>
/// A test aid, not a product: claims the httphost role and forwards requests from a loopback-only HttpListener on a fixed
/// test port. The SDK opens no port; this mod does, and only on 127.0.0.1.
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    /// <summary>Fixed loopback port of the test listener.</summary>
    public const int TestPort = 18765;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    protected override void ModEnabled()
    {
        if (!Contract.HttpHost.Start())
        {
            Log.Warn("Ducky.HttpHostSample did not start: another httphost holds the role");
            return;
        }

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{TestPort}/");
        _listener.Start();
        _cts = new CancellationTokenSource();
        Serve(_listener, _cts.Token);
        Log.Info("Ducky.HttpHostSample listening on 127.0.0.1:{Port}", TestPort);
    }

    protected override void ModDisabled()
    {
        // Stop first: in-flight requests get 502 while the listener can still answer them.
        Contract.HttpHost.Stop();
        _cts?.Cancel();
        _listener?.Close();
        _listener = null;
        Log.Info("Ducky.HttpHostSample stopped");
    }

    private async void Serve(HttpListener listener, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception)
            {
                return;
            }

            _ = HandleAsync(context);
        }
    }

    private static async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        try
        {
            var forwarded = new HttpEndpointRequest
            {
                Method = request.HttpMethod,
                Path = request.Url?.AbsolutePath ?? "/",
                Query = ReadQuery(request),
                Headers = ReadHeaders(request),
                ContentType = request.ContentType ?? string.Empty,
                Body = ReadBody(request)
            };

            var answer = await Contract.HttpHost.ForwardAsync(forwarded);
            response.StatusCode = answer.Status;
            response.ContentType = answer.ContentType;
            var bytes = Encoding.UTF8.GetBytes(answer.Body);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }
        catch (Exception)
        {
            // The client went away; nothing else to answer.
        }
        finally
        {
            response.Close();
        }
    }

    private static IReadOnlyList<KeyValuePair<string, string>> ReadQuery(HttpListenerRequest request)
    {
        var pairs = new List<KeyValuePair<string, string>>();
        foreach (var key in request.QueryString.AllKeys)
        {
            if (key == null)
            {
                continue;
            }

            foreach (var value in request.QueryString.GetValues(key) ?? Array.Empty<string>())
            {
                pairs.Add(new KeyValuePair<string, string>(key, value ?? string.Empty));
            }
        }

        return pairs;
    }

    private static IReadOnlyDictionary<string, string> ReadHeaders(HttpListenerRequest request)
    {
        var headers = new Dictionary<string, string>();
        foreach (var key in request.Headers.AllKeys)
        {
            if (key != null)
            {
                headers[key.ToLowerInvariant()] = request.Headers[key] ?? string.Empty;
            }
        }

        return headers;
    }

    private static string ReadBody(HttpListenerRequest request)
    {
        if (!request.HasEntityBody)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
        // One character past the 65536-byte limit, so the host can answer 413 for an oversized body.
        var buffer = new char[65536 + 1];
        var read = reader.ReadBlock(buffer, 0, buffer.Length);
        return new string(buffer, 0, read);
    }
}
