using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Event recording: the pure <see cref="TraceRing"/>. The static <see cref="EffectTrace"/> facade reads Unity time and
/// writes the game log, so it is not exercised here.
/// </summary>
public class EffectTraceTests
{
    [Test]
    public void OrderIsKept_AndSequenceNumbersCount()
    {
        var ring = new TraceRing();

        ring.Add(0f, "A", "first");
        ring.Add(1f, "B", "second");
        ring.Add(2f, "C", "third");

        var entries = ring.Recent(3);
        entries.Length.ShouldBe(3);
        entries[0].Code.ShouldBe("A");
        entries[1].Code.ShouldBe("B");
        entries[2].Code.ShouldBe("C");
        entries[0].Sequence.ShouldBe(1);
        entries[2].Sequence.ShouldBe(3);
        ring.LastSequence.ShouldBe(3);
    }

    [Test]
    public void Recent_ReturnsTheNewestEntriesOldestFirst()
    {
        var ring = new TraceRing();
        for (var i = 1; i <= 5; i++)
        {
            ring.Add(i, "X", "n" + i);
        }

        var entries = ring.Recent(2);

        entries.Length.ShouldBe(2);
        entries[0].Name.ShouldBe("n4");
        entries[1].Name.ShouldBe("n5");
    }

    [Test]
    public void OldestEntriesAreDroppedFirst_AtCapacity()
    {
        var ring = new TraceRing(capacity: 3);
        for (var i = 1; i <= 5; i++)
        {
            ring.Add(i, "X", "n" + i);
        }

        ring.Count.ShouldBe(3);
        var entries = ring.Recent(3);
        entries[0].Sequence.ShouldBe(3);
        entries[1].Sequence.ShouldBe(4);
        entries[2].Sequence.ShouldBe(5);
        ring.LastSequence.ShouldBe(5);
    }

    [Test]
    public void Capacity_IsAtLeastOne()
    {
        var ring = new TraceRing(capacity: 0);

        ring.Add(0f, "A", "a");
        ring.Add(0f, "B", "b");

        ring.Count.ShouldBe(1);
        ring.Recent(1)[0].Code.ShouldBe("B");
    }

    [Test]
    public void Clear_EmptiesTheRingAndResetsTheSequence()
    {
        var ring = new TraceRing();
        ring.Add(0f, "A", "a");

        ring.Clear();

        ring.Count.ShouldBe(0);
        ring.LastSequence.ShouldBe(0);
        ring.Recent(5).Length.ShouldBe(0);
    }

    [Test]
    public void Detail_DefaultsToEmpty()
    {
        var ring = new TraceRing();

        ring.Add(0f, "A", "a");

        ring.Recent(1)[0].Detail.ShouldBe(string.Empty);
    }
}
