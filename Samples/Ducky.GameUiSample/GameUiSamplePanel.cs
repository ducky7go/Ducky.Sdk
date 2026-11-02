using Ducky.Sdk.GameUi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ducky.GameUiSample;

/// <summary>
/// Builds the verification panel: a canvas scaled to the game, a rounded panel with a rim and a raised-shadow sibling behind it,
/// the type ladder in the game font, and one button in selected, hovered and disabled looks.
/// </summary>
internal static class GameUiSamplePanel
{
    private static readonly Color PanelFill = new Color(0.12f, 0.12f, 0.14f, 0.96f);
    private static readonly Color PanelRim = new Color(0.85f, 0.70f, 0.30f, 1f);
    private static readonly Color ShadowFill = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color ButtonNormal = new Color(0.22f, 0.22f, 0.26f, 1f);
    private static readonly Color ButtonHover = new Color(0.36f, 0.36f, 0.42f, 1f);
    private static readonly Color ButtonAccent = new Color(0.62f, 0.46f, 0.16f, 1f);
    private static readonly Color ButtonDisabled = new Color(0.14f, 0.14f, 0.15f, 0.7f);

    /// <summary>
    /// Verification handle only: the "Hover me" button, so a check can drive its highlighted state through
    /// Selectable.DoStateTransition (DebugMod research/invoke). Not used by the panel itself.
    /// </summary>
    public static Button? DebugHoverButton;

    private static readonly Vector2 PanelSize = new Vector2(900f, 580f);
    private static readonly Vector2 ShadowOffset = new Vector2(0f, -10f);

    /// <summary>Creates the panel under a new canvas. Call it each time the UI opens.</summary>
    internal static GameObject Build()
    {
        var root = new GameObject("Ducky.GameUiSample", typeof(RectTransform));
        // The mod is enabled on the title screen; without this the canvas is destroyed by the next scene load.
        // ModDisabled destroys it again, so hot reload does not leave a copy behind.
        UnityEngine.Object.DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        // Scale the same way as the game's own canvases (task 4.1).
        GameUiScaler.Apply(root.AddComponent<CanvasScaler>());
        root.AddComponent<GraphicRaycaster>();

        var font = GameUiFont.Get();

        // The shadow is a rounded sibling created first, so it is drawn behind the panel.
        var shadow = CreateRect(root, "Shadow", PanelSize, ShadowOffset);
        AddRoundedFill(shadow, ShadowFill, GameUiMetrics.RadiusPanel);

        var panel = CreateRect(root, "Panel", PanelSize, Vector2.zero);
        AddRoundedFill(panel, PanelFill, GameUiMetrics.RadiusPanel);
        AddPanelRim(panel);

        AddText(panel, "Display", "12:30", GameUiMetrics.Display, new Vector2(0f, -40f), font, TextAlignmentOptions.Top);
        AddText(panel, "Headline", "Escape from Duckov, mod UI check", GameUiMetrics.Headline, new Vector2(0f, -110f), font,
            TextAlignmentOptions.Top);
        // Left-aligned lines: a 600-wide box centred at x=-130 starts at x=-430, inside the 900-wide panel (edge at -450).
        AddText(panel, "Title", "Item name, bold in the game", GameUiMetrics.Title, new Vector2(-130f, -160f), font,
            TextAlignmentOptions.TopLeft, 600f);
        AddText(panel, "Body", "Label and list row text", GameUiMetrics.Body, new Vector2(-130f, -200f), font,
            TextAlignmentOptions.TopLeft, 600f);
        AddText(panel, "Caption", "Description and weights", GameUiMetrics.Caption, new Vector2(-130f, -235f), font,
            TextAlignmentOptions.TopLeft, 600f);
        AddText(panel, "Micro", "Stack count 16", GameUiMetrics.Micro, new Vector2(-130f, -265f), font,
            TextAlignmentOptions.TopLeft, 600f);

        AddButton(panel, "SelectedLook", "Selected", new Vector2(-240f, -380f), interactable: true, selectedLook: true, font);
        AddButton(panel, "HoverLook", "Hover me", new Vector2(0f, -380f), interactable: true, selectedLook: false, font);
        AddButton(panel, "DisabledLook", "Disabled", new Vector2(240f, -380f), interactable: false, selectedLook: false, font);

        return root;
    }

    private static GameObject CreateRect(GameObject parent, string name, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return go;
    }

    private static void AddRoundedFill(GameObject target, Color color, float radius)
    {
        var image = GameUiRounded.AddRounded(target, radius);
        if (image != null)
        {
            image.color = color;
        }
    }

    private static void AddPanelRim(GameObject panel)
    {
        var rim = GameUiRounded.AddRim(panel, GameUiMetrics.RadiusPanel, 2f);
        if (rim != null)
        {
            rim.color = PanelRim;
            return;
        }

        // Fallback: a square outline on the panel (the rounded rim is not available). Colour it like the rim.
        var outline = panel.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = PanelRim;
        }
    }

    private static void AddText(GameObject parent, string name, string text, float size, Vector2 position,
        TMP_FontAsset? font, TextAlignmentOptions alignment, float width = 860f)
    {
        var go = CreateRect(parent, name, new Vector2(width, size * 1.6f), position);
        var label = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
        }

        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = alignment;
        label.raycastTarget = false;
    }

    private static void AddButton(GameObject parent, string name, string label, Vector2 position, bool interactable,
        bool selectedLook, TMP_FontAsset? font)
    {
        var go = CreateRect(parent, name, new Vector2(300f, 64f), position);
        var image = GameUiRounded.AddRounded(go, GameUiMetrics.RadiusControl);
        if (image == null)
        {
            return;
        }

        image.color = Color.white;
        var button = go.AddComponent<Button>();
        if (label == "Hover me")
        {
            DebugHoverButton = button;
        }

        button.targetGraphic = image;
        button.interactable = interactable;

        var colors = ColorBlock.defaultColorBlock;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        colors.normalColor = selectedLook ? ButtonAccent : ButtonNormal;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = new Color(0.14f, 0.14f, 0.17f, 1f);
        colors.selectedColor = ButtonAccent;
        colors.disabledColor = ButtonDisabled;
        button.colors = colors;

        AddText(go, "Label", label, GameUiMetrics.Body, Vector2.zero, font, TextAlignmentOptions.Center, width: 300f);
    }
}
