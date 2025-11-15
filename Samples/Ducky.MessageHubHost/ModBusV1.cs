using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;
using Ducky.Sdk.Logging;

// 主程序专属脚本，编译在主程序 DLL 中
// ModBus v1
namespace Ducky.MessageHubHost;

/// <summary>
/// ModBus v1
/// </summary>
internal class ModBusV1 : MonoBehaviour
{
    public const string HubGameObjectName = "ModBusV1";
    internal static ModBusV1 Instance { get; set; }

    // 存储：mod id (string) → 委托（Func<fromModId, message, UniTask>），并发字典实现无锁访问
    private readonly ConcurrentDictionary<string, Func<string, string, UniTask>> _eventMap = new();

    private void Awake()
    {
        // 单例初始化，确保全局唯一
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.name = HubGameObjectName;
        DontDestroyOnLoad(gameObject);
    }

    public void Active()
    {
        Ducky.Sdk.Logging.Log.Info("ModBusV1 Active() called.");
    }

    #region 公共方法（供主程序直接调用、插件反射调用）

    /// <summary>
    /// 注册客户端（按 mod id 绑定逻辑，客户端注册）
    /// </summary>
    /// <param name="modId">mod 的唯一字符串 id</param>
    /// <param name="callback">异步回调委托，参数为 (fromModId, message)，返回 UniTask</param>
    public void RegisterClient(string modId, Func<string, string, UniTask> callback)
    {
        Log.Info($"ModBusV1: RegisterClient called for modId: {modId}");
        _eventMap[modId] = callback;
    }

    /// <summary>
    /// 注销客户端（按 mod id 注销，避免内存泄漏）
    /// </summary>
    public void UnregisterClient(string modId)
    {
        Log.Info($"ModBusV1: UnregisterClient called for modId: {modId}");
        _eventMap.TryRemove(modId, out _);
    }

    /// <summary>
    /// 异步通知消息（string 类型）
    /// </summary>
    /// <param name="fromModId">发送者的 mod id</param>
    /// <param name="toModId">接收者的 mod id</param>
    /// <param name="message">消息内容</param>
    public async UniTask Notify(string fromModId, string toModId, string message)
    {
        if (!_eventMap.TryGetValue(toModId, out var callback))
        {
            Log.Warn($"ModBusV1: Notify - No registered client for modId: {toModId}");
            return;
        }

        try
        {
            Log.Debug($"ModBusV1: Notify - Sending message from {fromModId} to {toModId}: {message}");
            await callback.Invoke(fromModId, message);
            Log.Debug($"ModBusV1: Notify - Message sent successfully from {fromModId} to {toModId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Notify 异常: {ex}");
        }
    }

    #endregion
}
