using System;
using System.Collections.Generic;
using System.Linq;
using Ducky.Sdk.State;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <see cref="AppliedSet{TKey, TMemo}"/>: the "one entry per enemy" state (the
/// <c>Dictionary&lt;CharacterMainControl, ...&gt;</c> a mod used to keep itself): the value to put back, the modifier
/// added, a hit count.
/// </summary>
[TestFixture]
public class AppliedSetWithMemoTests
{
    [Test]
    public void A_new_map_is_empty()
    {
        var map = new AppliedSet<string, int>();

        map.Count.ShouldBe(0);
        map.ContainsKey("wolf").ShouldBeFalse();
        map.TryGetValue("wolf", out var memo).ShouldBeFalse();
        memo.ShouldBe(0);
    }

    [Test]
    public void Setting_a_note_adds_the_key_and_it_can_be_read_back()
    {
        var map = new AppliedSet<string, float>();

        map["wolf"] = 12.5f;

        map.ContainsKey("wolf").ShouldBeTrue();
        map["wolf"].ShouldBe(12.5f);
        map.TryGetValue("wolf", out var memo).ShouldBeTrue();
        memo.ShouldBe(12.5f);
        map.Count.ShouldBe(1);
    }

    [Test]
    public void Setting_a_note_again_replaces_it_without_adding_a_second_key()
    {
        // We1034: the tracking distance a wolf had before is the note; a repeat must not lose or duplicate the entry
        var map = new AppliedSet<string, float>();
        map["wolf"] = 1f;

        map["wolf"] = 2f;

        map["wolf"].ShouldBe(2f);
        map.Count.ShouldBe(1);
    }

    [Test]
    public void Reading_a_missing_key_with_the_indexer_throws_like_a_dictionary()
    {
        var map = new AppliedSet<string, int>();

        Should.Throw<KeyNotFoundException>(() => _ = map["nobody"]);
    }

    [Test]
    public void A_note_may_be_the_default_value_and_the_key_is_still_present()
    {
        // a hit counter that starts at 0 must not be mistaken for "not tracked"
        var map = new AppliedSet<string, int>();

        map["wolf"] = 0;

        map.ContainsKey("wolf").ShouldBeTrue();
        map.TryGetValue("wolf", out var memo).ShouldBeTrue();
        memo.ShouldBe(0);
    }

    [Test]
    public void Removing_a_key_forgets_its_note()
    {
        var map = new AppliedSet<string, int>();
        map["a"] = 1;
        map["b"] = 2;

        map.Remove("a").ShouldBeTrue();

        map.ContainsKey("a").ShouldBeFalse();
        map["b"].ShouldBe(2);
        map.Remove("a").ShouldBeFalse();
    }

    [Test]
    public void Clear_forgets_every_key_and_note()
    {
        var map = new AppliedSet<string, int>();
        map["a"] = 1;
        map["b"] = 2;

        map.Clear();

        map.Count.ShouldBe(0);
        map.Keys.ShouldBeEmpty();
        map.Values.ShouldBeEmpty();
    }

    [Test]
    public void Keys_and_values_come_in_the_order_the_dictionary_holds_them()
    {
        var map = new AppliedSet<string, int>();
        var reference = new Dictionary<string, int>();
        foreach (var (key, value) in new[] { ("x", 1), ("y", 2), ("z", 3) })
        {
            map[key] = value;
            reference[key] = value;
        }

        map.Keys.ToList().ShouldBe(reference.Keys.ToList());
        map.Values.ToList().ShouldBe(reference.Values.ToList());
        map.ToList().ShouldBe(reference.ToList());
    }

    [Test]
    public void Entries_can_be_deconstructed_in_a_foreach()
    {
        // We1329 walks the wanted flags per enemy as (enemy, flags)
        var map = new AppliedSet<string, int>();
        map["a"] = 3;
        map["b"] = 5;
        var sum = 0;

        foreach (var (key, flags) in map)
        {
            key.ShouldNotBeNullOrEmpty();
            sum += flags;
        }

        sum.ShouldBe(8);
    }

    [Test]
    public void A_snapshot_of_the_entries_survives_removals_made_while_looping_over_it()
    {
        // the restore loops walk a copy and remove each entry as they put it back
        var map = new AppliedSet<string, int>();
        map["a"] = 1;
        map["b"] = 2;
        map["c"] = 3;
        var restored = new List<(string, int)>();

        foreach (var pair in map.Snapshot())
        {
            restored.Add((pair.Key, pair.Value));
            map.Remove(pair.Key);
        }

        restored.ShouldBe(new[] { ("a", 1), ("b", 2), ("c", 3) });
        map.Count.ShouldBe(0);
    }

    [Test]
    public void A_snapshot_of_the_keys_is_a_copy()
    {
        var map = new AppliedSet<string, int>();
        map["a"] = 1;
        var keys = map.SnapshotKeys();

        map["b"] = 2;
        map.Remove("a");

        keys.ShouldBe(new[] { "a" });
    }

    [Test]
    public void Adding_a_key_during_a_key_enumeration_throws_like_a_dictionary()
    {
        var map = new AppliedSet<string, int>();
        map["a"] = 1;
        map["b"] = 2;

        Should.Throw<InvalidOperationException>(() =>
        {
            foreach (var key in map.Keys)
            {
                map[key + "!"] = 0;
            }
        });
    }
}
