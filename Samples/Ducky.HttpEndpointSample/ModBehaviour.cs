using System;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.HttpEndpoints;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.HttpEndpointSample;

/// <summary>
/// Declares three endpoints under this mod's id: a JSON status, a frame counter that reads a Unity API on the main thread, and an
/// echo of the POST body. Works with or without a host mod; with the host sample enabled they answer over HTTP. The endpoints are
/// registered through <c>Subscriptions</c>, so a hot reload releases them without manual disposal, and <c>/status</c> names the
/// Core assembly that answered, so a caller can see which Core is live after a reload.
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        // Endpoints must sit under the id the SDK reports for this mod, which is "local.<folder>" for a local mod folder.
        var prefix = "/" + Helper.GetModId().Value;

        Subscriptions.Add(Contract.Http.Map("GET", prefix + "/status", _ =>
            HttpEndpointResponse.Json(200,
                "{\"mod\":\"" + Helper.GetModId().Value + "\",\"ok\":true,\"core\":\"" + CoreAssemblyName + "\"}")));

        Subscriptions.Add(Contract.Http.Map("GET", prefix + "/frame", _ =>
            HttpEndpointResponse.Text(200, Time.frameCount.ToString())));

        Subscriptions.Add(Contract.Http.Map("POST", prefix + "/echo", request =>
            HttpEndpointResponse.Text(200, request.Body)));

        Log.Info("Ducky.HttpEndpointSample mapped 3 endpoints from {Core}", CoreAssemblyName);
    }

    protected override void ModDisabled()
    {
        Log.Info("Ducky.HttpEndpointSample disabled");
    }

    /// <summary>The name of the assembly this Core was loaded from. It answers <c>/status</c>.</summary>
    private static string CoreAssemblyName => typeof(ModBehaviour).Assembly.GetName().Name;
}
