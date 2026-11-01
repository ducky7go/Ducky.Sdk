using System.Collections.Generic;
using Ducky.Sdk.Rules;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The move and run registries of <c>CharacterControlRules</c>, on the pure <see cref="RuleRegistry{TValue,TContext}"/> with a
/// test value type: stop, force overriding stop by priority, equal priority, a throwing rule, and independent move and run
/// registries. The Harmony prefixes need the game and are not exercised here.
/// </summary>
public class ControlRulesTests
{
    // a stand-in for the move vector: the move registry is generic over its value type
    private struct Move
    {
        public int X;
    }

    private static RuleStep<Move, string> Stop => (ref Move move, string _) => move.X = 0;

    private static RuleStep<Move, string> Force(int x) => (ref Move move, string _) => move.X = x;

    [Test]
    public void Stop_SetsTheInputToZero()
    {
        var registry = new RuleRegistry<Move, string>();
        registry.Add(Stop, priority: 0);

        var move = new Move { X = 5 };
        registry.Apply(ref move, "enemy", _ => { });

        move.X.ShouldBe(0);
    }

    [Test]
    public void Force_AtHigherPriority_OverridesStop()
    {
        var registry = new RuleRegistry<Move, string>();
        registry.Add(Stop, priority: 0);
        registry.Add(Force(3), priority: 10);

        var move = new Move { X = 5 };
        registry.Apply(ref move, "enemy", _ => { });

        move.X.ShouldBe(3);
    }

    [Test]
    public void EqualPriority_RunsInRegistrationOrder()
    {
        var registry = new RuleRegistry<Move, string>();
        registry.Add(Force(3), priority: 0);
        registry.Add(Force(4), priority: 0);

        var move = new Move { X = 0 };
        registry.Apply(ref move, "enemy", _ => { });

        move.X.ShouldBe(4); // the later registration runs after, so its value is left
    }

    [Test]
    public void ThrowingRule_IsUndone_AndTheOthersStillRun()
    {
        var registry = new RuleRegistry<Move, string>();
        var failures = new List<System.Exception>();
        registry.Add((ref Move move, string _) =>
        {
            move.X = 99;
            throw new System.InvalidOperationException("boom");
        }, priority: 0);
        registry.Add(Force(2), priority: 1);

        var move = new Move { X = 5 };
        registry.Apply(ref move, "enemy", failures.Add);

        move.X.ShouldBe(2);
        failures.Count.ShouldBe(1);
    }

    [Test]
    public void MoveAndRunRegistries_AreIndependent()
    {
        var move = new RuleRegistry<Move, string>();
        var run = new RuleRegistry<bool, string>();
        var moveHandle = move.Add(Stop, priority: 0);
        run.Add((ref bool running, string _) => running = true, priority: 0);

        moveHandle.Dispose();

        move.Count.ShouldBe(0);
        run.Count.ShouldBe(1);
    }
}
