using System.Collections.Generic;

namespace Ducky.Sdk.State;

/// <summary>One candidate for "the nearest one": its id, whether it may be picked, and its distance from the reference point.</summary>
public readonly struct NearbyCandidate
{
    /// <summary>Creates a candidate.</summary>
    public NearbyCandidate(int id, bool eligible, float distance)
    {
        Id = id;
        Eligible = eligible;
        Distance = distance;
    }

    /// <summary>The caller's id of the candidate (for example an instance id).</summary>
    public int Id { get; }

    /// <summary>Whether it may be picked.</summary>
    public bool Eligible { get; }

    /// <summary>Its distance from the reference point.</summary>
    public float Distance { get; }
}

/// <summary>Picks the nearest eligible candidate within a radius.</summary>
public static class NearestPicker
{
    /// <summary>
    /// The id of the nearest eligible candidate within <paramref name="radius"/> (on a tie the first one wins), skipping
    /// <paramref name="excludeId"/>; null when there is none.
    /// </summary>
    public static int? Pick(IReadOnlyList<NearbyCandidate> candidates, float radius, int excludeId = int.MinValue)
    {
        int? best = null;
        var bestDistance = float.MaxValue;
        foreach (var candidate in candidates)
        {
            if (candidate.Id == excludeId || !candidate.Eligible)
            {
                continue;
            }

            if (candidate.Distance <= radius && candidate.Distance < bestDistance)
            {
                best = candidate.Id;
                bestDistance = candidate.Distance;
            }
        }

        return best;
    }
}
