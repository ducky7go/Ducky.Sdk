using Ducky.Sdk.Contracts.ModProtocols;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Ownership of the MessageHub host (5.2): a Core stops only the host it started, and a host started by another
/// assembly is never claimed or released by this one.
/// </summary>
public class HostOwnershipTests
{
    private sealed class FakeHost
    {
    }

    [Test]
    public void FirstClaimWins_AndOnlyTheOwnerIsOwned()
    {
        var ownership = new HostOwnership<FakeHost>();
        var mine = new FakeHost();
        var other = new FakeHost();

        ownership.TryClaim(mine).ShouldBeTrue();
        ownership.TryClaim(other).ShouldBeFalse();
        ownership.IsOwned(mine).ShouldBeTrue();
        ownership.IsOwned(other).ShouldBeFalse();
    }

    [Test]
    public void Release_ReturnsTheClaimedHostOnce()
    {
        var ownership = new HostOwnership<FakeHost>();
        var mine = new FakeHost();
        ownership.TryClaim(mine);

        ownership.Release().ShouldBeSameAs(mine);
        ownership.Release().ShouldBeNull();
        ownership.IsOwned(mine).ShouldBeFalse();
    }

    [Test]
    public void HostOfAnotherAssembly_IsNotReleased()
    {
        // This assembly never claimed a host (it connected to one started elsewhere): releasing yields nothing to stop.
        var ownership = new HostOwnership<FakeHost>();

        ownership.Release().ShouldBeNull();
    }

    [Test]
    public void AfterRelease_TheNextCoreCanClaimANewHost()
    {
        var ownership = new HostOwnership<FakeHost>();
        var first = new FakeHost();
        var second = new FakeHost();
        ownership.TryClaim(first);
        ownership.Release();

        ownership.TryClaim(second).ShouldBeTrue();
        ownership.IsOwned(second).ShouldBeTrue();
    }
}
