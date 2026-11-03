using System;
using Duckov.UI;
using Ducky.Sdk.Logging;
using TMPro;
using UnityEngine;

namespace Ducky.Sdk.GameUi;

/// <summary>
/// Base class for a panel that sits in the game's Tab bar next to the inventory, stats, quest and map views. It is a game
/// <see cref="View"/>, so opening it closes the previous view, hides the HUD, blocks character input and closes on Esc,
/// the same as the game's own views. Register a subclass with <see cref="GameUiTabs.Register{TView}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The class name must be unique.</b> The game highlights a tab by comparing the opened view's <c>GetType().Name</c>
/// with the tab's <c>viewTypeName</c>, so two views with the same class name (also across mods) would light each other's
/// tab. <see cref="GameUiTabs"/> refuses to install a name that is already taken.
/// </para>
/// <para>
/// <b>Main thread only.</b> All members run on the Unity main thread, called by the game's UI.
/// </para>
/// </remarks>
public abstract class GameUiTabView : View
{
    private GameObject? _panel;

    /// <summary>Title shown at the top of the panel. Defaults to the class name.</summary>
    protected virtual string Title => GetType().Name;

    /// <summary>Panel size in design units (the 2560 reference; the canvas scaler converts it).</summary>
    protected virtual Vector2 PanelSize => new Vector2(1100f, 700f);

    /// <summary>
    /// Builds the panel content once, the first time the view opens. <paramref name="content"/> is the area below the
    /// title, stretched to the panel.
    /// </summary>
    protected abstract void BuildContent(RectTransform content);

    /// <summary>Called each time the view opens, after <see cref="BuildContent"/>; refresh the shown data here.</summary>
    protected virtual void OnShown()
    {
    }

    /// <inheritdoc />
    protected override void OnOpen()
    {
        base.OnOpen();
        try
        {
            EnsurePanel();
            _panel!.SetActive(true);
            OnShown();
        }
        catch (Exception e)
        {
            Log.Error(e, "[GameUiTabView] {0} failed to open", GetType().Name);
        }
    }

    /// <inheritdoc />
    protected override void OnClose()
    {
        base.OnClose();
        if (_panel != null)
        {
            _panel.SetActive(false);
        }
    }

    private void EnsurePanel()
    {
        if (_panel != null)
        {
            return;
        }

        _panel = new GameObject("Panel", typeof(RectTransform));
        _panel.transform.SetParent(transform, false);
        var rect = (RectTransform)_panel.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = PanelSize;

        var fill = GameUiRounded.AddRounded(_panel, GameUiMetrics.RadiusPanel);
        if (fill != null)
        {
            fill.color = new Color(0.12f, 0.12f, 0.14f, 0.96f);
        }

        var rim = GameUiRounded.AddRim(_panel, GameUiMetrics.RadiusPanel, 2f);
        if (rim != null)
        {
            rim.color = new Color(0.85f, 0.70f, 0.30f, 1f);
        }

        var title = new GameObject("Title", typeof(RectTransform));
        title.transform.SetParent(_panel.transform, false);
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-48f, GameUiMetrics.Headline * 1.6f);
        titleRect.anchoredPosition = new Vector2(0f, -24f);
        var text = title.AddComponent<TextMeshProUGUI>();
        var font = GameUiFont.Get();
        if (font != null)
        {
            text.font = font;
        }

        text.text = Title;
        text.fontSize = GameUiMetrics.Headline;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(_panel.transform, false);
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(32f, 24f);
        contentRect.offsetMax = new Vector2(-32f, -(24f + GameUiMetrics.Headline * 1.6f + 16f));
        BuildContent(contentRect);
    }
}
