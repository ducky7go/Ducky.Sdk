using System;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Fast-forwardable clock: the pure <see cref="ClockCore"/>. The static <see cref="Clock"/> facade reads Unity time.</summary>
public class ClockTests
{
    [Test]
    public void Advance_GrowsTheElapsedTimeByTheAmount()
    {
        var clock = new ClockCore(() => 10f);

        clock.Skip(90f);

        clock.Skipped.ShouldBe(90f);
        clock.Now.ShouldBe(100f);
    }

    [Test]
    public void Advance_Accumulates()
    {
        var clock = new ClockCore(() => 0f);

        clock.Skip(5f);
        clock.Skip(7f);

        clock.Skipped.ShouldBe(12f);
    }

    [Test]
    public void ZeroAdvance_ChangesNothing()
    {
        var clock = new ClockCore(() => 3f);

        clock.Skip(0f);

        clock.Skipped.ShouldBe(0f);
        clock.Now.ShouldBe(3f);
    }

    [Test]
    public void NegativeAdvance_IsRejectedAndTheTimeIsUnchanged()
    {
        var clock = new ClockCore(() => 10f);
        clock.Skip(4f);

        Should.Throw<ArgumentOutOfRangeException>(() => clock.Skip(-1f));

        clock.Skipped.ShouldBe(4f);
        clock.Now.ShouldBe(14f);
    }

    [Test]
    public void Reset_ForgetsTheSkippedSeconds()
    {
        var clock = new ClockCore(() => 1f);
        clock.Skip(30f);

        clock.Reset();

        clock.Skipped.ShouldBe(0f);
        clock.Now.ShouldBe(1f);
    }
}
