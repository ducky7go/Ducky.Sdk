using System;
using Ducky.Sdk.World;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The landmine owner rule and the option checks of <see cref="Grenades.Spawn"/>: owner none, player team and hostile team, each
/// with the requirement on and off and with a non-landmine, and invalid options.
/// </summary>
public class GrenadeRulesTests
{
    [Test]
    public void Landmine_WithRequirement_AndNoOwner_IsRefused()
    {
        GrenadeRules.ValidateLandmineOwner(isLandmine: true, requirePlayerTrigger: true, hasOwner: false, ownerHostileToPlayer: false)
            .ShouldNotBeNull();
    }

    [Test]
    public void Landmine_WithRequirement_AndPlayerTeamOwner_IsRefused()
    {
        GrenadeRules.ValidateLandmineOwner(isLandmine: true, requirePlayerTrigger: true, hasOwner: true, ownerHostileToPlayer: false)
            .ShouldNotBeNull();
    }

    [Test]
    public void Landmine_WithRequirement_AndHostileOwner_IsAccepted()
    {
        GrenadeRules.ValidateLandmineOwner(isLandmine: true, requirePlayerTrigger: true, hasOwner: true, ownerHostileToPlayer: true)
            .ShouldBeNull();
    }

    [Test]
    public void Landmine_WithoutRequirement_IsAcceptedForEveryOwner()
    {
        GrenadeRules.ValidateLandmineOwner(isLandmine: true, requirePlayerTrigger: false, hasOwner: false, ownerHostileToPlayer: false)
            .ShouldBeNull();
        GrenadeRules.ValidateLandmineOwner(isLandmine: true, requirePlayerTrigger: false, hasOwner: true, ownerHostileToPlayer: false)
            .ShouldBeNull();
    }

    [Test]
    public void NotALandmine_IsAcceptedWithoutAnOwner()
    {
        GrenadeRules.ValidateLandmineOwner(isLandmine: false, requirePlayerTrigger: true, hasOwner: false, ownerHostileToPlayer: false)
            .ShouldBeNull();
    }

    [Test]
    public void NegativeDelay_IsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => GrenadeRules.ValidateOptions(-1f, null, null, null));
    }

    [Test]
    public void NaNDamage_IsRejected()
    {
        Should.Throw<ArgumentException>(() => GrenadeRules.ValidateOptions(null, float.NaN, null, null));
    }

    [Test]
    public void UnsetOptions_AreAccepted()
    {
        Should.NotThrow(() => GrenadeRules.ValidateOptions(null, null, null, null));
    }
}
