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
}
