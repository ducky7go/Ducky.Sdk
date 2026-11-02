using System;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.HttpEndpoints;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.HttpEndpointSample;

/// <summary>
/// Declares three endpoints under this mod's id: a JSON status, a frame counter that reads a Unity API on the main thread, and an
/// echo of the POST body. Works with or without a host mod; with the host sample enabled they answer over HTTP.
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    private IDisposable? _status;
    private IDisposable? _frame;
    private IDisposable? _echo;

    protected override void ModEnabled()
    {
        // Endpoints must sit under the id the SDK reports for this mod, which is "local.<folder>" for a local mod folder.
        var prefix = "/" + Helper.GetModId().Value;

        _status = Contract.Http.Map("GET", prefix + "/status", _ =>
            HttpEndpointResponse.Json(200, "{\"mod\":\"" + Helper.GetModId().Value + "\",\"ok\":true}"));

        _frame = Contract.Http.Map("GET", prefix + "/frame", _ =>
            HttpEndpointResponse.Text(200, Time.frameCount.ToString()));

        _echo = Contract.Http.Map("POST", prefix + "/echo", request =>
            HttpEndpointResponse.Text(200, request.Body));

        Log.Info("Ducky.HttpEndpointSample mapped 3 endpoints");
    }

    protected override void ModDisabled()
    {
        _status?.Dispose();
        _status = null;
        _frame?.Dispose();
        _frame = null;
        _echo?.Dispose();
        _echo = null;
        Log.Info("Ducky.HttpEndpointSample disabled");
    }
}
