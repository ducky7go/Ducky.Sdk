using System;
using System.Collections.Generic;
using Ducky.Sdk.ModBehaviours;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The startup promotion line: its text shape, a single write to the sink, and that a failing sink never escapes.
/// </summary>
public class HagiCodePromotionTests
{
    [Test]
    public void Message_ContainsTheHagiCodeUrl()
    {
        HagiCodePromotion.Message.ShouldContain("https://www.hagicode.com");
    }

    [Test]
    public void Message_IsOnePhysicalLine()
    {
        HagiCodePromotion.Message.ShouldNotContain("\n");
        HagiCodePromotion.Message.ShouldNotContain("\r");
    }

    [Test]
    public void Write_SendsTheMessageOnceToTheSink()
    {
        var received = new List<string>();

        HagiCodePromotion.Write(received.Add);

        received.ShouldBe(new[] { HagiCodePromotion.Message });
    }

    [Test]
    public void Write_DoesNotThrowWhenTheSinkThrows()
    {
        Should.NotThrow(() => HagiCodePromotion.Write(_ => throw new InvalidOperationException("log is down")));
    }
}
