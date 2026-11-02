using System.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.HttpEndpoints;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Public host facade paths that need no game and no hub: a forward before the claim and a stop when the role is not held
/// (task 4.1). Map through the shared client needs the mod id from the game, so it is verified by the client tests and in the
/// game (tasks 6.2 to 6.4).
/// </summary>
[TestFixture]
public class HttpHostContractTests
{
    [Test]
    public async Task ForwardBeforeStartReturns503()
    {
        var response = await Contract.HttpHost.ForwardAsync(new HttpEndpointRequest { Method = "GET", Path = "/Acme.Dash/x" });

        response.Status.ShouldBe(503);
    }

    [Test]
    public void StopBeforeStartDoesNotThrow()
    {
        Should.NotThrow(() => Contract.HttpHost.Stop());
        Contract.HttpHost.IsHost.ShouldBeFalse();
    }

    [Test]
    public void StaticReleaseIsIdempotentWithoutASharedInstance()
    {
        // Both shared teardown calls of ModBehaviourBase are safe to repeat when nothing was created. They never reach the
        // hub or Unity, because they only look at the shared instance that does not exist yet.
        Should.NotThrow(() =>
        {
            HttpEndpointClient.Release();
            HttpEndpointClient.Release();
            HttpHostCore.Release();
            HttpHostCore.Release();
        });
    }
}
