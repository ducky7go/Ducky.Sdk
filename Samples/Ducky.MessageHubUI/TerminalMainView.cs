using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using Ducky.Sdk.Logging;
using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ducky.MessageHubUI;

/// <summary>
/// 终端主界面，占据屏幕左半边50%（单例模式）
/// </summary>
internal class TerminalMainView : MonoBehaviour
{
    // 单例实例
    private static TerminalMainView? _instance;

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static TerminalMainView? Instance => _instance;

    [Header("主面板内容")] [SerializeField] private GameObject? contentPanel;
    [SerializeField] private RectTransform? panelRect;

    [Header("UI组件")] [SerializeField] private TextMeshProUGUI? titleText;
    [SerializeField] private Button? closeButton;
    [SerializeField] private ScrollRect? scrollView;

    [SerializeField] private RectTransform? scrollContent;

    // 移除 ProviderSelector，统一使用过滤面板进行选择
    [SerializeField] private Button? providerButton;
    [SerializeField] private TextMeshProUGUI? providerButtonText;
    [SerializeField] private TMP_InputField? inputField;
    [SerializeField] private ProviderFilterPanel? filterPanel;

    [Header("动画设置")] [SerializeField] private float slideSpeed = 10f;
    private float hiddenXPosition = -960f; // 隐藏位置（屏幕左侧外，默认1920/2）
    [SerializeField] private float visibleXPosition = 0f; // 可见位置

    private bool initialized = false;

    // 防止双向绑定时造成 onValueChanged 循环回调
    private bool _suppressInputNotify = false;
    private TerminalHandlerView? handlerView;
    private TerminalViewModel? viewModel;
    private CompositeDisposable? bindings;

    // 线程安全的消息队列，用于从外部线程接收消息
    private readonly ConcurrentQueue<TerminalMessage> _incomingMessages = new();

    /// <summary>
    /// 面板是否可见
    /// </summary>
    public bool IsVisible => viewModel?.IsVisible.Value ?? false;

    /// <summary>
    /// ViewModel 实例
    /// </summary>
    public TerminalViewModel? ViewModel => viewModel;

    private void Awake()
    {
        // 单例模式：确保只有一个实例
        if (_instance != null && _instance != this)
        {
            Log.Warn("[TerminalMainView] Duplicate instance detected, destroying...");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        Log.Info("[TerminalMainView] Singleton instance set");
    }

    private void Start()
    {
        Log.Info("[TerminalMainView] Start() called");

        // 初始化 ViewModel
        if (viewModel == null)
        {
            viewModel = new TerminalViewModel();
            viewModel.BuildProviders();
            Log.Info("[TerminalMainView] ViewModel created");
        }

        // 初始化绑定
        SetupBindings();

        // 计算隐藏位置（使用屏幕宽度的一半）
        if (panelRect != null)
        {
            hiddenXPosition = -Screen.width / 2f;
            Log.Info($"[TerminalMainView] Hidden position calculated: {hiddenXPosition}");
        }

        // 确保初始状态内容是隐藏的
        if (contentPanel != null)
        {
            contentPanel.SetActive(false);
            Log.Info("[TerminalMainView] Content panel set to inactive");
        }

        // 启动消息处理协程
        StartCoroutine(ProcessIncomingMessages());

        initialized = true;
        Log.Info("[TerminalMainView] Initialization complete");
    }

    private void Update()
    {
        // 每帧检查是否有待处理的消息（作为协程的备份机制）
        // 协程会处理大部分情况，这里只是确保不会遗漏
        if (!_incomingMessages.IsEmpty && viewModel != null)
        {
            ProcessPendingMessages();
        }
    }

    /// <summary>
    /// 处理传入消息的协程（在主线程中运行）
    /// </summary>
    private IEnumerator ProcessIncomingMessages()
    {
        while (true)
        {
            ProcessPendingMessages();
            yield return new WaitForSeconds(0.1f); // 每0.1秒处理一次
        }
    }

    /// <summary>
    /// 处理队列中的所有待处理消息
    /// </summary>
    private void ProcessPendingMessages()
    {
        int processedCount = 0;
        const int maxBatchSize = 50; // 每次最多处理50条消息，避免阻塞主线程

        while (processedCount < maxBatchSize && _incomingMessages.TryDequeue(out TerminalMessage message))
        {
            try
            {
                Log.Debug($"[TerminalMainView] Processing incoming message: {message.Text}");
                // 将 TerminalMessage 转换为 ViewModel 接受的参数
                viewModel?.AddMessage(message.Text, message.Type);
                processedCount++;
            }
            catch (Exception ex)
            {
                Log.Error($"[TerminalMainView] Error processing message: {ex.Message}");
            }
        }

        if (processedCount > 0)
        {
            Log.Debug($"[TerminalMainView] Processed {processedCount} incoming messages");
        }
    }

    private void OnDestroy()
    {
        Log.Info("[TerminalMainView] OnDestroy() called");

        // 清理单例引用
        if (_instance == this)
        {
            _instance = null;
        }

        // 清理绑定
        bindings?.Dispose();
        bindings = null;

        // 清理 ViewModel
        viewModel?.Dispose();
        viewModel = null;
    }

    /// <summary>
    /// 设置 ViewModel 绑定
    /// </summary>
    private void SetupBindings()
    {
        if (viewModel == null)
        {
            Log.Error("[TerminalMainView] Cannot setup bindings: ViewModel is null");
            return;
        }

        bindings?.Dispose();
        bindings = new CompositeDisposable();

        Log.Info("[TerminalMainView] Setting up ViewModel bindings...");

        // 绑定标题
        if (titleText != null)
        {
            viewModel.Title.Subscribe(title => { titleText.text = title; }).AddTo(bindings);
        }

        // 绑定关闭按钮命令
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => { viewModel.HideCommand.Execute(Unit.Default); });
        }

