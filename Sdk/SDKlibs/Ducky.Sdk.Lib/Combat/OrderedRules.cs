using System;
using System.Collections.Generic;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Combat;

/// <summary>
/// A registry of items with a priority. Items run by ascending priority, then by registration order. Removing one item
/// leaves the others. Touches no Unity object, so the ordering rules are tested with plain items.
/// </summary>
internal sealed class OrderedRules<TItem>
    where TItem : class
{
    private readonly Action? _onFirst;
    private readonly Action? _onLast;
    private readonly List<Entry> _entries = new();
    private long _sequence;

    /// <param name="onFirst">Called when the first item is added (the patch is installed here).</param>
    /// <param name="onLast">Called when the last item is removed (the patch is removed here).</param>
    public OrderedRules(Action? onFirst = null, Action? onLast = null)
    {
        _onFirst = onFirst;
        _onLast = onLast;
    }

    private sealed class Entry
    {
        public int Priority;
        public long Sequence;
        public TItem Item = null!;
    }

    /// <summary>How many items are registered.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Adds <paramref name="item"/> at <paramref name="priority"/>. The returned handle removes this registration only and
    /// does nothing when disposed again.
    /// </summary>
    public IDisposable Add(TItem item, int priority)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_entries.Count == 0)
        {
            _onFirst?.Invoke();
        }

        var entry = new Entry { Priority = priority, Sequence = _sequence++, Item = item };
        _entries.Add(entry);
        return new DisposeOnce(() => Remove(entry));
    }

    /// <summary>The items in run order: ascending priority, then registration order.</summary>
    public IReadOnlyList<TItem> InRunOrder()
    {
        var ordered = new List<Entry>(_entries);
        ordered.Sort((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : a.Sequence.CompareTo(b.Sequence));

        var items = new List<TItem>(ordered.Count);
        foreach (var entry in ordered)
        {
            items.Add(entry.Item);
        }

        return items;
    }

    /// <summary>
    /// Runs <paramref name="run"/> for each item in run order. An item that throws is reported to
    /// <paramref name="onFailure"/> and the rest still run.
    /// </summary>
    public void RunIsolated(Action<TItem> run, Action<Exception> onFailure)
    {
        foreach (var item in InRunOrder())
        {
            try
            {
                run(item);
            }
            catch (Exception ex)
            {
                onFailure(ex);
            }
        }
    }

    private void Remove(Entry entry)
    {
        if (!_entries.Remove(entry))
        {
            return;
        }

        if (_entries.Count == 0)
        {
            _onLast?.Invoke();
        }
    }
}
