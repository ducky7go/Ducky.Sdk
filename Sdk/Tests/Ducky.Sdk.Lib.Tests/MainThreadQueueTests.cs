using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ducky.Sdk.Threading;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The Unity-free queue behind <c>MainThread</c>. Work is drained by the test thread, as the pump's Update does on
/// the main thread.
/// </summary>
public class MainThreadQueueTests
{
    [Test]
    public void Post_RunsInPostingOrderOnDrain()
    {
        var queue = new MainThreadQueue();
        var order = new List<int>();
        queue.Post(() => order.Add(1));
        queue.Post(() => order.Add(2));
        queue.Post(() => order.Add(3));

        queue.Drain();

        order.ShouldBe(new[] { 1, 2, 3 });
    }

    [Test]
    public void Post_NeverRunsInline()
    {
        var queue = new MainThreadQueue();
        var ran = false;

        queue.Post(() => ran = true);

        ran.ShouldBeFalse();
        queue.Drain();
        ran.ShouldBeTrue();
    }

    [Test]
    public void Post_FromAnotherThread_ReturnsImmediatelyAndRunsOnDrain()
    {
        var queue = new MainThreadQueue();
        var ran = false;

        Task.Run(() => queue.Post(() => ran = true)).Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        ran.ShouldBeFalse();

        queue.Drain();
        ran.ShouldBeTrue();
    }

    [Test]
    public void Post_ThrowingAction_IsReportedAndDoesNotStopTheDrain()
    {
        var queue = new MainThreadQueue();
        var errors = new List<Exception>();
        var ranAfter = false;
        queue.Post(() => throw new InvalidOperationException("boom"));
        queue.Post(() => ranAfter = true);

        queue.Drain(errors.Add);

        errors.Count.ShouldBe(1);
        errors[0].ShouldBeOfType<InvalidOperationException>();
        ranAfter.ShouldBeTrue();
    }

    [Test]
    public void Invoke_ReturnsTheResultOfTheWork()
    {
        var queue = new MainThreadQueue();
        var task = queue.Invoke(() => 42, CancellationToken.None, null);

        queue.Drain();

        task.Result.ShouldBe(42);
    }

    [Test]
    public void Invoke_ThrowingWork_FaultsTheTaskWithItsException()
    {
        var queue = new MainThreadQueue();
        var task = queue.Invoke<int>(() => throw new InvalidOperationException("nope"), CancellationToken.None, null);

        queue.Drain();

        task.IsFaulted.ShouldBeTrue();
        ((Task)task).Exception!.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [Test]
    public void Invoke_ThrowingWork_DoesNotStopTheDrain()
    {
        var queue = new MainThreadQueue();
        var faulted = queue.Invoke<int>(() => throw new InvalidOperationException(), CancellationToken.None, null);
        var next = queue.Invoke(() => 7, CancellationToken.None, null);

        queue.Drain();

        faulted.IsFaulted.ShouldBeTrue();
        next.Result.ShouldBe(7);
    }

    [Test]
    public void Invoke_NeverRunsInline()
    {
        var queue = new MainThreadQueue();
        var ran = false;

        var task = queue.Invoke(() => ran = true, CancellationToken.None, null);

        ran.ShouldBeFalse();
        task.IsCompleted.ShouldBeFalse();
    }

    [Test]
    public void CancelBeforeStart_NeverRuns_AndTheTaskIsCancelled()
    {
        var queue = new MainThreadQueue();
        var ran = false;
        using var cts = new CancellationTokenSource();
        var task = queue.Invoke(() => ran = true, cts.Token, null);

        cts.Cancel();
        queue.Drain();

        ran.ShouldBeFalse();
        task.IsCanceled.ShouldBeTrue();
    }

    [Test]
    public void AlreadyCancelledToken_CompletesAsCancelledWithoutQueueing()
    {
        var queue = new MainThreadQueue();
        var ran = false;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var task = queue.Invoke(() => ran = true, cts.Token, null);
        queue.Drain();

        ran.ShouldBeFalse();
        task.IsCanceled.ShouldBeTrue();
    }

    [Test]
    public void TimeoutBeforeStart_FaultsWithTimeout_AndNeverRuns()
    {
        var queue = new MainThreadQueue();
        var ran = false;
        var task = queue.Invoke(() => ran = true, CancellationToken.None, TimeSpan.FromMilliseconds(30));

        task.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously).Wait(TimeSpan.FromSeconds(5));
        queue.Drain();

        ran.ShouldBeFalse();
        task.IsFaulted.ShouldBeTrue();
        ((Task)task).Exception!.InnerException.ShouldBeOfType<TimeoutException>();
    }

    [Test]
    public void StartedBeforeTimeout_CompletesWithItsOwnOutcome()
    {
        var queue = new MainThreadQueue();
        // The work starts at once and runs past the timeout; the timeout must not replace its answer.
        var task = queue.Invoke(() =>
        {
            Thread.Sleep(300);
            return 7;
        }, CancellationToken.None, TimeSpan.FromMilliseconds(100));

        queue.Drain();

        task.Result.ShouldBe(7);
    }

    [Test]
    public void Close_CancelsPendingInvokeAndDiscardsPost()
    {
        var queue = new MainThreadQueue();
        var postRan = false;
        var invokeRan = false;
        var task = queue.Invoke(() => invokeRan = true, CancellationToken.None, null);
        queue.Post(() => postRan = true);

        queue.Close();
        queue.Drain();

        task.IsCanceled.ShouldBeTrue();
        postRan.ShouldBeFalse();
        invokeRan.ShouldBeFalse();
        queue.IsClosed.ShouldBeTrue();
    }

    [Test]
    public void Post_AfterClose_DoesNotThrowAndNeverRuns()
    {
        var queue = new MainThreadQueue();
        var ran = false;
        queue.Close();

        Should.NotThrow(() => queue.Post(() => ran = true));
        queue.Drain();

        ran.ShouldBeFalse();
    }

    [Test]
    public void Invoke_AfterClose_CompletesAsCancelled()
    {
        var queue = new MainThreadQueue();
        queue.Close();

        var task = queue.Invoke(() => 1, CancellationToken.None, null);

        task.IsCanceled.ShouldBeTrue();
    }

    [Test]
    public void Close_TwiceIsHarmless()
    {
        var queue = new MainThreadQueue();
        queue.Close();

        Should.NotThrow(() => queue.Close());
    }

    [Test]
    public void ReOpen_WithAFreshQueue_AcceptsWork()
    {
        var closed = new MainThreadQueue();
        closed.Close();

        var fresh = new MainThreadQueue();
        var task = fresh.Invoke(() => 3, CancellationToken.None, null);
        fresh.Drain();

        fresh.IsClosed.ShouldBeFalse();
        task.Result.ShouldBe(3);
    }
}
