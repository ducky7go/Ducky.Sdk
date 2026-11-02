using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.ModProtocols;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// mod HTTP V1 client contract for mod-to-mod communication.
/// </summary>
public class ModHttpV1ClientContract : MonoBehaviour
{
    private static readonly ContractInstanceTable<ModHttpV1ClientContract> Instances = new();
    private static GameObject? _containerGameObject;

    // Follows the live hub; a virtual proxy holds messages sent before any hub exists.
    private HubBinding? _binding;
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

        // 已存在且仍然有效的实例直接返回；否则创建新实例并记录
        return Instances.GetOrAdd(modId, CreateContract, instance => instance != null);
    }

    private static ModHttpV1ClientContract CreateContract(string modId)
    {
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

        Log.Info($"Created ModHttpV1ClientContract singleton for modId: {modId}");
        return contract;
    }

    /// <summary>
    /// Releases every client contract of this mod copy: unregisters their hub clients, clears the instance table and
    /// destroys their objects. Called from the shared teardown of <c>ModBehaviourBase</c>. Idempotent and never throws.
    /// </summary>
    public static void Release()
    {
        List<ModHttpV1ClientContract> contracts;
        try
        {
            contracts = Instances.TakeAll();
        }
        catch (Exception e)
        {
            Log.Error(e, "ModHttpV1ClientContract failed to list its instances");
            return;
        }

        foreach (var contract in contracts)
        {
            try
            {
                contract.ReleaseBinding();
            }
            catch (Exception e)
            {
                Log.Error(e, "ModHttpV1ClientContract failed to release {ModId}", contract._modId);
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
            Log.Error(e, "ModHttpV1ClientContract failed to destroy its container");
        }

        _containerGameObject = null;
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
        if (_binding != null)
        {
            _binding.Unregister(_modId);
            if (OnUnregisterClient != null)
            {
                await OnUnregisterClient.Invoke(_modId);
            }
        }

        var binding = _binding ??= HubBinding.ForSingleton(virtualFallback: true);
        binding.Register(_modId, Callback);
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
        ReleaseBinding();

        // A late OnDestroy of an older instance must not remove a newer instance of the same mod id.
        if (!string.IsNullOrEmpty(_modId) && Instances.RemoveIfOwned(_modId, this))
        {
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
        if (_binding == null)
        {
            throw new InvalidOperationException(
                "ModHttpV1ClientContract is not connected. Call ConnectToModBus first.");
        }

        return _binding.Notify(_modId, modId, contentType, body);
    }

    /// <summary>Unregisters this contract's hub client and drops its binding. Idempotent.</summary>
    private void ReleaseBinding()
    {
        var binding = _binding;
        _binding = null;
        binding?.Release();
    }
}
