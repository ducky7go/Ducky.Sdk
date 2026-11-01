using System;
using Ducky.Sdk.Logging;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Ducky.Sdk.GameEvents;

/// <summary>Scene events, as disposable subscriptions.</summary>
public static class SceneEvents
{
    /// <summary>
    /// Runs <paramref name="handler"/> with (previous phase, new phase) each time the active scene changes to a
    /// different <see cref="ScenePhase"/>. The current phase is taken from the active scene when subscribing, and a
    /// change between two scenes of the same phase is not reported. Dispose the handle to unsubscribe.
    /// </summary>
    public static IDisposable OnPhaseChanged(Action<ScenePhase, ScenePhase> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var current = ScenePhases.Of(SceneManager.GetActiveScene().name);
        UnityAction<Scene, Scene> onActiveSceneChanged = (_, newScene) =>
        {
            var next = ScenePhases.Of(newScene.name);
            var previous = current;
            if (next == previous)
            {
                return;
            }

            current = next;
            try
            {
                handler(previous, next);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GameEvents] A scene phase handler failed; other subscribers still run");
            }
        };

        return EventSubscription.Create<UnityAction<Scene, Scene>>(
            onActiveSceneChanged,
            add: h => SceneManager.activeSceneChanged += h,
            remove: h => SceneManager.activeSceneChanged -= h);
    }
}
