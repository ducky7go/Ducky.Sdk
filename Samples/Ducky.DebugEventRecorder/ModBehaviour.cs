using System;
using System.IO;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace DebugEventSamples.Recorder;

/// <summary>Recorder of the debug event sample: writes every reported event into one JSONL file, appending across restarts.</summary>
public class ModBehaviour : ModBehaviourBase
{
    /// <summary>Remote file the test reads: %USERPROFILE%\duckov-test\debug-events\run.jsonl.</summary>
    internal static string RecordPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "duckov-test", "debug-events", "run.jsonl");

    protected override void ModEnabled()
    {
        Log.Current = new SampleUnityLog();
        var started = Contract.DebugEvents.StartRecorder(RecordPath, append: true);
        Debug.Log($"[Ducky.DebugEventRecorder] StartRecorder {RecordPath} -> {started} at t={Time.realtimeSinceStartup:F2}");
    }

    protected override void ModDisabled()
    {
        Contract.DebugEvents.StopRecorder();
        Debug.Log($"[Ducky.DebugEventRecorder] StopRecorder at t={Time.realtimeSinceStartup:F2}, IsRecorder={Contract.DebugEvents.IsRecorder}");
    }
}
