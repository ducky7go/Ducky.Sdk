using System;
using System.Collections.Generic;
using System.Linq;

namespace Ducky.Sdk.Lifecycle;

/// <summary>
/// Runs a mod's start or stop as an ordered list of steps: it checks that the list is the planned order, then runs the steps one
/// after the other. Nothing is caught here. An exception from a step ends the run and reaches the caller, as it would in one
/// long method. The plan (the ids in order) is the mod's contract; a list in another order, or one that misses or repeats a step,
/// is refused before any step runs, so no half-started mod is left behind.
/// </summary>
public static class LifecycleRunner
{
    /// <summary>
    /// Runs the start steps. A step that returns <see cref="StepOutcome.Halt"/> ends the start, and the result is Halt; otherwise
    /// every step ran and the result is Continue.
    /// </summary>
    /// <param name="plannedOrder">The ids of the steps, in the order they must run.</param>
    /// <param name="steps">The steps; their ids must equal <paramref name="plannedOrder"/>.</param>
    /// <param name="context">What the steps share.</param>
    /// <exception cref="ArgumentNullException">A list is null.</exception>
    /// <exception cref="InvalidOperationException">The steps are not the planned order.</exception>
    public static StepOutcome RunStartup<TId, TContext>(
        IReadOnlyList<TId> plannedOrder, IReadOnlyList<IStartupStep<TId, TContext>> steps, TContext context)
    {
        if (steps == null)
        {
            throw new ArgumentNullException(nameof(steps));
        }

        Check("start", steps.Select(step => step.Id).ToList(), plannedOrder);
        foreach (var step in steps)
        {
            if (step.Run(context) == StepOutcome.Halt)
            {
                return StepOutcome.Halt;
            }
        }

        return StepOutcome.Continue;
    }

    /// <summary>Runs the stop steps, in the planned order.</summary>
    /// <param name="plannedOrder">The ids of the steps, in the order they must run.</param>
    /// <param name="steps">The steps; their ids must equal <paramref name="plannedOrder"/>.</param>
    /// <param name="context">What the steps share.</param>
    /// <exception cref="ArgumentNullException">A list is null.</exception>
    /// <exception cref="InvalidOperationException">The steps are not the planned order.</exception>
    public static void RunStop<TId, TContext>(
        IReadOnlyList<TId> plannedOrder, IReadOnlyList<IStopStep<TId, TContext>> steps, TContext context)
    {
        if (steps == null)
        {
            throw new ArgumentNullException(nameof(steps));
        }

        Check("stop", steps.Select(step => step.Id).ToList(), plannedOrder);
        foreach (var step in steps)
        {
            step.Run(context);
        }
    }

    // a list that is not the plan is refused before its first step runs, so no half-started mod is left behind
    private static void Check<TId>(string what, IReadOnlyList<TId> actual, IReadOnlyList<TId>? planned)
    {
        if (planned == null)
        {
            throw new ArgumentNullException(nameof(planned));
        }

        if (!actual.SequenceEqual(planned))
        {
            throw new InvalidOperationException(
                "the " + what + " steps are not the planned order: got [" + string.Join(", ", actual)
                + "], planned [" + string.Join(", ", planned) + "]");
        }
    }
}
