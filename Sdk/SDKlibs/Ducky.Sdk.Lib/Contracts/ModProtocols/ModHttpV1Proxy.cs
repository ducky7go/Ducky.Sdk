using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Duckov.Utilities;
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

    // 队列检查机制相关字段
    private static readonly ConcurrentQueue<CheckRequest> s_checkQueue = new();
    private static readonly ConcurrentBag<WeakReference<ModHttpV1Proxy>> s_virtualProxies = new();
    private static CancellationTokenSource? s_checkProcessorCts;
    private static bool s_checkProcessorRunning = false;

    private ModHttpV1Proxy()
    {
        _virtualHub = new VirtualHub();
        _isVirtual = true;
        _isSearching = false;

        // 注册虚拟代理实例
        s_virtualProxies.Add(new WeakReference<ModHttpV1Proxy>(this));

        // 启动场景监控（如果还未启动）
        StartSceneMonitoringIfNeeded();
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

    /// <summary>
    /// 检查请求类型
    /// </summary>
    public enum CheckRequestType
    {
        /// <summary>
        /// 进入base时的立即检查（失败则终止）
        /// </summary>
        EnterBase,
        /// <summary>
        /// 定时检查（5秒间隔，最多3600秒）
        /// </summary>
        Periodic
    }

    /// <summary>
    /// 检查请求
    /// </summary>
    public class CheckRequest
    {
        public CheckRequestType Type { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public ModHttpV1Proxy Requester { get; set; } = null!;
        public int MaxAttempts { get; set; }
        public int AttemptCount { get; set; }
        public TimeSpan CheckInterval { get; set; }
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

    private void TryUpgradeToRealHub()
    {
        if (_isSearching || !_isVirtual) return;

        _isSearching = true;
        Log.Info("[ModHttpV1Proxy] 真实对象未挂载，启动基于队列的后台检测任务");

        // 添加定时检查请求：5秒间隔，最多720次（3600秒）
        var periodicRequest = new CheckRequest
        {
            Type = CheckRequestType.Periodic,
            Requester = this,
            MaxAttempts = 720, // 3600秒 / 5秒 = 720次
            CheckInterval = TimeSpan.FromSeconds(5)
        };

        EnqueueCheckRequest(periodicRequest);
        StartCheckProcessorIfNeeded();
    }

    /// <summary>
    /// 启动场景监控（检测进入base）
    /// </summary>
    private static void StartSceneMonitoringIfNeeded()
    {
        // 注册场景加载事件监听（直接访问，无需反射）
        try
        {
            // 直接注册场景加载事件
            SceneLoader.onAfterSceneInitialize += OnSceneLoad;
            Log.Info("[ModHttpV1Proxy] 场景监控已启动，将监听base场景进入事件");
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "[ModHttpV1Proxy] 无法注册场景监控，将仅依赖定时检查");
            Log.Info("[ModHttpV1Proxy] 场景监控启动完成（仅定时检查模式）");
        }
    }

    /// <summary>
    /// 场景加载事件处理
    /// </summary>
    private static void OnSceneLoad(SceneLoadingContext context)
    {
        try
        {
            Log.Debug($"[ModHttpV1Proxy] 检测到场景加载: {context.sceneName}");

            // 检查是否是base场景（参考ModBehaviour中的逻辑）
            if (IsBaseScene(context.sceneName))
            {
                Log.Info("[ModHttpV1Proxy] 检测到进入base场景，触发立即检查");

                // 为所有虚拟代理实例触发进入base检查
                TriggerEnterBaseCheckForAllVirtualProxies();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ModHttpV1Proxy] 处理场景加载事件时发生异常");
        }
    }

    /// <summary>
    /// 检查是否是base场景（参考ModBehaviour中的逻辑）
    /// </summary>
    private static bool IsBaseScene(string sceneName)
    {
        // 使用常见的base场景名称进行判断
        return sceneName == GameplayDataSettings.SceneManagement.BaseScene.Name;
    }

    /// <summary>
    /// 为所有虚拟代理实例触发进入base检查
    /// </summary>
    private static void TriggerEnterBaseCheckForAllVirtualProxies()
    {
        var checkedCount = 0;

        // 遍历所有注册的虚拟代理实例
        foreach (var weakRef in s_virtualProxies)
        {
            if (weakRef.TryGetTarget(out var proxy))
            {
                // 只对仍在搜索且为虚拟状态的代理触发检查
                if (proxy._isVirtual && !proxy._isSearching)
                {
                    var enterBaseRequest = new CheckRequest
                    {
                        Type = CheckRequestType.EnterBase,
                        Requester = proxy,
                        MaxAttempts = 1,
                        CheckInterval = TimeSpan.Zero
                    };

                    EnqueueCheckRequest(enterBaseRequest);
                    checkedCount++;
                }
            }
        }

        // 清理失效的弱引用
        CleanUpWeakReferences();

        if (checkedCount > 0)
        {
            StartCheckProcessorIfNeeded();
            Log.Info($"[ModHttpV1Proxy] 已为 {checkedCount} 个虚拟代理实例触发进入base检查");
        }
    }

    /// <summary>
    /// 清理失效的弱引用
    /// </summary>
    private static void CleanUpWeakReferences()
    {
        // 由于ConcurrentBag不支持删除操作，我们创建一个新的bag来存储有效的引用
        var validRefs = new List<WeakReference<ModHttpV1Proxy>>();

        foreach (var weakRef in s_virtualProxies)
        {
            if (weakRef.TryGetTarget(out _))
            {
                validRefs.Add(weakRef);
            }
        }

        // 清空原bag并重新添加有效引用
        while (s_virtualProxies.TryTake(out _))
        {
            // 空操作，只是为了清空
        }

        foreach (var validRef in validRefs)
        {
            s_virtualProxies.Add(validRef);
        }
    }

    /// <summary>
    /// 将检查请求加入队列
    /// </summary>
    private static void EnqueueCheckRequest(CheckRequest request)
    {
        s_checkQueue.Enqueue(request);
        Log.Debug($"[ModHttpV1Proxy] 检查请求已入队: {request.Type}, 请求者: {request.Requester.GetHashCode()}");
    }

    /// <summary>
    /// 启动检查处理器（如果尚未运行）
    /// </summary>
    private static void StartCheckProcessorIfNeeded()
    {
        if (s_checkProcessorRunning) return;

        lock (s_checkQueue)
        {
            if (s_checkProcessorRunning) return;

            s_checkProcessorRunning = true;
            s_checkProcessorCts = new CancellationTokenSource();

            // 启动后台检查处理器
            UniTask.RunOnThreadPool(() => ProcessCheckQueue(s_checkProcessorCts.Token),
                cancellationToken: s_checkProcessorCts.Token);

            Log.Info("[ModHttpV1Proxy] 检查处理器已启动");
        }
    }

    /// <summary>
    /// 处理检查队列
    /// </summary>
    private static async UniTask ProcessCheckQueue(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !s_checkQueue.IsEmpty)
            {
                if (s_checkQueue.TryDequeue(out var request))
                {
                    await ProcessCheckRequest(request, cancellationToken);
                }
                else
                {
                    // 队列为空，短暂等待
                    await UniTask.Delay(1000, cancellationToken: cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Info("[ModHttpV1Proxy] 检查处理器已取消");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[ModHttpV1Proxy] 检查处理器发生异常");
        }
        finally
        {
            lock (s_checkQueue)
            {
                s_checkProcessorRunning = false;
            }
            Log.Info("[ModHttpV1Proxy] 检查处理器已停止");
        }
    }

    /// <summary>
    /// 处理单个检查请求
    /// </summary>
    private static async UniTask ProcessCheckRequest(CheckRequest request, CancellationToken cancellationToken)
    {
        Log.Debug($"[ModHttpV1Proxy] 处理检查请求: {request.Type}, 尝试次数: {request.AttemptCount + 1}/{request.MaxAttempts}");

        request.AttemptCount++;

        try
        {
            // 执行实际的ModHttpV1对象检查
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
                    // 找到真实对象，执行升级
                    Log.Info($"[ModHttpV1Proxy] 检测到真实对象挂载 (请求类型: {request.Type}, 尝试次数: {request.AttemptCount})，开始迁移数据");
                    request.Requester.InitializeRealHub(hub);
                    await request.Requester._virtualHub.MigrateToRealHub(request.Requester);
                    Log.Info("[ModHttpV1Proxy] 数据迁移完成，虚拟载体已销毁");
                    request.Requester._isSearching = false;
                    return;
                }
            }

            // 检查失败，处理重试逻辑
            if (request.AttemptCount >= request.MaxAttempts)
            {
                // 达到最大尝试次数
                if (request.Type == CheckRequestType.EnterBase)
                {
                    Log.Error("[ModHttpV1Proxy] 进入base检查失败，终止检查");
                    request.Requester._isSearching = false;
                }
                else
                {
                    Log.Warn($"[ModHttpV1Proxy] {request.Type} 检查达到最大尝试次数 ({request.MaxAttempts})，继续使用虚拟载体");
                    request.Requester._isSearching = false;
                }
            }
            else
            {
                // 需要继续检查，重新入队
                if (request.CheckInterval > TimeSpan.Zero)
                {
                    await UniTask.Delay(request.CheckInterval, cancellationToken: cancellationToken);
                }

                // 重新入队继续检查
                EnqueueCheckRequest(request);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[ModHttpV1Proxy] 处理检查请求时发生异常: {request.Type}");
            request.Requester._isSearching = false;
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
