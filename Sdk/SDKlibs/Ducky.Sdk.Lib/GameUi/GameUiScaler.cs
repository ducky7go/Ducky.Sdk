using System;
using Ducky.Sdk.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace Ducky.Sdk.GameUi;

/// <summary>
/// Gives a mod canvas the game's own scaling, so a size written in design units in the mod is the same size on screen as
/// the same number in the game's UI. Follows a live game canvas when one exists, and falls back to the measured constants in
/// <see cref="GameUiMetrics"/> when none is up yet (the title screen, a canvas that is still loading) or when it cannot be read.
/// </summary>
/// <remarks>
/// <para>
/// <b>Main thread only.</b> The lookup searches the scene and sets Unity properties, so call <see cref="Apply"/> on the Unity
/// main thread.
/// </para>
/// <para>
/// <b>Re-apply when the UI opens, not per frame.</b> Call <see cref="Apply"/> each time a mod UI opens, so a changed game
/// scaler is picked up. The lookup scans the scene (<c>FindObjectsOfType</c>), which is acceptable at UI-open frequency and
/// too costly to run every frame.
/// </para>
/// <para>
/// <b>No retained state.</b> Nothing is cached and no event is subscribed, so a mod's <c>ModDisabled</c> has nothing to undo
/// for this class.
/// </para>
/// </remarks>
public static class GameUiScaler
{
    // The game's two always-present gameplay canvases carry the scaler that the whole HUD and every view use.
    private static readonly string[] GameCanvasNames = { "HUDCanvas", "GameplayUICanvas" };

    private static bool _warned;

    /// <summary>
    /// Sets <paramref name="target"/> to scale with screen size, using the reference resolution and match of the game's live
    /// gameplay canvas scaler, or the measured values from <see cref="GameUiMetrics"/> when no such scaler is available.
    /// </summary>
    /// <remarks>
    /// With the game's match of 0 the scale is the screen width divided by 2560 (1.125 at 2880×1800, whatever the height). If the game's
    /// scaler exists but cannot be read, one warning is written through <see cref="Log"/> and the measured values are used.
    /// Passing a null target does nothing.
    /// </remarks>
    /// <param name="target">The mod's canvas scaler. It is never used as the source of the game's values.</param>
    public static void Apply(CanvasScaler target)
    {
        if (target == null)
        {
            return;
        }

        var reference = new Vector2(GameUiMetrics.ReferenceWidth, GameUiMetrics.ReferenceHeight);
        var match = GameUiMetrics.MatchWidthOrHeight;
        try
        {
            var live = FindGameScaler(target);
            if (live != null)
            {
                reference = live.referenceResolution;
                match = live.matchWidthOrHeight;
            }
        }
        catch (Exception ex)
        {
            WarnOnce(ex);
        }

        target.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        target.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        target.referenceResolution = reference;
        target.matchWidthOrHeight = match;
    }

    private static CanvasScaler? FindGameScaler(CanvasScaler self)
    {
        foreach (var scaler in UnityEngine.Object.FindObjectsOfType<CanvasScaler>())
        {
            if (scaler == null || scaler == self || !scaler.isActiveAndEnabled
                || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                continue;
            }

            if (Array.IndexOf(GameCanvasNames, scaler.gameObject.name) >= 0)
            {
                return scaler;
            }
        }

        return null;
    }

    private static void WarnOnce(Exception ex)
    {
        if (_warned)
        {
            return;
        }

        _warned = true;
        Log.Warn(ex, "[GameUi] The game's canvas scaler could not be read, using the measured values instead.");
    }
}