        // ProviderSelector 已移除，选择逻辑通过过滤面板与 '#'+筛选实现
        // 选择按钮：点击弹出过滤面板，文本显示当前选择
        if (providerButton != null && providerButtonText != null)
        {
            providerButton.onClick.AddListener(() => { viewModel.ToggleProviderPanel(); });

            viewModel.SelectedProviderLabel.Subscribe(label => { providerButtonText.text = label; }).AddTo(bindings);
        }

        // 绑定输入框
        if (inputField != null)
        {
            // 双向绑定输入文本
            inputField.onValueChanged.AddListener(text =>
            {
                if (_suppressInputNotify) return;
                viewModel.InputText.Value = text;
            });

            viewModel.InputText.Subscribe(text =>
            {
                // 使用 SetTextWithoutNotify 防止触发 onValueChanged，避免递归
                if (inputField.text != text)
                {
                    _suppressInputNotify = true;
                    inputField.SetTextWithoutNotify(text);
                    _suppressInputNotify = false;
                }
            }).AddTo(bindings);

            // 绑定回车发送
            inputField.onSubmit.AddListener(_ =>
            {
                if (viewModel.SendMessageCommand.CanExecute())
                    viewModel.SendMessageCommand.Execute(Unit.Default);
            });
        }

        // 绑定可见性变化
        viewModel.IsVisible.Subscribe(visible =>
        {
            if (visible)
                ShowInternal();
            else
                HideInternal();
        }).AddTo(bindings);

        // 绑定消息添加事件 - 自动滚动到底部
        viewModel.MessageAdded.Subscribe(message =>
        {
            AddMessageToUI(message);
            ScrollToBottom();
        }).AddTo(bindings);

        // 绑定消息清空事件
        viewModel.MessagesCleared.Subscribe(_ => { ClearMessagesFromUI(); }).AddTo(bindings);

        // 绑定过滤面板
        if (filterPanel != null)
        {
            // 监听过滤后的 provider 列表变化
            viewModel.FilteredProviders.Subscribe(filtered =>
            {
                filterPanel.SetProviders(filtered, viewModel.ShowProviderPanel.Value);
            }).AddTo(bindings);

            // 监听面板显示状态
            viewModel.ShowProviderPanel.Subscribe(show =>
            {
                if (show)
                {
                    filterPanel.SetProviders(viewModel.FilteredProviders.Value, true);
                }
                else
                {
                    filterPanel.Hide();
                }
            }).AddTo(bindings);

            // 绑定选择事件
            filterPanel.OnProviderSelected += index => { viewModel.SelectFilteredProvider(index); };
        }

