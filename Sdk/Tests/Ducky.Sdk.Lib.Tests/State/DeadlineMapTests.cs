using System.Collections.Generic;
using System.Linq;
using Ducky.Sdk.State;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <see cref="DeadlineMap{TKey}"/>: the timed state of features that slow or break an enemy for a few seconds.
/// </summary>
[TestFixture]
public class DeadlineMapTests
{
    private static bool NobodyIsGone(string key) => false;

    [Test]
    public void A_state_ends_exactly_at_its_end_time()
    {
        var map = new DeadlineMap<string>();
        map.Set("wolf", 10f);

        map.Expired(9.99f, NobodyIsGone).ShouldBeEmpty();
        map.Expired(10f, NobodyIsGone).ShouldBe(new[] { "wolf" });
        map.Expired(10.5f, NobodyIsGone).ShouldBe(new[] { "wolf" });
    }

    [Test]
    public void Expired_does_not_remove_anything()
    {
        // the caller removes each key itself (and takes the slow back) after it asks
        var map = new DeadlineMap<string>();
        map.Set("wolf", 1f);

        map.Expired(5f, NobodyIsGone);

        map.Contains("wolf").ShouldBeTrue();
        map.Count.ShouldBe(1);
    }

    [Test]
    public void Only_the_keys_that_are_over_are_returned()
    {
        var map = new DeadlineMap<string>();
        map.Set("early", 3f);
        map.Set("late", 8f);
        map.Set("middle", 5f);

        map.Expired(5f, NobodyIsGone).OrderBy(k => k).ShouldBe(new[] { "early", "middle" });
    }

    [Test]
    public void A_key_whose_game_object_is_gone_is_over_before_its_time()
    {
        // the destroyed-object check (character == null on a Unity object) is passed in by the caller
        var map = new DeadlineMap<string>();
        map.Set("destroyed", 100f);
        map.Set("alive", 100f);

        var done = map.Expired(1f, key => key == "destroyed");

        done.ShouldBe(new[] { "destroyed" });
    }

    [Test]
    public void The_gone_check_is_not_asked_when_the_time_is_already_up()
    {
        // the original condition is `now >= until || key == null`: the time is tested first
        var map = new DeadlineMap<string>();
        map.Set("wolf", 1f);
        var asked = new List<string>();

        map.Expired(2f, key =>
        {
            asked.Add(key);
            return false;
        }).ShouldBe(new[] { "wolf" });

        asked.ShouldBeEmpty();
    }

    [Test]
    public void Expired_keys_come_in_the_order_the_map_holds_them()
    {
        var map = new DeadlineMap<string>();
        map.Set("first", 1f);
        map.Set("second", 1f);
        map.Set("third", 1f);

        map.Expired(2f, NobodyIsGone).ShouldBe(map.Keys.ToList());
    }

    [Test]
    public void Setting_a_key_again_moves_its_end_time()
    {
        // a new critical hit restarts the slow
        var map = new DeadlineMap<string>();
        map.Set("wolf", 3f);

        map.Set("wolf", 9f);

        map.Count.ShouldBe(1);
        map.TryGet("wolf", out var until).ShouldBeTrue();
        until.ShouldBe(9f);
        map.Expired(5f, NobodyIsGone).ShouldBeEmpty();
    }

    [Test]
    public void A_removed_key_is_no_longer_reported_or_present()
    {
        var map = new DeadlineMap<string>();
        map.Set("wolf", 1f);

        map.Remove("wolf").ShouldBeTrue();

        map.Contains("wolf").ShouldBeFalse();
        map.TryGet("wolf", out _).ShouldBeFalse();
        map.Expired(5f, NobodyIsGone).ShouldBeEmpty();
        map.Remove("wolf").ShouldBeFalse();
    }

    [Test]
    public void A_cooldown_map_holds_the_ready_time_and_reports_it_when_it_is_reached()
    {
        // Crit Slow keeps a second map: the time before which the same enemy is not slowed again
        var cooldown = new DeadlineMap<string>();
        cooldown.Set("wolf", 4f);

        cooldown.TryGet("wolf", out var readyAt).ShouldBeTrue();
        readyAt.ShouldBe(4f);
        cooldown.Expired(3.9f, NobodyIsGone).ShouldBeEmpty();
        cooldown.Expired(4f, NobodyIsGone).ShouldBe(new[] { "wolf" });
    }

    [Test]
    public void Clear_takes_every_key_out()
    {
        var map = new DeadlineMap<string>();
        map.Set("a", 1f);
        map.Set("b", 2f);

        map.Clear();

        map.Count.ShouldBe(0);
        map.Keys.ShouldBeEmpty();
        map.Expired(100f, NobodyIsGone).ShouldBeEmpty();
    }

    [Test]
    public void An_empty_map_has_nothing_to_report()
    {
        new DeadlineMap<string>().Expired(0f, NobodyIsGone).ShouldBeEmpty();
    }
}
