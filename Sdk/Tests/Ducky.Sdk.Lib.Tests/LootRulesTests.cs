using Ducky.Sdk.GameEvents;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The loot decision (<see cref="LootRules"/>) for every combination of looted, pet and include-pets, and the first-loot cases.
/// </summary>
public class LootRulesTests
{
    // isPet, includePets, expected delivery
    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public void Delivery_IsOnlyWithheldForAPetWhenPetsAreNotIncluded(bool isPet, bool includePets, bool expected)
    {
        LootRules.ShouldDeliver(isPet, includePets).ShouldBe(expected);
    }

    // looted, expected first loot
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void FirstLoot_IsTheOppositeOfLooted(bool looted, bool expected)
    {
        LootRules.IsFirstLoot(looted).ShouldBe(expected);
    }

    [Test]
    public void FirstOpen_IsFirstLoot()
    {
        // a box with Looted false, opened for the first time
        LootRules.IsFirstLoot(looted: false).ShouldBeTrue();
    }

    [Test]
    public void Reopen_IsNotFirstLoot()
    {
        // the same box after its first loot ended: Looted is now true
        LootRules.IsFirstLoot(looted: true).ShouldBeFalse();
    }

    [Test]
    public void AnotherBox_IsFirstLoot()
    {
        // a different box that has not been looted
        LootRules.IsFirstLoot(looted: false).ShouldBeTrue();
    }
}
