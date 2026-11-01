using System;
using Ducky.Sdk.Combat;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The decisions of <see cref="LethalDamageGuard"/> (<see cref="LethalDamageRules"/>) on plain values, and the per-copy patch
/// identifier (<see cref="PatchIds"/>). The Harmony patch itself needs the game and is not exercised here.
/// </summary>
public class LethalDamageGuardTests
{
    [TestCase(0f, true)]
    [TestCase(-5f, true)]
    [TestCase(0.1f, false)]
    public void Lethal_IsZeroOrBelow(float written, bool expected) =>
        LethalDamageRules.IsLethal(written).ShouldBe(expected);

    [Test]
    public void InvincibleOrDeadTargets_AreSkipped()
    {
        LethalDamageRules.MayAct(targetInvincible: true, targetDead: false).ShouldBeFalse();
        LethalDamageRules.MayAct(targetInvincible: false, targetDead: true).ShouldBeFalse();
        LethalDamageRules.MayAct(targetInvincible: false, targetDead: false).ShouldBeTrue();
    }

    [Test]
    public void ShouldAsk_OnlyForALethalHitOnAGameTarget_Once()
    {
        LethalDamageRules.ShouldAsk(0f, alreadyAsked: false, targetInvincible: false, targetDead: false).ShouldBeTrue();
        LethalDamageRules.ShouldAsk(0f, alreadyAsked: true, targetInvincible: false, targetDead: false).ShouldBeFalse();
        LethalDamageRules.ShouldAsk(0f, alreadyAsked: false, targetInvincible: true, targetDead: false).ShouldBeFalse();
        LethalDamageRules.ShouldAsk(0f, alreadyAsked: false, targetInvincible: false, targetDead: true).ShouldBeFalse();
        LethalDamageRules.ShouldAsk(5f, alreadyAsked: false, targetInvincible: false, targetDead: false).ShouldBeFalse();
    }

    [TestCase(null, false)]
    [TestCase(0f, false)]
    [TestCase(-1f, false)]
    [TestCase(1f, true)]
    public void Rescue_SavesOnlyWithAPositiveHealth(float? keep, bool expected) =>
        LethalDamageRules.IsSaving(keep).ShouldBe(expected);

    [Test]
    public void PatchId_HasTheAssemblyFeatureAndInstance()
    {
        var instance = Guid.NewGuid();

        PatchIds.For("Ducky.Sample", "LethalDamageGuard", instance)
            .ShouldBe("Ducky.Sample.LethalDamageGuard." + instance.ToString("N"));
    }

    [Test]
    public void TwoInstances_ProduceDifferentPatchIds()
    {
        var first = PatchIds.For("Ducky.Sample", "LethalDamageGuard", Guid.NewGuid());
        var second = PatchIds.For("Ducky.Sample", "LethalDamageGuard", Guid.NewGuid());

        first.ShouldNotBe(second);
    }
}
