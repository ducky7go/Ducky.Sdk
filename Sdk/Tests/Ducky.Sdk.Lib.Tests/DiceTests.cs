using System.Collections.Generic;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Controllable randomness: the pure <see cref="DiceCore"/> and <see cref="DrawStream"/>. The static <see cref="Dice"/>
/// facade reads Unity's random source and is not exercised here.
/// </summary>
public class DiceTests
{
    private static DiceCore Scripted(params float[] values)
    {
        var queue = new Queue<float>(values);
        return new DiceCore(() => queue.Dequeue());
    }

    [Test]
    public void SameSeed_GivesTheSameSequence()
    {
        var first = new DrawStream(42, () => null);
        var second = new DrawStream(42, () => null);

        for (var i = 0; i < 20; i++)
        {
            second.Value().ShouldBe(first.Value());
        }
    }

    [Test]
    public void DifferentSeeds_GiveDifferentSequences()
    {
        var first = new DrawStream(1, () => null);
        var second = new DrawStream(2, () => null);

        var values = new List<float>();
        for (var i = 0; i < 5; i++)
        {
            values.Add(first.Value());
        }

        var others = new List<float>();
        for (var i = 0; i < 5; i++)
        {
            others.Add(second.Value());
        }

        others.ShouldNotBe(values);
    }

    [Test]
    public void StreamValues_LieInZeroToOne()
    {
        var stream = new DrawStream(7, () => null);

        for (var i = 0; i < 1000; i++)
        {
            var value = stream.Value();
            value.ShouldBeGreaterThanOrEqualTo(0f);
            value.ShouldBeLessThan(1f);
        }
    }

    [Test]
    public void ScriptedValues_ComeBackInOrder()
    {
        var core = Scripted(0.1f, 0.6f, 0.9f);

        core.Value().ShouldBe(0.1f);
        core.Value().ShouldBe(0.6f);
        core.Value().ShouldBe(0.9f);
    }

    [Test]
    public void Chance_IsTrueBelowTheProbability()
    {
        var core = Scripted(0.4f, 0.5f);

        core.Chance(0.5f).ShouldBeTrue();
        core.Chance(0.5f).ShouldBeFalse();
    }

    [Test]
    public void Range_StaysWithinTheRangeAndExcludesTheUpperBound()
    {
        var core = Scripted(0f, 0.999f, 0.5f);

        core.Range(3, 7).ShouldBe(3);
        core.Range(3, 7).ShouldBe(6);
        core.Range(3, 7).ShouldBe(5);
    }

    [Test]
    public void Range_EmptyRange_ReturnsTheMinimum()
    {
        var core = Scripted();

        core.Range(5, 5).ShouldBe(5);
        core.Range(5, 2).ShouldBe(5);
    }

    [Test]
    public void Force_ReplacesEveryRoll_AndClampsToZeroOne()
    {
        var core = Scripted();

        core.Force(0.3f);
        core.Value().ShouldBe(0.3f);
        core.Value().ShouldBe(0.3f);

        core.Force(2f);
        core.Forced.ShouldBe(1f);

        core.Force(-5f);
        core.Forced.ShouldBe(0f);
    }

    [Test]
    public void Force_Null_GivesTheRealSourceBack()
    {
        var core = new DiceCore(() => 0.25f);
        core.Force(0.9f);

        core.Force(null);

        core.Forced.ShouldBeNull();
        core.Value().ShouldBe(0.25f);
    }

    [Test]
    public void Stream_FollowsTheForcedValue()
    {
        var core = new DiceCore(() => 0.5f);
        var stream = core.Stream(3);

        core.Force(0.25f);
        stream.Value().ShouldBe(0.25f);
        stream.Value().ShouldBe(0.25f);
    }
}
