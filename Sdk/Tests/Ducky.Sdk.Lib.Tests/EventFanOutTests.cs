using System;
using System.Collections.Generic;
using Ducky.Sdk.Combat;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The fan-out behind the game-event helpers (<see cref="HandlerSet{TArgs}"/>, which the first batch's combat dispatch
/// also uses): attach on first, detach on last, idempotent handles, double dispose, failure isolation and null rejection.
/// Tested with a fake event source, so no game object is loaded.
/// </summary>
public class EventFanOutTests
{
    private sealed class Game
    {
        public int Attached;
        public int Detached;
        public readonly List<Exception> Failures = new();
    }

    private static HandlerSet<int> NewSet(Game game) => new(
        onFirst: () => game.Attached++,
        onLast: () => game.Detached++,
        onFailure: game.Failures.Add);

    [Test]
    public void AttachOnFirst_DetachOnLast()
    {
        var game = new Game();
        var set = NewSet(game);

        var a = set.Add(_ => { });
        var b = set.Add(_ => { });
        game.Attached.ShouldBe(1);
        game.Detached.ShouldBe(0);

        a.Dispose();
        game.Detached.ShouldBe(0);
        b.Dispose();
        game.Detached.ShouldBe(1);
    }

    [Test]
    public void IdempotentHandle_DisposedTwice_RemovesOnlyOnce()
    {
        var game = new Game();
        var set = NewSet(game);
        var calls = new List<int>();
        var first = set.Add(v => calls.Add(v));
        set.Add(v => calls.Add(v * 10));

        first.Dispose();
        first.Dispose();

        set.Count.ShouldBe(1);
        game.Detached.ShouldBe(0);
    }

    [Test]
    public void DoubleDispose_DoesNotRemoveAnotherHandler_EvenOfTheSameMethod()
    {
        var game = new Game();
        var set = NewSet(game);
        var calls = new List<int>();
        Action<int> handler = v => calls.Add(v);
        var first = set.Add(handler);
        set.Add(handler);

        first.Dispose();
        first.Dispose();

        set.Raise(7);
        calls.ShouldBe(new[] { 7 }); // the second registration still runs
    }

    [Test]
    public void ThrowingHandler_DoesNotStopTheOthers()
    {
        var game = new Game();
        var set = NewSet(game);
        var ran = new List<string>();
        set.Add(_ => throw new InvalidOperationException("boom"));
        set.Add(_ => ran.Add("other"));

        set.Raise(1);

        ran.ShouldBe(new[] { "other" });
        game.Failures.Count.ShouldBe(1);
    }

    [Test]
    public void NullHandler_IsRejected_AndAttachesNothing()
    {
        var game = new Game();
        var set = NewSet(game);

        Should.Throw<ArgumentNullException>(() => set.Add(null!));

        game.Attached.ShouldBe(0);
        set.Count.ShouldBe(0);
    }
}
