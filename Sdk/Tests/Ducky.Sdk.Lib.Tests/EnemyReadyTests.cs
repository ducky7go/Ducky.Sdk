using Ducky.Sdk.GameEvents;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The readiness decision of <see cref="EnemyReady"/> (<see cref="EnemyReadiness"/>) on plain values. The frame loop on the
/// main thread and the game's spawn hook need the game and are not exercised here.
/// </summary>
public class EnemyReadyTests
{
    private const int Max = EnemyReadiness.DefaultMaxFrames;

    [Test]
    public void DefaultBound_IsTenFrames() => Max.ShouldBe(10);

    [Test]
    public void NotReadyAtSpawn_WhileTheWaitContinues()
    {
        EnemyReadiness.Decide(isMainCharacter: false, alive: true, aiPresent: false, mainSlotItemPresent: false,
            framesWaited: 0, maxFrames: Max).ShouldBe(ReadyStep.Wait);
    }

    [Test]
    public void ReadyWhenAiAndTheMainSlotItemExist()
    {
        EnemyReadiness.Decide(false, true, aiPresent: true, mainSlotItemPresent: true, framesWaited: 1, maxFrames: Max)
            .ShouldBe(ReadyStep.Ready);
    }

    [Test]
    public void OnlyOnePartPresent_IsNotReady()
    {
        EnemyReadiness.Decide(false, true, aiPresent: true, mainSlotItemPresent: false, framesWaited: 1, maxFrames: Max)
            .ShouldBe(ReadyStep.Wait);
        EnemyReadiness.Decide(false, true, aiPresent: false, mainSlotItemPresent: true, framesWaited: 1, maxFrames: Max)
            .ShouldBe(ReadyStep.Wait);
    }

    [Test]
    public void Timeout_GivesUpAtTheBound()
    {
        EnemyReadiness.Decide(false, true, false, false, framesWaited: Max - 1, maxFrames: Max).ShouldBe(ReadyStep.Wait);
        EnemyReadiness.Decide(false, true, false, false, framesWaited: Max, maxFrames: Max).ShouldBe(ReadyStep.GiveUp);
    }

    [Test]
    public void DestroyedEnemy_StopsSilently()
    {
        EnemyReadiness.Decide(false, alive: false, aiPresent: true, mainSlotItemPresent: true, framesWaited: 0, maxFrames: Max)
            .ShouldBe(ReadyStep.GiveUp);
    }

    [Test]
    public void PlayerTeam_IsSkipped()
    {
        EnemyReadiness.Decide(isMainCharacter: true, alive: true, aiPresent: true, mainSlotItemPresent: true,
            framesWaited: 0, maxFrames: Max).ShouldBe(ReadyStep.Skip);
    }

    [Test]
    public void GunInTheMainSlot_ButNotHeld_IsReportedAsAGun()
    {
        EnemyReadiness.HasGun(mainSlotHoldsGun: true, holdingGun: false).ShouldBeTrue();
    }

    [Test]
    public void GunHeldOutsideTheMainSlot_IsNotReportedAsAGun()
    {
        EnemyReadiness.HasGun(mainSlotHoldsGun: false, holdingGun: true).ShouldBeFalse();
    }

    [Test]
    public void EmptyMainSlot_HasNoGun()
    {
        EnemyReadiness.HasGun(mainSlotHoldsGun: false, holdingGun: false).ShouldBeFalse();
    }
}
