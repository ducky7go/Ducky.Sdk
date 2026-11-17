using Ducky.Sdk.State;

namespace Ducky.Sdk.Lib.Tests;

/// <summary><see cref="NearestPicker"/>: the nearest eligible candidate within a radius.</summary>
public class NearestPickerTests
{
    [Test]
    public void NoCandidates_PicksNothing()
    {
        NearestPicker.Pick(System.Array.Empty<NearbyCandidate>(), 10f).ShouldBeNull();
    }

    [Test]
    public void PicksTheNearestEligibleOne()
    {
        var candidates = new[]
        {
            new NearbyCandidate(1, true, 5f),
            new NearbyCandidate(2, true, 2f),
            new NearbyCandidate(3, true, 8f),
        };

        NearestPicker.Pick(candidates, 10f).ShouldBe(2);
    }

    [Test]
    public void SkipsTheIneligibleAndTheExcludedAndThoseBeyondTheRadius()
    {
        var candidates = new[]
        {
            new NearbyCandidate(1, false, 1f),
            new NearbyCandidate(2, true, 2f),
            new NearbyCandidate(3, true, 20f),
            new NearbyCandidate(4, true, 4f),
        };

        NearestPicker.Pick(candidates, 10f, excludeId: 2).ShouldBe(4);
        NearestPicker.Pick(candidates, 3f, excludeId: 2).ShouldBeNull();
    }

    [Test]
    public void OnATie_TheFirstWins_AndTheRadiusIsInclusive()
    {
        var candidates = new[]
        {
            new NearbyCandidate(7, true, 6f),
            new NearbyCandidate(8, true, 6f),
        };

        NearestPicker.Pick(candidates, 6f).ShouldBe(7);
    }
}
