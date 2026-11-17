using Ducky.Sdk.State;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <see cref="GenerationLedger{TKey}"/>: "only the timer of the last hit takes the penalty back".
/// </summary>
[TestFixture]
public class GenerationLedgerTests
{
    [Test]
    public void The_first_generation_is_one_and_each_begin_counts_up()
    {
        var ledger = new GenerationLedger<string>();

        ledger.Begin("a").ShouldBe(1);
        ledger.Begin("b").ShouldBe(2);
        ledger.Begin("a").ShouldBe(3);
    }

    [Test]
    public void The_latest_timer_of_a_key_finishes_and_ends_the_state()
    {
        var ledger = new GenerationLedger<string>();
        var generation = ledger.Begin("wolf");

        ledger.TryFinish("wolf", generation).ShouldBeTrue();

        ledger.Count.ShouldBe(0);
    }

    [Test]
    public void An_older_timer_does_nothing_after_a_newer_hit_restarted_the_state()
    {
        // two hits: the first timer runs out while the second hit's penalty must stay
        var ledger = new GenerationLedger<string>();
        var first = ledger.Begin("wolf");
        var second = ledger.Begin("wolf");

        ledger.TryFinish("wolf", first).ShouldBeFalse();
        ledger.Count.ShouldBe(1);
        ledger.TryFinish("wolf", second).ShouldBeTrue();
        ledger.Count.ShouldBe(0);
    }

    [Test]
    public void A_timer_finishes_only_once()
    {
        var ledger = new GenerationLedger<string>();
        var generation = ledger.Begin("wolf");
        ledger.TryFinish("wolf", generation);

        ledger.TryFinish("wolf", generation).ShouldBeFalse();
    }

    [Test]
    public void Keys_are_independent_of_each_other()
    {
        var ledger = new GenerationLedger<string>();
        var a = ledger.Begin("a");
        var b = ledger.Begin("b");

        ledger.TryFinish("a", a).ShouldBeTrue();

        ledger.Count.ShouldBe(1);
        ledger.Keys.ShouldBe(new[] { "b" });
        ledger.TryFinish("b", b).ShouldBeTrue();
    }

    [Test]
    public void A_generation_of_another_key_does_not_finish_this_one()
    {
        var ledger = new GenerationLedger<string>();
        ledger.Begin("a");
        var b = ledger.Begin("b");

        ledger.TryFinish("a", b).ShouldBeFalse();

        ledger.Count.ShouldBe(2);
    }

    [Test]
    public void A_key_that_was_never_begun_cannot_be_finished()
    {
        var ledger = new GenerationLedger<string>();

        ledger.TryFinish("nobody", 1).ShouldBeFalse();
        ledger.TryFinish("nobody", 0).ShouldBeFalse();
    }

    [Test]
    public void Clear_releases_every_key_so_a_pending_timer_does_nothing()
    {
        // the feature went off: ReleaseAll took every penalty back, a timer that fires later must not take it back again
        var ledger = new GenerationLedger<string>();
        var generation = ledger.Begin("wolf");

        ledger.Clear();

        ledger.Count.ShouldBe(0);
        ledger.Keys.ShouldBeEmpty();
        ledger.TryFinish("wolf", generation).ShouldBeFalse();
    }

    [Test]
    public void Clear_does_not_reset_the_counter_so_an_old_timer_cannot_match_a_new_state()
    {
        // switch off, switch on again, hit again: the old timer of generation 1 must not match the new state
        var ledger = new GenerationLedger<string>();
        var old = ledger.Begin("wolf");
        ledger.Clear();

        var fresh = ledger.Begin("wolf");

        fresh.ShouldBeGreaterThan(old);
        ledger.TryFinish("wolf", old).ShouldBeFalse();
        ledger.TryFinish("wolf", fresh).ShouldBeTrue();
    }
}
