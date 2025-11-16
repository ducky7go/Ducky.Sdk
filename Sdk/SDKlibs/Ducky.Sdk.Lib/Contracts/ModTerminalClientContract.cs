using System;
using System.Collections.Generic;
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

    private static readonly Dictionary<string, ModTerminalClientContract> Instances = new();
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

        // 如果已存在该 modId 的实例，直接返回
        if (Instances.TryGetValue(modId, out var existingInstance) && existingInstance != null)
        {
            return existingInstance;
        }

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

        // 注册到字典
        Instances[modId] = contract;

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
        // 获取或创建专用客户端
        _client = ModHttpV1ClientContract.GetOrCreate(_modId);
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

    private void OnDestroy()
    {
        // 客户端由 ModHttpV1ClientContract 的单例管理，不需要手动清理
        _client = null;

        // 从单例字典中移除
        if (!string.IsNullOrEmpty(_modId))
        {
            Instances.Remove(_modId);
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
