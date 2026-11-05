using System;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Rules;

/// <summary>One rule step: it may change <paramref name="value"/> by reference, given the context of the call.</summary>
internal delegate void RuleStep<TValue, TContext>(ref TValue value, TContext context);

/// <summary>
/// An ordered, isolated set of rules that rewrite one value. Rules run by ascending priority, then registration order, and
/// each sees the value the previous one left. A call reads one snapshot of the rules, so a rule that removes itself or another
/// rule during the call is safe. <see cref="onFirst"/> runs when the first rule is added (the game-side patch is installed there)
/// and <see cref="onLast"/> when the last one is removed. Touches no Unity object.
/// </summary>
internal sealed class RuleRegistry<TValue, TContext>
{
    private readonly PrioritizedRegistry<RuleStep<TValue, TContext>> _registry;

    /// <param name="onFirst">Called when the first rule is added.</param>
    /// <param name="onLast">Called when the last rule is removed.</param>
    public RuleRegistry(Action? onFirst = null, Action? onLast = null) =>
        _registry = new PrioritizedRegistry<RuleStep<TValue, TContext>>(onFirst, onLast);

    /// <summary>How many rules are registered.</summary>
    public int Count => _registry.Count;

    /// <summary>
    /// Adds <paramref name="step"/> at <paramref name="priority"/>. The returned handle removes this registration only and
    /// does nothing when disposed again.
    /// </summary>
    public IDisposable Add(RuleStep<TValue, TContext> step, int priority)
    {
        if (step == null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        return _registry.Add(step, priority);
    }

    /// <summary>
    /// Runs every rule on <paramref name="value"/> in order. A rule that throws has its change undone (the value is what it
    /// was before that rule), the failure is passed to <paramref name="onFailure"/>, and the remaining rules still run.
    /// </summary>
    public void Apply(ref TValue value, TContext context, Action<Exception> onFailure)
    {
        // one snapshot for the whole call: later changes replace the array and do not affect this call
        var entries = _registry.Snapshot();
        for (var i = 0; i < entries.Length; i++)
        {
            var before = value;
            try
            {
                entries[i].Item(ref value, context);
            }
            catch (Exception ex)
            {
                value = before;
                onFailure(ex);
            }
        }
    }
}
