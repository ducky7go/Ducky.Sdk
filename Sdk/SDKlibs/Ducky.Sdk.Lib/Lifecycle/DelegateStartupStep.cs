using System;

namespace Ducky.Sdk.Lifecycle;

/// <summary>A startup step whose work is a delegate, so existing code can become a step without being moved.</summary>
public sealed class DelegateStartupStep<TId, TContext> : IStartupStep<TId, TContext>
{
    private readonly Func<TContext, StepOutcome> _run;

    /// <summary>A step that always lets the sequence go on.</summary>
    public DelegateStartupStep(TId id, Action<TContext> run)
    {
        if (run == null)
        {
            throw new ArgumentNullException(nameof(run));
        }

        Id = id;
        _run = context =>
        {
            run(context);
            return StepOutcome.Continue;
        };
    }

    /// <summary>A step that decides whether the sequence goes on.</summary>
    public DelegateStartupStep(TId id, Func<TContext, StepOutcome> run)
    {
        Id = id;
        _run = run ?? throw new ArgumentNullException(nameof(run));
    }

    /// <inheritdoc />
    public TId Id { get; }

    /// <inheritdoc />
    public StepOutcome Run(TContext context) => _run(context);
}
