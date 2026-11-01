using System.Collections.Generic;
using Ducky.Sdk.Contracts;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>The pure parts of <see cref="BuffsContract"/>: region defaults and owned-list bookkeeping.</summary>
public class BuffsContractTests
{
    [TestCase(0, 1500000)]
    [TestCase(1, 1510000)]
    [TestCase(3, 1530000)]
    public void ComputeRegionStart_SpacesRegionsByTenThousandIds(int existingRegions, int expected) =>
        BuffsContract.ComputeRegionStart(existingRegions).ShouldBe(expected);

    [Test]
    public void TakeAll_ReturnsTheItemsAndEmptiesTheList()
    {
        var items = new List<string> { "a", "b" };

        var taken = BuffsContract.TakeAll(items, items);

        taken.ShouldBe(new[] { "a", "b" });
        items.Count.ShouldBe(0);
    }

    [Test]
    public void TakeAll_OnEmptyListReturnsAnEmptyCopy()
    {
        var items = new List<string>();

        var taken = BuffsContract.TakeAll(items, items);

        taken.ShouldBeEmpty();
        taken.ShouldNotBeSameAs(items);
    }
}
