using Ducky.Sdk.GameUi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ducky.GameUiTabSample;

/// <summary>The demo panel: lists the player's attributes and copies a brief text version to the clipboard.</summary>
internal sealed class PlayerAttributesView : GameUiTabView
{
    private const int LinesPerColumn = 18;
    private const int MaxColumns = 4;
    private const int MaxLabelChars = 19;

    private TextMeshProUGUI? _vitals;
    private readonly List<TextMeshProUGUI> _columns = new();
    private TextMeshProUGUI? _status;

    protected override string Title => "Player Attributes";

    protected override void BuildContent(RectTransform content)
    {
        // One row of vitals on top, the stats in columns below.
        _vitals = AddText(content, "Vitals", new Vector2(0f, 1f), new Vector2(1f, 1f), TextAlignmentOptions.TopLeft, GameUiMetrics.Caption,
            new Vector2(0f, -56f), Vector2.zero);
        for (var i = 0; i < MaxColumns; i++)
        {
            var min = i * 0.25f;
            _columns.Add(AddText(content, "Stats" + i, new Vector2(min, 0f), new Vector2(min + 0.24f, 1f), TextAlignmentOptions.TopLeft,
                GameUiMetrics.Micro, new Vector2(0f, 72f), new Vector2(0f, -68f)));
        }

        AddCopyButton(content);
    }

    protected override void OnShown()
    {
        Refresh();
    }

    private void Refresh()
    {
        var snapshot = PlayerAttributeSnapshot.Capture();
        if (snapshot == null)
        {
            _vitals!.text = "No player";
            foreach (var c in _columns) c.text = string.Empty;
            return;
        }

        _vitals!.text = "<b>Vitals</b>   " + string.Join("    ", snapshot.Vitals.Select(v => v.Label + " " + v.Value));
        for (var i = 0; i < _columns.Count; i++)
        {
            var lines = snapshot.Stats.Skip(i * LinesPerColumn).Take(LinesPerColumn)
                .Select(s => Line(s.Label, s.Value, s.InBrief));
            _columns[i].text = (i == 0 ? "<b>Stats</b> (" + snapshot.Stats.Count + ")\n" : string.Empty) + string.Join("\n", lines);
        }
    }

    // The stats that the brief text includes are gold, so it is visible what the copy button takes.
    private static string Line(string label, string value, bool inBrief)
    {
        var text = (label.Length > MaxLabelChars ? label.Substring(0, MaxLabelChars - 1) + "…" : label) + ": " + value;
        return inBrief ? "<color=#E8B84A>" + text + "</color>" : text;
    }

    private void CopyBrief()
    {
        var snapshot = PlayerAttributeSnapshot.Capture();
        if (snapshot == null)
        {
            _status!.text = "No player to copy";
            return;
        }

        var text = snapshot.ToBriefText();
        GUIUtility.systemCopyBuffer = text;
        _status!.text = "Copied " + text.Length + " chars";
        Refresh();
    }

    private static TextMeshProUGUI AddText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions align,
        float fontSize, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var text = go.AddComponent<TextMeshProUGUI>();
        var font = GameUiFont.Get();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = align;
        text.enableWordWrapping = false;
        // Not Truncate: with wrapping off, TMP's Truncate drops every line after the first one that is too wide.
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void AddCopyButton(RectTransform content)
    {
        var go = new GameObject("CopyButton", typeof(RectTransform));
        go.transform.SetParent(content, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(320f, 56f);
        rect.anchoredPosition = Vector2.zero;

        var image = GameUiRounded.AddRounded(go, GameUiMetrics.RadiusControl);
        if (image == null) image = go.AddComponent<Image>();
        image.color = new Color(0.62f, 0.46f, 0.16f, 1f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;
        button.onClick.AddListener(CopyBrief);

        var label = new GameObject("Label", typeof(RectTransform));
        label.transform.SetParent(go.transform, false);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        var text = label.AddComponent<TextMeshProUGUI>();
        var font = GameUiFont.Get();
        if (font != null) text.font = font;
        text.text = "Copy brief text";
        text.fontSize = GameUiMetrics.Body;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        var status = new GameObject("Status", typeof(RectTransform));
        status.transform.SetParent(content, false);
        var statusRect = (RectTransform)status.transform;
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(1f, 0f);
        statusRect.sizeDelta = new Vector2(500f, 56f);
        statusRect.anchoredPosition = new Vector2(-340f, 0f);
        _status = status.AddComponent<TextMeshProUGUI>();
        if (font != null) _status.font = font;
        _status.fontSize = GameUiMetrics.Body;
        _status.color = new Color(0.7f, 1f, 0.7f, 1f);
        _status.alignment = TextAlignmentOptions.MidlineRight;
        _status.raycastTarget = false;
    }
}
