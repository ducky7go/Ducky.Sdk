using System;
using System.Collections.Generic;
using Ducky.Sdk.Combat;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The dispatch rules of <see cref="PlayerCombat"/>, checked on the pure <see cref="HandlerSet{TArgs}"/> with string
/// arguments, and the hit-direction rule of <see cref="HitDirection"/>. The game-side events are not touched here.
/// </summary>
public class PlayerCombatTests
{
    private sealed class Counters
    {
        public int Subscribed;
        public int Released;
        public List<Exception> Failures { get; } = new();
    }

    private static HandlerSet<string> NewSet(Counters counters) => new(
        onFirst: () => counters.Subscribed++,
        onLast: () => counters.Released++,
        onFailure: ex => counters.Failures.Add(ex));

    [Test]
    public void TwoHandlers_MakeOneGameSideSubscription()
    {
        var counters = new Counters();
        var set = NewSet(counters);
        var seen = new List<string>();

        using var first = set.Add(args => seen.Add("a:" + args));
        using var second = set.Add(args => seen.Add("b:" + args));
        set.Raise("hit");

        counters.Subscribed.ShouldBe(1);
        seen.ShouldBe(new[] { "a:hit", "b:hit" });
    }

    [Test]
    public void GameSideSubscription_IsReleasedWithTheLastHandler()
    {
        var counters = new Counters();
        var set = NewSet(counters);
        var first = set.Add(_ => { });
        var second = set.Add(_ => { });

        first.Dispose();
        counters.Released.ShouldBe(0);

        second.Dispose();
        counters.Released.ShouldBe(1);
        set.Count.ShouldBe(0);
    }

    [Test]
    public void ThrowingHandler_DoesNotStopTheOther()
    {
        var counters = new Counters();
        var set = NewSet(counters);
        var calledAfterThrow = false;
        using var thrower = set.Add(_ => throw new InvalidOperationException("boom"));
        using var other = set.Add(_ => calledAfterThrow = true);

        set.Raise("hit");

        calledAfterThrow.ShouldBeTrue();
        counters.Failures.Count.ShouldBe(1);
    }

    [Test]
    public void DisposingAHandleTwice_HasNoFurtherEffect()
    {
        var counters = new Counters();
        var set = NewSet(counters);
        var keep = set.Add(_ => { });
        var handle = set.Add(_ => { });

        handle.Dispose();
        handle.Dispose();

        set.Count.ShouldBe(1);
        counters.Released.ShouldBe(0);
        keep.Dispose();
        counters.Released.ShouldBe(1);
    }

    [Test]
    public void SameHandlerAddedTwice_IsTwoRegistrations()
    {
        var counters = new Counters();
        var set = NewSet(counters);
        var calls = 0;
        Action<string> handler = _ => calls++;

        var first = set.Add(handler);
        var second = set.Add(handler);
        first.Dispose();

        set.Count.ShouldBe(1);
        set.Raise("hit");
        calls.ShouldBe(1);
        second.Dispose();
    }

    [Test]
    public void SubscriptionIsMadeAgainAfterTheLastRelease()
    {
        var counters = new Counters();
        var set = NewSet(counters);

        set.Add(_ => { }).Dispose();
        set.Add(_ => { }).Dispose();

        counters.Subscribed.ShouldBe(2);
        counters.Released.ShouldBe(2);
    }

    [Test]
    public void RaiseWithoutHandlers_DoesNothing()
    {
        var counters = new Counters();
        var set = NewSet(counters);

        Should.NotThrow(() => set.Raise("hit"));
        counters.Subscribed.ShouldBe(0);
    }

    [Test]
    public void PlayerToEnemy_OnlyWhenThePlayerHitsAnotherCharacter()
    {
        HitDirection.PlayerToEnemy(victimIsCharacter: true, hasAttacker: true, victimIsPlayer: false, attackerIsPlayer: true)
            .ShouldBeTrue();
    }

    [Test]
    public void PlayerToEnemy_NoAttacker_IsFalse() =>
        HitDirection.PlayerToEnemy(true, hasAttacker: false, victimIsPlayer: false, attackerIsPlayer: false).ShouldBeFalse();

    [Test]
    public void PlayerToEnemy_PlayerIsTheVictim_IsFalse() =>
        HitDirection.PlayerToEnemy(true, true, victimIsPlayer: true, attackerIsPlayer: true).ShouldBeFalse();

    [Test]
    public void PlayerToEnemy_EnemyHitsPlayer_IsFalse() =>
        HitDirection.PlayerToEnemy(true, true, victimIsPlayer: true, attackerIsPlayer: false).ShouldBeFalse();

    [Test]
    public void PlayerToEnemy_NotACharacter_IsFalse() =>
        HitDirection.PlayerToEnemy(victimIsCharacter: false, true, false, true).ShouldBeFalse();
}
