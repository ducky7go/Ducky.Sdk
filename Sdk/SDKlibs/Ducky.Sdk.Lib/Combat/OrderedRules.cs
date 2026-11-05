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
    private readonly PrioritizedRegistry<TItem> _registry;

    /// <param name="onFirst">Called when the first item is added (the patch is installed here).</param>
    /// <param name="onLast">Called when the last item is removed (the patch is removed here).</param>
    public OrderedRules(Action? onFirst = null, Action? onLast = null) =>
        _registry = new PrioritizedRegistry<TItem>(onFirst, onLast);

    /// <summary>How many items are registered.</summary>
    public int Count => _registry.Count;

    /// <summary>
    /// Adds <paramref name="item"/> at <paramref name="priority"/>. The returned handle removes this registration only and
    /// does nothing when disposed again.
    /// </summary>
    public IDisposable Add(TItem item, int priority) => _registry.Add(item, priority);

    /// <summary>The items in run order: ascending priority, then registration order.</summary>
    public IReadOnlyList<TItem> InRunOrder()
    {
        var entries = _registry.Snapshot();
        var items = new List<TItem>(entries.Length);
        foreach (var entry in entries)
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
}
