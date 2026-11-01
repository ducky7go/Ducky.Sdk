namespace Ducky.Sdk.Combat;

/// <summary>
/// The decisions of <see cref="LethalDamageGuard"/> on plain values. Touches no Unity object, so it is tested directly.
/// </summary>
internal static class LethalDamageRules
{
    /// <summary>True when the health the game is about to write is zero or below, so the hit would kill.</summary>
    internal static bool IsLethal(float writtenHealth) => writtenHealth <= 0f;

    /// <summary>The game ignores damage to an invincible or dead character, so the guard must not act on it.</summary>
    internal static bool MayAct(bool targetInvincible, bool targetDead) => !targetInvincible && !targetDead;

    /// <summary>
    /// True when the rescues should be asked: the write is lethal, this hit has not been offered to them yet, and the
    /// target is one the game would hurt.
    /// </summary>
    internal static bool ShouldAsk(float writtenHealth, bool alreadyAsked, bool targetInvincible, bool targetDead) =>
        IsLethal(writtenHealth) && !alreadyAsked && MayAct(targetInvincible, targetDead);

    /// <summary>A rescue saves the character only with a health above zero; null or zero declines.</summary>
    internal static bool IsSaving(float? keep) => keep is > 0f;
}
