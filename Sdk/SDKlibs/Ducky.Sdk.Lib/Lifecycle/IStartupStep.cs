namespace Ducky.Sdk.Lifecycle;

/// <summary>
/// One step of a mod's start. <see cref="LifecycleRunner"/> runs the steps in the order the mod planned; the concrete steps (the
/// game-bound ones) live in the mod and share a context with each other.
/// </summary>
/// <typeparam name="TId">The mod's own id type for a step, usually an enum. A list handed to the runner must hold each planned id once, in plan order.</typeparam>
/// <typeparam name="TContext">What the steps share.</typeparam>
public interface IStartupStep<out TId, in TContext>
{
    /// <summary>The step this is.</summary>
    TId Id { get; }

    /// <summary>Runs the step. An exception is not caught: it ends the start and no later step runs.</summary>
    StepOutcome Run(TContext context);
}
