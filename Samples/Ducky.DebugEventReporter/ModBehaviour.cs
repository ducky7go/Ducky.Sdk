using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace DebugEventSamples.Reporter;

/// <summary>
/// Reporter of the debug event sample. On enable it reports a burst of 1000 events at once, then one event per second
/// with a multi-line detail once. It never starts or stops a recorder, so it keeps running while the recorder restarts.
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    private const string RecorderId = "Ducky.DebugRecorder";
    private CancellationTokenSource? _cts;

    protected override void ModEnabled()
    {
        Log.Current = new SampleUnityLog();
        _cts = new CancellationTokenSource();
        for (var i = 0; i < 1000; i++)
        {
            Contract.DebugEvents.Report("burst", i.ToString());
        }

        Debug.Log($"[Ducky.DebugEventReporter] burst of 1000 reported; queued for {RecorderId}: {QueuedForRecorder()} (t={Time.realtimeSinceStartup:F2})");
        ReportEverySecond(_cts.Token, CoreAssemblyName).Forget();
    }

    protected override void ModDisabled()
    {
        _cts?.Cancel();
        _cts = null;
        Debug.Log($"[Ducky.DebugEventReporter] stopped reporting (t={Time.realtimeSinceStartup:F2})");
    }

    private static async UniTaskVoid ReportEverySecond(CancellationToken token, string coreName)
    {
        var n = 0;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);
                n++;
                if (n == 1)
                {
                    // Once per enable, after the first second: a reloaded reporter starts Idle and drops events until the
                    // recorder has answered its hello, so an event reported straight from ModEnabled would be lost.
                    Contract.DebugEvents.Report("core-enabled", coreName);
                }

                if (n == 2)
                {
                    Contract.DebugEvents.Report("multiline", "first line\nsecond \"quoted\" line");
                }

                Contract.DebugEvents.Report("tick", "n=" + n, new Dictionary<string, object?> { ["count"] = n });
            }
        }
        catch (OperationCanceledException)
        {
            // Disabled: the loop ends.
        }
    }

    /// <summary>The name of the assembly this Core was loaded from. It is the detail of the <c>core-enabled</c> event.</summary>
    private static string CoreAssemblyName => typeof(ModBehaviour).Assembly.GetName().Name;

    /// <summary>
    /// Messages waiting in the hub's queue for the recorder id, read from the hub's own state. -1: no hub, -2: no queue
    /// table, -3: the lookup failed. A queue is created only when a message is sent, so 0 means nothing was sent.
    /// </summary>
    private static int QueuedForRecorder()
    {
        try
        {
            var hub = GameObject.Find("ModHttpV1");
            if (hub == null)
            {
                return -1;
            }

            foreach (var component in hub.GetComponents<MonoBehaviour>())
            {
                if (component.GetType().Name != "ModHttpV1")
                {
                    continue;
                }

                var table = component.GetType()
                    .GetField("_messageQueues", BindingFlags.Instance | BindingFlags.NonPublic)?
                    .GetValue(component) as IDictionary;
                if (table == null)
                {
                    return -2;
                }

                if (!table.Contains(RecorderId))
                {
                    return 0;
                }

                var queue = table[RecorderId]!;
                return (int)queue.GetType().GetProperty("Count")!.GetValue(queue)!;
            }

            return -1;
        }
        catch (Exception e)
        {
            Debug.LogError("[Ducky.DebugEventReporter] could not read the hub queue: " + e);
            return -3;
        }
    }
}
