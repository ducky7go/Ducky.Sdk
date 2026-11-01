namespace Ducky.Sdk.GameEvents;

/// <summary>What the spawn wait does after one frame of checking an enemy.</summary>
internal enum ReadyStep
{
    /// <summary>Not ready yet; check again next frame.</summary>
    Wait,

    /// <summary>The AI controller and the main weapon-slot item exist: call the handlers once.</summary>
    Ready,

    /// <summary>The enemy is gone or the frame bound ran out: stop silently.</summary>
    GiveUp,

    /// <summary>The player's own team is not an enemy: never called.</summary>
    Skip,
}

/// <summary>
/// The readiness decision of <see cref="EnemyReady"/> on plain values. Touches no Unity object, so it is tested directly.
/// </summary>
internal static class EnemyReadiness
{
    /// <summary>How many frames an enemy may take to become ready before the wait gives up.</summary>
    internal const int DefaultMaxFrames = 10;

    /// <summary>
    /// The decision for one frame. A player character is skipped first; a missing or dead enemy stops the wait. An enemy is
    /// ready when its AI controller and its main weapon-slot item both exist. Otherwise the wait gives up once
    /// <paramref name="framesWaited"/> reaches <paramref name="maxFrames"/>.
    /// </summary>
    internal static ReadyStep Decide(bool isMainCharacter, bool alive, bool aiPresent, bool mainSlotItemPresent,
        int framesWaited, int maxFrames)
    {
        if (isMainCharacter)
        {
            return ReadyStep.Skip;
        }

        if (!alive)
        {
            return ReadyStep.GiveUp;
        }

        if (aiPresent && mainSlotItemPresent)
        {
            return ReadyStep.Ready;
        }

        return framesWaited >= maxFrames ? ReadyStep.GiveUp : ReadyStep.Wait;
    }

    /// <summary>The gun flag comes from the main weapon slot only; what the enemy holds in its hand does not count.</summary>
    internal static bool HasGun(bool mainSlotHoldsGun, bool holdingGun) => mainSlotHoldsGun;
}
