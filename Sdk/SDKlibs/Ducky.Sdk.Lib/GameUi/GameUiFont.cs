using System;
using Duckov.Utilities;
using Ducky.Sdk.Logging;
using TMPro;

namespace Ducky.Sdk.GameUi;

/// <summary>
/// The one place a mod gets the TMP font the game's own UI text uses, so mod text matches the game. The font comes from the
/// game's UI style template text (<c>GameplayDataSettings.UIStyle.TemplateTextUGUI.font</c>). When that cannot be resolved,
/// the TextMesh Pro default font is returned instead, so a game update that moves the font never breaks a mod.
/// </summary>
/// <remarks>
/// <para>
/// <b>Main thread only.</b> <see cref="Get"/> reads game settings and Unity objects, so call it on the Unity main thread.
/// </para>
/// <para>
/// <b>Call it when you need the font, not per frame.</b> The game font is cached once it has been found, and a destroyed cached
/// font is looked up again. A fallback is not cached, so the game font is picked up as soon as it becomes available. Call
/// <see cref="Get"/> when a UI opens or when you build text, not in <c>Update</c>.
/// </para>
/// <para>
/// <b>No static initialisation.</b> The cache is filled only inside <see cref="Get"/>, never in a static constructor or field
/// initialiser, so loading this type before the game's UI style exists is safe.
/// </para>
/// </remarks>
public static class GameUiFont
{
    private static TMP_FontAsset? _gameFont;
    private static bool _warned;

    /// <summary>
    /// Returns the game's UI text font, or the TextMesh Pro default font when the game's font cannot be resolved.
    /// </summary>
    /// <remarks>
    /// The first fallback in a session writes one warning through <see cref="Log"/>. The method does not throw.
    /// </remarks>
    /// <returns>The font to use for mod text. Null only if TextMesh Pro itself has no default font.</returns>
    public static TMP_FontAsset? Get()
    {
        // Unity's overloaded comparison treats a destroyed asset (for example after a scene change) as null.
        if (_gameFont != null)
        {
            return _gameFont;
        }

        var (font, reason) = TryGameFont();
        if (font != null)
        {
            _gameFont = font;
            return font;
        }

        WarnOnce(reason);
        return TryDefaultFont();
    }

    private static (TMP_FontAsset? font, string reason) TryGameFont()
    {
        try
        {
            var style = GameplayDataSettings.UIStyle;
            if (style == null)
            {
                return (null, "The game's UI style is not loaded yet.");
            }

            var template = style.TemplateTextUGUI;
            if (template == null)
            {
                return (null, "The game's UI template text is missing.");
            }

            var font = template.font;
            return font == null ? (null, "The game's UI template text has no font.") : (font, string.Empty);
        }
        catch (Exception ex)
        {
            return (null, ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static TMP_FontAsset? TryDefaultFont()
    {
        try
        {
            return TMPro.TMP_Settings.defaultFontAsset;
        }
        catch (Exception ex)
        {
            WarnOnce(ex.GetType().Name + ": " + ex.Message);
            return null;
        }
    }

    private static void WarnOnce(string reason)
    {
        if (_warned)
        {
            return;
        }

        _warned = true;
        Log.Warn("[GameUi] The game's UI font could not be resolved, using the TextMesh Pro default font. " + reason);
    }
}
