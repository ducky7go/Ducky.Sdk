using System;
using System.Collections.Generic;
using Ducky.Sdk.ModBehaviours;

namespace Ducky.Sdk.Lib.Tests;

public class ModSubscriptionsTests
{
    private sealed class Recorder : IDisposable
    {
        private readonly List<string> _log;
        private readonly string _name;
        private readonly bool _throws;

        public Recorder(List<string> log, string name, bool throws = false)
        {
            _log = log;
            _name = name;
            _throws = throws;
        }

        public int Disposed { get; private set; }

        public void Dispose()
        {
            Disposed++;
            _log.Add(_name);
            if (_throws)
            {
                throw new InvalidOperationException($"{_name} failed");
            }
        }
    }

    [Test]
    public void DisposeAll_RunsItemsInReverseOrderOfAddition()
    {
        var log = new List<string>();
        var subscriptions = new ModSubscriptions();
        subscriptions.Activate();
        subscriptions.Add(new Recorder(log, "A"));
        subscriptions.Add(new Recorder(log, "B"));
        subscriptions.Add(new Recorder(log, "C"));

        subscriptions.DisposeAll();

        log.ShouldBe(new[] { "C", "B", "A" });
    }

    [Test]
    public void OneThrowingDisposal_DoesNotStopTheRest()
    {
        var log = new List<string>();
        var subscriptions = new ModSubscriptions();
        subscriptions.Activate();
        var first = new Recorder(log, "first");
        subscriptions.Add(first);
        subscriptions.Add(new Recorder(log, "boom", throws: true));
        subscriptions.Add(new Recorder(log, "last"));

        Should.NotThrow(() => subscriptions.DisposeAll());

        log.ShouldBe(new[] { "last", "boom", "first" });
        first.Disposed.ShouldBe(1);
    }

    [Test]
    public void DoubleDispose_DisposesEachItemOnce()
    {
        var log = new List<string>();
        var subscriptions = new ModSubscriptions();
        subscriptions.Activate();
        var item = new Recorder(log, "A");
        subscriptions.Add(item);

        subscriptions.DisposeAll();
        subscriptions.Dispose();

        item.Disposed.ShouldBe(1);
    }

    [Test]
    public void AddWhileNotAccepting_DisposesImmediately()
    {
        var log = new List<string>();
        var subscriptions = new ModSubscriptions();
        var item = new Recorder(log, "early");

        subscriptions.Add(item);

        item.Disposed.ShouldBe(1);
        subscriptions.DisposeAll();
        item.Disposed.ShouldBe(1);
    }

    [Test]
    public void AddAfterDisposal_DisposesImmediately()
    {
        var log = new List<string>();
        var subscriptions = new ModSubscriptions();
        subscriptions.Activate();
        subscriptions.DisposeAll();
        var late = new Recorder(log, "late");

        subscriptions.Add(late);

        late.Disposed.ShouldBe(1);
    }

    [Test]
    public void NullInput_ThrowsAndLeavesTheContainerUnchanged()
    {
        var log = new List<string>();
        var subscriptions = new ModSubscriptions();
        subscriptions.Activate();
        var kept = new Recorder(log, "kept");
        subscriptions.Add(kept);

        Should.Throw<ArgumentNullException>(() => subscriptions.Add((IDisposable)null!));
        Should.Throw<ArgumentNullException>(() => subscriptions.Add((Action)null!));

        subscriptions.DisposeAll();
        kept.Disposed.ShouldBe(1);
        log.ShouldBe(new[] { "kept" });
    }

    [Test]
    public void ReEnable_StartsEmpty_AndNewAdditionsAreReleasedOnNextDisable()
    {
        var log = new List<string>();
        var subscriptions = new ModSubscriptions();
        subscriptions.Activate();
        var first = new Recorder(log, "first");
        subscriptions.Add(first);
        subscriptions.DisposeAll();

        subscriptions.Activate();
        var second = new Recorder(log, "second");
        subscriptions.Add(second);
        subscriptions.DisposeAll();

        first.Disposed.ShouldBe(1);
        second.Disposed.ShouldBe(1);
    }

    [Test]
    public void TwoContainers_AreIndependent()
    {
        var log = new List<string>();
        var one = new ModSubscriptions();
        var two = new ModSubscriptions();
        one.Activate();
        two.Activate();
        var itemOne = new Recorder(log, "one");
        var itemTwo = new Recorder(log, "two");
        one.Add(itemOne);
        two.Add(itemTwo);

        one.DisposeAll();

        itemOne.Disposed.ShouldBe(1);
        itemTwo.Disposed.ShouldBe(0);
    }

    [Test]
    public void ActionAdd_RunsTheActionOnceWhenDisposed()
    {
        var calls = 0;
        var subscriptions = new ModSubscriptions();
        subscriptions.Activate();
        var handle = subscriptions.Add(() => calls++);

        handle.Dispose();
        subscriptions.DisposeAll();

        calls.ShouldBe(1);
    }

    [Test]
    public void DisableSequence_DisposesSubscriptionsBetweenModDisabledAndTheReleases()
    {
        var log = new List<string>();

        ModBehaviourBase.RunDisableSequence(
            modDisabled: () => log.Add("ModDisabled"),
            logError: _ => log.Add("logError"),
            markDisabled: () => log.Add("markDisabled"),
            disposeSubscriptions: () => log.Add("subscriptions"),
            releaseHotReloadState: () => log.Add("hotReload"),
            releaseSharedResources: () => log.Add("shared"));

        log.ShouldBe(new[] { "ModDisabled", "markDisabled", "subscriptions", "hotReload", "shared" });
    }

    [Test]
    public void DisableSequence_WhenModDisabledThrows_StillDisposesAndRethrowsTheOriginal()
    {
        var log = new List<string>();
        var original = new InvalidOperationException("ModDisabled failed");

        var thrown = Should.Throw<InvalidOperationException>(() => ModBehaviourBase.RunDisableSequence(
            modDisabled: () => throw original,
            logError: _ => log.Add("logError"),
            markDisabled: () => log.Add("markDisabled"),
            disposeSubscriptions: () => log.Add("subscriptions"),
            releaseHotReloadState: () => log.Add("hotReload"),
            releaseSharedResources: () => log.Add("shared")));

        thrown.ShouldBeSameAs(original);
        log.ShouldBe(new[] { "logError", "markDisabled", "subscriptions", "hotReload", "shared" });
    }
}
