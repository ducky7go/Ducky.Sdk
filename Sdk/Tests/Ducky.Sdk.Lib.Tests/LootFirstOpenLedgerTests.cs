using Ducky.Sdk.GameEvents;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The captured <c>Looted</c> value per box (<see cref="LootFirstOpenLedger{TBox}"/>): first open, reopening, another box, a
/// missing value, a replaced value, one-time use and clearing. Plain objects stand in for the loot boxes.
/// </summary>
public class LootFirstOpenLedgerTests
{
    [Test]
    public void FirstOpen_WhenLiveLootedIsAlreadyTrue_IsStillFirstLoot()
    {
        // the game's LootView marked the box during the dispatch, but the value captured at interact start was false
        var ledger = new LootFirstOpenLedger<object>();
        var box = new object();
        ledger.Record(box, looted: false);

        ledger.Resolve(box, liveLooted: true).ShouldBeTrue();
    }

    [Test]
    public void Reopen_CapturedTrue_IsNotFirstLoot()
    {
        // the same box after its first loot ended: the value captured at the second interact start is true
        var ledger = new LootFirstOpenLedger<object>();
        var box = new object();
        ledger.Record(box, looted: true);

        ledger.Resolve(box, liveLooted: true).ShouldBeFalse();
    }

    [Test]
    public void CapturedValue_IsUsedOnce_SecondResolveFallsBackToLiveValue()
    {
        var ledger = new LootFirstOpenLedger<object>();
        var box = new object();
        ledger.Record(box, looted: false);

        ledger.Resolve(box, liveLooted: false).ShouldBeTrue();

        // the entry is gone: the live value decides now
        ledger.Resolve(box, liveLooted: false).ShouldBeTrue();
        ledger.Resolve(box, liveLooted: true).ShouldBeFalse();
    }

    [Test]
    public void AnotherBox_HasItsOwnEntry()
    {
        var ledger = new LootFirstOpenLedger<object>();
        var first = new object();
        var other = new object();
        ledger.Record(first, looted: false);
        ledger.Record(other, looted: true);

        ledger.Resolve(first, liveLooted: true).ShouldBeTrue();
        ledger.Resolve(other, liveLooted: false).ShouldBeFalse();
    }

    [Test]
    public void MissingEntry_UsesLiveValueBothWays()
    {
        var ledger = new LootFirstOpenLedger<object>();
        var box = new object();

        ledger.Resolve(box, liveLooted: false).ShouldBeTrue();
        ledger.Resolve(box, liveLooted: true).ShouldBeFalse();
    }

    [Test]
    public void ReplacedEntry_AfterCancelledInteraction_UsesTheLatestCapture()
    {
        // an interaction captured false and was cancelled before the loot; the next interaction captured true
        var ledger = new LootFirstOpenLedger<object>();
        var box = new object();
        ledger.Record(box, looted: false);
        ledger.Record(box, looted: true);

        ledger.Resolve(box, liveLooted: false).ShouldBeFalse();
    }

    [Test]
    public void Clear_DropsEveryEntry()
    {
        var ledger = new LootFirstOpenLedger<object>();
        var box = new object();
        ledger.Record(box, looted: false);

        ledger.Clear();

        // the captured false is gone: the live value decides
        ledger.Resolve(box, liveLooted: true).ShouldBeFalse();
    }

    [Test]
    public void NullBox_IsIgnoredAndUsesLiveValue()
    {
        var ledger = new LootFirstOpenLedger<object>();

        ledger.Record(null!, looted: false);

        ledger.Resolve(null!, liveLooted: false).ShouldBeTrue();
    }
}
