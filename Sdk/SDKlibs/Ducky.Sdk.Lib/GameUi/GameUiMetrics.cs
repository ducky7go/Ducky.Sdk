using System;

namespace Ducky.Sdk.GameUi;

/// <summary>
/// The game's own UI numbers, measured in the running game (2026-11-01, 2880x1800): the game's canvases use a
/// CanvasScaler of 2560x1440 with MatchWidthOrHeight at 0 (the width decides), so the scale factor is
/// <c>screen width / 2560</c> (1.125 at 2880, 1.5 at 3840). A mod canvas must use the same scaler, or one design unit
/// is a different number of pixels than in the game. The type ladder is the one the game's own texts use, in the same
/// design units. The radius tiers are recommended conventions, not measured game values.
/// </summary>
/// <remarks>
/// Plain values with no Unity dependency, so they can be read and tested without a running game.
/// </remarks>
public static class GameUiMetrics
{
    /// <summary>The game's canvas reference width, in design units.</summary>
    public const float ReferenceWidth = 2560f;

    /// <summary>The game's canvas reference height, in design units.</summary>
    public const float ReferenceHeight = 1440f;

    /// <summary>The game's MatchWidthOrHeight: 0 means the width decides the scale.</summary>
    public const float MatchWidthOrHeight = 0f;

    /// <summary>Clock, countdown and big numbers (36).</summary>
    public const float Display = 36f;

    /// <summary>Window and section titles (28).</summary>
    public const float Headline = 28f;

    /// <summary>Names and the title of a card, bold in the game (24).</summary>
    public const float Title = 24f;

    /// <summary>The default text: labels, list rows and buttons (20).</summary>
    public const float Body = 20f;

    /// <summary>Descriptions, weights, hints and secondary values (18).</summary>
    public const float Caption = 18f;

    /// <summary>The smallest text the game uses: stack counts, ids and tags (16). Nothing goes below this.</summary>
    public const float Micro = 16f;

    /// <summary>Recommended corner radius for a panel, in 2560-reference design units. A convention, not a measured value.</summary>
    public const float RadiusPanel = 16f;

    /// <summary>Recommended corner radius for a group inside a panel, in design units. A convention, not a measured value.</summary>
    public const float RadiusGroup = 11f;

    /// <summary>Recommended corner radius for a control such as a button, in design units. A convention, not a measured value.</summary>
    public const float RadiusControl = 8f;

    /// <summary>Recommended corner radius for a tag or small badge, in design units. A convention, not a measured value.</summary>
    public const float RadiusTag = 6f;

    /// <summary>
    /// The factor Unity's CanvasScaler (ScaleWithScreenSize) applies: the width and height ratios to the reference are
    /// blended on a log2 scale by <paramref name="match"/> (0 = width, 1 = height). <paramref name="match"/> is clamped
    /// to 0..1, and a NaN match is treated as 0.
    /// </summary>
    /// <returns>The scale factor, or 1 when any dimension is zero, negative or not a number.</returns>
    public static float ScaleFor(float screenWidth, float screenHeight, float referenceWidth = ReferenceWidth,
        float referenceHeight = ReferenceHeight, float match = MatchWidthOrHeight)
    {
        if (IsInvalid(screenWidth) || IsInvalid(screenHeight) || IsInvalid(referenceWidth) || IsInvalid(referenceHeight))
        {
            return 1f;
        }

        var clampedMatch = float.IsNaN(match) ? 0f : Math.Min(Math.Max(match, 0f), 1f);
        var logWidth = Math.Log(screenWidth / referenceWidth, 2);
        var logHeight = Math.Log(screenHeight / referenceHeight, 2);
        var blended = logWidth + (logHeight - logWidth) * clampedMatch;
        return (float)Math.Pow(2, blended);
    }

    private static bool IsInvalid(float value) => float.IsNaN(value) || value <= 0f;
}
