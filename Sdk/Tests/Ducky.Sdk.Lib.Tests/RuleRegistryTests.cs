using System;
using System.Collections.Generic;
using Ducky.Sdk.Rules;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The shared rule registry of restore and control rules: priority order, equal priority, single removal, failure isolation
/// with the value restored, removal during a call, first and last callbacks and idempotent handles.
/// </summary>
public class RuleRegistryTests
{
    private sealed class Lifetime
    {
        public int First;
        public int Last;
    }

    private static RuleRegistry<int, string> NewRegistry(Lifetime lifetime) =>
        new(onFirst: () => lifetime.First++, onLast: () => lifetime.Last++);

    private static RuleStep<int, string> Double => (ref int value, string _) => value *= 2;

    private static RuleStep<int, string> AddFive => (ref int value, string _) => value += 5;

    [Test]
    public void Priority_OrdersTheRules_DoubleThenAddFive()
    {
        var registry = NewRegistry(new Lifetime());
        registry.Add(AddFive, priority: 1);
        registry.Add(Double, priority: 0);

        var value = 5;
        registry.Apply(ref value, "ctx", _ => { });

        value.ShouldBe(15); // 5 * 2 = 10 (priority 0), then + 5 = 15 (priority 1)
    }

    [Test]
    public void Priority_ExampleTenToTwentyFive()
    {
        var registry = NewRegistry(new Lifetime());
        RuleStep<int, string> addFifteen = (ref int value, string _) => value += 15;
        registry.Add(addFifteen, priority: 1);
        registry.Add(Double, priority: 0);

        var value = 5;
        registry.Apply(ref value, "ctx", _ => { });

        // 5 -> 10 (priority 0) -> 25 (priority 1)
        value.ShouldBe(25);
    }

    [Test]
    public void EqualPriority_RunsInRegistrationOrder()
    {
        var registry = NewRegistry(new Lifetime());
        var order = new List<string>();
        registry.Add((ref int _, string _) => order.Add("first"), priority: 0);
        registry.Add((ref int _, string _) => order.Add("second"), priority: 0);

        var value = 0;
        registry.Apply(ref value, "ctx", _ => { });

        order.ShouldBe(new[] { "first", "second" });
    }

    [Test]
    public void RemovingOneRule_LeavesTheOthers()
    {
        var registry = NewRegistry(new Lifetime());
        var keep = registry.Add(AddFive, priority: 0);
        var remove = registry.Add(Double, priority: 1);

        remove.Dispose();

        registry.Count.ShouldBe(1);
        var value = 5;
        registry.Apply(ref value, "ctx", _ => { });
        value.ShouldBe(10);
        keep.Dispose();
    }

    [Test]
    public void ThrowingRule_IsUndone_AndTheNextRuleSeesTheValueBeforeIt()
    {
        var registry = NewRegistry(new Lifetime());
        RuleStep<int, string> changeThenThrow = (ref int value, string _) =>
        {
            value = 99;
            throw new InvalidOperationException("boom");
        };
        var seen = new List<int>();
        registry.Add(changeThenThrow, priority: 0);
        registry.Add((ref int value, string _) =>
        {
            seen.Add(value);
            value += 1;
        }, priority: 1);
        var failures = new List<Exception>();

        var value = 10;
        registry.Apply(ref value, "ctx", failures.Add);

        seen.ShouldBe(new[] { 10 }); // the second rule saw 10, not 99
        value.ShouldBe(11);
        failures.Count.ShouldBe(1);
    }

    [Test]
    public void RemovingAllRules_RaisesOnLastOnce()
    {
        var lifetime = new Lifetime();
        var registry = NewRegistry(lifetime);
        var a = registry.Add(Double, priority: 0);
        var b = registry.Add(AddFive, priority: 0);

        a.Dispose();
        lifetime.Last.ShouldBe(0);
        b.Dispose();
        lifetime.Last.ShouldBe(1);
    }

    [Test]
    public void AddingRules_RaisesOnFirstOnlyOnce()
    {
        var lifetime = new Lifetime();
        var registry = NewRegistry(lifetime);

        registry.Add(Double, priority: 0);
        registry.Add(AddFive, priority: 0);

        lifetime.First.ShouldBe(1);
    }

    [Test]
    public void RemovingDuringACall_DoesNotChangeThatCall()
    {
        var registry = NewRegistry(new Lifetime());
        IDisposable? self = null;
        var ran = new List<string>();
        self = registry.Add((ref int _, string _) =>
        {
            ran.Add("self");
            self?.Dispose(); // removes itself while the call is running
        }, priority: 0);
        registry.Add((ref int _, string _) => ran.Add("other"), priority: 1);

        var value = 0;
        registry.Apply(ref value, "ctx", _ => { });
        registry.Apply(ref value, "ctx", _ => { });

        ran.ShouldBe(new[] { "self", "other", "other" }); // the first call still ran "other"; the second skips "self"
    }

    [Test]
    public void DoubleDispose_RemovesOnlyItsOwnRule()
    {
        var registry = NewRegistry(new Lifetime());
        var first = registry.Add(Double, priority: 0);
        var second = registry.Add(Double, priority: 0);

        first.Dispose();
        first.Dispose();

        registry.Count.ShouldBe(1);
        second.Dispose();
        registry.Count.ShouldBe(0);
    }

    [Test]
    public void NullRule_IsRejected()
    {
        var registry = NewRegistry(new Lifetime());

        Should.Throw<ArgumentNullException>(() => registry.Add(null!, priority: 0));
    }
}
