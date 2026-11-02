using System;
using System.IO;
using Ducky.Sdk.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace Ducky.Sdk.GameUi;

/// <summary>
/// Rounded panels, fills and borders for game-native mod UI. Uses the game's rounded graphic when it is available, and
/// falls back to ordinary <see cref="Image"/> and <see cref="Outline"/> components when it is not (a game update that
/// renamed or removed it, or a build without the game's <c>Plugins.dll</c>), so a mod never crashes on this.
/// </summary>
/// <remarks>
/// <para>
/// <b>Main thread only.</b> Every method here uses Unity objects; call it from the Unity main thread, not from a thread-pool
/// continuation.
/// </para>
/// <para>
/// <b>Radius units.</b> Radii are in the game's 2560-reference design units. They are passed through unchanged; the canvas
/// scaler converts them to pixels, so do not multiply by the screen scale. Use <see cref="GameUiMetrics"/> radius tiers
/// (16, 11, 8, 6) as recommended conventions, not as values measured from the game.
/// </para>
/// <para>
/// <b>Reporting.</b> The first fallback in a session writes one warning through <see cref="Log"/>; later fallbacks are silent.
/// </para>
/// <para>
/// <b>No retained state.</b> Nothing is cached and no event is subscribed, so there is nothing to release in <c>ModDisabled</c>.
/// </para>
/// </remarks>
public static class GameUiRounded
{
    private const string RimName = "Rim";

    private static bool _warned;

    /// <summary>
    /// Diagnostic switch. When true, <see cref="AddRounded"/> and <see cref="AddRim"/> always use the ordinary-graphic
    /// fallback even though the game's rounded graphic is available. Use it to check the fallback look in the real game; set
    /// it back to false to restore the rounded look. Main thread only.
    /// </summary>
    public static bool ForceFallback { get; set; }

    /// <summary>
    /// Turns <paramref name="target"/> into a solid rounded rectangle with the corner radius <paramref name="radius"/> (design
    /// units at the 2560 reference). Calling it again on the same object updates the radius and keeps one rounded graphic.
    /// </summary>
    /// <remarks>
    /// If <paramref name="target"/> carries another kind of <see cref="Graphic"/> (for example a <see cref="Text"/>), that
    /// component is removed with <c>DestroyImmediate</c> so the rounded graphic can be added in the same frame. Call this on a
    /// fresh object when another component still needs that graphic, because a reference to the removed component becomes
    /// invalid.
    /// </remarks>
    /// <param name="target">The object to round. A null target returns null and does nothing.</param>
    /// <param name="radius">The corner radius in 2560-reference design units.</param>
    /// <returns>The graphic that was added or updated (set its colour and raycast behaviour on it), or the plain
    /// <see cref="Image"/> in the fallback. Null only when <paramref name="target"/> is null.</returns>
    public static Image? AddRounded(GameObject target, float radius)
    {
        if (target == null)
        {
            return null;
        }

        if (!ForceFallback)
        {
            var rounded = TryAddRounded(target, radius);
            if (rounded != null)
            {
                return rounded;
            }
        }
        else
        {
            WarnOnce("ForceFallback is on.");
        }

        return AddPlainImage(target);
    }

