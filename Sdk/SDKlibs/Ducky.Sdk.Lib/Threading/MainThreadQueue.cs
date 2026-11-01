using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ducky.Sdk.Threading;

/// <summary>
/// Hands work from any thread to the thread that calls <see cref="Drain"/> (Unity's main thread, from the pump's
/// Update). Nothing is run inline by <see cref="Post"/> or <see cref="Invoke{T}"/>. Each item moves once from
/// Pending to Running or Cancelled, so work cancelled or timed out before it starts never runs, and work that has
/// started always completes with its own outcome. No Unity dependency, so it is unit-tested without a game.
/// </summary>
internal sealed class MainThreadQueue
{
    private const int Pending = 0;
    private const int Running = 1;
    private const int Cancelled = 2;

    private readonly ConcurrentQueue<WorkItem> _queue = new();
    private volatile bool _closed;

    /// <summary>True once <see cref="Close"/> was called. A closed queue refuses work.</summary>
    internal bool IsClosed => _closed;

    /// <summary>Queues fire-and-forget work. Discarded silently when the queue is closed.</summary>
    internal void Post(Action work)
    {
        if (work == null)
        {
            throw new ArgumentNullException(nameof(work));
        }

        if (_closed)
        {
            return;
        }

        Enqueue(new WorkItem(work, abandon: null));
    }

    /// <summary>
    /// Queues work that produces a result. The task completes with the result or exception of the work once it has
    /// run; it completes as cancelled if the token is cancelled or the queue closes before the work starts, and
    /// faults with <see cref="TimeoutException"/> if the timeout passes before it starts.
    /// </summary>
    internal Task<T> Invoke<T>(Func<T> work, CancellationToken cancellationToken, TimeSpan? timeout)
    {
        if (work == null)
        {
            throw new ArgumentNullException(nameof(work));
        }

        var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_closed || cancellationToken.IsCancellationRequested)
        {
            source.TrySetCanceled(cancellationToken);
            return source.Task;
        }

        var item = new WorkItem(
            body: () =>
            {
                try
                {
                    source.TrySetResult(work());
                }
                catch (Exception ex)
                {
                    source.TrySetException(ex);
                }
            },
            abandon: error =>
            {
                if (error == null)
                {
                    source.TrySetCanceled(cancellationToken);
                }
                else
                {
                    source.TrySetException(error);
                }
            });

        if (cancellationToken.CanBeCanceled)
        {
            item.AddResource(cancellationToken.Register(() => TryAbandon(item, null)));
        }

        if (timeout.HasValue)
        {
            var timer = new Timer(
                _ => TryAbandon(item, new TimeoutException(
                    $"The main thread did not start the work within {timeout.Value.TotalSeconds:0.###} seconds.")),
                null,
                timeout.Value,
                Timeout.InfiniteTimeSpan);
            item.AddResource(timer);
        }

        Enqueue(item);
        return source.Task;
    }

    /// <summary>
    /// Runs every queued item in order, on the calling thread. An item whose body throws is passed to
    /// <paramref name="onError"/> and the drain continues with the next item.
    /// </summary>
    internal void Drain(Action<Exception>? onError = null)
    {
        while (_queue.TryDequeue(out var item))
        {
            if (Interlocked.CompareExchange(ref item.State, Running, Pending) != Pending)
            {
                continue; // cancelled or timed out before it started
            }

            item.ReleaseResources();
            try
            {
                item.Body();
            }
            catch (Exception ex)
            {
                try
                {
                    onError?.Invoke(ex);
                }
                catch
                {
                    // reporting an error must not stop the drain
                }
            }
        }
    }

    /// <summary>
    /// Stops accepting work. Pending work is discarded, and every pending <see cref="Invoke{T}"/> completes as
    /// cancelled. Safe to call more than once.
    /// </summary>
    internal void Close()
    {
        _closed = true;
        DiscardPending();
    }

    private void Enqueue(WorkItem item)
    {
        _queue.Enqueue(item);

        // Close may have run between the check and the enqueue; never leave work that nobody will drain.
        if (_closed)
        {
            DiscardPending();
        }
    }

    private void DiscardPending()
    {
        while (_queue.TryDequeue(out var item))
        {
            TryAbandon(item, null);
        }
    }

    /// <summary>Moves a pending item to Cancelled and completes its task. Returns false when it already started.</summary>
    private static bool TryAbandon(WorkItem item, Exception? error)
    {
        if (Interlocked.CompareExchange(ref item.State, Cancelled, Pending) != Pending)
        {
            return false;
        }

        item.ReleaseResources();
        item.Abandon?.Invoke(error);
        return true;
    }

    private sealed class WorkItem
    {
        private readonly object _sync = new();
        private readonly List<IDisposable> _resources = new();
        private bool _released;

        public WorkItem(Action body, Action<Exception?>? abandon)
        {
            Body = body;
            Abandon = abandon;
        }

        public Action Body { get; }

        /// <summary>Completes the awaiting task when the item never ran. A null error means cancelled.</summary>
        public Action<Exception?>? Abandon { get; }

        public int State = Pending;

        /// <summary>Registers a timer or cancellation registration that ends when the item starts or is cancelled.</summary>
        public void AddResource(IDisposable resource)
        {
            lock (_sync)
            {
                if (!_released && Volatile.Read(ref State) == Pending)
                {
                    _resources.Add(resource);
                    return;
                }
            }

            resource.Dispose();
        }

        public void ReleaseResources()
        {
            List<IDisposable> toDispose;
            lock (_sync)
            {
                _released = true;
                toDispose = new List<IDisposable>(_resources);
                _resources.Clear();
            }

            foreach (var resource in toDispose)
            {
                resource.Dispose();
            }
        }
    }
}
