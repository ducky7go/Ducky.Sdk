using System;
using System.Collections.Generic;
using Ducky.Sdk.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ducky.MessageHubUI;

/// <summary>
/// Provider 过滤面板 - 当输入 # 时弹出
/// </summary>
internal class ProviderFilterPanel : MonoBehaviour
{
    [Header("UI组件")] [SerializeField] private GameObject? panel;
    [SerializeField] private RectTransform? itemsContainer;
    [SerializeField] private GameObject? itemTemplate;

    private List<CommandProvider> providers = new();
    private readonly List<GameObject> itemObjects = new();

    public event Action<int>? OnProviderSelected;

    /// <summary>
    /// 初始化组件
    /// </summary>
    private void Initialize()
    {
        Log.Info("[ProviderFilterPanel] Initialize() called");

        // 默认隐藏面板
        if (panel != null)
        {
            panel.SetActive(false);
            Log.Info("[ProviderFilterPanel] Panel hidden by default");
        }
    }

    /// <summary>
    /// 设置 Provider 列表并显示面板
    /// </summary>
    public void SetProviders(List<CommandProvider> newProviders, bool show)
    {
        providers = newProviders ?? new List<CommandProvider>();

        Log.Info($"[ProviderFilterPanel] SetProviders called with {providers.Count} providers, show: {show}");

        // 清空现有项
        ClearItems();

        if (itemTemplate == null || itemsContainer == null)
        {
            Log.Error(
                $"[ProviderFilterPanel] Cannot create items! itemTemplate null: {itemTemplate == null}, itemsContainer null: {itemsContainer == null}");
            return;
        }

        // 为每个 provider 创建一个列表项
        for (int i = 0; i < providers.Count; i++)
        {
            var provider = providers[i];
            var itemObj = CreateProviderItem(provider, i);
            itemObjects.Add(itemObj);
        }

        // 显示/隐藏面板
        if (panel != null)
        {
            panel.SetActive(show && providers.Count > 0);
        }
    }

    /// <summary>
    /// 创建一个 Provider 列表项
    /// </summary>
    private GameObject CreateProviderItem(CommandProvider provider, int index)
    {
        if (itemTemplate == null || itemsContainer == null)
        {
            Log.Error("[ProviderFilterPanel] Cannot create item: template or container is null");
            return new GameObject("ErrorItem");
        }

        var itemObj = Instantiate(itemTemplate, itemsContainer);
        itemObj.SetActive(true);

        // 设置显示文本
        var textComp = itemObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            string displayText =
                $"<size=16><color=#00FFFF>#{provider.ProviderId}</color></size> <size=12>{provider.DisplayName}</size>";
            textComp.text = displayText;
            textComp.richText = true;
            textComp.color = Color.white;
            textComp.alignment = TextAlignmentOptions.MidlineLeft;
            textComp.overflowMode = TextOverflowModes.Ellipsis;
            textComp.enableWordWrapping = false;
            textComp.raycastTarget = false;
            textComp.ForceMeshUpdate();
        }

        // 绑定点击事件
        var button = itemObj.GetComponent<Button>();
        if (button != null)
        {
            int capturedIndex = index;
            button.onClick.AddListener(() => OnItemClicked(capturedIndex));
        }

        return itemObj;
    }

    /// <summary>
    /// 清空所有列表项
    /// </summary>
    private void ClearItems()
    {
        foreach (var item in itemObjects)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        itemObjects.Clear();
    }

    /// <summary>
    /// 列表项被点击
    /// </summary>
    private void OnItemClicked(int index)
    {
        Log.Info($"[ProviderFilterPanel] Item {index} clicked");
        OnProviderSelected?.Invoke(index);

        // 隐藏面板
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>
    /// 创建过滤面板的工厂方法
    /// </summary>
    public static ProviderFilterPanel Create(Transform parent, float width)
    {
        Log.Info("[ProviderFilterPanel] Creating provider filter panel...");

        // 创建根对象（父容器是 ProviderSelector，与选择器弹窗使用相同的定位逻辑）
        var rootObj = new GameObject("ProviderFilterPanel");
        rootObj.transform.SetParent(parent, false);

        var rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0, 1); // 锚点在左上角（与 ProviderSelector 弹窗一致）
        rootRect.anchorMax = new Vector2(0, 1); // 锚点在左上角
        rootRect.pivot = new Vector2(0, 0); // 轴心在左下角（使面板左下角对齐选择器左上角）
        rootRect.anchoredPosition = new Vector2(0, 0); // 左下角与选择器左上角对齐（无偏移）
        rootRect.sizeDelta = new Vector2(width, 0);

        // ===== 面板容器 =====
        var panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(rootObj.transform, false);
        panelObj.SetActive(false); // 默认隐藏

        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(0, 0);
        panelRect.sizeDelta = new Vector2(width, 0);

        var panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.15f, 0.15f, 0.15f, 0.98f);

        // 添加 ContentSizeFitter 使面板高度自适应
        var panelFitter = panelObj.AddComponent<ContentSizeFitter>();
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Content（列表容器）
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(panelObj.transform, false);

        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(5, 5);
        contentRect.offsetMax = new Vector2(-5, -5);

        var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 2;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);

        var contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ===== 列表项模板 =====
        var itemTemplateObj = new GameObject("ItemTemplate");
        itemTemplateObj.transform.SetParent(contentObj.transform, false);
        itemTemplateObj.SetActive(false);

        var itemTemplateRect = itemTemplateObj.AddComponent<RectTransform>();
        itemTemplateRect.sizeDelta = new Vector2(0, 35);

        var itemLayoutElement = itemTemplateObj.AddComponent<LayoutElement>();
        itemLayoutElement.preferredHeight = 35;

        var itemImage = itemTemplateObj.AddComponent<Image>();
        itemImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var itemButton = itemTemplateObj.AddComponent<Button>();
        itemButton.targetGraphic = itemImage;

        // 设置按钮悬停效果
        var colors = itemButton.colors;
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        colors.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        itemButton.colors = colors;

        // 列表项文本
        var itemTextObj = new GameObject("Text");
        itemTextObj.transform.SetParent(itemTemplateObj.transform, false);

        var itemTextRect = itemTextObj.AddComponent<RectTransform>();
        itemTextRect.anchorMin = Vector2.zero;
        itemTextRect.anchorMax = Vector2.one;
        itemTextRect.offsetMin = new Vector2(10, 2);
        itemTextRect.offsetMax = new Vector2(-10, -2);

        var itemText = itemTextObj.AddComponent<TextMeshProUGUI>();
        itemText.text = "Item Text";
        itemText.fontSize = 14;
        itemText.color = Color.white;
        itemText.alignment = TextAlignmentOptions.Left;
        itemText.verticalAlignment = VerticalAlignmentOptions.Middle;
        itemText.richText = true;
        itemText.overflowMode = TextOverflowModes.Ellipsis;
        itemText.enableWordWrapping = false;
        itemText.raycastTarget = false;

        // 添加脚本组件
        var filterPanel = rootObj.AddComponent<ProviderFilterPanel>();
        filterPanel.panel = panelObj;
        filterPanel.itemsContainer = contentRect;
        filterPanel.itemTemplate = itemTemplateObj;

        // 初始化
        filterPanel.Initialize();

        Log.Info("[ProviderFilterPanel] Provider filter panel created successfully");

        return filterPanel;
    }
}
