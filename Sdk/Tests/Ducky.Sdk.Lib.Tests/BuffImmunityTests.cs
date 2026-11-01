using System;
using Ducky.Sdk.Buffs;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The ownership and reference rules of <see cref="BuffImmunity"/>, checked on the pure <see cref="BuffImmunityLedger{TTag}"/>
/// and <see cref="DisposeOnce"/> with plain integer tags (0 is <c>NotExclusive</c>). The shell needs the game's character.
/// </summary>
public class BuffImmunityTests
{
    private const int NotExclusive = 0;
    private const int Bleeding = 1;

    private static BuffImmunityLedger<int> NewLedger() => new(NotExclusive);

    [Test]
    public void NotExclusive_IsRejected_AndNothingIsGranted()
    {
        var ledger = NewLedger();

        Should.Throw<ArgumentException>(() => ledger.ThrowIfNotExclusive(NotExclusive));

        ledger.IsGranted(NotExclusive).ShouldBeFalse();
        ledger.GrantedTags.Count.ShouldBe(0);
    }

    [Test]
    public void OtherTags_PassTheCheck()
    {
        var ledger = NewLedger();

        Should.NotThrow(() => ledger.ThrowIfNotExclusive(Bleeding));
    }

    [Test]
    public void ForeignEntry_IsKeptAfterRelease()
    {
        var ledger = NewLedger();

        ledger.Retain(Bleeding, presentInList: true);

        ledger.Owns(Bleeding).ShouldBeFalse();
        ledger.Release(Bleeding).ShouldBeFalse("the list entry was not added by the helper, so it must stay");
    }

    [Test]
    public void OwnEntry_IsRemovedWhenTheLastGrantIsReleased()
    {
        var ledger = NewLedger();

        ledger.Retain(Bleeding, presentInList: false);

        ledger.Owns(Bleeding).ShouldBeTrue();
        ledger.Release(Bleeding).ShouldBeTrue();
        ledger.IsGranted(Bleeding).ShouldBeFalse();
    }

    [Test]
    public void SeveralGrants_ShareOneEntry_AndTheLastReleaseRemovesIt()
    {
        var ledger = NewLedger();
        ledger.Retain(Bleeding, presentInList: false);
        ledger.Retain(Bleeding, presentInList: false);

        ledger.ReferenceCount(Bleeding).ShouldBe(2);
        ledger.Release(Bleeding).ShouldBeFalse("one grant still holds the tag");
        ledger.IsGranted(Bleeding).ShouldBeTrue();
        ledger.Release(Bleeding).ShouldBeTrue();
        ledger.IsGranted(Bleeding).ShouldBeFalse();
    }

    [Test]
    public void ReleasingAnUngrantedTag_DoesNothing()
    {
        var ledger = NewLedger();

        ledger.Release(Bleeding).ShouldBeFalse();
        ledger.ReferenceCount(Bleeding).ShouldBe(0);
    }

    [Test]
    public void ReapplicationAfterLevelInit_KeepsTheTagGranted_AndMakesTheEntryOwn()
    {
        var ledger = NewLedger();
        ledger.Retain(Bleeding, presentInList: true);

        // the game clears its list on a scene change; the shell then puts the tag back and reports it
        ledger.GrantedTags.ShouldContain(Bleeding);
        ledger.MarkAdded(Bleeding);

        ledger.Owns(Bleeding).ShouldBeTrue();
        ledger.Release(Bleeding).ShouldBeTrue();
    }

    [Test]
    public void DisposeOnce_RunsTheActionOnlyOnTheFirstDispose()
    {
        var calls = 0;
        var handle = new DisposeOnce(() => calls++);

        handle.Dispose();
        handle.Dispose();

        calls.ShouldBe(1);
    }

    [Test]
    public void DisposeOnce_RejectsANullAction() =>
        Should.Throw<ArgumentNullException>(() => new DisposeOnce(null!));
}
