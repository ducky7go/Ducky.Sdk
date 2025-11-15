using System;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.Sdk.Contracts;

public class ModBusTerminalContract : MonoBehaviour
{
    public const string TerminalModId = "TerminalMod";

    static ModBusTerminalContract()
    {
        var modId = Helper.GetModId();
        Log.Info($"Initializing ModBusTerminalContract for mod {modId}.");
        const string objName = $"{TerminalModId}_ModBusTerminalContract";
        var existing = GameObject.Find(objName);
        if (existing != null)
        {
            Instance = existing.GetComponent<ModBusTerminalContract>();
            if (Instance != null)
            {
                Log.Info($"ModBusTerminalContract instance for mod {modId} already exists.");
                return;
            }
        }

        var go = new GameObject(objName);
        Instance = go.AddComponent<ModBusTerminalContract>();
        DontDestroyOnLoad(go);
    }

    public static ModBusTerminalContract Instance { get; }
    private ModBusV1Proxy? _proxy;
    private string _modId = TerminalModId;

    public UniTask Connect(ModBusTerminalHandler handler)
    {
        if (_proxy != null)
        {
            _proxy.UnregisterClient(_modId);
        }

        _proxy = ModBusV1Proxy.CreateFromSingleton();
        _proxy.RegisterClient(_modId, Callback);
        return UniTask.CompletedTask;

        UniTask Callback(string fromModId, string message)
        {
            return handler(fromModId, message);
        }
    }

    private void OnDestroy()
    {
        if (_proxy != null)
        {
            _proxy.UnregisterClient(_modId);
        }
    }

    public UniTask SendTo(string modId, string message)
    {
        if (_proxy == null)
        {
            throw new InvalidOperationException("ModBusClientContract is not connected. Call ConnectToModBus first.");
        }


        return _proxy.Notify(_modId, modId, message);
    }
}

public static class TerminalCommand
{
    public static string Online(string modId) => $"online {modId}";
    public static string Offline(string modId) => $"offline {modId}";
    public static string Show(string content) => $"show {content}";
}

public delegate UniTask ModBusTerminalHandler(string fromModId,
    string message);
