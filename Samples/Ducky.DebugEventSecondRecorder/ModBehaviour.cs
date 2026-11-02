using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace DebugEventSamples.SecondRecorder;

/// <summary>
/// Another mod that tries to become the recorder while the first recorder runs. It waits 20 seconds first so the first
/// recorder is already running, then logs what StartRecorder returned and whether its file was created.
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    private CancellationTokenSource? _cts;

    protected override void ModEnabled()
    {
        Log.Current = new SampleUnityLog();
        _cts = new CancellationTokenSource();
        TryStart(_cts.Token).Forget();
    }

    protected override void ModDisabled()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private static async UniTaskVoid TryStart(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(20), cancellationToken: token);
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "duckov-test", "debug-events", "second.jsonl");
            var started = Contract.DebugEvents.StartRecorder(path, append: false);
            Debug.Log($"[Ducky.DebugEventSecondRecorder] second StartRecorder -> {started}, IsRecorder={Contract.DebugEvents.IsRecorder}, file created={File.Exists(path)} (t={Time.realtimeSinceStartup:F2})");
        }
        catch (OperationCanceledException)
        {
            // Disabled before the attempt.
        }
    }
}
