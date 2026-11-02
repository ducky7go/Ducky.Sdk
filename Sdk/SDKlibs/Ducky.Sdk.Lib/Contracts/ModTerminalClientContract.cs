using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// ModTerminalClientContract is a client contract for terminal communication with terminal UI mod.
/// </summary>
public class ModTerminalClientContract : MonoBehaviour
{
    public const string TerminalUIModId = "TerminalUIMod";
    public const string ContentTypeCli = "cli";

    private static readonly ContractInstanceTable<ModTerminalClientContract> Instances = new();
    private static GameObject? _containerGameObject;

    private ModHttpV1ClientContract? _client;
    private string _modId = string.Empty;

    /// <summary>
    /// 获取或创建基于 modId 的单例实例
    /// </summary>
    /// <param name="modId">mod 的唯一标识符</param>
    /// <returns>ModTerminalClientContract 实例</returns>
    public static ModTerminalClientContract GetOrCreate(string modId)
    {
        if (string.IsNullOrEmpty(modId))
        {
            throw new ArgumentException("modId cannot be null or empty", nameof(modId));
        }

        // 已存在且仍然有效的实例直接返回；否则创建新实例并记录
        return Instances.GetOrAdd(modId, CreateContract, instance => instance != null);
    }

    private static ModTerminalClientContract CreateContract(string modId)
    {
        // 创建容器 GameObject（如果不存在）
        if (_containerGameObject == null)
        {
            _containerGameObject = new GameObject("ModTerminalClientContracts");
            DontDestroyOnLoad(_containerGameObject);
        }

        // 创建新实例
        var go = new GameObject($"ModBusTerminal_{modId}");
        go.transform.SetParent(_containerGameObject.transform);
        var contract = go.AddComponent<ModTerminalClientContract>();
        contract.Setup(modId);

        Log.Info($"Created ModTerminalClientContract singleton for modId: {modId}");
        return contract;
    }

    private void Setup(string modId)
    {
        _modId = modId;
    }

    /// <summary>
    /// connect to terminal UI mod and setup handler for incoming messages
    /// </summary>
    /// <param name="handler"></param>
    public async UniTask Connect(ModTerminalHandler handler)
    {
        // 先移除旧客户端和新客户端上已有的处理器，保证重复 Connect 后每个事件只有一个处理器
        if (_client != null)
        {
            _client.OnRegisterClient -= ClientOnOnRegisterClient;
            _client.OnUnregisterClient -= ClientOnOnUnregisterClient;
        }

        // 获取或创建专用客户端
        _client = ModHttpV1ClientContract.GetOrCreate(_modId);
        _client.OnRegisterClient -= ClientOnOnRegisterClient;
        _client.OnUnregisterClient -= ClientOnOnUnregisterClient;
        _client.OnRegisterClient += ClientOnOnRegisterClient;
        _client.OnUnregisterClient += ClientOnOnUnregisterClient;

        // 连接并包装 handler，只处理 contentType 为 "cli" 的消息
        await _client.Connect(async (fromModId, contentType, body) =>
        {
            // 只处理 CLI 类型的消息
            if (contentType == ContentTypeCli)
            {
                await handler(fromModId == TerminalUIModId, fromModId, body, ShowToTerminal);
            }
            else
            {
                Log.Warn(
                    $"ModTerminalClientContract received non-cli message from {fromModId}, contentType: {contentType}");
            }
        });
    }

    private UniTask ClientOnOnUnregisterClient(string arg)
    {
        return SendToTerminalUI(TerminalUICommand.Offline(_modId));
    }

    private UniTask ClientOnOnRegisterClient(string arg)
    {
        return SendToTerminalUI(TerminalUICommand.Online(_modId));
    }

    /// <summary>
    /// show response body to terminal UI
    /// </summary>
    /// <param name="responseBody"></param>
    /// <returns></returns>
    public UniTask ShowToTerminal(string responseBody)
        => SendToTerminalUI(TerminalUICommand.Show(responseBody));

    private UniTask SendToTerminalUI(string command)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("ModTerminalClientContract is not connected. Call Connect first.");
        }

        // 固定使用 "cli" 作为 contentType
        return _client.SendTo(TerminalUIModId, ContentTypeCli, command);
    }

    /// <summary>
    /// Releases every terminal client contract of this mod copy: tells the terminal UI this mod is offline, removes the
    /// handlers, clears the instance table and destroys the container. Called before the HTTP client contracts are
    /// released. Idempotent and never throws.
    /// </summary>
    public static void Release()
    {
        List<ModTerminalClientContract> contracts;
        try
        {
            contracts = Instances.TakeAll();
        }
        catch (Exception e)
        {
            Log.Error(e, "ModTerminalClientContract failed to list its instances");
            return;
        }

        foreach (var contract in contracts)
        {
            try
            {
                contract.ReleaseClient();
            }
            catch (Exception e)
            {
                Log.Error(e, "ModTerminalClientContract failed to release {ModId}", contract._modId);
            }
        }

        try
        {
            if (_containerGameObject != null)
            {
                Destroy(_containerGameObject);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "ModTerminalClientContract failed to destroy its container");
        }

        _containerGameObject = null;
    }

    /// <summary>Sends "offline" to the terminal UI, then removes the handlers. Idempotent.</summary>
    private void ReleaseClient()
    {
        var client = _client;
        if (client == null)
        {
            return;
        }

        try
        {
            client.SendTo(TerminalUIModId, ContentTypeCli, TerminalUICommand.Offline(_modId)).Forget();
        }
        catch (Exception e)
        {
            Log.Error(e, "ModTerminalClientContract failed to send offline for {ModId}", _modId);
        }

        client.OnRegisterClient -= ClientOnOnRegisterClient;
        client.OnUnregisterClient -= ClientOnOnUnregisterClient;
        _client = null;
    }

    private void OnDestroy()
    {
        // 客户端由 ModHttpV1ClientContract 的单例管理，不需要手动清理；只移除本实例的处理器
        if (_client != null)
        {
            _client.OnRegisterClient -= ClientOnOnRegisterClient;
            _client.OnUnregisterClient -= ClientOnOnUnregisterClient;
            _client = null;
        }

        // 只移除属于本实例的单例记录，较晚的 OnDestroy 不会删除同一 modId 的新实例
        if (!string.IsNullOrEmpty(_modId) && Instances.RemoveIfOwned(_modId, this))
        {
            Log.Info("Removed ModTerminalClientContract singleton for modId: {_modId}", _modId);
        }
    }
}

public static class TerminalUICommand
{
    public static string Online(string modId) => $"online {modId}";

    public static string Offline(string modId) => $"offline {modId}";

    // Show command with JSON serialized content to prevent multiline issues
    public static string Show(string content) => $"show {JsonConvert.SerializeObject(content)}";
}

public delegate UniTask ResponseToTerminalHandler(string responseBody);

public delegate UniTask ModTerminalHandler(bool isFromTerminal,
    string fromModId,
    string body,
    ResponseToTerminalHandler responseToTerminal);
