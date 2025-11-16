using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// ModHttpV1 proxy for efficient reflection-based calls without direct dependency on the original library.
/// </summary>
public class ModHttpV1Proxy
{
    private object? _hubInstance;
    private Action<object, string, Func<string, string, string, UniTask>>? _registerClientDelegate;
    private Action<object, string>? _unregisterClientDelegate;
    private Func<object, string, string, string, string, UniTask>? _notifyDelegate;
    private Func<object, IReadOnlyList<string>>? _getModIdsDelegate;

    private readonly VirtualHub _virtualHub;
    private bool _isVirtual;
    private bool _isSearching;

    private ModHttpV1Proxy()
    {
        _virtualHub = new VirtualHub();
        _isVirtual = true;
        _isSearching = false;
    }

    private ModHttpV1Proxy(object hubInstance)
    {
        _virtualHub = new VirtualHub();
        _isVirtual = false;
        _isSearching = false;
        InitializeRealHub(hubInstance);
    }

    private void InitializeRealHub(object hubInstance)
    {
        var type = hubInstance.GetType();
        _hubInstance = hubInstance;

        // RegisterClient(string, Func<string, string, string, UniTask>)
        var regMethod = type.GetMethod("RegisterClient", BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(string), typeof(Func<string, string, string, UniTask>) }, null);
        var regInstance = Expression.Parameter(typeof(object), "instance");
        var regModId = Expression.Parameter(typeof(string), "modId");
        var regCallback = Expression.Parameter(typeof(Func<string, string, string, UniTask>), "callback");
        var regCall = Expression.Call(
            Expression.Convert(regInstance, type),
            regMethod!,
            regModId,
            regCallback
        );
        _registerClientDelegate = Expression
            .Lambda<Action<object, string, Func<string, string, string, UniTask>>>(regCall, regInstance, regModId,
                regCallback)
            .Compile();

