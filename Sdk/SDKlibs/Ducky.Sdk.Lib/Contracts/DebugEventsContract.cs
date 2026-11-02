using System;
using System.Collections.Generic;
using Ducky.Sdk.Contracts.DebugEvents;
using Ducky.Sdk.Utils;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// Debug event API: any mod reports events with <see cref="Report"/>; one mod at a time can record them all into a
/// JSON Lines file with <see cref="StartRecorder"/>. See docs/debug-events.md.
/// </summary>
public sealed class DebugEventsContract
{
    internal static readonly DebugEventsContract Instance = new();

    internal DebugEventsContract()
    {
    }

    /// <summary>
    /// Reports one debugging event. Callable from any thread and never throws. Without a recorder the event is
    /// discarded at once. An empty <paramref name="kind"/> is discarded.
    /// </summary>
    /// <param name="kind">Non-empty event kind, for example "hurt".</param>
    /// <param name="detail">Optional free text.</param>
    /// <param name="fields">Optional structured values, serialised as JSON.</param>
    public void Report(string kind, string? detail = null, IReadOnlyDictionary<string, object?>? fields = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(kind))
            {
                return;
            }

            JObject? json = null;
            if (fields != null)
            {
                json = JObject.FromObject(new Dictionary<string, object?>(fields));
            }

            var reporter = DebugEventReporter.Shared;
            reporter.Report(kind, detail, json);
            reporter.RequestFlushLoop();
        }
        catch (Exception)
        {
            // Reporting never throws into the caller; a failed event is simply lost.
        }
    }

    /// <summary>
    /// Makes this mod the recorder: opens <paramref name="filePath"/> and receives the events every reporting mod
    /// sends. Returns false, after one warning, when a recorder already exists or the file cannot be opened. Call on
    /// the main thread (for example in <c>ModEnabled()</c>).
    /// </summary>
    /// <param name="filePath">Where the JSON Lines file goes. Only this code decides the path.</param>
    /// <param name="append">True to add to an existing file; false to replace it.</param>
    public bool StartRecorder(string filePath, bool append)
    {
        return DebugEventRecorder.Start(filePath, append);
    }

    /// <summary>Flushes and closes this mod's recorder file and releases the role. Safe to call when not recording.</summary>
    public void StopRecorder()
    {
        DebugEventRecorder.Release();
    }

    /// <summary>True while this mod holds the recorder role.</summary>
    public bool IsRecorder => DebugEventRecorder.IsRecorderActive;

    /// <summary>True while a compatible recorder is registered and negotiated with this mod's reporter.</summary>
    public bool IsRecorderAvailable => DebugEventReporter.IsRecorderReady();
}
