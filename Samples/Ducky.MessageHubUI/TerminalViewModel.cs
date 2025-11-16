using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Duckov.Modding;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Logging;
using R3;
using R3.Collections;
using UnityEngine;

namespace Ducky.MessageHubUI;

/// <summary>
/// 终端视图的响应式 ViewModel
/// </summary>
internal class TerminalViewModel : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    // ===== 可观察属性 =====

    /// <summary>
    /// 终端标题
    /// </summary>
    public ReactiveProperty<string> Title { get; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public ReactiveProperty<bool> IsVisible { get; }

    /// <summary>
    /// 输入框文本
    /// </summary>
    public ReactiveProperty<string> InputText { get; }

    /// <summary>
    /// 当前选中的 Provider 索引
    /// </summary>
    public ReactiveProperty<int> SelectedProviderIndex { get; }

    /// <summary>
    /// 当前选中 Provider 的显示标签（用于按钮展示）
    /// </summary>
    public ReactiveProperty<string> SelectedProviderLabel { get; }

    /// <summary>
    /// 命令 Provider 列表
    /// </summary>
    public ReactiveProperty<List<CommandProvider>> CommandProviders { get; }

    /// <summary>
    /// 过滤后的 Provider 列表
    /// </summary>
    public ReactiveProperty<List<CommandProvider>> FilteredProviders { get; }

    /// <summary>
    /// 是否显示 Provider 选择面板
    /// </summary>
    public ReactiveProperty<bool> ShowProviderPanel { get; }

    /// <summary>
    /// 请求重新聚焦输入框的事件流
    /// </summary>
    public Subject<Unit> RequestInputFocus { get; }

    /// <summary>
    /// 消息添加事件流
    /// </summary>
    public Subject<TerminalMessage> MessageAdded { get; }

    /// <summary>
    /// 消息清空事件流
    /// </summary>
    public Subject<Unit> MessagesCleared { get; }

    /// <summary>
    /// 终端消息列表（内部存储）
    /// </summary>
    private readonly List<TerminalMessage> _messages = new();


    // ===== 命令 (ReactiveCommand) =====

    /// <summary>
    /// 显示终端命令
    /// </summary>
    public ReactiveCommand<Unit> ShowCommand { get; }

    /// <summary>
    /// 隐藏终端命令
    /// </summary>
    public ReactiveCommand<Unit> HideCommand { get; }

    /// <summary>
    /// 发送消息命令
    /// </summary>
    public ReactiveCommand<Unit> SendMessageCommand { get; }

    /// <summary>
    /// 清空消息命令
    /// </summary>
    public ReactiveCommand<Unit> ClearMessagesCommand { get; }

    public TerminalViewModel()
    {
        Log.Info("[TerminalViewModel] Initializing...");

        // 初始化属性
        Title = new ReactiveProperty<string>("Terminal Console").AddTo(_disposables);
        IsVisible = new ReactiveProperty<bool>(false).AddTo(_disposables);
        InputText = new ReactiveProperty<string>(string.Empty).AddTo(_disposables);
        SelectedProviderIndex = new ReactiveProperty<int>(0).AddTo(_disposables);
        SelectedProviderLabel = new ReactiveProperty<string>("Select Provider").AddTo(_disposables);

        // 初始化 CommandProviders（空列表，稍后通过 BuildProviders 填充）
        CommandProviders = new ReactiveProperty<List<CommandProvider>>(new List<CommandProvider>())
            .AddTo(_disposables);

        // 初始化过滤后的 Provider 列表
        FilteredProviders = new ReactiveProperty<List<CommandProvider>>(new List<CommandProvider>())
            .AddTo(_disposables);

        // 初始化 Provider 面板显示状态
        ShowProviderPanel = new ReactiveProperty<bool>(false).AddTo(_disposables);

        // 初始化命令 - R3 的 ReactiveCommand 不需要构造参数
        ShowCommand = new ReactiveCommand<Unit>();
        HideCommand = new ReactiveCommand<Unit>();
        SendMessageCommand = new ReactiveCommand<Unit>();
        ClearMessagesCommand = new ReactiveCommand<Unit>();

        // 订阅命令
        ShowCommand.Subscribe(_ => OnShow()).AddTo(_disposables);
        HideCommand.Subscribe(_ => OnHide()).AddTo(_disposables);
        SendMessageCommand.Subscribe(_ => OnSendMessage()).AddTo(_disposables);
        ClearMessagesCommand.Subscribe(_ => OnClearMessages()).AddTo(_disposables);

        // 监听属性变化
        IsVisible.Subscribe(visible => { Log.Info($"[TerminalViewModel] IsVisible changed to: {visible}"); })
            .AddTo(_disposables);

        SelectedProviderIndex.Subscribe(index =>
        {
            Log.Info($"[TerminalViewModel] SelectedProviderIndex changed to: {index}");
            UpdateSelectedProviderLabel();
        }).AddTo(_disposables);

        // 监听输入框变化，处理 # 开头的过滤逻辑（仅用于选择，不触发发送）
        InputText.Subscribe(text =>
        {
            if (string.IsNullOrEmpty(text))
            {
                ShowProviderPanel.Value = false;
                FilteredProviders.Value = new List<CommandProvider>();
                return;
            }

            // 检查是否以 # 开头（进入 Provider 过滤/选择模式）
            if (text.StartsWith("#"))
            {
                // 提取过滤关键字（去掉第一个 #）
                var filterText = text.Substring(1).ToLowerInvariant();

                // 过滤 providers
                var filtered = CommandProviders.Value
                    .Where(p => p.ProviderId.ToLowerInvariant().Contains(filterText))
                    .ToList();

                FilteredProviders.Value = filtered;

                // 当只有一项匹配时，自动选择该 Provider（后续在选择逻辑里触发 API 发送帮助，而不是在输入逻辑里发送）
                if (filtered.Count == 1 && !string.IsNullOrEmpty(filterText))
                {
                    // 将选择操作推迟到下一帧，避免在同一订阅回调中同步修改输入文本造成的递归
                    UniTask.Void(async () =>
                    {
                        await UniTask.NextFrame();
                        SelectFilteredProvider(0);
                        ShowProviderPanel.Value = false;
                    });
                }
                else
                {
                    // 仅控制面板显示
                    ShowProviderPanel.Value = filtered.Count > 0;
                }
            }
            else
            {
                ShowProviderPanel.Value = false;
                FilteredProviders.Value = new List<CommandProvider>();
            }
        }).AddTo(_disposables);

        // 初始化事件流
        RequestInputFocus = new Subject<Unit>().AddTo(_disposables);
        MessageAdded = new Subject<TerminalMessage>().AddTo(_disposables);
        MessagesCleared = new Subject<Unit>().AddTo(_disposables);

        // 订阅 TerminalUIProtocol 的在线 ModId 列表变化，自动重建 Provider 列表
        TerminalUIProtocol.Instance.OnlineModIds
            .Subscribe(_ =>
            {
                Log.Info("[TerminalViewModel] OnlineModIds changed, rebuilding providers...");
                BuildProviders();
            })
            .AddTo(_disposables);

        Log.Info("[TerminalViewModel] Initialized successfully");
    }

    private void OnShow()
    {
        Log.Info("[TerminalViewModel] Show command executed");
        if (!IsVisible.Value)
        {
            IsVisible.Value = true;
            AddSystemMessage("Terminal opened");
        }
    }

    private void OnHide()
    {
        Log.Info("[TerminalViewModel] Hide command executed");
        if (IsVisible.Value)
        {
            IsVisible.Value = false;
            AddSystemMessage("Terminal closed");
        }
    }

    private void OnSendMessage()
    {
        var message = InputText.Value;
        if (string.IsNullOrWhiteSpace(message))
            return;

        Log.Info($"[TerminalViewModel] Sending message: {message}");

        // 解析消息，提取 providerId 和实际消息内容
        string? targetModId = null;
        var actualMessage = message;

        // '#' 模式仅用于选择，不发送
        if (!message.StartsWith("#"))
        {
            // 使用当前选中的 provider
            var selectedProvider =
                CommandProviders.Value.Count > 0 && SelectedProviderIndex.Value < CommandProviders.Value.Count
                    ? CommandProviders.Value[SelectedProviderIndex.Value]
                    : null;

            if (selectedProvider != null)
            {
                targetModId = selectedProvider.ModId;
            }
        }

        // 创建显示消息
        var displayText = targetModId != null
            ? $"[{CommandProviders.Value.FirstOrDefault(p => p.ModId == targetModId)?.ProviderId ?? "?"}] {actualMessage}"
            : actualMessage;

        var terminalMessage = new TerminalMessage
        {
            Text = displayText,
            Type = MessageType.Command,
            Timestamp = DateTime.Now
        };

        _messages.Add(terminalMessage);
        MessageAdded.OnNext(terminalMessage);

        // 如果有目标 modId，通过 ModBusTerminal 发送消息
        if (targetModId != null)
        {
            TerminalUIProtocol.Instance.SendCommand(targetModId, actualMessage).Forget();
            Log.Info($"[TerminalViewModel] Message sent to {targetModId}: {actualMessage}");
        }
        else
        {
            Log.Warn("[TerminalViewModel] No target modId, message not sent via ModBusTerminal");
        }

        // 清空输入框
        InputText.Value = string.Empty;

        // 请求重新聚焦输入框
        RequestInputFocus.OnNext(Unit.Default);
    }

    private void OnClearMessages()
    {
        Log.Info("[TerminalViewModel] Clearing all messages");
        _messages.Clear();
        MessagesCleared.OnNext(Unit.Default);
        AddSystemMessage("Messages cleared");
    }

    /// <summary>
    /// 规范化换行符：将 \r\n 和 \r 统一替换为 \n
    /// 确保跨平台的换行符兼容性（Windows、Linux、macOS）
    /// </summary>
    private string NormalizeLineEndings(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// 添加系统消息
    /// </summary>
    public void AddSystemMessage(string text)
    {
        var message = new TerminalMessage
        {
            Text = NormalizeLineEndings(text),
            Type = MessageType.System,
            Timestamp = DateTime.Now
        };
        _messages.Add(message);
        MessageAdded.OnNext(message);
    }

    /// <summary>
    /// 添加普通消息
    /// </summary>
    public void AddMessage(string text, MessageType type = MessageType.Info)
    {
        var message = new TerminalMessage
        {
            Text = NormalizeLineEndings(text),
            Type = type,
            Timestamp = DateTime.Now
        };
        _messages.Add(message);
        MessageAdded.OnNext(message);
    }

    /// <summary>
    /// 获取所有消息（只读）
    /// </summary>
    public IReadOnlyList<TerminalMessage> GetMessages() => _messages.AsReadOnly();

    /// <summary>
    /// 从过滤列表中选择一个 Provider（点击或按键选择时调用）
    /// </summary>
    public void SelectFilteredProvider(int index)
    {
        if (index < 0 || index >= FilteredProviders.Value.Count)
            return;

        var selectedProvider = FilteredProviders.Value[index];
        // 同步到完整列表中的选中索引
        var providerIndex = CommandProviders.Value.IndexOf(selectedProvider);
        if (providerIndex >= 0)
        {
            SelectedProviderIndex.Value = providerIndex;
        }

        // 关闭面板并清空输入，准备用户输入命令（如 /cmd）
        ShowProviderPanel.Value = false;
        // 程序化清空输入，配合视图层的 SetTextWithoutNotify，不会形成输入回调回路
        InputText.Value = string.Empty;

        // 请求重新聚焦输入框
        RequestInputFocus.OnNext(Unit.Default);

        Log.Info($"[TerminalViewModel] Selected provider: {selectedProvider.ProviderId}, input cleared");

        // 选择完成后，直接通过 API 发送帮助命令，而不是通过输入框机制
        // 避免对特殊 Provider("#")发送，防止自触发
        if (selectedProvider.ProviderId != "#")
        {
            var help = "/?";
            TerminalUIProtocol.Instance.SendCommand(selectedProvider.ModId, help).Forget();

            // 在 UI 中也记录这条发出的命令
            var displayText = $"[{selectedProvider.ProviderId}] {help}";
            var terminalMessage = new TerminalMessage
            {
                Text = displayText,
                Type = MessageType.Command,
                Timestamp = DateTime.Now
            };
            _messages.Add(terminalMessage);
            MessageAdded.OnNext(terminalMessage);
        }
    }

    /// <summary>
    /// 构建 Command Provider 列表
    /// </summary>
    internal void BuildProviders()
    {
        Log.Info("[TerminalViewModel] Building command providers...");

        try
        {
            // 从 TerminalUIProtocol 获取在线的 ModIds（已排除 TerminalUI 自身）
            var modIds = TerminalUIProtocol.Instance.OnlineModIds.Value;
            Log.Info($"[TerminalViewModel] Found {modIds.Count} online mod IDs");

            var providers = new List<CommandProvider>();
            var providerIdMap = new Dictionary<string, List<CommandProvider>>();

            // 第一轮：创建所有 providers 并生成初始 ProviderId
            foreach (var modId in modIds)
            {
                var displayName = GetDisplayNameForModId(modId);
                var providerId = GenerateInitialProviderId(modId);

                var provider = new CommandProvider
                {
                    ModId = modId,
                    DisplayName = displayName,
                    ProviderId = providerId
                };

                Log.Info(
                    $"[TerminalViewModel] Created provider: ModId={modId}, DisplayName={displayName}, ProviderId={providerId}");
                providers.Add(provider);

                // 记录相同 ProviderId 的 providers
                if (!providerIdMap.ContainsKey(provider.ProviderId))
                {
                    providerIdMap[provider.ProviderId] = new List<CommandProvider>();
                }

                providerIdMap[provider.ProviderId].Add(provider);
            }

            // 第二轮：解决 ProviderId 冲突
            foreach (var group in providerIdMap.Values.Where(g => g.Count > 1))
            {
                ResolveProviderIdConflicts(group);
            }

            // 按 ProviderId 排序
            providers = providers.OrderBy(p => p.ProviderId).ToList();

            // 输出最终的 provider 列表
            Log.Info($"[TerminalViewModel] Final provider list:");
            foreach (var p in providers)
            {
                Log.Info($"  - [{p.ProviderId}] {p.DisplayName} (ModId: {p.ModId})");
                Log.Info($"    DropdownText: {p.DropdownText}");
            }

            // 强制触发 ReactiveProperty 的变化通知
            CommandProviders.Value = new List<CommandProvider>(providers);

            Log.Info($"[TerminalViewModel] Built {providers.Count} command providers successfully");

            // 如果当前选中索引不合法，默认选中第一项
            if (providers.Count > 0 &&
                (SelectedProviderIndex.Value < 0 || SelectedProviderIndex.Value >= providers.Count))
            {
                SelectedProviderIndex.Value = 0;
            }

            // 更新按钮展示文本
            UpdateSelectedProviderLabel();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[TerminalViewModel] Failed to build command providers");
            CommandProviders.Value = [];
            UpdateSelectedProviderLabel();
        }
    }

    /// <summary>
    /// 获取 ModId 的显示名称
    /// </summary>
    private string GetDisplayNameForModId(string modId)
    {
        try
        {
            if (modId.StartsWith("steam."))
            {
                // Steam mod: 通过数字ID查找
                var steamId = ulong.Parse(modId[6..]); // 去掉 "steam." 前缀
                var modInfo = ModManager.modInfos.FirstOrDefault(m =>
                    m.publishedFileId == steamId);

                return modInfo.displayName ?? $"Steam {steamId}";
            }

            if (modId.StartsWith("local."))
            {
                // Local mod: 通过 name 查找
                var localName = modId[6..]; // 去掉 "local." 前缀
                var modInfo = ModManager.modInfos.FirstOrDefault(m =>
                    m.name == localName);

                return modInfo.displayName ?? localName;
            }

            return modId;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[TerminalViewModel] Failed to get display name for {modId}");
            return modId;
        }
    }

    /// <summary>
    /// 预处理字符串：去除非字母数字字符，转小写
    /// </summary>
    private string PreprocessProviderId(string input)
    {
        var result = new System.Text.StringBuilder();
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c))
            {
                result.Append(char.ToLowerInvariant(c));
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 生成初始的 ProviderId
    /// </summary>
    private string GenerateInitialProviderId(string modId)
    {
        // TerminalUI 自身的 ModId 已在列表中排除，不需要特殊处理
        string processedId;

        if (modId.StartsWith("steam."))
        {
            var steamId = modId[6..];
            // 预处理：只保留字母数字，转小写
            processedId = PreprocessProviderId(steamId);
            // 取最后三位
            return processedId.Length >= 3
                ? processedId[^3..]
                : processedId;
        }

        if (modId.StartsWith("local."))
        {
            var localName = modId[6..];
            // 预处理：只保留字母数字，转小写
            processedId = PreprocessProviderId(localName);
            // 取前三个字符
            return processedId.Length >= 3
                ? processedId[..3]
                : processedId;
        }

        // 其他情况也预处理
        processedId = PreprocessProviderId(modId);
        return processedId.Length >= 3 ? processedId[..3] : processedId;
    }

    /// <summary>
    /// 解决 ProviderId 冲突
    /// </summary>
    private void ResolveProviderIdConflicts(List<CommandProvider> conflictingProviders)
    {
        Log.Info($"[TerminalViewModel] Resolving conflicts for {conflictingProviders.Count} providers");

        foreach (var provider in conflictingProviders)
        {
            var modId = provider.ModId;
            var length = 4; // 从4位开始尝试

            while (true)
            {
                string processedId;
                string newProviderId;

                if (modId.StartsWith("steam."))
                {
                    var steamId = modId[6..];
                    processedId = PreprocessProviderId(steamId);
                    newProviderId = processedId.Length >= length
                        ? processedId[^length..]
                        : processedId;
                }
                else if (modId.StartsWith("local."))
                {
                    var localName = modId[6..];
                    processedId = PreprocessProviderId(localName);
                    newProviderId = processedId.Length >= length
                        ? processedId[..length]
                        : processedId;
                }
                else
                {
                    processedId = PreprocessProviderId(modId);
                    newProviderId = processedId.Length >= length
                        ? processedId[..length]
                        : processedId;
                    if (newProviderId == provider.ProviderId)
                    {
                        // 已经是预处理后的完整ID，无法再增加长度
                        break;
                    }
                }

                // 检查新的 ProviderId 是否在当前冲突组中唯一
                if (!conflictingProviders.Any(p => p != provider && p.ProviderId == newProviderId))
                {
                    provider.ProviderId = newProviderId;
                    break;
                }

                length++;

                // 防止无限循环
                if (length > 20)
                {
                    provider.ProviderId = PreprocessProviderId(modId); // 使用完整预处理ID
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 切换 Provider 面板的显示；当打开时展示全部 Provider 列表
    /// </summary>
    public void ToggleProviderPanel()
    {
        if (ShowProviderPanel.Value)
        {
            ShowProviderPanel.Value = false;
        }
        else
        {
            FilteredProviders.Value = new List<CommandProvider>(CommandProviders.Value);
            ShowProviderPanel.Value = true;
        }
    }

    /// <summary>
    /// 同步当前选中 Provider 的展示标签
    /// </summary>
    private void UpdateSelectedProviderLabel()
    {
        try
        {
            if (CommandProviders.Value.Count == 0)
            {
                SelectedProviderLabel.Value = "Select Provider";
                return;
            }

            var index = Mathf.Clamp(SelectedProviderIndex.Value, 0, CommandProviders.Value.Count - 1);
            var p = CommandProviders.Value[index];
            SelectedProviderLabel.Value = $"[{p.ProviderId}] {p.DisplayName}";
        }
        catch
        {
            SelectedProviderLabel.Value = "Select Provider";
        }
    }

    public void Dispose()
    {
        Log.Info("[TerminalViewModel] Disposing...");
        _disposables.Dispose();
        _messages.Clear();
    }
}

/// <summary>
/// 终端消息数据模型
/// </summary>
public class TerminalMessage
{
    public string Text { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime Timestamp { get; set; }

    public string FormattedText => $"[{Timestamp:HH:mm:ss}] [{Type}] {Text}";
}

/// <summary>
/// 消息类型
/// </summary>
public enum MessageType
{
    Info,
    Command,
    System,
    Debug,
    Error,
    Warning
}

/// <summary>
/// 命令 Provider 数据模型
/// </summary>
public class CommandProvider
{
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Mod ID（steam.xxx 或 local.xxx）
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// Provider ID（用于标识的短ID）
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// 用于下拉框显示的文本（ProviderId 部分使用颜色着重显示）
    /// </summary>
    public string DropdownText => $"<color=#00FFFF>[{ProviderId}]</color> {DisplayName}";
}
