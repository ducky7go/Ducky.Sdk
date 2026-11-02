using Ducky.Sdk.Contracts;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The per-mod instance table behind the client contracts (task 7.5): one live instance per mod id, owner-aware removal,
/// release that empties the table and a fresh instance after a release. The contracts themselves are MonoBehaviours and
/// need Unity, so their rules are tested through this table.
/// </summary>
[TestFixture]
public class ContractInstanceTableTests
{
    private const string ModId = "Acme.Client";

    private sealed class FakeContract
    {
        public bool Alive { get; set; } = true;
    }

    private ContractInstanceTable<FakeContract> _table = null!;
    private int _created;

    [SetUp]
    public void SetUp()
    {
        _table = new ContractInstanceTable<FakeContract>();
        _created = 0;
    }

    private FakeContract Create(string modId)
    {
        _created++;
        return new FakeContract();
    }

    private FakeContract Get() => _table.GetOrAdd(ModId, Create, c => c.Alive);

    [Test]
    public void SameModIdReturnsTheSameLiveInstance()
    {
        var first = Get();

        Get().ShouldBeSameAs(first);
        _created.ShouldBe(1);
    }

    [Test]
    public void DeadInstanceIsReplacedByAFreshOne()
    {
        var first = Get();
        first.Alive = false;

        var second = Get();

        second.ShouldNotBeSameAs(first);
        _created.ShouldBe(2);
    }

    [Test]
    public void RemoveIfOwnedDoesNotRemoveANewerInstance()
    {
        var older = Get();
        older.Alive = false;
        var newer = Get();

        _table.RemoveIfOwned(ModId, older).ShouldBeFalse();

        Get().ShouldBeSameAs(newer);
    }

    [Test]
    public void RemoveIfOwnedRemovesTheCurrentInstance()
    {
        var current = Get();

        _table.RemoveIfOwned(ModId, current).ShouldBeTrue();
        Get().ShouldNotBeSameAs(current);
    }

    [Test]
    public void TakeAllTwiceReturnsNothingTheSecondTime()
    {
        Get();

        _table.TakeAll().Count.ShouldBe(1);
        _table.TakeAll().ShouldBeEmpty();
    }

    [Test]
    public void GetAfterReleaseCreatesAFreshInstance()
    {
        var released = Get();
        _table.TakeAll();

        var fresh = Get();

        fresh.ShouldNotBeSameAs(released);
        _created.ShouldBe(2);
    }

    [Test]
    public void ReleasedInstanceCannotRemoveTheFreshOne()
    {
        var released = Get();
        _table.TakeAll();
        var fresh = Get();

        _table.RemoveIfOwned(ModId, released).ShouldBeFalse();

        Get().ShouldBeSameAs(fresh);
    }

    [Test]
    public void TakeAllOfAnEmptyTableIsEmpty()
    {
        _table.TakeAll().ShouldBeEmpty();
    }
}
