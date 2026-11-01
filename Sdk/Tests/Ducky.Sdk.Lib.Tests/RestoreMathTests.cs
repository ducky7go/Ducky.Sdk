using System;
using Ducky.Sdk.Restore;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The restoration arithmetic (<see cref="RestoreMath"/>) and the origin scope (<see cref="RestoreOriginScope"/>), with
/// the examples of the notes.
/// </summary>
public class RestoreMathTests
{
    private static void ShouldBeApprox(float actual, float expected) =>
        Math.Abs(actual - expected).ShouldBeLessThan(0.0001f);

    [Test]
    public void Overflow_Aspirin_IsThree()
    {
        ShouldBeApprox(RestoreMath.Overflow(current: 70, amount: 10, gain: 0, max: 77), 3f);
    }

    [Test]
    public void Overflow_FirstAidKit_IsZero()
    {
        ShouldBeApprox(RestoreMath.Overflow(current: 71, amount: 6, gain: 0, max: 77), 0f);
    }

    [Test]
    public void Overflow_WithGainHalf_IsEight()
    {
        ShouldBeApprox(RestoreMath.Overflow(current: 70, amount: 10, gain: 0.5f, max: 77), 8f);
    }

    [Test]
    public void Overflow_IsNeverBelowZero()
    {
        ShouldBeApprox(RestoreMath.Overflow(current: 10, amount: 1, gain: 0, max: 77), 0f);
    }

    [Test]
    public void AmountToFill_WithNegativeGain_ReachesFull()
    {
        // 20 below max with HealGain -0.5: asking for 40 gives 40 * 0.5 = 20
        ShouldBeApprox(RestoreMath.AmountToFill(current: 57, max: 77, gain: -0.5f), 40f);
    }

    [Test]
    public void AmountToFill_AtMax_IsZero()
    {
        ShouldBeApprox(RestoreMath.AmountToFill(current: 77, max: 77, gain: 0), 0f);
    }

    [Test]
    public void AmountToFill_WhenFactorNotPositive_IsZero()
    {
        ShouldBeApprox(RestoreMath.AmountToFill(current: 10, max: 77, gain: -1f), 0f);
        ShouldBeApprox(RestoreMath.AmountToFill(current: 10, max: 77, gain: -2f), 0f);
    }
}

/// <summary>The nesting and release rules of <see cref="RestoreOriginScope"/>.</summary>
public class RestoreOriginScopeTests
{
    [Test]
    public void Nested_ScopesStayActiveUntilTheOutermostIsReleased()
    {
        RestoreOriginScope.IsActive.ShouldBeFalse();

        using var outer = RestoreOriginScope.Enter();
        using (RestoreOriginScope.Enter())
        {
            RestoreOriginScope.IsActive.ShouldBeTrue();
        }

        RestoreOriginScope.IsActive.ShouldBeTrue();
        outer.Dispose();
        RestoreOriginScope.IsActive.ShouldBeFalse();
    }

    [Test]
    public void ScopeIsReleasedAfterAnException()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            using var scope = RestoreOriginScope.Enter();
            throw new InvalidOperationException("the game call failed");
        });

        RestoreOriginScope.IsActive.ShouldBeFalse();
    }

    [Test]
    public void DisposingTwice_ReleasesOnce()
    {
        var outer = RestoreOriginScope.Enter();
        var inner = RestoreOriginScope.Enter();

        inner.Dispose();
        inner.Dispose();

        RestoreOriginScope.IsActive.ShouldBeTrue(); // the outer scope is still open
        outer.Dispose();
        RestoreOriginScope.IsActive.ShouldBeFalse();
    }
}
