using System;
using Ducky.Sdk.World;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The targeting table and argument checks of <see cref="Explosions.Create"/>: the four source and self-damage combinations,
/// zero radius, negative damage, NaN, infinity, and zero damage allowed.
/// </summary>
public class ExplosionRulesTests
{
    [Test]
    public void NoSource_NoSelfDamage_DoesNotExcludeAnyone()
    {
        ExplosionRules.ExcludesSourceTeam(hasSource: false, canHurtSelf: false).ShouldBeFalse();
    }

    [Test]
    public void NoSource_SelfDamage_DoesNotExcludeAnyone()
    {
        ExplosionRules.ExcludesSourceTeam(hasSource: false, canHurtSelf: true).ShouldBeFalse();
    }

    [Test]
    public void PlayerSource_NoSelfDamage_ExcludesThePlayersTeam()
    {
        ExplosionRules.ExcludesSourceTeam(hasSource: true, canHurtSelf: false).ShouldBeTrue();
    }

    [Test]
    public void PlayerSource_SelfDamage_DoesNotExcludeAnyone()
    {
        ExplosionRules.ExcludesSourceTeam(hasSource: true, canHurtSelf: true).ShouldBeFalse();
    }

    [Test]
    public void ZeroRadius_IsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ExplosionRules.Validate(0f, 10f));
    }

    [Test]
    public void NegativeDamage_IsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ExplosionRules.Validate(5f, -1f));
    }

    [Test]
    public void NaN_IsRejected()
    {
        Should.Throw<ArgumentException>(() => ExplosionRules.Validate(float.NaN, 10f));
        Should.Throw<ArgumentException>(() => ExplosionRules.Validate(5f, float.NaN));
    }

    [Test]
    public void Infinity_IsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => ExplosionRules.Validate(float.PositiveInfinity, 10f));
        Should.Throw<ArgumentOutOfRangeException>(() => ExplosionRules.Validate(5f, float.PositiveInfinity));
    }

    [Test]
    public void ZeroDamage_IsAccepted_ForTheFlashEffect()
    {
        Should.NotThrow(() => ExplosionRules.Validate(5f, 0f));
    }
}
