using System;

namespace Ducky.Sdk.Lifecycle;

/// <summary>A stop step whose work is a delegate, so existing code can become a step without being moved.</summary>
public sealed class DelegateStopStep<TId, TContext> : IStopStep<TId, TContext>
{
    private readonly Action<TContext> _run;

    /// <summary>A stop step that runs <paramref name="run"/>.</summary>
    public DelegateStopStep(TId id, Action<TContext> run)
    {
        Id = id;
        _run = run ?? throw new ArgumentNullException(nameof(run));
    }

    /// <inheritdoc />
    public TId Id { get; }

    /// <inheritdoc />
    public void Run(TContext context) => _run(context);
}
