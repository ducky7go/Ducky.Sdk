using System;
using System.Collections.Generic;
using Ducky.Sdk.Control;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The stop counting behind <c>MouseAimControl</c> (<see cref="AimBlockLedger{TOwner}"/>), with fake owners and recorded
/// block and unblock calls. No Unity type is loaded.
/// </summary>
public class AimBlockLedgerTests
{
    private sealed class FakeOwner
    {
        public FakeOwner(int id) => Id = id;

        public int Id { get; }

        public bool Live { get; set; } = true;
    }

    private sealed class Recorder
    {
        public List<string> Calls { get; } = new();

        public List<Exception> Errors { get; } = new();

        public bool ThrowOnBlock { get; set; }

        public bool ThrowOnUnblock { get; set; }

        public AimBlockLedger<FakeOwner> CreateLedger() => new(
            owner => owner.Id,
            owner => owner.Live,
            owner =>
            {
                Calls.Add($"block {owner.Id}");
                if (ThrowOnBlock)
                {
                    throw new InvalidOperationException("block failed");
                }
            },
            owner =>
            {
                Calls.Add($"unblock {owner.Id}");
                if (ThrowOnUnblock)
                {
                    throw new InvalidOperationException("unblock failed");
                }
            },
            Errors.Add);
    }

    [Test]
    public void TwoOwners_ReleasedInEitherOrder_UnblockEachOnItsRelease()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var a = new FakeOwner(1);
        var b = new FakeOwner(2);
        var handleA = ledger.Hold(a);
        var handleB = ledger.Hold(b);

        handleB.Dispose();
        ledger.IsStopped.ShouldBeTrue(); // a is still held
        handleA.Dispose();

        recorder.Calls.ShouldBe(new[] { "block 1", "block 2", "unblock 2", "unblock 1" });
        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void TwoHoldsOnOneOwner_BlockOnce_UnblockAfterTheLast()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);
        var first = ledger.Hold(owner);
        var second = ledger.Hold(owner);

        first.Dispose();
        recorder.Calls.ShouldBe(new[] { "block 1" }); // not the last: no unblock yet
        ledger.IsStopped.ShouldBeTrue();

        second.Dispose();
        recorder.Calls.ShouldBe(new[] { "block 1", "unblock 1" });
        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void SingleHold_BlocksThenUnblocks()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);

        var handle = ledger.Hold(owner);
        ledger.IsStopped.ShouldBeTrue();
        handle.Dispose();

        recorder.Calls.ShouldBe(new[] { "block 1", "unblock 1" });
        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void DoubleDispose_WhileAnotherHoldExists_HasNoEffect()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);
        var first = ledger.Hold(owner);
        var second = ledger.Hold(owner);

        first.Dispose();
        first.Dispose();

        ledger.IsStopped.ShouldBeTrue(); // the second hold still stands
        recorder.Calls.ShouldBe(new[] { "block 1" });
    }

    [Test]
    public void StaleHandleAfterClear_DoesNotUnblock()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);
        var stale = ledger.Hold(owner);

        ledger.Clear(); // a level change: the game's state is gone, so no unblock
        stale.Dispose();

        recorder.Calls.ShouldBe(new[] { "block 1" });
        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void ReleaseAll_WithThreeHolds_UnblocksLiveOwners_AndLaterDisposeDoesNothing()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var a = new FakeOwner(1);
        var b = new FakeOwner(2);
        var handles = new[] { ledger.Hold(a), ledger.Hold(a), ledger.Hold(b) };

        ledger.ReleaseAll();
        recorder.Calls.ShouldBe(new[] { "block 1", "block 2", "unblock 1", "unblock 2" });
        ledger.IsStopped.ShouldBeFalse();

        foreach (var handle in handles)
        {
            handle.Dispose();
        }

        recorder.Calls.ShouldBe(new[] { "block 1", "block 2", "unblock 1", "unblock 2" });
        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void ReleaseAll_WithNoHold_ChangesNothing()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();

        ledger.ReleaseAll();

        recorder.Calls.ShouldBeEmpty();
        recorder.Errors.ShouldBeEmpty();
        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void DeadOwner_IsNotCountedByIsStopped()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);
        ledger.Hold(owner);

        owner.Live = false;

        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void DeadOwner_ReheldLater_CallsBlockAgain()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);
        ledger.Hold(owner);
        owner.Live = false;
        ledger.IsStopped.ShouldBeFalse(); // forgets the dead owner's count

        owner.Live = true; // the panel is reopened
        var reopened = ledger.Hold(owner);

        recorder.Calls.ShouldBe(new[] { "block 1", "block 1" });
        ledger.IsStopped.ShouldBeTrue();
        reopened.Dispose();
        recorder.Calls.ShouldBe(new[] { "block 1", "block 1", "unblock 1" });
    }

    [Test]
    public void DeadOwner_ForgottenByPerFrameCall_ReopenBlocksAgain()
    {
        // The holder calls ForgetDeadOwners every frame, so a reopen with no ledger call in between still blocks.
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);
        var stale = ledger.Hold(owner);

        owner.Live = false;
        ledger.ForgetDeadOwners();
        owner.Live = true;
        var reopened = ledger.Hold(owner);

        recorder.Calls.ShouldBe(new[] { "block 1", "block 1" });
        stale.Dispose(); // the old handle of the closed panel release nothing
        ledger.IsStopped.ShouldBeTrue();
        reopened.Dispose();
        recorder.Calls.ShouldBe(new[] { "block 1", "block 1", "unblock 1" });
        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void DeadOwner_ReleasedWithoutUnblock()
    {
        var recorder = new Recorder();
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);
        var handle = ledger.Hold(owner);

        owner.Live = false;
        handle.Dispose();

        recorder.Calls.ShouldBe(new[] { "block 1" });
        recorder.Errors.ShouldBeEmpty();
    }

    [Test]
    public void ThrowingBlock_DoesNotEscapeHold_AndIsStillCounted()
    {
        var recorder = new Recorder { ThrowOnBlock = true };
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);

        IDisposable? handle = null;
        Should.NotThrow(() => handle = ledger.Hold(owner));

        recorder.Errors.Count.ShouldBe(1);
        ledger.IsStopped.ShouldBeTrue();
        Should.NotThrow(() => handle!.Dispose());
        recorder.Calls.ShouldBe(new[] { "block 1", "unblock 1" });
    }

    [Test]
    public void ThrowingUnblock_DoesNotEscapeDispose()
    {
        var recorder = new Recorder { ThrowOnUnblock = true };
        var ledger = recorder.CreateLedger();
        var owner = new FakeOwner(1);
        var handle = ledger.Hold(owner);

        Should.NotThrow(() => handle.Dispose());

        recorder.Errors.Count.ShouldBe(1);
        ledger.IsStopped.ShouldBeFalse();
    }

    [Test]
    public void NullOwner_ThrowsArgumentNullException()
    {
        var ledger = new Recorder().CreateLedger();

        Should.Throw<ArgumentNullException>(() => ledger.Hold(null!));
    }
}
