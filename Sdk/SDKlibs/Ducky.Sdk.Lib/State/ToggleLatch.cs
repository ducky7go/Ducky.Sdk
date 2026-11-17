namespace Ducky.Sdk.State;

/// <summary>
/// The result of one <see cref="ToggleLatch.Step"/>. <see cref="Paint"/>: this frame must write the picture; <see cref="On"/>:
/// whether the switch is on after this frame; <see cref="Changed"/>: the state differs from the last frame.
/// </summary>
public readonly record struct ToggleFrame(bool Paint, bool On, bool Changed);

/// <summary>
/// Remembers the switch state of the last frame. While it is off and was off in the last frame nothing needs to be painted;
/// while it is on every frame writes the picture; on the frame it goes from off to on, or from on to off,
/// <see cref="ToggleFrame.Changed"/> is true, which starts or ends the state record of the feature.
/// </summary>
public sealed class ToggleLatch
{
    /// <summary>Whether the switch was on after the last frame.</summary>
    public bool On { get; private set; }

    /// <summary>Records <paramref name="wanted"/> as this frame's state and reports what the frame has to do.</summary>
    public ToggleFrame Step(bool wanted)
    {
        var changed = wanted != On;
        var paint = wanted || On;
        On = wanted;
        return new ToggleFrame(paint, wanted, changed);
    }

    /// <summary>Records the switch as off without producing a change (no <see cref="ToggleFrame.Changed"/> frame).</summary>
    public void Clear() => On = false;
}
