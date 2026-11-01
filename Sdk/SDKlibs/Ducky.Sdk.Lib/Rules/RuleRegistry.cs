using System;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Rules;

/// <summary>One rule step: it may change <paramref name="value"/> by reference, given the context of the call.</summary>
internal delegate void RuleStep<TValue, TContext>(ref TValue value, TContext context);

/// <summary>
/// An ordered, isolated set of rules that rewrite one value. Rules run by ascending priority, then registration order, and
/// each sees the value the previous one left. The sorted array is replaced on every change (copy on write), so a call
/// reads one reference and allocates nothing, and a rule that removes itself or another rule during the call is safe.
/// <see cref="onFirst"/> runs when the first rule is added (the game-side patch is installed there) and
/// <see cref="onLast"/> when the last one is removed. Touches no Unity object.
/// </summary>
internal sealed class RuleRegistry<TValue, TContext>
{
    private readonly object _gate = new();
    private readonly Action? _onFirst;
    private readonly Action? _onLast;
    private Entry[] _entries = Array.Empty<Entry>(); // replaced under _gate, never mutated in place
    private long _sequence;

    /// <param name="onFirst">Called when the first rule is added.</param>
    /// <param name="onLast">Called when the last rule is removed.</param>
    public RuleRegistry(Action? onFirst = null, Action? onLast = null)
    {
        _onFirst = onFirst;
        _onLast = onLast;
    }

    private sealed class Entry
    {
        public int Priority;
        public long Sequence;
        public RuleStep<TValue, TContext> Step = null!;
    }

    /// <summary>How many rules are registered.</summary>
    public int Count => _entries.Length;

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

        Entry entry;
        bool first;
        lock (_gate)
        {
            first = _entries.Length == 0;
            entry = new Entry { Priority = priority, Sequence = _sequence++, Step = step };

            // insert after every rule of priority <= this one: equal priority keeps registration order
            var insertAt = 0;
            while (insertAt < _entries.Length && _entries[insertAt].Priority <= priority)
            {
                insertAt++;
            }

            var next = new Entry[_entries.Length + 1];
            Array.Copy(_entries, 0, next, 0, insertAt);
            next[insertAt] = entry;
            Array.Copy(_entries, insertAt, next, insertAt + 1, _entries.Length - insertAt);
            _entries = next;
        }

        if (first)
        {
            _onFirst?.Invoke();
        }

        return new DisposeOnce(() => Remove(entry));
    }

    /// <summary>
    /// Runs every rule on <paramref name="value"/> in order. A rule that throws has its change undone (the value is what it
    /// was before that rule), the failure is passed to <paramref name="onFailure"/>, and the remaining rules still run.
    /// </summary>
    public void Apply(ref TValue value, TContext context, Action<Exception> onFailure)
    {
        // the array read once: later changes replace it and do not affect this call
        var entries = _entries;
        for (var i = 0; i < entries.Length; i++)
        {
            var before = value;
            try
            {
                entries[i].Step(ref value, context);
            }
            catch (Exception ex)
            {
                value = before;
                onFailure(ex);
            }
        }
    }

    private void Remove(Entry entry)
    {
        bool last;
        lock (_gate)
        {
            var index = Array.IndexOf(_entries, entry);
            if (index < 0)
            {
                return;
            }

            var next = new Entry[_entries.Length - 1];
            Array.Copy(_entries, 0, next, 0, index);
            Array.Copy(_entries, index + 1, next, index, _entries.Length - index - 1);
            _entries = next;
            last = next.Length == 0;
        }

        if (last)
        {
            _onLast?.Invoke();
        }
    }
}
