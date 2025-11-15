using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace Ducky.Sdk.Contracts;

public class ModBusClientContract : MonoBehaviour
{
    static ModBusClientContract()
    {
        var modId = Helper.GetModId();
        Log.Info($"Initializing ModBusClientContract for mod {modId}.");
        var objName = $"{modId}_ModBusClientContractHolder";
        var existing = GameObject.Find(objName);
        if (existing != null)
        {
            Instance = existing.GetComponent<ModBusClientContract>();
            if (Instance != null)
            {
                Log.Info($"ModBusClientContract instance for mod {modId} already exists.");
                return;
            }
        }

        var go = new GameObject(objName);
        Instance = go.AddComponent<ModBusClientContract>();
        DontDestroyOnLoad(go);
    }

    public static ModBusClientContract Instance { get; }
    private ModBusV1Proxy? _proxy;
    private string _modId = Helper.GetModId();

    public async UniTask Connect(ModBusClientHandler handler)
    {
        if (_proxy != null)
        {
            await SendToTerminal(TerminalCommand.Offline(_modId));
            _proxy.UnregisterClient(_modId);
        }

        _proxy = ModBusV1Proxy.CreateFromSingleton();
        _modId = Helper.GetModId();
        _proxy.RegisterClient(_modId, Callback);
        await SendToTerminal(TerminalCommand.Online(_modId));
        return;

        UniTask Callback(string fromModId, string message) =>
            handler(fromModId == ModBusTerminalContract.TerminalModId, fromModId, message, SendToTerminal);
    }

    private void OnDestroy()
    {
        UniTask.RunOnThreadPool(async () =>
        {
            if (_proxy != null)
            {
                await SendToTerminal(TerminalCommand.Offline(_modId));
                _proxy.UnregisterClient(_modId);
            }
        }).Forget();
    }

    public UniTask SendTo(string modId, string message)
    {
        if (_proxy == null)
        {
            throw new InvalidOperationException("ModBusClientContract is not connected. Call ConnectToModBus first.");
        }

        return _proxy.Notify(_modId, modId, message);
    }

    public UniTask SendToTerminal(string message)
    {
        if (_proxy == null)
        {
            throw new InvalidOperationException("ModBusClientContract is not connected. Call ConnectToModBus first.");
        }

        return _proxy.Notify(_modId, ModBusTerminalContract.TerminalModId, message);
    }
}

public delegate UniTask ResponseToTerminalHandler(string responseMessage);

public delegate UniTask ModBusClientHandler(bool isFromTerminal,
    string fromModId,
    string message,
    ResponseToTerminalHandler responseToTerminal);