    /// <summary>
    /// Adds a border-only rounded outline to <paramref name="host"/> as a child object named <c>Rim</c> that follows the
    /// host's corners. The child is stretched to the host, excluded from layout, and does not block clicks, so it never
    /// covers the host's content with a fill.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Calling it again replaces the earlier <c>Rim</c> child, so a host has one rim.
    /// </para>
    /// <para>
    /// In the fallback the rounded rim cannot be drawn. The host gets a square <see cref="Outline"/> instead (only when the
    /// host already has a <see cref="Graphic"/>, because an outline needs one), and no filled graphic is added. The outline's
    /// colour is the default black; read it with <c>GetComponent&lt;Outline&gt;()</c> if you need another colour. The method
    /// returns null in the fallback.
    /// </para>
    /// <para>Main thread only; the radius is in 2560-reference design units, as for <see cref="AddRounded"/>.</para>
    /// </remarks>
    /// <param name="host">The panel to outline. A null host returns null and does nothing.</param>
    /// <param name="radius">The corner radius in 2560-reference design units.</param>
    /// <param name="borderWidth">The border thickness in design units. Must be greater than zero for a border-only rim.</param>
    /// <returns>The rim graphic, so the caller can set its colour, or null when the fallback was used.</returns>
    public static Image? AddRim(GameObject host, float radius, float borderWidth = 2f)
    {
        if (host == null)
        {
            return null;
        }

        if (!ForceFallback)
        {
            var rim = TryAddRim(host, radius, borderWidth);
            if (rim != null)
            {
                return rim;
            }
        }
        else
        {
            WarnOnce("ForceFallback is on.");
        }

        AddSquareOutline(host, borderWidth);
        return null;
    }

    private static Image? TryAddRounded(GameObject target, float radius)
    {
        if (!ProceduralImageBridge.Compiled)
        {
            WarnOnce("The rounded graphics are not compiled into this build.");
            return null;
        }

        try
        {
            var existing = ProceduralImageBridge.FindFill(target);
            if (existing != null)
            {
                ProceduralImageBridge.SetRadius(existing, radius);
                return existing;
            }

            RemoveOtherGraphics(target);
            return ProceduralImageBridge.CreateFill(target, radius);
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            RemoveRoundedGraphics(target);
            WarnOnce(ex);
            return null;
        }
    }

    private static Image? TryAddRim(GameObject host, float radius, float borderWidth)
    {
        if (!ProceduralImageBridge.Compiled)
        {
            WarnOnce("The rounded graphics are not compiled into this build.");
            return null;
        }

        GameObject? rimObject = null;
        try
        {
            var old = host.transform.Find(RimName);
            if (old != null)
            {
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            rimObject = new GameObject(RimName, typeof(RectTransform));
            rimObject.transform.SetParent(host.transform, false);
            var rect = (RectTransform)rimObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rimObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var image = ProceduralImageBridge.CreateRim(rimObject, radius, borderWidth);
            image.raycastTarget = false;
            return image;
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            if (rimObject != null)
            {
                UnityEngine.Object.DestroyImmediate(rimObject);
            }

            WarnOnce(ex);
            return null;
        }
    }

    private static Image AddPlainImage(GameObject target)
    {
        RemoveRoundedGraphics(target);
        var image = target.GetComponent<Image>();
        if (image != null)
        {
            return image;
        }

        RemoveOtherGraphics(target);
        return target.AddComponent<Image>();
    }

    private static void AddSquareOutline(GameObject host, float borderWidth)
    {
        if (host.GetComponent<Graphic>() == null)
        {
            return;
        }

        var outline = host.GetComponent<Outline>() ?? host.AddComponent<Outline>();
        outline.effectDistance = new Vector2(borderWidth, -borderWidth);
    }

    private static void RemoveOtherGraphics(GameObject target)
    {
        foreach (var graphic in target.GetComponents<Graphic>())
        {
            UnityEngine.Object.DestroyImmediate(graphic);
        }
    }

    private static void RemoveRoundedGraphics(GameObject target)
    {
        try
        {
            ProceduralImageBridge.Cleanup(target);
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            // The rounded graphic cannot be touched at all; the caller already reports the fallback.
        }
    }

    private static bool IsUnavailable(Exception ex) =>
        ex is TypeLoadException or MissingMemberException or FileNotFoundException or FileLoadException;

    private static void WarnOnce(Exception ex) => WarnOnce(ex.GetType().Name + ": " + ex.Message);

    private static void WarnOnce(string reason)
    {
        if (_warned)
        {
            return;
        }

        _warned = true;
        Log.Warn("[GameUi] Rounded game graphics are unavailable, using plain graphics instead. " + reason);
    }
}