        // 绑定重新聚焦输入框事件
        viewModel.RequestInputFocus.Subscribe(_ =>
        {
            if (inputField != null)
            {
                inputField.ActivateInputField();
                inputField.Select();
            }
        }).AddTo(bindings);

        Log.Info("[TerminalMainView] ViewModel bindings completed");
    }

    /// <summary>
    /// 在 UI 上添加消息
    /// </summary>
    private void AddMessageToUI(TerminalMessage message)
    {
        if (scrollContent == null) return;

        // 创建消息文本对象
        GameObject messageObj = new GameObject($"Message_{message.Timestamp:HHmmss}");
        messageObj.transform.SetParent(scrollContent, false);

        // 确保有 RectTransform（AddComponent<TextMeshProUGUI> 会自动添加 CanvasRenderer）
        var _ = messageObj.AddComponent<RectTransform>();

        TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
        // 使用原始文本而非 FormattedText，以便更好地控制格式
        messageText.text = message.FormattedText;
        messageText.fontSize = 16;
        messageText.color = GetColorForMessageType(message.Type);
        messageText.alignment = TextAlignmentOptions.TopLeft;
        messageText.enableWordWrapping = true;
        messageText.richText = true;
        // 确保支持多行文本，包括 \n 转义字符
        messageText.overflowMode = TextOverflowModes.Overflow;
        messageText.enableAutoSizing = false;

        // 计算文本所需的首选高度并告知布局系统
        messageText.ForceMeshUpdate();
        float preferredHeight = Mathf.Max(22f, messageText.preferredHeight + 4f); // 添加一些额外间距

        // 添加 LayoutElement 用于明确高度，避免为 0 导致不可见
        LayoutElement layoutElement = messageObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = 0f;
    }

    /// <summary>
    /// 清空 UI 上的所有消息
    /// </summary>
    private void ClearMessagesFromUI()
    {
        if (scrollContent == null) return;

        // 销毁所有子对象
        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 滚动到底部（最新消息位置）
    /// </summary>
    private void ScrollToBottom()
    {
        if (scrollView != null)
        {
            // 延迟一帧执行，确保布局已更新
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        // 等待布局系统更新内容大小
        yield return null;
        yield return new WaitForEndOfFrame();

        if (scrollView != null)
        {
            // 底部锚点时，0 表示底部（最新消息）
            scrollView.verticalNormalizedPosition = 0f;

            // 强制重建布局以确保正确显示
            Canvas.ForceUpdateCanvases();
        }
    }

    /// <summary>
    /// 根据消息类型获取颜色
    /// </summary>
    private Color GetColorForMessageType(MessageType type)
    {
        return type switch
        {
            MessageType.Info => Color.white,
            MessageType.Command => new Color(0.5f, 0.8f, 1f), // 浅蓝色
            MessageType.System => new Color(0.8f, 0.8f, 0.5f), // 浅黄色
            MessageType.Debug => new Color(0.7f, 0.7f, 0.7f), // 灰色
            MessageType.Error => new Color(1f, 0.3f, 0.3f), // 红色
            MessageType.Warning => new Color(1f, 0.7f, 0.3f), // 橙色
            _ => Color.white
        };
    }

    /// <summary>
    /// 设置触发区域的引用
    /// </summary>
    public void SetHandlerView(TerminalHandlerView handler)
    {
        handlerView = handler;
    }

    /// <summary>
    /// 公共方法：显示终端（通过 ViewModel）
    /// </summary>
    public void Show()
    {
        if (viewModel != null)
        {
            viewModel.ShowCommand.Execute(Unit.Default);
        }
    }

    /// <summary>
    /// 公共方法：隐藏终端（通过 ViewModel）
    /// </summary>
    public void Hide()
    {
        if (viewModel != null)
        {
            viewModel.HideCommand.Execute(Unit.Default);
        }
    }

    /// <summary>
    /// 绑定 Provider - 在收到 online/offline 消息时调用以刷新 Provider 列表
    /// </summary>
    public void BindProvider()
    {
        Log.Info("[TerminalMainView] BindProvider() called");

        if (viewModel != null)
        {
            viewModel.BuildProviders();
            Log.Info("[TerminalMainView] Provider list rebuilt");
        }
        else
        {
            Log.Warn("[TerminalMainView] Cannot bind providers: ViewModel is null");
        }
    }

    /// <summary>
    /// 从外部添加消息（线程安全）
    /// 此方法可以从任何线程调用，消息会被加入队列并在主线程中处理
    /// </summary>
    /// <param name="text">消息文本</param>
    /// <param name="type">消息类型</param>
    public void PostMessage(string text, MessageType type = MessageType.Info)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var message = new TerminalMessage
        {
            Text = text,
            Type = type,
            Timestamp = DateTime.Now
        };

        _incomingMessages.Enqueue(message);
    }

    /// <summary>
    /// 从外部添加消息（线程安全）- 使用 TerminalMessage 对象
    /// 此方法可以从任何线程调用，消息会被加入队列并在主线程中处理
    /// </summary>
    /// <param name="message">消息对象</param>
    public void PostMessage(TerminalMessage message)
    {
        if (message == null || string.IsNullOrEmpty(message.Text))
        {
            return;
        }

        _incomingMessages.Enqueue(message);
    }

    /// <summary>
    /// 批量添加消息（线程安全）
    /// </summary>
    /// <param name="messages">消息列表</param>
    public void PostMessages(params TerminalMessage[] messages)
    {
        if (messages == null || messages.Length == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            if (message != null && !string.IsNullOrEmpty(message.Text))
            {
                _incomingMessages.Enqueue(message);
            }
        }
    }

    /// <summary>
    /// 内部方法：实际执行显示动画
    /// </summary>
    private void ShowInternal()
    {
        Log.Info($"[TerminalMainView] ShowInternal() called");

        // 禁用触发区域
        if (handlerView != null)
        {
            handlerView.SetActive(false);
        }

        if (contentPanel != null)
        {
            Log.Info("[TerminalMainView] Setting contentPanel active");
            contentPanel.SetActive(true);
        }
        else
        {
            Log.Error("[TerminalMainView] contentPanel is null!");
        }

        Log.Info($"[TerminalMainView] Starting slide animation to {visibleXPosition}");
        StopAllCoroutines();
        StartCoroutine(SlidePanel(visibleXPosition, () =>
        {
            // 动画完成后，自动聚焦到输入框
            if (inputField != null)
            {
                inputField.ActivateInputField();
                inputField.Select();
                Log.Info("[TerminalMainView] Input field focused after panel shown");
            }
        }));
    }

    /// <summary>
    /// 内部方法：实际执行隐藏动画
    /// </summary>
    private void HideInternal()
    {
        Log.Info("[TerminalMainView] HideInternal() called");

        StopAllCoroutines();
        StartCoroutine(SlidePanel(hiddenXPosition, () =>
        {
            if (contentPanel != null)
            {
                contentPanel.SetActive(false);
            }

            // 重新启用触发区域
            if (handlerView != null)
            {
                handlerView.SetActive(true);
            }
        }));
    }

    private IEnumerator SlidePanel(float targetX, Action? onComplete = null)
    {
        if (panelRect == null) yield break;

        while (Mathf.Abs(panelRect.anchoredPosition.x - targetX) > 0.1f)
        {
            var pos = panelRect.anchoredPosition;
            pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * slideSpeed);
            panelRect.anchoredPosition = pos;
            yield return null;
        }

        // 确保最终位置精确
        var finalPos = panelRect.anchoredPosition;
        finalPos.x = targetX;
        panelRect.anchoredPosition = finalPos;

        onComplete?.Invoke();
    }

    /// <summary>
    /// 创建UI结构的静态工厂方法
    /// </summary>
    public static TerminalMainView Create(Canvas canvas)
    {
        Log.Info("[TerminalMainView] Creating main panel...");

        // 创建主面板容器（根对象）
        GameObject rootObj = new GameObject("TerminalMainPanel");
        rootObj.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 0); // 左下角锚点
        rootRect.anchorMax = new Vector2(0.5f, 1); // 右上角锚点到屏幕中间
        rootRect.pivot = new Vector2(0, 0.5f);
        rootRect.offsetMin = Vector2.zero; // 左下偏移
        rootRect.offsetMax = Vector2.zero; // 右上偏移
        // 设置初始位置在屏幕左侧外
        rootRect.anchoredPosition = new Vector2(-Screen.width / 2, 0);

        // 创建内容面板（实际显示/隐藏的对象）
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(rootObj.transform, false);

        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // 添加背景到内容面板
        Image contentImage = contentObj.AddComponent<Image>();
        contentImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // 深灰色半透明背景

        // ===== 1. 标题栏 =====
        GameObject titleBarObj = new GameObject("TitleBar");
        titleBarObj.transform.SetParent(contentObj.transform, false);

        RectTransform titleBarRect = titleBarObj.AddComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0, 1); // 顶部
        titleBarRect.anchorMax = new Vector2(1, 1);
        titleBarRect.pivot = new Vector2(0.5f, 1);
        titleBarRect.anchoredPosition = Vector2.zero;
        titleBarRect.sizeDelta = new Vector2(0, 50); // 高度50

        Image titleBarImage = titleBarObj.AddComponent<Image>();
        titleBarImage.color = new Color(0.15f, 0.15f, 0.15f, 1f); // 稍亮一点的背景

        // 标题文本
        GameObject titleTextObj = new GameObject("TitleText");
        titleTextObj.transform.SetParent(titleBarObj.transform, false);

        RectTransform titleTextRect = titleTextObj.AddComponent<RectTransform>();
        titleTextRect.anchorMin = new Vector2(0, 0);
        titleTextRect.anchorMax = new Vector2(1, 1);
        titleTextRect.offsetMin = new Vector2(15, 0); // 左边距
        titleTextRect.offsetMax = new Vector2(-60, 0); // 右边距（留空间给关闭按钮）

        TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Terminal Console";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // 关闭按钮（在标题栏上）
        GameObject closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(titleBarObj.transform, false);

        RectTransform closeRect = closeButtonObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 0.5f); // 右侧中间
        closeRect.anchorMax = new Vector2(1, 0.5f);
        closeRect.pivot = new Vector2(1, 0.5f);
        closeRect.anchoredPosition = new Vector2(-10, 0);
        closeRect.sizeDelta = new Vector2(35, 35);

        Image closeImage = closeButtonObj.AddComponent<Image>();
        closeImage.color = new Color(0.8f, 0.2f, 0.2f, 1f); // 红色按钮

        Button closeButton = closeButtonObj.AddComponent<Button>();

        // 关闭按钮文本 "X"
        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeButtonObj.transform, false);

        RectTransform closeTextRect = closeTextObj.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "×";
        closeText.fontSize = 24;
        closeText.fontStyle = FontStyles.Bold;
        closeText.color = Color.white;
        closeText.alignment = TextAlignmentOptions.Center;

        // ===== 2. 滚动显示区域 =====
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(contentObj.transform, false);

        RectTransform scrollViewRect = scrollViewObj.AddComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0, 0);
        scrollViewRect.anchorMax = new Vector2(1, 1);
        scrollViewRect.pivot = new Vector2(0.5f, 0.5f);
        scrollViewRect.offsetMin = new Vector2(10, 80); // 底部留80px给输入区域（增加10px）
        scrollViewRect.offsetMax = new Vector2(-10, -60); // 顶部留60px给标题栏（50+10边距）

        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        // Viewport (为滚动条留出空间，使用RectMask2D而非Mask)
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);

        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = new Vector2(0.95f, 1); // 右侧留5%给滚动条
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        viewportObj.AddComponent<RectMask2D>(); // 使用RectMask2D进行裁剪

        scrollRect.viewport = viewportRect;

        // Content (从底部开始布局，最新消息在下方)
        GameObject scrollContentObj = new GameObject("Content");
        scrollContentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform scrollContentRect = scrollContentObj.AddComponent<RectTransform>();
        scrollContentRect.anchorMin = new Vector2(0, 0); // 底部锚点
        scrollContentRect.anchorMax = new Vector2(1, 0); // 底部锚点
        scrollContentRect.pivot = new Vector2(0.5f, 0); // 底部中心为轴心点
        scrollContentRect.offsetMin = Vector2.zero;
        scrollContentRect.offsetMax = Vector2.zero;

        // 添加 VerticalLayoutGroup 用于自动排列内容（从底部向上）
        VerticalLayoutGroup contentLayout = scrollContentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.LowerLeft; // 底部对齐
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 2;
        contentLayout.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter contentFitter = scrollContentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = scrollContentRect;

        // ===== 滚动条 =====
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(scrollViewObj.transform, false);

        RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(0.96f, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.offsetMin = Vector2.zero;
        scrollbarRect.offsetMax = Vector2.zero;

        Image scrollbarImg = scrollbarObj.AddComponent<Image>();
        scrollbarImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        Scrollbar scrollbarComp = scrollbarObj.AddComponent<Scrollbar>();
        scrollbarComp.direction = Scrollbar.Direction.BottomToTop;

        // 滑块区域
        GameObject slidingAreaObj = new GameObject("Sliding Area");
        slidingAreaObj.transform.SetParent(scrollbarObj.transform, false);

        RectTransform slidingAreaRect = slidingAreaObj.AddComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = new Vector2(2, 2);
        slidingAreaRect.offsetMax = new Vector2(-2, -2);

        // 滑块
        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(slidingAreaObj.transform, false);

        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.5f, 0.5f, 0.5f, 0.9f);

        scrollbarComp.handleRect = handleRect;
        scrollbarComp.targetGraphic = handleImg;

        scrollRect.verticalScrollbar = scrollbarComp;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // ===== 3. 底部输入区域 =====
        GameObject inputAreaObj = new GameObject("InputArea");
        inputAreaObj.transform.SetParent(contentObj.transform, false);

        RectTransform inputAreaRect = inputAreaObj.AddComponent<RectTransform>();
        inputAreaRect.anchorMin = new Vector2(0, 0); // 底部
        inputAreaRect.anchorMax = new Vector2(1, 0);
        inputAreaRect.pivot = new Vector2(0.5f, 0);
        inputAreaRect.anchoredPosition = Vector2.zero;
        inputAreaRect.sizeDelta = new Vector2(0, 70); // 高度70（增加10px）

        Image inputAreaImage = inputAreaObj.AddComponent<Image>();
        inputAreaImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // 创建一个容器来包含选择按钮、输入框和过滤面板
        GameObject inputContainerObj = new GameObject("InputContainer");
        inputContainerObj.transform.SetParent(inputAreaObj.transform, false);

        RectTransform inputContainerRect = inputContainerObj.AddComponent<RectTransform>();
        inputContainerRect.anchorMin = Vector2.zero;
        inputContainerRect.anchorMax = Vector2.one;
        inputContainerRect.offsetMin = Vector2.zero;
        inputContainerRect.offsetMax = Vector2.zero;

        // 使用 HorizontalLayoutGroup 自动布局
        HorizontalLayoutGroup inputLayout = inputContainerObj.AddComponent<HorizontalLayoutGroup>();
        inputLayout.childAlignment = TextAnchor.MiddleLeft;
        inputLayout.childControlWidth = true; // 控制宽度以支持flexibleWidth
        inputLayout.childControlHeight = true; // 控制高度以保持一致
        inputLayout.childForceExpandWidth = false;
        inputLayout.childForceExpandHeight = true; // 强制扩展高度以填充整个输入区域
        inputLayout.spacing = 10;
        inputLayout.padding = new RectOffset(10, 10, 10, 10);

        // 选择按钮（在左侧，固定宽度，显示当前选择的 Provider）
        GameObject providerBtnObj = new GameObject("ProviderButton");
        providerBtnObj.transform.SetParent(inputContainerObj.transform, false);

        RectTransform providerBtnRect = providerBtnObj.AddComponent<RectTransform>();
        LayoutElement providerBtnLayout = providerBtnObj.AddComponent<LayoutElement>();
        providerBtnLayout.preferredWidth = 240f; // 与过滤面板宽度一致
        providerBtnLayout.minHeight = 40;
        providerBtnLayout.preferredHeight = 40;

        Image providerBtnImage = providerBtnObj.AddComponent<Image>();
        providerBtnImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        Button providerBtn = providerBtnObj.AddComponent<Button>();
        var providerBtnColors = providerBtn.colors;
        providerBtnColors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        providerBtnColors.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        providerBtn.colors = providerBtnColors;

        // 按钮内文本
        GameObject providerBtnTextObj = new GameObject("Text");
        providerBtnTextObj.transform.SetParent(providerBtnObj.transform, false);

        RectTransform providerBtnTextRect = providerBtnTextObj.AddComponent<RectTransform>();
        providerBtnTextRect.anchorMin = Vector2.zero;
        providerBtnTextRect.anchorMax = Vector2.one;
        providerBtnTextRect.offsetMin = new Vector2(10, 2);
        providerBtnTextRect.offsetMax = new Vector2(-10, -2);

        TextMeshProUGUI providerBtnText = providerBtnTextObj.AddComponent<TextMeshProUGUI>();
        providerBtnText.text = "Select Provider";
        providerBtnText.fontSize = 14;
        providerBtnText.color = Color.white;
        providerBtnText.alignment = TextAlignmentOptions.MidlineLeft;

        // 输入框（在右侧，占据剩余空间）
        GameObject inputFieldObj = new GameObject("InputField");
        inputFieldObj.transform.SetParent(inputContainerObj.transform, false);

        RectTransform inputFieldRect = inputFieldObj.AddComponent<RectTransform>();
        // 使用 LayoutElement 让输入框占据剩余空间并与下拉框同高
        LayoutElement inputFieldLayout = inputFieldObj.AddComponent<LayoutElement>();
        inputFieldLayout.flexibleWidth = 1f; // 填充剩余宽度
        inputFieldLayout.minHeight = 40; // 最小高度
        inputFieldLayout.preferredHeight = 40; // 首选高度，与下拉框一致

        Image inputFieldImage = inputFieldObj.AddComponent<Image>();
        inputFieldImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
        inputField.textViewport = inputFieldRect;

        // Input Field Text Area
        GameObject textAreaObj = new GameObject("Text Area");
        textAreaObj.transform.SetParent(inputFieldObj.transform, false);

        RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textAreaObj.transform, false);

        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Enter command...";
        placeholder.fontSize = 14;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;

        inputField.placeholder = placeholder;

        // Input Text
        GameObject inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(textAreaObj.transform, false);

        RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = Vector2.zero;
        inputTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
        inputText.text = "";
        inputText.fontSize = 14;
        inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.MidlineLeft;

        inputField.textComponent = inputText;

        // 创建过滤面板（挂在选择按钮上，使其左下角对齐按钮左上角）
        ProviderFilterPanel filterPanel = ProviderFilterPanel.Create(providerBtnObj.transform, 240f);
        Log.Info("[TerminalMainView] ProviderFilterPanel created");

        // 添加主脚本到根对象
        TerminalMainView mainView = rootObj.AddComponent<TerminalMainView>();
        mainView.contentPanel = contentObj;
        mainView.panelRect = rootRect;
        mainView.titleText = titleText;
        mainView.closeButton = closeButton;
        mainView.scrollView = scrollRect;
        mainView.scrollContent = scrollContentRect;
        // 绑定到主视图字段
        mainView.inputField = inputField;
        mainView.filterPanel = filterPanel;
        mainView.providerButton = providerBtn;
        mainView.providerButtonText = providerBtnText;

        Log.Info("[TerminalMainView] Main panel created successfully");

        return mainView;
    }
}
