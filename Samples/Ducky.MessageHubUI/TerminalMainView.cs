using System.Collections;
using Ducky.Sdk.Logging;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ducky.MessageHubUI;

/// <summary>
/// 终端主界面，占据屏幕左半边50%
/// </summary>
public class TerminalMainView : MonoBehaviour
{
    [Header("主面板内容")]
    [SerializeField] private GameObject? contentPanel;
    [SerializeField] private RectTransform? panelRect;
    
    [Header("UI组件")]
    [SerializeField] private TextMeshProUGUI? titleText;
    [SerializeField] private Button? closeButton;
    [SerializeField] private ScrollRect? scrollView;
    [SerializeField] private RectTransform? scrollContent;
    [SerializeField] private TMP_Dropdown? commandDropdown;
    [SerializeField] private TMP_InputField? inputField;
    
    [Header("动画设置")]
    [SerializeField] private float slideSpeed = 10f;
    private float hiddenXPosition = -960f; // 隐藏位置（屏幕左侧外，默认1920/2）
    [SerializeField] private float visibleXPosition = 0f;     // 可见位置
    
    private bool isVisible = false;
    private bool initialized = false;
    private TerminalHandlerView? handlerView;
    
    /// <summary>
    /// 面板是否可见
    /// </summary>
    public bool IsVisible => isVisible;
    
    private void Start()
    {
        Log.Info("[TerminalMainView] Start() called");
        
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
        
        // 绑定关闭按钮
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
            Log.Info("[TerminalMainView] Close button listener added");
        }
        
