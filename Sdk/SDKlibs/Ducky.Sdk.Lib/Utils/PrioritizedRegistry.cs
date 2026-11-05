using System;

namespace Ducky.Sdk.Utils;

/// <summary>
/// The registrations shared by <c>HandlerSet</c>, <c>OrderedRules</c> and <c>RuleRegistry</c>. Entries are kept in ascending
/// priority, then registration order. The first entry calls <paramref name="onFirst"/> (the game-side subscription or patch is
/// made there) and the last removal calls <paramref name="onLast"/>. Each handle removes its own entry once.
/// </summary>
/// <remarks>
/// The entries array is replaced on every change and never mutated, so a reader takes one reference with <see cref="Snapshot"/>
/// and a registration added or removed during that read does not change it. A failing <c>onFirst</c> removes the entry it was
/// called for and rethrows, so no handle is left that cannot be disposed. The SDK calls this from the game's main thread; the
/// lock only keeps the array consistent. Touches no Unity object.
/// </remarks>
internal sealed class PrioritizedRegistry<T>
    where T : class
{
    private readonly object _gate = new();
    private readonly Action? _onFirst;
    private readonly Action? _onLast;
    private Entry[] _entries = Array.Empty<Entry>();

    /// <param name="onFirst">Called when the first entry is added.</param>
    /// <param name="onLast">Called when the last entry is removed.</param>
    public PrioritizedRegistry(Action? onFirst = null, Action? onLast = null)
    {
        _onFirst = onFirst;
        _onLast = onLast;
    }

    /// <summary>One registration: its item and its priority.</summary>
    public sealed class Entry
    {
        internal Entry(T item, int priority)
        {
            Item = item;
            Priority = priority;
        }

        /// <summary>The registered item.</summary>
        public T Item { get; }

        internal int Priority { get; }
    }

    /// <summary>How many entries are registered.</summary>
    public int Count => _entries.Length;

    /// <summary>The entries in run order. The returned array is never changed afterwards.</summary>
    public Entry[] Snapshot() => _entries;

    /// <summary>
    /// Adds <paramref name="item"/> at <paramref name="priority"/> (lower runs first; equal priority keeps registration order).
    /// The returned handle removes this registration only and does nothing when disposed again.
    /// </summary>
    public IDisposable Add(T item, int priority)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var entry = new Entry(item, priority);
        bool first;
        lock (_gate)
        {
            first = _entries.Length == 0;

            // insert after every entry of priority <= this one, so equal priorities keep registration order
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

        if (first && _onFirst != null)
        {
            try
            {
                _onFirst();
            }
            catch
            {
                // the registration failed, so it must not stay: nothing can remove it later
                lock (_gate)
                {
                    TryRemoveLocked(entry, out _);
                }

                throw;
            }
        }

        return new DisposeOnce(() => Remove(entry));
    }

    private void Remove(Entry entry)
    {
        bool last;
        lock (_gate)
        {
            if (!TryRemoveLocked(entry, out last))
            {
                return;
            }
        }

        if (last)
        {
            _onLast?.Invoke();
        }
    }

    // Caller holds _gate. last is true when this removal left the registry empty.
    private bool TryRemoveLocked(Entry entry, out bool last)
    {
        var index = Array.IndexOf(_entries, entry);
        if (index < 0)
        {
            last = false;
            return false;
        }

        var next = new Entry[_entries.Length - 1];
        Array.Copy(_entries, 0, next, 0, index);
        Array.Copy(_entries, index + 1, next, index, _entries.Length - index - 1);
        _entries = next;
        last = next.Length == 0;
        return true;
    }
}
