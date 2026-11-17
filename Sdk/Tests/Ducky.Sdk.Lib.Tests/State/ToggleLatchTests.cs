using Ducky.Sdk.State;

namespace Ducky.Sdk.Lib.Tests;

/// <summary><see cref="ToggleLatch"/>: paint while on, paint once more on the frame it goes off, report the change frames.</summary>
public class ToggleLatchTests
{
    [Test]
    public void OffAndWasOff_PaintsNothingAndChangesNothing()
    {
        var latch = new ToggleLatch();

        latch.Step(false).ShouldBe(new ToggleFrame(Paint: false, On: false, Changed: false));
    }

    [Test]
    public void SwitchingOn_PaintsAndReportsTheChange_ThenKeepsPaintingWithoutChange()
    {
        var latch = new ToggleLatch();

        latch.Step(true).ShouldBe(new ToggleFrame(Paint: true, On: true, Changed: true));
        latch.Step(true).ShouldBe(new ToggleFrame(Paint: true, On: true, Changed: false));
        latch.On.ShouldBeTrue();
    }

    [Test]
    public void SwitchingOff_PaintsOnceMoreAndReportsTheChange()
    {
        var latch = new ToggleLatch();
        latch.Step(true);

        latch.Step(false).ShouldBe(new ToggleFrame(Paint: true, On: false, Changed: true));
        latch.Step(false).ShouldBe(new ToggleFrame(Paint: false, On: false, Changed: false));
    }

    [Test]
    public void Clear_RecordsOffWithoutAChangeFrame()
    {
        var latch = new ToggleLatch();
        latch.Step(true);

        latch.Clear();

        latch.On.ShouldBeFalse();
        latch.Step(false).Changed.ShouldBeFalse();
    }
}
