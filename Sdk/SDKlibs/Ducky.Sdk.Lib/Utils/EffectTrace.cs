using System;
using System.Collections.Generic;

namespace Ducky.Sdk.Utils;

/// <summary>One recorded event: which code and name, what happened, and a short detail.</summary>
public readonly struct TraceEntry
{
    /// <summary>Creates an entry.</summary>
    /// <param name="sequence">Position in the recording order, starting at 1.</param>
    /// <param name="time">The time the event was recorded.</param>
    /// <param name="code">A short code for the source of the event.</param>
    /// <param name="name">The name of the event.</param>
    /// <param name="detail">A short detail, may be empty.</param>
    public TraceEntry(int sequence, float time, string code, string name, string detail)
    {
        Sequence = sequence;
        Time = time;
        Code = code;
        Name = name;
        Detail = detail;
    }

    /// <summary>Position in the recording order; a larger number is newer.</summary>
    public int Sequence { get; }

    /// <summary>The time the event was recorded.</summary>
    public float Time { get; }

    /// <summary>A short code for the source of the event.</summary>
    public string Code { get; }

    /// <summary>The name of the event.</summary>
    public string Name { get; }

    /// <summary>A short detail, may be empty.</summary>
    public string Detail { get; }
}

/// <summary>
/// The most recent events, in order, up to a fixed capacity; the oldest are dropped first. A test reads these instead
/// of searching the game log for text, and the sequence number tells it which entries are new. Touches no Unity object.
/// </summary>
public sealed class TraceRing
{
    private readonly int _capacity;
    private readonly Queue<TraceEntry> _entries = new();
    private int _sequence;

    /// <param name="capacity">The most entries kept; at least 1.</param>
    public TraceRing(int capacity = 200) => _capacity = Math.Max(1, capacity);

    /// <summary>How many entries are kept now.</summary>
    public int Count => _entries.Count;

    /// <summary>Sequence number of the newest entry, 0 when nothing was added since the last <see cref="Clear"/>.</summary>
    public int LastSequence => _sequence;

    /// <summary>Records one entry; when the ring is full, the oldest entry is dropped.</summary>
    /// <param name="time">The time the event happened.</param>
    /// <param name="code">A short code for the source of the event.</param>
    /// <param name="name">The name of the event.</param>
    /// <param name="detail">A short detail, may be empty.</param>
    public void Add(float time, string code, string name, string detail = "")
    {
        _sequence++;
        _entries.Enqueue(new TraceEntry(_sequence, time, code, name, detail));
        while (_entries.Count > _capacity)
        {
            _entries.Dequeue();
        }
    }

    /// <summary>The newest <paramref name="count"/> entries, oldest first.</summary>
    public TraceEntry[] Recent(int count)
    {
        var all = _entries.ToArray();
        if (count >= all.Length)
        {
            return all;
        }

        var result = new TraceEntry[Math.Max(0, count)];
        Array.Copy(all, all.Length - result.Length, result, 0, result.Length);
        return result;
    }

    /// <summary>Forgets every entry and restarts the sequence numbers.</summary>
    public void Clear()
    {
        _entries.Clear();
        _sequence = 0;
    }
}

/// <summary>
/// Records what mod logic did, in order, in memory. Each event is also written to the game log, so a person still
/// sees it there.
/// </summary>
public static class EffectTrace
{
    private static readonly TraceRing Ring = new();

    /// <summary>Sequence number of the newest event, 0 after <see cref="Clear"/>.</summary>
    public static int LastSequence => Ring.LastSequence;

    /// <summary>Records one event and writes it to the game log.</summary>
    public static void Add(string code, string name, string detail = "")
    {
        Ring.Add(UnityEngine.Time.time, code, name, detail);
        UnityEngine.Debug.Log($"[Trace] {code} {name} {detail}".TrimEnd());
    }

    /// <summary>The newest <paramref name="count"/> events, oldest first.</summary>
    public static TraceEntry[] Recent(int count) => Ring.Recent(count);

    /// <summary>Forgets every recorded event.</summary>
    public static void Clear() => Ring.Clear();
}
