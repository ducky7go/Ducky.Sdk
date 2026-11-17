namespace Ducky.Sdk.Lifecycle;

/// <summary>
/// One step of a mod's stop. <see cref="LifecycleRunner"/> runs the steps in the order the mod planned, which need not be the
/// reverse of the start.
/// </summary>
/// <typeparam name="TId">The mod's own id type for a step, usually an enum.</typeparam>
/// <typeparam name="TContext">What the steps share.</typeparam>
public interface IStopStep<out TId, in TContext>
{
    /// <summary>The step this is.</summary>
    TId Id { get; }

    /// <summary>Runs the step. An exception is not caught: it ends the stop and no later step runs.</summary>
    void Run(TContext context);
}
