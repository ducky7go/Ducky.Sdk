using System;

namespace Ducky.Sdk.World;

/// <summary>
/// The size check of <see cref="BulletBlockers.Spawn"/>, with no game object: width, height and thickness must each be a finite
/// positive number.
/// </summary>
internal static class BlockerGeometry
{
    /// <summary>Throws an argument error when a dimension is not a number, is infinite or is not positive.</summary>
    public static void Validate(float width, float height, float thickness)
    {
        Check(width, nameof(width));
        Check(height, nameof(height));
        Check(thickness, nameof(thickness));
    }

    private static void Check(float value, string name)
    {
        if (float.IsNaN(value))
        {
            throw new ArgumentException($"{name} must be a number.", name);
        }

        if (float.IsInfinity(value) || value <= 0f)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be finite and positive.");
        }
    }
}
