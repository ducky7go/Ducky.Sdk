using UnityEngine;

namespace Ducky.Sdk.GameUi;

/// <summary>How a tab added with <see cref="GameUiTabs.Register{TView}"/> looks and where it goes in the game's tab bar.</summary>
public sealed class GameUiTabOptions
{
    /// <summary>Name of the existing tab button that is cloned. The clone keeps its frame, hover and selection look.</summary>
    public string TemplateButtonName { get; set; } = "MapButton";

    /// <summary>Icon for the new tab. Null keeps the template's icon (then set <see cref="Tint"/> so it can be told apart).</summary>
    public Sprite? Icon { get; set; }

    /// <summary>Colour multiplied into the icon. Null keeps the template's colour.</summary>
    public Color? Tint { get; set; }

    /// <summary>Name of the tab button the new one goes after. Null puts it last. An unknown name also puts it last.</summary>
    public string? InsertAfter { get; set; }
}
