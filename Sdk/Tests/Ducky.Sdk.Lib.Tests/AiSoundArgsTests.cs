using System;
using Ducky.Sdk.Ai;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>The radius check of <see cref="AiSounds.Make"/>: 0, positive, negative, NaN and infinity.</summary>
public class AiSoundArgsTests
{
    [TestCase(0f)]
    [TestCase(40f)]
    [TestCase(0.001f)]
    public void ZeroAndPositive_AreAccepted(float radius)
    {
        Should.NotThrow(() => AiSoundArgs.Validate(radius));
    }

    [Test]
    public void Negative_IsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => AiSoundArgs.Validate(-1f));
    }

    [Test]
    public void NaN_IsRejected()
    {
        Should.Throw<ArgumentException>(() => AiSoundArgs.Validate(float.NaN));
    }

    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void Infinity_IsRejected(float radius)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => AiSoundArgs.Validate(radius));
    }
}
