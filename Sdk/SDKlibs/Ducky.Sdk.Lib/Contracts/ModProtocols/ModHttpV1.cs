using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.Contracts.ModProtocols;

/// <summary>
/// MessageHub Host v1 - Integrated into SDK core for inter-mod communication
/// </summary>
internal class ModHttpV1 : MonoBehaviour
{
    public const string HubGameObjectName = "ModHttpV1";
    internal static ModHttpV1? Instance { get; set; }

    // 存储：mod id (string) → 委托（Func<fromModId, contentType, body, UniTask>），并发字典实现无锁访问
    private readonly ConcurrentDictionary<string, Func<string, string, string, UniTask>> _eventMap = new();

    // 消息暂存区：mod id (string) → 消息队列
    private readonly ConcurrentDictionary<string, ConcurrentQueue<MessageItem>> _messageQueues = new();

    // 每个 modId 的处理协程取消令牌
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _processingCts = new();

    private const int MaxQueueSize = 200;

    // 默认 TTL：60 秒
    private const int DefaultTtlSeconds = 60;

    public IReadOnlyList<string> ModIds => _eventMap.Keys.ToList();

    /// <summary>
    /// 消息项
    /// </summary>
    private record MessageItem(string FromModId, string ContentType, string Body, DateTime Timestamp, int TtlSeconds = DefaultTtlSeconds)
    {
        /// <summary>
        /// 检查消息是否已过期
        /// </summary>
        public bool IsExpired => DateTime.UtcNow > Timestamp.AddSeconds(TtlSeconds);
    }

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
        Ducky.Sdk.Logging.Log.Info("ModHttpV1 Active() called.");
    }

    #region 公共方法（供主程序直接调用、插件反射调用）

    /// <summary>
    /// 注册客户端（按 mod id 绑定逻辑，客户端注册）
    /// </summary>
    /// <param name="modId">mod 的唯一字符串 id</param>
    /// <param name="callback">异步回调委托，参数为 (fromModId, contentType, body)，返回 UniTask</param>
    public void RegisterClient(string modId, Func<string, string, string, UniTask> callback)
    {
        Log.Info($"ModHttpV1: RegisterClient called for modId: {modId}");
        _eventMap[modId] = callback;

        // 确保消息队列存在
        _messageQueues.TryAdd(modId, new ConcurrentQueue<MessageItem>());

        // 启动消息处理协程
        StartMessageProcessing(modId);
    }

    /// <summary>
    /// 注销客户端（按 mod id 注销，避免内存泄漏）
    /// </summary>
    public void UnregisterClient(string modId)
    {
        Log.Info($"ModHttpV1: UnregisterClient called for modId: {modId}");
        _eventMap.TryRemove(modId, out _);

        // 停止消息处理协程
        if (_processingCts.TryRemove(modId, out var cts))
        {
            cts?.Cancel();
            cts?.Dispose();
        }

        // 清空消息队列
        _messageQueues.TryRemove(modId, out _);
    }

    /// <summary>
    /// 异步通知消息（string 类型）
    /// </summary>
    /// <param name="fromModId">发送者的 mod id</param>
    /// <param name="toModId">接收者的 mod id</param>
    /// <param name="contentType">内容类型</param>
    /// <param name="body">消息内容</param>
    public async UniTask Notify(string fromModId, string toModId, string contentType, string body)
    {
        // 确保消息队列存在
        var queue = _messageQueues.GetOrAdd(toModId, _ => new ConcurrentQueue<MessageItem>());

        // 检查队列大小，如果超过限制，移除最旧的消息
        if (queue.Count >= MaxQueueSize)
        {
            queue.TryDequeue(out _);
            Log.Warn($"ModHttpV1: Message queue for {toModId} is full, removing oldest message");
        }

        // 将消息添加到队列，包含当前时间戳
        var messageItem = new MessageItem(fromModId, contentType, body, DateTime.UtcNow);
        queue.Enqueue(messageItem);
        Log.Debug($"ModHttpV1: Notify - Message enqueued from {fromModId} to {toModId}: {body}");

        // 如果还没有启动处理协程，现在启动
        if (!_processingCts.ContainsKey(toModId))
        {
            StartMessageProcessing(toModId);
        }

        await UniTask.CompletedTask;
    }

    #endregion

    #region 消息队列处理

    /// <summary>
    /// 启动消息处理协程
    /// </summary>
    private void StartMessageProcessing(string modId)
    {
        // 如果已经在处理，不要重复启动
        if (_processingCts.ContainsKey(modId))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        if (_processingCts.TryAdd(modId, cts))
        {
            Log.Info($"ModHttpV1: Starting message processing for modId: {modId}");
            ProcessMessageQueueAsync(modId, cts.Token).Forget();
            CleanupExpiredMessagesAsync(modId, cts.Token).Forget();
        }
        else
        {
            cts.Dispose();
        }
    }

    /// <summary>
    /// 处理消息队列的异步协程
    /// </summary>
    private async UniTaskVoid ProcessMessageQueueAsync(string modId, CancellationToken ct)
    {
        Log.Info($"ModHttpV1: Message processing started for modId: {modId}");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 检查是否有消息队列
                if (!_messageQueues.TryGetValue(modId, out var queue))
                {
                    await UniTask.Delay(100, cancellationToken: ct);
                    continue;
                }

                // 先检查是否有注册的处理器
                if (!_eventMap.TryGetValue(modId, out var callback))
                {
                    // 没有处理器，等待后继续检查，不要取出消息以保持顺序
                    // 过期消息将由 CleanupExpiredMessagesAsync 定期清理
                    Log.Debug($"ModHttpV1: No handler for {modId}, waiting...");
                    await UniTask.Delay(500, cancellationToken: ct);
                    continue;
                }

                // 有处理器，尝试取出消息
                if (queue.TryDequeue(out var messageItem))
                {
                    try
                    {
                        // 检查消息是否已过期
                        if (messageItem.IsExpired)
                        {
                            Log.Debug($"ModHttpV1: Message expired for {modId} from {messageItem.FromModId}, discarding");
                            continue;
                        }

                        Log.Debug(
                            $"ModHttpV1: Processing message for {modId} from {messageItem.FromModId}: {messageItem.Body}");
                        await callback.Invoke(messageItem.FromModId, messageItem.ContentType, messageItem.Body);
                        Log.Debug($"ModHttpV1: Message processed successfully for {modId}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"ModHttpV1: Error processing message for {modId}: {ex}");
                    }
                }
                else
                {
                    // 队列为空，等待一段时间
                    await UniTask.Delay(100, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info($"ModHttpV1: Message processing cancelled for modId: {modId}");
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"ModHttpV1: Unexpected error in message processing for {modId}: {ex}");
                await UniTask.Delay(1000, cancellationToken: ct);
            }
        }

        Log.Info($"ModHttpV1: Message processing stopped for modId: {modId}");
    }

    /// <summary>
    /// 定期清理过期消息的异步协程
    /// </summary>
    private async UniTaskVoid CleanupExpiredMessagesAsync(string modId, CancellationToken ct)
    {
        Log.Info($"ModHttpV1: Cleanup task started for modId: {modId}");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 每30秒清理一次过期消息
                await UniTask.Delay(TimeSpan.FromSeconds(30), cancellationToken: ct);

                if (!_messageQueues.TryGetValue(modId, out var queue))
                {
                    continue;
                }

                // 使用临时队列来存储有效消息，保持顺序
                var tempQueue = new ConcurrentQueue<MessageItem>();
                var expiredCount = 0;

                // 遍历队列中的所有消息
                while (queue.TryDequeue(out var messageItem))
                {
                    if (messageItem.IsExpired)
                    {
                        expiredCount++;
                        continue;
                    }

                    // 将有效消息加入临时队列
                    tempQueue.Enqueue(messageItem);
                }

                // 将有效消息重新放回原队列，保持原有顺序
                while (tempQueue.TryDequeue(out var validMessage))
                {
                    queue.Enqueue(validMessage);
                }

                if (expiredCount > 0)
                {
                    Log.Debug($"ModHttpV1: Cleaned up {expiredCount} expired messages for {modId}");
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info($"ModHttpV1: Cleanup task cancelled for modId: {modId}");
                break;
            }
            catch (Exception ex)
            {
                Log.Error($"ModHttpV1: Error in cleanup task for {modId}: {ex}");
            }
        }

        Log.Info($"ModHttpV1: Cleanup task stopped for modId: {modId}");
    }

    #endregion
}