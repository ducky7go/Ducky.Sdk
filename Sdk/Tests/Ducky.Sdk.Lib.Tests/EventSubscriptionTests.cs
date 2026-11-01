using System;
using System.Collections.Generic;
using Ducky.Sdk.GameEvents;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Registration logic of the game event wrappers, tested with a fake event (no Unity).</summary>
public class EventSubscriptionTests
{
    private sealed class FakeEvent
    {
        public List<Action> Handlers { get; } = new();

        public void Add(Action handler) => Handlers.Add(handler);

        public void Remove(Action handler) => Handlers.Remove(handler);

        public void Raise()
        {
            foreach (var handler in Handlers.ToArray())
            {
                handler();
            }
        }
    }

    [Test]
    public void Create_RegistersTheHandler_AndDisposeRemovesIt()
    {
        var fake = new FakeEvent();
        var runs = 0;

        using var subscription = EventSubscription.Create<Action>(() => runs++, fake.Add, fake.Remove);
        fake.Raise();

        runs.ShouldBe(1);
        fake.Handlers.Count.ShouldBe(1);
    }

    [Test]
    public void Dispose_RemovesTheHandlerOnceAndLaterCallsDoNothing()
    {
        var fake = new FakeEvent();
        var runs = 0;
        var subscription = EventSubscription.Create<Action>(() => runs++, fake.Add, fake.Remove);

        subscription.Dispose();
        subscription.Dispose();
        fake.Raise();

        runs.ShouldBe(0);
        fake.Handlers.Count.ShouldBe(0);
    }

    [Test]
    public void Create_WithNullHandler_Throws()
    {
        var fake = new FakeEvent();

        Should.Throw<ArgumentNullException>(() => EventSubscription.Create<Action>(null!, fake.Add, fake.Remove));
    }

    [Test]
    public void Isolate_FailingHandler_DoesNotReachTheRaiser()
    {
        var fake = new FakeEvent();
        var laterRan = false;
        fake.Add(EventSubscription.Isolate(() => throw new InvalidOperationException("handler failed")));
        fake.Add(() => laterRan = true);

        Should.NotThrow(() => fake.Raise());

        laterRan.ShouldBeTrue();
    }

    [Test]
    public void Isolated_HandlerIsRemovedByTheSameSubscription()
    {
        var fake = new FakeEvent();
        var runs = 0;
        var isolated = EventSubscription.Isolate(() => runs++);

        using (EventSubscription.Create<Action>(isolated, fake.Add, fake.Remove))
        {
            fake.Raise();
        }

        fake.Raise();
        runs.ShouldBe(1);
    }

    [Test]
    public void TwoSubscriptions_OnOneFakeEvent_EachRunsOncePerRaise()
    {
        var fake = new FakeEvent();
        var first = 0;
        var second = 0;
        using var a = EventSubscription.Create<Action>(() => first++, fake.Add, fake.Remove);
        using var b = EventSubscription.Create<Action>(() => second++, fake.Add, fake.Remove);

        fake.Raise();

        first.ShouldBe(1);
        second.ShouldBe(1);
    }
}
