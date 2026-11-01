using System;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace Ducky.Sdk.Threading;

/// <summary>
/// The object that drains a <see cref="MainThreadQueue"/> once per frame on Unity's main thread. Created by
/// <see cref="MainThread.EnsureStarted"/>; its type name stays stable so ILRepack and Unity lookups keep working.
/// </summary>
internal sealed class MainThreadPump : MonoBehaviour
{
    private MainThreadQueue? _queue;

    internal void Bind(MainThreadQueue queue)
    {
        _queue = queue;
    }

    private void Update()
    {
        var queue = _queue;
        if (queue == null)
        {
            return;
        }

        try
        {
            queue.Drain(ex => Log.Error(ex, "[MainThread] A posted action failed"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[MainThread] Draining the main-thread queue failed");
        }
    }
}
