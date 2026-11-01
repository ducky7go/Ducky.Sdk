using System;
using Ducky.Sdk.Economy;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>The boundaries of <see cref="CashPolicy.TryPlan"/>: zero, negative, one less, equal, one more, zero cash, max value.</summary>
public class CashPolicyTests
{
    private const long Cash = 10;

    [Test]
    public void Zero_IsAllowedAndConsumesNothing()
    {
        CashPolicy.TryPlan(Cash, 0, out var toConsume).ShouldBeTrue();
        toConsume.ShouldBe(0);
    }

    [Test]
    public void Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CashPolicy.TryPlan(Cash, -1, out _));
    }

    [Test]
    public void OneLessThanCash_IsPlanned()
    {
        CashPolicy.TryPlan(Cash, Cash - 1, out var toConsume).ShouldBeTrue();
        toConsume.ShouldBe(Cash - 1);
    }

    [Test]
    public void EqualToCash_IsPlanned()
    {
        CashPolicy.TryPlan(Cash, Cash, out var toConsume).ShouldBeTrue();
        toConsume.ShouldBe(Cash);
    }

    [Test]
    public void OneMoreThanCash_IsRefused()
    {
        CashPolicy.TryPlan(Cash, Cash + 1, out var toConsume).ShouldBeFalse();
        toConsume.ShouldBe(0);
    }

    [Test]
    public void AnyAmountWithZeroCash_IsRefusedExceptZero()
    {
        CashPolicy.TryPlan(0, 1, out _).ShouldBeFalse();
        CashPolicy.TryPlan(0, 0, out var toConsume).ShouldBeTrue();
        toConsume.ShouldBe(0);
    }

    [Test]
    public void MaxValue_EqualToCash_IsPlanned()
    {
        CashPolicy.TryPlan(long.MaxValue, long.MaxValue, out var toConsume).ShouldBeTrue();
        toConsume.ShouldBe(long.MaxValue);
    }

    [Test]
    public void MaxValue_OneMoreThanCash_IsRefused()
    {
        CashPolicy.TryPlan(long.MaxValue - 1, long.MaxValue, out _).ShouldBeFalse();
    }
}
