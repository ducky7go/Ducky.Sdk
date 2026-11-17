namespace Ducky.Sdk.Lifecycle;

/// <summary>What a startup step tells the sequence after it has run.</summary>
public enum StepOutcome
{
    /// <summary>The sequence goes on with the next step.</summary>
    Continue,

    /// <summary>The sequence ends here and no later step runs: for example the early return of a start that finds its host already there.</summary>
    Halt,
}
