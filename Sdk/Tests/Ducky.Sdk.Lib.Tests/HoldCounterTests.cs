using Ducky.Sdk.Control;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The reference count behind <c>CharacterVisibility</c> (<see cref="HoldCounter{TKey}"/>): nested holds, release order,
/// double release, a stale ticket after a clear, and release of an unknown key.
/// </summary>
public class HoldCounterTests
{
    [Test]
    public void TwoHolds_ReleasedInEitherOrder_OnlyLastReleaseEnds()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out var first).ShouldBeTrue();
        counter.Acquire(1, out var second).ShouldBeFalse();

        counter.Release(first).ShouldBeFalse(); // not the last
        counter.Release(second).ShouldBeTrue(); // last: the character is shown
    }

    [Test]
    public void TwoHolds_ReleasedInReverseOrder_OnlyLastReleaseEnds()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out var first);
        counter.Acquire(1, out var second);

        counter.Release(second).ShouldBeFalse();
        counter.Release(first).ShouldBeTrue();
    }

    [Test]
    public void SingleHold_ReleaseEndsIt()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out var hold);

        counter.Release(hold).ShouldBeTrue();
        counter.CountOf(1).ShouldBe(0);
    }

    [Test]
    public void DoubleRelease_HasNoFurtherEffect()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out var first);
        counter.Acquire(1, out var second);

        counter.Release(first).ShouldBeFalse();
        counter.Release(first).ShouldBeFalse();

        counter.CountOf(1).ShouldBe(1); // the other hold still stands
        counter.Release(second).ShouldBeTrue();
    }

    [Test]
    public void StaleTicket_AfterClear_ReleasesNothing()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out var old);
        counter.Clear();
        counter.Acquire(1, out var fresh).ShouldBeTrue(); // counts from zero again

        counter.Release(old).ShouldBeFalse();

        counter.CountOf(1).ShouldBe(1);
        counter.Release(fresh).ShouldBeTrue();
    }

    [Test]
    public void ReleaseOfAnUnknownKey_ChangesNothing()
    {
        var counter = new HoldCounter<int>();
        var other = new HoldCounter<int>();
        other.Acquire(7, out var fromOtherCounter);
        counter.Acquire(1, out var hold);

        counter.Release(fromOtherCounter).ShouldBeFalse();
        counter.Release(default).ShouldBeFalse();

        counter.CountOf(1).ShouldBe(1);
        counter.Total.ShouldBe(1);
    }

    [Test]
    public void Forget_TicketsOfThatKey_ReleaseNothing()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out var first);
        counter.Acquire(1, out var second);

        counter.Forget(1);

        counter.Release(first).ShouldBeFalse();
        counter.Release(second).ShouldBeFalse();
        counter.CountOf(1).ShouldBe(0);
    }

    [Test]
    public void Forget_OtherKeys_AreUntouched()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out _);
        counter.Acquire(2, out var otherKey);
        counter.Acquire(2, out var otherKeySecond);

        counter.Forget(1);

        counter.CountOf(2).ShouldBe(2);
        counter.Release(otherKey).ShouldBeFalse(); // the other key still has a hold
        counter.Release(otherKeySecond).ShouldBeTrue(); // and its last release still ends it
    }

    [Test]
    public void Forget_KeyCanBeAcquiredAgain_ReportsFirst()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out var old);

        counter.Forget(1);

        counter.Acquire(1, out var fresh).ShouldBeTrue(); // counts from zero again
        counter.Release(old).ShouldBeFalse(); // the forgotten ticket does not touch the new hold
        counter.CountOf(1).ShouldBe(1);
        counter.Release(fresh).ShouldBeTrue();
    }

    [Test]
    public void Forget_ShrinksTotalByTheForgottenTickets()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out _);
        counter.Acquire(1, out _);
        counter.Acquire(2, out _);
        counter.Total.ShouldBe(3);

        counter.Forget(1);

        counter.Total.ShouldBe(1);
    }

    [Test]
    public void Forget_UnknownKey_ChangesNothing()
    {
        var counter = new HoldCounter<int>();
        counter.Acquire(1, out var hold);

        counter.Forget(99);

        counter.CountOf(1).ShouldBe(1);
        counter.Total.ShouldBe(1);
        counter.Release(hold).ShouldBeTrue();
    }
}
