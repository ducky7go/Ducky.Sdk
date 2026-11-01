using System;
using System.Threading;
using System.Threading.Tasks;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.Sdk.Threading;

/// <summary>
/// Runs work on Unity's main thread from any thread. <see cref="Post"/> queues an action and returns at once;
/// <see cref="InvokeAsync(Func{T},CancellationToken,TimeSpan?)"/> returns a task for the result. Work runs in a later
/// frame, never inline, even when called from the main thread.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ModBehaviours.ModBehaviourBase"/> calls <see cref="EnsureStarted"/> when the mod is enabled and
/// <see cref="Close"/> when its last enabled behaviour is disabled. A mod that does not derive from
/// <c>ModBehaviourBase</c> must call <see cref="EnsureStarted"/> itself, on the main thread, before posting work.
/// Until then, posted work waits in the queue.
/// </para>
/// <para>
/// The static constructor touches no Unity object. The pump <c>GameObject</c> is created only by
/// <see cref="EnsureStarted"/>, on the main thread.
/// </para>
/// </remarks>
public static class MainThread
{
    private static readonly object Gate = new();

    private static volatile MainThreadQueue _queue = new();

    private static MainThreadPump? _pump;

    /// <summary>
    /// Queues <paramref name="action"/> to run on the main thread during a later frame. Callable from any thread and
    /// never blocks. Exceptions are logged and do not stop the other queued actions. After the mod is disabled the
    /// action is discarded without an exception.
    /// </summary>
    public static void Post(Action action) => _queue.Post(action ?? throw new ArgumentNullException(nameof(action)));

    /// <summary>Runs <paramref name="action"/> on the main thread and returns a task that completes when it has run.</summary>
    /// <param name="action">The work to run on the main thread.</param>
    /// <param name="cancellationToken">Cancels the work if it has not started yet.</param>
    /// <param name="timeout">Faults the task with <see cref="TimeoutException"/> if the work has not started within this time.</param>
    public static Task InvokeAsync(Action action, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return InvokeAsync(() =>
        {
            action();
            return true;
        }, cancellationToken, timeout);
    }

    /// <summary>
    /// Runs <paramref name="function"/> on the main thread and returns a task with its result. The task completes with
    /// the function's exception if it throws. Cancelled or timed-out work that has not started never runs; work that
    /// has started completes with its own outcome.
    /// </summary>
    /// <inheritdoc cref="InvokeAsync(Action,CancellationToken,TimeSpan?)"/>
    public static Task<T> InvokeAsync<T>(Func<T> function, CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        if (function == null)
        {
            throw new ArgumentNullException(nameof(function));
        }

        return _queue.Invoke(function, cancellationToken, timeout);
    }

    /// <summary>
    /// Opens the scheduler and makes sure its pump object exists. Must be called on the main thread. Idempotent: a
    /// running pump is kept; a pump that Unity destroyed, or one left after <see cref="Close"/>, is re-created.
    /// </summary>
    public static void EnsureStarted()
    {
        lock (Gate)
        {
            if (_queue.IsClosed)
            {
                _queue = new MainThreadQueue();
            }

            if (_pump != null)
            {
                return;
            }

            var go = new GameObject($"{ModInfo.Id}_MainThreadPump");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _pump = go.AddComponent<MainThreadPump>();
            _pump.Bind(_queue);
        }
    }

    /// <summary>
    /// Stops accepting work, cancels every not-yet-started <see cref="InvokeAsync(Func{T},CancellationToken,TimeSpan?)"/>,
    /// discards pending <see cref="Post"/> actions and destroys the pump object. Safe to call more than once.
    /// </summary>
    public static void Close()
    {
        lock (Gate)
        {
            _queue.Close();
            if (_pump != null)
            {
                UnityEngine.Object.Destroy(_pump.gameObject);
            }

            _pump = null;
        }
    }
}
