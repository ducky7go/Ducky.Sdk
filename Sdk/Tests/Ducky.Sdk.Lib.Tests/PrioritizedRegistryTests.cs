using System;
using System.Collections.Generic;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The shared registry under handler sets, ordered rules and rule registries: first and last callbacks, priority order,
/// idempotent handles, snapshots that a later change does not affect, and a failing first callback that leaves no entry behind.
/// </summary>
public class PrioritizedRegistryTests
{
    private sealed class Lifetime
    {
        public int First;
        public int Last;
    }

    private static PrioritizedRegistry<string> NewRegistry(Lifetime lifetime) =>
        new(onFirst: () => lifetime.First++, onLast: () => lifetime.Last++);

    private static string[] Items(PrioritizedRegistry<string> registry)
    {
        var entries = registry.Snapshot();
        var items = new string[entries.Length];
        for (var i = 0; i < entries.Length; i++)
        {
            items[i] = entries[i].Item;
        }

        return items;
    }

    [Test]
    public void FirstAndLast_CalledOnceEachAcrossAddsAndRemovals()
    {
        var lifetime = new Lifetime();
        var registry = NewRegistry(lifetime);

        var a = registry.Add("a", 0);
        var b = registry.Add("b", 0);
        lifetime.First.ShouldBe(1);
        lifetime.Last.ShouldBe(0);

        a.Dispose();
        lifetime.Last.ShouldBe(0);

        b.Dispose();
        lifetime.Last.ShouldBe(1);
        registry.Count.ShouldBe(0);
    }

    [Test]
    public void Priority_SortsAscending_EqualPriorityKeepsRegistrationOrder()
    {
        var registry = NewRegistry(new Lifetime());
        registry.Add("late", 5);
        registry.Add("first-of-zero", 0);
        registry.Add("second-of-zero", 0);
        registry.Add("early", -1);

        Items(registry).ShouldBe(new[] { "early", "first-of-zero", "second-of-zero", "late" });
    }

    [Test]
    public void Dispose_TwiceRemovesOnce_AndDoesNotRunLastTwice()
    {
        var lifetime = new Lifetime();
        var registry = NewRegistry(lifetime);
        var handle = registry.Add("only", 0);

        handle.Dispose();
        handle.Dispose();

        lifetime.Last.ShouldBe(1);
        registry.Count.ShouldBe(0);
    }

    [Test]
    public void Snapshot_IsNotChangedByLaterAddOrRemove()
    {
        var registry = NewRegistry(new Lifetime());
        var keep = registry.Add("keep", 0);
        var drop = registry.Add("drop", 0);

        var before = registry.Snapshot();
        drop.Dispose();
        registry.Add("new", 0);

        before.Length.ShouldBe(2);
        before[0].Item.ShouldBe("keep");
        before[1].Item.ShouldBe("drop");
        Items(registry).ShouldBe(new[] { "keep", "new" });
        keep.Dispose();
    }

    [Test]
    public void FailingFirst_LeavesNoEntryAndRethrows()
    {
        var lastCalls = 0;
        var registry = new PrioritizedRegistry<string>(
            onFirst: () => throw new InvalidOperationException("install failed"),
            onLast: () => lastCalls++);

        Assert.Throws<InvalidOperationException>(() => registry.Add("x", 0));

        registry.Count.ShouldBe(0);
        lastCalls.ShouldBe(0);
    }

    [Test]
    public void NullItem_Throws()
    {
        var registry = NewRegistry(new Lifetime());

        Assert.Throws<ArgumentNullException>(() => registry.Add(null!, 0));
    }

    [Test]
    public void Remove_OfOneEntry_LeavesTheOthersInOrder()
    {
        var registry = NewRegistry(new Lifetime());
        registry.Add("a", 0);
        var middle = registry.Add("b", 0);
        registry.Add("c", 0);

        middle.Dispose();

        Items(registry).ShouldBe(new[] { "a", "c" });
    }
}
