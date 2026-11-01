namespace Ducky.Sdk.Restore;

/// <summary>
/// A rule that rewrites the amount of one restoration before the game applies it. The game then scales the amount by
/// <c>1 + gain</c> itself, so a rule that halves a heal of 6 makes the game receive 3. A rule that throws is logged and
/// its change undone; the game still runs.
/// </summary>
/// <param name="value">The amount the game is about to restore. Changing it changes what the game uses.</param>
/// <param name="context">What is known about this restoration, read-only.</param>
public delegate void RestoreRule(ref float value, in RestoreContext context);
