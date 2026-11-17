using System;
using System.Collections.Generic;
using System.Linq;
using Ducky.Sdk.State;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <see cref="AppliedSet{TKey}"/>: the "enemies this feature has already changed" set (the
/// <c>HashSet&lt;CharacterMainControl&gt;</c> a mod used to keep itself).
/// </summary>
[TestFixture]
public class AppliedSetTests
{
    /// <summary>A key that is equal by id, like a Unity object that the game hands out again as a new wrapper.</summary>
    private sealed class Enemy(int id)
    {
        public int Id { get; } = id;

        public override bool Equals(object? obj) => obj is Enemy other && other.Id == Id;

        public override int GetHashCode() => Id;
    }

    [Test]
    public void A_new_set_is_empty_and_contains_nothing()
    {
        var set = new AppliedSet<string>();

        set.Count.ShouldBe(0);
        set.Contains("wolf").ShouldBeFalse();
        set.ToList().ShouldBeEmpty();
    }

    [Test]
    public void Adding_a_key_makes_it_present_once()
    {
        var set = new AppliedSet<string>();

        set.Add("wolf").ShouldBeTrue();

        set.Contains("wolf").ShouldBeTrue();
        set.Count.ShouldBe(1);
    }

    [Test]
    public void A_repeated_add_reports_false_and_changes_nothing()
    {
        // the "a repeat spawn report adds nothing" rule
        var set = new AppliedSet<string>();
        set.Add("wolf");

        set.Add("wolf").ShouldBeFalse();

        set.Count.ShouldBe(1);
    }

    [Test]
    public void Keys_equal_by_value_count_as_the_same_key()
    {
        // HashSet's default equality is kept: the game's own Equals/GetHashCode decide when two characters are one
        var set = new AppliedSet<Enemy>();
        set.Add(new Enemy(7));

        set.Contains(new Enemy(7)).ShouldBeTrue();
        set.Add(new Enemy(7)).ShouldBeFalse();
        set.Contains(new Enemy(8)).ShouldBeFalse();
        set.Count.ShouldBe(1);
    }

    [Test]
    public void Removing_a_key_forgets_only_that_key()
    {
        var set = new AppliedSet<string>();
        set.Add("a");
        set.Add("b");

        set.Remove("a").ShouldBeTrue();

        set.Contains("a").ShouldBeFalse();
        set.Contains("b").ShouldBeTrue();
        set.Count.ShouldBe(1);
    }

    [Test]
    public void Removing_a_key_that_was_never_added_reports_false()
    {
        var set = new AppliedSet<string>();
        set.Add("a");

        set.Remove("zzz").ShouldBeFalse();

        set.Count.ShouldBe(1);
    }

    [Test]
    public void A_removed_key_can_be_added_again()
    {
        // an enemy restored when the feature goes off and changed again when it comes back on
        var set = new AppliedSet<string>();
        set.Add("a");
        set.Remove("a");

        set.Add("a").ShouldBeTrue();

        set.Contains("a").ShouldBeTrue();
    }

    [Test]
    public void Clear_forgets_every_key()
    {
        var set = new AppliedSet<string>();
        set.Add("a");
        set.Add("b");
        set.Add("c");

        set.Clear();

        set.Count.ShouldBe(0);
        set.Contains("b").ShouldBeFalse();
        set.ToList().ShouldBeEmpty();
    }

    [Test]
    public void Clear_on_an_empty_set_is_harmless()
    {
        var set = new AppliedSet<string>();

        Should.NotThrow(() => set.Clear());

        set.Count.ShouldBe(0);
    }

    [Test]
    public void Union_adds_the_new_keys_and_keeps_the_old_ones()
    {
        var set = new AppliedSet<string>();
        set.Add("a");

        set.UnionWith(new[] { "b", "a", "c" });

        set.OrderBy(k => k).ShouldBe(new[] { "a", "b", "c" });
    }

    [Test]
    public void Enumeration_yields_each_key_once_in_the_order_of_a_hash_set()
    {
        var set = new AppliedSet<int>();
        var reference = new HashSet<int>();
        foreach (var key in new[] { 5, 3, 9, 3, 1 })
        {
            set.Add(key);
            reference.Add(key);
        }

        set.ToList().ShouldBe(reference.ToList());
        set.Count.ShouldBe(4);
    }

    [Test]
    public void A_snapshot_survives_removals_made_while_looping_over_it()
    {
        // the restore loops (ForgetGone, RemoveAll) remove keys while they walk the keys they started with
        var set = new AppliedSet<string>();
        set.Add("a");
        set.Add("b");
        set.Add("c");
        var seen = new List<string>();

        foreach (var key in set.Snapshot())
        {
            seen.Add(key);
            set.Remove(key);
        }

        seen.Count.ShouldBe(3);
        set.Count.ShouldBe(0);
    }

    [Test]
    public void A_snapshot_is_a_copy_that_later_changes_do_not_touch()
    {
        var set = new AppliedSet<string>();
        set.Add("a");
        var copy = set.Snapshot();

        set.Add("b");
        set.Remove("a");

        copy.ShouldBe(new[] { "a" });
    }

    [Test]
    public void Adding_during_enumeration_throws_like_a_hash_set()
    {
        // behavior kept from HashSet: a caller must not add a key inside a plain foreach over its set
        var set = new AppliedSet<string>();
        set.Add("a");
        set.Add("b");

        Should.Throw<InvalidOperationException>(() =>
        {
            foreach (var key in set)
            {
                set.Add(key + "!");
            }
        });
    }
}
