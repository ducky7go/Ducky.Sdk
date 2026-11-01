using System;
using Ducky.Sdk.World;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>The size check of <see cref="BulletBlockers.Spawn"/>: valid and invalid sizes.</summary>
public class BlockerGeometryTests
{
    [Test]
    public void DefaultSize_IsAccepted()
    {
        Should.NotThrow(() => BlockerGeometry.Validate(width: 3f, height: 2f, thickness: 0.3f));
    }

    [Test]
    public void ZeroWidth_IsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BlockerGeometry.Validate(0f, 2f, 0.3f));
    }

    [Test]
    public void NegativeHeight_IsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BlockerGeometry.Validate(3f, -2f, 0.3f));
    }

    [Test]
    public void NaNThickness_IsRejected()
    {
        Should.Throw<ArgumentException>(() => BlockerGeometry.Validate(3f, 2f, float.NaN));
    }

    [Test]
    public void InfiniteWidth_IsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => BlockerGeometry.Validate(float.PositiveInfinity, 2f, 0.3f));
    }
}
