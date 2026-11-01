using System.Collections.Generic;
using Ducky.Sdk.Buffs;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The add and remove rules of <see cref="StatBuff"/>, checked on the pure <see cref="StatModifierLedger{TStat,TModifier}"/>
/// with a fake stat store. The real buff needs the game's character and stat objects.
/// </summary>
public class StatBuffTests
{
    private static Dictionary<string, List<string>> NewStore() => new()
    {
        ["ViewDistance"] = new List<string> { "base" },
    };

    // What the buff does on each setup: take back its earlier modifiers, then add its new ones.
    private static void Apply(Dictionary<string, List<string>> store, StatModifierLedger<string, string> ledger,
        params string[] modifiers)
    {
        ledger.RemoveAll((stat, modifier) => store[stat].Remove(modifier));
        foreach (var modifier in modifiers)
        {
            store["ViewDistance"].Add(modifier);
            ledger.Add("ViewDistance", modifier);
        }
    }

    private static void Remove(Dictionary<string, List<string>> store, StatModifierLedger<string, string> ledger) =>
        ledger.RemoveAll((stat, modifier) => store[stat].Remove(modifier));

    [Test]
    public void Apply_AddsTheModifiers()
    {
        var store = NewStore();
        var ledger = new StatModifierLedger<string, string>();

        Apply(store, ledger, "buff1");

        store["ViewDistance"].ShouldBe(new[] { "base", "buff1" });
        ledger.Count.ShouldBe(1);
    }

    [Test]
    public void Removal_TakesBackExactlyTheBuffsModifiers()
    {
        var store = NewStore();
        var ledger = new StatModifierLedger<string, string>();
        Apply(store, ledger, "buff1");

        Remove(store, ledger);

        store["ViewDistance"].ShouldBe(new[] { "base" });
        ledger.Count.ShouldBe(0);
    }

    [Test]
    public void OtherModifiers_AreUntouchedWhenTheBuffIsDestroyed()
    {
        var store = NewStore();
        var ledger = new StatModifierLedger<string, string>();
        store["ViewDistance"].Add("gear");
        Apply(store, ledger, "buff1");

        Remove(store, ledger);

        store["ViewDistance"].ShouldBe(new[] { "base", "gear" });
    }

    [Test]
    public void AppliedTwice_LeavesNoLeftoversAfterDestroy()
    {
        var store = NewStore();
        var ledger = new StatModifierLedger<string, string>();

        Apply(store, ledger, "buff1");
        Apply(store, ledger, "buff1");

        // the second apply replaces the first one's modifier instead of stacking a second copy
        store["ViewDistance"].ShouldBe(new[] { "base", "buff1" });

        Remove(store, ledger);

        store["ViewDistance"].ShouldBe(new[] { "base" });
    }

    [Test]
    public void RemoveAll_OnAnEmptyLedger_DoesNothing()
    {
        var store = NewStore();
        var ledger = new StatModifierLedger<string, string>();

        Remove(store, ledger);

        store["ViewDistance"].ShouldBe(new[] { "base" });
    }

    [Test]
    public void RemoveAll_PassesThePairsInTheOrderTheyWereAdded()
    {
        var ledger = new StatModifierLedger<string, string>();
        ledger.Add("A", "first");
        ledger.Add("B", "second");
        var seen = new List<string>();

        ledger.RemoveAll((stat, modifier) => seen.Add(stat + ":" + modifier));

        seen.ShouldBe(new[] { "A:first", "B:second" });
    }
}
