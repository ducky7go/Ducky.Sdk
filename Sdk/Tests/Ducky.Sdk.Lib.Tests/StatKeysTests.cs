using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ducky.Sdk.Buffs;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The <see cref="StatKeys"/> constants: each value is its own name, all are distinct and non-empty, and the required
/// keys are present. The modify path is checked on the pure <see cref="StatModifierLedger{TStat,TModifier}"/>, so no game
/// object is loaded.
/// </summary>
public class StatKeysTests
{
    private static readonly string[] RequiredKeys =
    {
        "EnergyCost", "WaterCost", "FoodGain", "HealGain", "DashSpeed", "DashCanControl", "WalkSoundRange",
        "RunSoundRange", "MeleeDamageMultiplier", "GunScatterMultiplier", "RecoilControl", "InventoryCapacity",
        "MaxWeight", "Moveability", "ViewDistance",
    };

    private static IReadOnlyList<(string Name, string Value)> Constants() =>
        typeof(StatKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (f.Name, (string)f.GetRawConstantValue()!))
            .ToList();

    [Test]
    public void EachValue_EqualsTheConstantsName()
    {
        foreach (var (name, value) in Constants())
        {
            value.ShouldBe(name);
        }
    }

    [Test]
    public void Values_AreDistinctAndNonEmpty()
    {
        var values = Constants().Select(c => c.Value).ToList();

        values.ShouldAllBe(v => !string.IsNullOrEmpty(v));
        values.Distinct().Count().ShouldBe(values.Count);
    }

    [Test]
    public void AllRequiredKeys_ArePresent()
    {
        var values = Constants().Select(c => c.Value).ToHashSet();

        foreach (var key in RequiredKeys)
        {
            values.ShouldContain(key);
        }
    }

    [Test]
    public void ModifyWithAConstant_ReachesTheStatOfThatKey()
    {
        // the same store and ledger a StatBuff uses, keyed by the constant instead of a literal
        var store = new Dictionary<string, List<string>>
        {
            [StatKeys.ViewDistance] = new List<string> { "base" },
            [StatKeys.EnergyCost] = new List<string> { "base" },
        };
        var ledger = new StatModifierLedger<string, string>();

        store[StatKeys.ViewDistance].Add("buff");
        ledger.Add(StatKeys.ViewDistance, "buff");

        store[StatKeys.ViewDistance].ShouldBe(new[] { "base", "buff" });
        store[StatKeys.EnergyCost].ShouldBe(new[] { "base" });
    }
}
