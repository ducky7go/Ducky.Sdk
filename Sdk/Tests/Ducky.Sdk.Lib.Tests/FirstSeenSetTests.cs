using Ducky.Sdk.GameEvents;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The deduplication of <see cref="EnemyNoticed"/> (<see cref="FirstSeenSet{TKey}"/>): first call, repeats, a second instance,
/// clearing on level change, and the scope of one key.
/// </summary>
public class FirstSeenSetTests
{
    [Test]
    public void FirstCall_IsFirst()
    {
        var set = new FirstSeenSet<int>();

        set.TryMarkFirst(42).ShouldBeTrue();
    }

    [Test]
    public void Repeats287Times_GiveOneFirst()
    {
        var set = new FirstSeenSet<int>();

        var firsts = 0;
        for (var i = 0; i < 287; i++)
        {
            if (set.TryMarkFirst(42))
            {
                firsts++;
            }
        }

        firsts.ShouldBe(1);
        set.Count.ShouldBe(1);
    }

    [Test]
    public void SecondInstance_IsFirstToo()
    {
        var set = new FirstSeenSet<int>();
        set.TryMarkFirst(42);

        set.TryMarkFirst(43).ShouldBeTrue();
    }

    [Test]
    public void Clear_ForgetsEveryKey()
    {
        var set = new FirstSeenSet<int>();
        set.TryMarkFirst(42);

        set.Clear();

        set.TryMarkFirst(42).ShouldBeTrue();
        set.Count.ShouldBe(1);
    }
}
