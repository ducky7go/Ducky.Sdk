using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// mod HTTP V1 client contract for mod-to-mod communication.
/// </summary>
public class ModHttpV1ClientContract : MonoBehaviour
{
    private static readonly Dictionary<string, ModHttpV1ClientContract> Instances = new();
    private static GameObject? _containerGameObject;

    private ModHttpV1Proxy? _proxy;
    private string _modId = string.Empty;

    /// <summary>
    /// 注册客户端时触发的事件
    /// </summary>
    public event Func<string, UniTask>? OnRegisterClient;

    /// <summary>
    /// 注销客户端时触发的事件
    /// </summary>
    public event Func<string, UniTask>? OnUnregisterClient;

    /// <summary>
    /// 获取或创建基于 modId 的单例实例
    /// </summary>
    /// <param name="modId">mod 的唯一标识符</param>
    /// <returns>ModHttpV1ClientContract 实例</returns>
    public static ModHttpV1ClientContract GetOrCreate(string modId)
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
            _containerGameObject = new GameObject("ModHttpV1ClientContracts");
            DontDestroyOnLoad(_containerGameObject);
        }

        // 创建新实例
        var go = new GameObject($"ModHttpV1Client_{modId}");
        go.transform.SetParent(_containerGameObject.transform);
        var contract = go.AddComponent<ModHttpV1ClientContract>();
        contract.Setup(modId);

        // 注册到字典
        Instances[modId] = contract;

        Log.Info($"Created ModHttpV1ClientContract singleton for modId: {modId}");
        return contract;
    }

    private void Setup(string modId)
    {
        _modId = modId;
    }

    /// <summary>
    /// connect to mod bus and setup handler for incoming messages
    /// </summary>
    /// <param name="handler"></param>
    public async UniTask Connect(Func<string, string, string, UniTask> handler)
    {
        if (_proxy != null)
        {
            _proxy.UnregisterClient(_modId);
            if (OnUnregisterClient != null)
            {
                await OnUnregisterClient.Invoke(_modId);
            }
        }

        _proxy = ModHttpV1Proxy.CreateFromSingleton();
        _proxy.RegisterClient(_modId, Callback);
        if (OnRegisterClient != null)
        {
            await OnRegisterClient.Invoke(_modId);
        }

        return;

        UniTask Callback(string fromModId, string contentType, string body) =>
            handler(fromModId, contentType, body);
    }

    private void OnDestroy()
    {
        _proxy?.UnregisterClient(_modId);

        // 从单例字典中移除
        if (!string.IsNullOrEmpty(_modId))
        {
            Instances.Remove(_modId);
            Log.Info("Removed ModHttpV1ClientContract singleton for modId: {_modId}", _modId);
        }
    }

    /// <summary>
    /// send message to another mod
    /// </summary>
    /// <param name="modId"></param>
    /// <param name="contentType"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public UniTask SendTo(string modId, string contentType, string body)
    {
        if (_proxy == null)
        {
            throw new InvalidOperationException(
                "ModHttpV1ClientContract is not connected. Call ConnectToModBus first.");
        }

        return _proxy.Notify(_modId, modId, contentType, body);
    }
}
