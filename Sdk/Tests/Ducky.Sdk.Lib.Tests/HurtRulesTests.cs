using System;
using System.Collections.Generic;
using Ducky.Sdk.Combat;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The rule registry of <see cref="HurtRules"/> (<see cref="OrderedRules{TItem}"/>): order, removal, failure isolation and
/// patch lifetime. The Harmony prefix itself needs the game and is not exercised here.
/// </summary>
public class HurtRulesTests
{
    private sealed class Lifetime
    {
        public int Installed;
        public int Removed;
    }

    private static OrderedRules<string> NewRegistry(Lifetime lifetime) =>
        new(onFirst: () => lifetime.Installed++, onLast: () => lifetime.Removed++);

    [Test]
    public void RunsByPriority_ThenRegistrationOrder()
    {
        var registry = NewRegistry(new Lifetime());
        registry.Add("b-priority-1", 1);
        registry.Add("a-priority-0", 0);
        registry.Add("c-priority-0", 0);

        registry.InRunOrder().ShouldBe(new[] { "a-priority-0", "c-priority-0", "b-priority-1" });
    }

    [Test]
    public void Rules_RunInOrderAndEachSeesTheValueLeftByThePrevious()
    {
        var registry = new OrderedRules<Func<int, int>>();
        registry.Add(value => value * 2, priority: 0); // doubles first
        registry.Add(value => value + 5, priority: 1); // adds five after

        var value = 10;
        foreach (var rule in registry.InRunOrder())
        {
            value = rule(value);
        }

        value.ShouldBe(25);
    }

    [Test]
    public void RemovingOneRule_LeavesTheOther()
    {
        var registry = NewRegistry(new Lifetime());
        var first = registry.Add("first", 0);
        registry.Add("second", 0);

        first.Dispose();

        registry.InRunOrder().ShouldBe(new[] { "second" });
    }

    [Test]
    public void ThrowingRule_IsReportedAndTheRestStillRun()
    {
        var registry = NewRegistry(new Lifetime());
        registry.Add("thrower", 0);
        registry.Add("other", 0);
        var ran = new List<string>();
        var failures = new List<Exception>();

        registry.RunIsolated(
            item =>
            {
                if (item == "thrower")
                {
                    throw new InvalidOperationException("boom");
                }

                ran.Add(item);
            },
            failures.Add);

        ran.ShouldBe(new[] { "other" });
        failures.Count.ShouldBe(1);
    }

    [Test]
    public void PatchIsInstalledWithTheFirstRule_AndRemovedWithTheLast()
    {
        var lifetime = new Lifetime();
        var registry = NewRegistry(lifetime);

        var first = registry.Add("first", 0);
        var second = registry.Add("second", 0);
        lifetime.Installed.ShouldBe(1);
        lifetime.Removed.ShouldBe(0);

        first.Dispose();
        lifetime.Removed.ShouldBe(0);

        second.Dispose();
        lifetime.Removed.ShouldBe(1);
    }

    [Test]
    public void DisposingARuleTwice_HasNoFurtherEffect()
    {
        var lifetime = new Lifetime();
        var registry = NewRegistry(lifetime);
        var keep = registry.Add("keep", 0);
        var handle = registry.Add("handle", 0);

        handle.Dispose();
        handle.Dispose();

        registry.Count.ShouldBe(1);
        lifetime.Removed.ShouldBe(0);
        keep.Dispose();
        lifetime.Removed.ShouldBe(1);
    }
}
