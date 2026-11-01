using System;
using System.Collections.Generic;
using Ducky.Sdk.Restore;
using Ducky.Sdk.Rules;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The pure part of <see cref="RestoreRules"/>: each kind has its own registry and patch callbacks, and patch identifiers
/// differ between kinds and between copies. The Harmony prefixes need the game and are not exercised here.
/// </summary>
public class RestoreRulesTests
{
    private static RestoreRuleTable NewTable(List<string> events) => new(
        kind => () => events.Add("install:" + kind),
        kind => () => events.Add("uninstall:" + kind));

    private static RuleStep<float, RestoreContext> NoOp => (ref float _, RestoreContext __) => { };

    [Test]
    public void Kinds_AreIndependent_RemovingHealthLeavesEnergy()
    {
        var events = new List<string>();
        var table = NewTable(events);

        var health = table.For(RestoreKind.Health).Add(NoOp, 0);
        table.For(RestoreKind.Energy).Add(NoOp, 0);
        events.ShouldBe(new[] { "install:Health", "install:Energy" });

        health.Dispose();

        events.ShouldBe(new[] { "install:Health", "install:Energy", "uninstall:Health" });
        table.For(RestoreKind.Energy).Count.ShouldBe(1);
    }

    [Test]
    public void FirstRuleInstallsOnce_LastRuleUninstallsOnce()
    {
        var events = new List<string>();
        var table = NewTable(events);

        var a = table.For(RestoreKind.Water).Add(NoOp, 0);
        var b = table.For(RestoreKind.Water).Add(NoOp, 0);
        a.Dispose();
        b.Dispose();

        events.ShouldBe(new[] { "install:Water", "uninstall:Water" });
    }

    [Test]
    public void PatchIds_DifferBetweenTwoInstances()
    {
        var first = RestoreRuleTable.PatchIdFor("Ducky.Sdk", RestoreKind.Health, Guid.NewGuid());
        var second = RestoreRuleTable.PatchIdFor("Ducky.Sdk", RestoreKind.Health, Guid.NewGuid());

        first.ShouldNotBe(second);
    }

    [Test]
    public void PatchIds_DifferBetweenKinds_AndHaveTheDocumentedShape()
    {
        var guid = Guid.NewGuid();
        var health = RestoreRuleTable.PatchIdFor("Ducky.Sdk", RestoreKind.Health, guid);
        var energy = RestoreRuleTable.PatchIdFor("Ducky.Sdk", RestoreKind.Energy, guid);

        health.ShouldNotBe(energy);
        health.ShouldBe("Ducky.Sdk.RestoreRules.Health." + guid.ToString("N"));
    }
}
