using Ducky.Sdk.State;

namespace Ducky.Sdk.Lib.Tests;

/// <summary><see cref="SearchThrottle"/>: a search for scene objects that runs once per interval until it succeeds.</summary>
public class SearchThrottleTests
{
    [Test]
    public void FirstCall_IsAlwaysAllowed()
    {
        new SearchThrottle().TryBegin(0f).ShouldBeTrue();
    }

    [Test]
    public void CallInsideTheInterval_IsRefused()
    {
        var throttle = new SearchThrottle();
        throttle.TryBegin(10f);

        throttle.TryBegin(10f + SearchThrottle.IntervalSeconds - 0.01f).ShouldBeFalse();
    }

    [Test]
    public void CallAtTheEndOfTheInterval_IsAllowedAndSchedulesTheNextOne()
    {
        var throttle = new SearchThrottle();
        throttle.TryBegin(10f);
        var second = 10f + SearchThrottle.IntervalSeconds;

        throttle.TryBegin(second).ShouldBeTrue();
        throttle.TryBegin(second + 0.1f).ShouldBeFalse();
    }
}
