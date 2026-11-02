using System;
using System.IO;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace DebugEventSamples.Recorder;

/// <summary>
/// Recorder of the debug event sample: writes every reported event into one JSONL file, appending across restarts. It logs
/// the line count of the file after each start and stop, so a duplicated event shows up as a count that is too high.
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    /// <summary>Remote file the test reads: %USERPROFILE%\duckov-test\debug-events\run.jsonl.</summary>
    internal static string RecordPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "duckov-test", "debug-events", "run.jsonl");

    protected override void ModEnabled()
    {
        Log.Current = new SampleUnityLog();
        var started = Contract.DebugEvents.StartRecorder(RecordPath, append: true);
        Debug.Log($"[Ducky.DebugEventRecorder] StartRecorder {RecordPath} -> {started} at t={Time.realtimeSinceStartup:F2}, lines={RecordedLineCount()}");
    }

    protected override void ModDisabled()
    {
        Contract.DebugEvents.StopRecorder();
        Debug.Log($"[Ducky.DebugEventRecorder] StopRecorder at t={Time.realtimeSinceStartup:F2}, IsRecorder={Contract.DebugEvents.IsRecorder}, lines={RecordedLineCount()}");
    }

    /// <summary>Counts the lines of the record file. The writer may still hold it, so the file is opened for shared reading.</summary>
    private static int RecordedLineCount()
    {
        try
        {
            if (!File.Exists(RecordPath))
            {
                return 0;
            }

            using var stream = new FileStream(RecordPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var count = 0;
            while (reader.ReadLine() != null)
            {
                count++;
            }

            return count;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ducky.DebugEventRecorder] could not count the record file: {e.Message}");
            return -1;
        }
    }
}