        initialized = true;
        Log.Info("[TerminalMainView] Initialization complete");
    }
    
    /// <summary>
    /// 设置触发区域的引用
    /// </summary>
    public void SetHandlerView(TerminalHandlerView handler)
    {
        handlerView = handler;
    }
    
    public void Show()
    {
        Log.Info($"[TerminalMainView] Show() called. isVisible={isVisible}");
        if (isVisible)
        {
            Log.Info("[TerminalMainView] Already visible, returning");
            return;
        }
        
        isVisible = true;
        
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
        StartCoroutine(SlidePanel(visibleXPosition));
    }
    
    public void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
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
    
    private IEnumerator SlidePanel(float targetX, System.Action? onComplete = null)
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
        scrollViewRect.offsetMin = new Vector2(10, 70); // 底部留70px给输入区域
        scrollViewRect.offsetMax = new Vector2(-10, -60); // 顶部留60px给标题栏（50+10边距）
        
        Image scrollViewImage = scrollViewObj.AddComponent<Image>();
        scrollViewImage.color = new Color(0.05f, 0.05f, 0.05f, 1f); // 更深的背景
        
        ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;
        
        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = Color.clear;
        
        Mask viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        
        scrollRect.viewport = viewportRect;
        
        // Content
        GameObject scrollContentObj = new GameObject("Content");
        scrollContentObj.transform.SetParent(viewportObj.transform, false);
        
        RectTransform scrollContentRect = scrollContentObj.AddComponent<RectTransform>();
        scrollContentRect.anchorMin = new Vector2(0, 1); // 顶部对齐
        scrollContentRect.anchorMax = new Vector2(1, 1);
        scrollContentRect.pivot = new Vector2(0.5f, 1);
        scrollContentRect.anchoredPosition = Vector2.zero;
        scrollContentRect.sizeDelta = new Vector2(0, 0);
        
        // 添加 VerticalLayoutGroup 用于自动排列内容
        VerticalLayoutGroup contentLayout = scrollContentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 2;
        contentLayout.padding = new RectOffset(10, 10, 10, 10);
        
        ContentSizeFitter contentFitter = scrollContentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = scrollContentRect;
        
        // ===== 3. 底部输入区域 =====
        GameObject inputAreaObj = new GameObject("InputArea");
        inputAreaObj.transform.SetParent(contentObj.transform, false);
        
        RectTransform inputAreaRect = inputAreaObj.AddComponent<RectTransform>();
        inputAreaRect.anchorMin = new Vector2(0, 0); // 底部
        inputAreaRect.anchorMax = new Vector2(1, 0);
        inputAreaRect.pivot = new Vector2(0.5f, 0);
        inputAreaRect.anchoredPosition = Vector2.zero;
        inputAreaRect.sizeDelta = new Vector2(0, 60); // 高度60
        
        Image inputAreaImage = inputAreaObj.AddComponent<Image>();
        inputAreaImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        
        // 使用 HorizontalLayoutGroup 自动布局
        HorizontalLayoutGroup inputLayout = inputAreaObj.AddComponent<HorizontalLayoutGroup>();
        inputLayout.childAlignment = TextAnchor.MiddleLeft;
        inputLayout.childControlWidth = true; // 控制宽度以支持flexibleWidth
        inputLayout.childControlHeight = true; // 控制高度以保持一致
        inputLayout.childForceExpandWidth = false;
        inputLayout.childForceExpandHeight = true; // 强制扩展高度以填充整个输入区域
        inputLayout.spacing = 10;
        inputLayout.padding = new RectOffset(10, 10, 10, 10);
        
        // Dropdown（最左侧）
        GameObject dropdownObj = new GameObject("CommandDropdown");
        dropdownObj.transform.SetParent(inputAreaObj.transform, false);
        
        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        // 固定宽度120，高度由LayoutElement控制
        LayoutElement dropdownLayout = dropdownObj.AddComponent<LayoutElement>();
        dropdownLayout.preferredWidth = 120;
        dropdownLayout.minHeight = 40;
        dropdownLayout.preferredHeight = 40;
        
        Image dropdownImage = dropdownObj.AddComponent<Image>();
        dropdownImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Command"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("System"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Debug"));
        dropdown.value = 0;
        dropdown.RefreshShownValue();
        
        // Dropdown Label
        GameObject dropdownLabelObj = new GameObject("Label");
        dropdownLabelObj.transform.SetParent(dropdownObj.transform, false);
        
        RectTransform dropdownLabelRect = dropdownLabelObj.AddComponent<RectTransform>();
        dropdownLabelRect.anchorMin = Vector2.zero;
        dropdownLabelRect.anchorMax = Vector2.one;
        dropdownLabelRect.offsetMin = new Vector2(10, 0);
        dropdownLabelRect.offsetMax = new Vector2(-25, 0);
        
        TextMeshProUGUI dropdownLabel = dropdownLabelObj.AddComponent<TextMeshProUGUI>();
        dropdownLabel.text = "Command";
        dropdownLabel.fontSize = 14;
        dropdownLabel.color = Color.white;
        dropdownLabel.alignment = TextAlignmentOptions.MidlineLeft;
        
        dropdown.captionText = dropdownLabel;
        
        // Dropdown Arrow
        GameObject dropdownArrowObj = new GameObject("Arrow");
        dropdownArrowObj.transform.SetParent(dropdownObj.transform, false);
        
        RectTransform dropdownArrowRect = dropdownArrowObj.AddComponent<RectTransform>();
        dropdownArrowRect.anchorMin = new Vector2(1, 0.5f);
        dropdownArrowRect.anchorMax = new Vector2(1, 0.5f);
        dropdownArrowRect.pivot = new Vector2(0.5f, 0.5f);
        dropdownArrowRect.anchoredPosition = new Vector2(-12, 0);
        dropdownArrowRect.sizeDelta = new Vector2(20, 20);
        
        TextMeshProUGUI dropdownArrow = dropdownArrowObj.AddComponent<TextMeshProUGUI>();
        dropdownArrow.text = "▲"; // 向上的箭头
        dropdownArrow.fontSize = 12;
        dropdownArrow.color = Color.white;
        dropdownArrow.alignment = TextAlignmentOptions.Center;
        
        // Dropdown Template（下拉列表模板）
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        templateObj.SetActive(false);
        
        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, 2);
        templateRect.sizeDelta = new Vector2(0, 150);
        
        Image templateImage = templateObj.AddComponent<Image>();
        templateImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        
        ScrollRect templateScrollRect = templateObj.AddComponent<ScrollRect>();
        templateScrollRect.horizontal = false;
        templateScrollRect.vertical = true;
        
        // Template Viewport
        GameObject templateViewportObj = new GameObject("Viewport");
        templateViewportObj.transform.SetParent(templateObj.transform, false);
        
        RectTransform templateViewportRect = templateViewportObj.AddComponent<RectTransform>();
        templateViewportRect.anchorMin = Vector2.zero;
        templateViewportRect.anchorMax = Vector2.one;
        templateViewportRect.offsetMin = new Vector2(5, 5);
        templateViewportRect.offsetMax = new Vector2(-5, -5);
        
        Mask templateMask = templateViewportObj.AddComponent<Mask>();
        templateMask.showMaskGraphic = false;
        
        Image templateViewportImage = templateViewportObj.AddComponent<Image>();
        templateViewportImage.color = Color.clear;
        
        templateScrollRect.viewport = templateViewportRect;
        
        // Template Content
        GameObject templateContentObj = new GameObject("Content");
        templateContentObj.transform.SetParent(templateViewportObj.transform, false);
        
        RectTransform templateContentRect = templateContentObj.AddComponent<RectTransform>();
        templateContentRect.anchorMin = new Vector2(0, 1);
        templateContentRect.anchorMax = new Vector2(1, 1);
        templateContentRect.pivot = new Vector2(0.5f, 1);
        templateContentRect.anchoredPosition = Vector2.zero;
        templateContentRect.sizeDelta = new Vector2(0, 0);
        
        templateScrollRect.content = templateContentRect;
        
        // Template Item
        GameObject templateItemObj = new GameObject("Item");
        templateItemObj.transform.SetParent(templateContentObj.transform, false);
        
        RectTransform templateItemRect = templateItemObj.AddComponent<RectTransform>();
        templateItemRect.anchorMin = new Vector2(0, 1);
        templateItemRect.anchorMax = new Vector2(1, 1);
        templateItemRect.pivot = new Vector2(0.5f, 1);
        templateItemRect.sizeDelta = new Vector2(0, 30);
        
        Toggle templateToggle = templateItemObj.AddComponent<Toggle>();
        templateToggle.isOn = false;
        
        // Item Background
        GameObject itemBackgroundObj = new GameObject("Item Background");
        itemBackgroundObj.transform.SetParent(templateItemObj.transform, false);
        
        RectTransform itemBackgroundRect = itemBackgroundObj.AddComponent<RectTransform>();
        itemBackgroundRect.anchorMin = Vector2.zero;
        itemBackgroundRect.anchorMax = Vector2.one;
        itemBackgroundRect.offsetMin = Vector2.zero;
        itemBackgroundRect.offsetMax = Vector2.zero;
        
        Image itemBackgroundImage = itemBackgroundObj.AddComponent<Image>();
        itemBackgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        
        templateToggle.targetGraphic = itemBackgroundImage;
        
        // Item Label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(templateItemObj.transform, false);
        
        RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(10, 0);
        itemLabelRect.offsetMax = new Vector2(-10, 0);
        
        TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemLabel.text = "Option";
        itemLabel.fontSize = 14;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
        
        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        
        // 输入框（右侧，占据剩余空间）
        GameObject inputFieldObj = new GameObject("InputField");
        inputFieldObj.transform.SetParent(inputAreaObj.transform, false);
        
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
        
        // 添加主脚本到根对象
        TerminalMainView mainView = rootObj.AddComponent<TerminalMainView>();
        mainView.contentPanel = contentObj;
        mainView.panelRect = rootRect;
        mainView.titleText = titleText;
        mainView.closeButton = closeButton;
        mainView.scrollView = scrollRect;
        mainView.scrollContent = scrollContentRect;
        mainView.commandDropdown = dropdown;
        mainView.inputField = inputField;
        
        Log.Info("[TerminalMainView] Main panel created successfully");
        
        return mainView;
    }
}
