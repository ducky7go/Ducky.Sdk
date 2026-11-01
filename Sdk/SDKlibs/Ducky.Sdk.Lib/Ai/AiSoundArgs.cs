using System;

namespace Ducky.Sdk.Ai;

/// <summary>
/// The argument check of <see cref="AiSounds.Make"/>, with no game object. A radius must be a finite number that is zero or
/// more. Zero is accepted.
/// </summary>
internal static class AiSoundArgs
{
    /// <summary>Throws an argument error when <paramref name="radius"/> is not a number, is infinite or is negative.</summary>
    public static void Validate(float radius)
    {
        if (float.IsNaN(radius))
        {
            throw new ArgumentException("The sound radius must be a number.", nameof(radius));
        }

        if (float.IsInfinity(radius))
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "The sound radius must be finite.");
        }

        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "The sound radius must not be negative.");
        }
    }
}