        // UnregisterClient(string)
        var unregMethod = type.GetMethod("UnregisterClient", BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(string) }, null);
        var unregInstance = Expression.Parameter(typeof(object), "instance");
        var unregModId = Expression.Parameter(typeof(string), "modId");
        var unregCall = Expression.Call(
            Expression.Convert(unregInstance, type),
            unregMethod!,
            unregModId
        );
        _unregisterClientDelegate = Expression.Lambda<Action<object, string>>(unregCall, unregInstance, unregModId)
            .Compile();

        // Notify(string, string, string, string) : UniTask
        var notifyMethod = type.GetMethod("Notify", BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(string), typeof(string), typeof(string), typeof(string) }, null);
        var notifyInstance = Expression.Parameter(typeof(object), "instance");
        var notifyFromModId = Expression.Parameter(typeof(string), "fromModId");
        var notifyToModId = Expression.Parameter(typeof(string), "toModId");
        var notifyContentType = Expression.Parameter(typeof(string), "contentType");
        var notifyBody = Expression.Parameter(typeof(string), "body");
        var notifyCall = Expression.Call(
            Expression.Convert(notifyInstance, type),
            notifyMethod!,
            notifyFromModId,
            notifyToModId,
            notifyContentType,
            notifyBody
        );
        _notifyDelegate = Expression
            .Lambda<Func<object, string, string, string, string, UniTask>>(notifyCall, notifyInstance, notifyFromModId,
                notifyToModId, notifyContentType, notifyBody).Compile();

        // ModIds property getter (IReadOnlyList<string>)
        var modIdsProperty = type.GetProperty("ModIds", BindingFlags.Public | BindingFlags.Instance);
        var modIdsInstance = Expression.Parameter(typeof(object), "instance");
        var modIdsGet = Expression.Property(
            Expression.Convert(modIdsInstance, type),
            modIdsProperty!
        );
        _getModIdsDelegate = Expression
            .Lambda<Func<object, IReadOnlyList<string>>>(modIdsGet, modIdsInstance)
            .Compile();

        _isVirtual = false;
    }

    // 高效注册委托（表达式编译）
    public void RegisterClient(string modId, Func<string, string, string, UniTask> callback)
    {
        if (_isVirtual)
        {
            _virtualHub.RegisterClient(modId, callback);
            TryUpgradeToRealHub();
        }
        else
        {
            _registerClientDelegate!(_hubInstance!, modId, callback);
        }
    }

    public void UnregisterClient(string modId)
    {
        if (_isVirtual)
        {
            _virtualHub.UnregisterClient(modId);
        }
        else
        {
            _unregisterClientDelegate!(_hubInstance!, modId);
        }
    }

    public UniTask Notify(string fromModId, string toModId, string contentType, string body)
    {
        if (_isVirtual)
        {
            return _virtualHub.Notify(fromModId, toModId, contentType, body);
        }
        else
        {
            return _notifyDelegate!(_hubInstance!, fromModId, toModId, contentType, body);
        }
    }

    public IReadOnlyList<string> GetModIds()
    {
        if (_isVirtual)
        {
            return _virtualHub.GetModIds();
        }
        else
        {
            return _getModIdsDelegate!(_hubInstance!);
        }
    }

    private async void TryUpgradeToRealHub()
    {
        if (_isSearching || !_isVirtual) return;

        _isSearching = true;
        Log.Info("[ModHttpV1Proxy] 真实对象未挂载，启动后台检测任务 (最多60秒，每秒检测一次)");

        try
        {
            for (int i = 0; i < 60; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1));

                var go = UnityEngine.GameObject.Find("ModHttpV1");
                if (go != null)
                {
                    var components = go.GetComponents<UnityEngine.MonoBehaviour>();
                    object? hub = null;
                    foreach (var component in components)
                    {
                        if (component.GetType().Name == "ModHttpV1")
                        {
                            hub = component;
                            break;
                        }
                    }

                    if (hub != null)
                    {
                        Log.Info($"[ModHttpV1Proxy] 检测到真实对象挂载 (第{i + 1}秒)，开始迁移数据");
                        InitializeRealHub(hub);
                        await _virtualHub.MigrateToRealHub(this);
                        Log.Info("[ModHttpV1Proxy] 数据迁移完成，虚拟载体已销毁");
                        return;
                    }
                }
            }

            Log.Warn("[ModHttpV1Proxy] 60秒内未检测到真实对象，停止检测，继续使用虚拟载体");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ModHttpV1Proxy] 升级到真实Hub时发生错误");
        }
        finally
        {
            _isSearching = false;
        }
    }

    // 工厂方法：通过 GameObject 名称查找 ModHttpV1 实例，如果未找到则创建虚拟载体
    public static ModHttpV1Proxy CreateFromSingleton()
    {
        var go = UnityEngine.GameObject.Find("ModHttpV1");
        if (go != null)
        {
            // 通过反射获取 ModHttpV1 类型并获取组件
            var components = go.GetComponents<UnityEngine.MonoBehaviour>();
            object? hub = null;
            foreach (var component in components)
            {
                if (component.GetType().Name == "ModHttpV1")
                {
                    hub = component;
                    break;
                }
            }

            if (hub != null)
            {
                Log.Info("[ModHttpV1Proxy] 找到真实 ModHttpV1 对象，直接使用");
                return new ModHttpV1Proxy(hub);
            }
        }

        // 未找到真实对象，创建虚拟载体
        Log.Warn("[ModHttpV1Proxy] 未找到 ModHttpV1 对象，创建虚拟载体并启动后台检测");
        return new ModHttpV1Proxy();
    }

    /// <summary>
    /// 虚拟Hub，在真实ModHttpV1未挂载时暂存消息和注册信息
    /// </summary>
    private class VirtualHub
    {
        private const int MaxPendingMessages = 100;
        private readonly Dictionary<string, Func<string, string, string, UniTask>> _clients = new();
        private readonly Queue<PendingMessage> _pendingMessages = new();
        private int _droppedMessageCount = 0;

        public void RegisterClient(string modId, Func<string, string, string, UniTask> callback)
        {
            _clients[modId] = callback;
            Log.Debug($"[VirtualHub] 注册客户端: {modId}");
        }

        public void UnregisterClient(string modId)
        {
            _clients.Remove(modId);
            Log.Debug($"[VirtualHub] 注销客户端: {modId}");
        }

        public UniTask Notify(string fromModId, string toModId, string contentType, string body)
        {
            if (_pendingMessages.Count >= MaxPendingMessages)
            {
                _pendingMessages.Dequeue();
                _droppedMessageCount++;
                Log.Warn($"[VirtualHub] 消息队列已满，丢弃最早的消息 (已丢弃: {_droppedMessageCount})");
            }

            _pendingMessages.Enqueue(new PendingMessage
            {
                FromModId = fromModId,
                ToModId = toModId,
                ContentType = contentType,
                Body = body,
                Timestamp = DateTime.UtcNow
            });

            Log.Debug($"[VirtualHub] 暂存消息: {fromModId} -> {toModId} (队列长度: {_pendingMessages.Count})");
            return UniTask.CompletedTask;
        }

        public IReadOnlyList<string> GetModIds()
        {
            return _clients.Keys.ToList();
        }

        public async UniTask MigrateToRealHub(ModHttpV1Proxy realProxy)
        {
            Log.Info($"[VirtualHub] 开始迁移 {_clients.Count} 个客户端和 {_pendingMessages.Count} 条消息");

            // 迁移客户端注册
            foreach (var kvp in _clients)
            {
                realProxy.RegisterClient(kvp.Key, kvp.Value);
            }

            // 重放暂存的消息
            var migratedCount = 0;
            while (_pendingMessages.Count > 0)
            {
                var msg = _pendingMessages.Dequeue();
                try
                {
                    await realProxy.Notify(msg.FromModId, msg.ToModId, msg.ContentType, msg.Body);
                    migratedCount++;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[VirtualHub] 重放消息失败: {msg.FromModId} -> {msg.ToModId}");
                }
            }

            Log.Info(
                $"[VirtualHub] 迁移完成: 客户端 {_clients.Count}, 消息 {migratedCount}/{migratedCount + _droppedMessageCount} (丢弃: {_droppedMessageCount})");

            _clients.Clear();
        }

        private class PendingMessage
        {
            public string FromModId { get; set; } = string.Empty;
            public string ToModId { get; set; } = string.Empty;
            public string ContentType { get; set; } = string.Empty;
            public string Body { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
    }
}
