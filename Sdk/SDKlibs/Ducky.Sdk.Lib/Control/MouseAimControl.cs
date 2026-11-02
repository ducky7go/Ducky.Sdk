using System;
using Ducky.Sdk.GameEvents;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.Sdk.Control;

/// <summary>
/// Stops mouse aiming while a panel is open, so the real cursor is visible and clicks reach the panel, and gives aiming back when
/// the panel closes. Several panels and mods can stop aiming at once: aiming returns only when the last stop is released.
/// </summary>
/// <remarks>
/// <para>
/// Side effects: stopping aiming uses the game's own input block (<c>InputManager.DisableInput</c>), so movement, firing, item
/// shortcuts and camera edge-pan stop too. There is no aim-only block. The aim point stays where it was; when aiming is restored,
/// it continues from the cursor. The game's input block sets a two-frame cool-down when it is taken, so the click that closes the
/// panel does not fire (static read of the decompiled game, not yet verified in the game).
/// </para>
/// <para>
/// Release each handle when the panel closes, on Esc and on scene change, and add it to <c>ModBehaviourBase.Subscriptions</c> so it
/// is released when the mod is disabled. Holds are counted per owner object, and each SDK copy of a mod has its own count; both
/// stops are honored by the game. Stops are forgotten when a level starts, and a stop taken outside a level has no effect on the
/// game.
/// </para>
/// <para>
/// Main thread only: every method here touches Unity objects, so call it on the Unity main thread (for example from
/// <c>ModEnabled()</c> or a button callback), never from a thread-pool continuation.
/// </para>
/// </remarks>
public static class MouseAimControl
{
    private static AimBlockLedger<GameObject>? _ledger;

    private static GameObject? _holder;

    private static IDisposable? _levelReset;

    /// <summary>
    /// True while a stop taken through this copy of the SDK is still in force. False when no stop is held, or when every owner of a
    /// held stop has been destroyed or deactivated. Main thread only.
    /// </summary>
    public static bool IsAimingStopped => _ledger != null && _ledger.IsStopped;

    /// <summary>
    /// Stops mouse aiming until the returned handle is disposed. Disposing it again has no further effect. The stop is held by this
    /// SDK copy's holder object, so it is not tied to any panel: release it yourself or call <see cref="RestoreAiming"/>.
    /// Main thread only.
    /// </summary>
    public static IDisposable StopAiming()
    {
        return StopAimingFor(ResolveHolder());
    }

    /// <summary>
    /// Stops mouse aiming for <paramref name="owner"/> until the returned handle is disposed, or until the owner is destroyed or
    /// deactivated, whichever comes first. A deactivated owner releases its stop by itself, even if the handle is never disposed;
    /// reactivating it does not restore the stop, so take a new one when the panel opens again. Disposing the handle after the owner
    /// is destroyed is safe. Throws <see cref="ArgumentNullException"/> for null. Main thread only.
    /// </summary>
    public static IDisposable StopAiming(GameObject owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        ResolveHolder();
        return StopAimingFor(owner);
    }

    /// <summary>
    /// Releases every stop taken through this copy of the SDK at once, for example when a panel system shuts down. Handles issued
    /// before the call do nothing when disposed afterwards. Does nothing when no stop is held. Main thread only.
    /// </summary>
    public static void RestoreAiming()
    {
        _ledger?.ReleaseAll();
        _levelReset?.Dispose();
        _levelReset = null;
    }

    private static AimBlockLedger<GameObject> Ledger => _ledger ??= new AimBlockLedger<GameObject>(
        owner => owner.GetInstanceID(),
        owner => owner != null && owner.activeInHierarchy,
        Block,
        Unblock,
        ReportError);

    private static IDisposable StopAimingFor(GameObject owner)
    {
        var ledger = Ledger;
        EnsureLevelReset();
        var hold = ledger.Hold(owner);
        return new DisposeOnce(() =>
        {
            hold.Dispose();
            ReleaseLevelResetIfIdle();
        });
    }

    /// <summary>
    /// Called every frame by the holder object. Forgets stops whose owner was destroyed or deactivated, in the same frame the game
    /// drops that owner, so a reopened panel blocks again. Main thread only.
    /// </summary>
    internal static void PruneDeadOwners()
    {
        if (_ledger == null)
        {
            return;
        }

        _ledger.ForgetDeadOwners();
        ReleaseLevelResetIfIdle();
    }

    // Subscribed on the first stop and released when no stop is held, as CharacterVisibility does (D4).
    private static void EnsureLevelReset()
    {
        _levelReset ??= LevelEvents.OnInitialized(ForgetAtLevelStart);
    }

    private static void ReleaseLevelResetIfIdle()
    {
        if (_levelReset != null && _ledger != null && !_ledger.IsStopped)
        {
            _levelReset.Dispose();
            _levelReset = null;
        }
    }

    private static void ForgetAtLevelStart()
    {
        // the new level's InputManager has no sources, so the old stops are only forgotten, never released
        _ledger?.Clear();
    }

    // The holder is an active object that stays in the scene, so the game keeps its source. Its Update runs the per-frame prune.
    // It is created on the first stop of any form, owner-bound included, and re-created if Unity destroyed it.
    private static GameObject ResolveHolder()
    {
        if (_holder != null)
        {
            return _holder;
        }

        var name = $"{ModInfo.Id}_MouseAimHolder";
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            // a holder left by an old copy of this SDK (hot reload) may still be in the game's set: release it once and reuse it
            Unblock(existing);
            if (existing.GetComponent<MouseAimHolder>() == null)
            {
                existing.AddComponent<MouseAimHolder>();
            }

            _holder = existing;
            return existing;
        }

        var go = new GameObject(name);
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<MouseAimHolder>();
        _holder = go;
        return go;
    }

    // The game's block is per object. Calling it needs a level's InputManager: DisableInput is safe without one, ActiveInput throws.
    private static bool GameInputReady() => LevelManager.Instance != null && LevelManager.Instance.InputManager != null;

    private static void Block(GameObject owner)
    {
        try
        {
            if (!GameInputReady())
            {
                return;
            }

            InputManager.DisableInput(owner);
        }
        catch (Exception e)
        {
            Log.Error(e, "[MouseAimControl] Failed to stop mouse aiming");
        }
    }

    private static void Unblock(GameObject owner)
    {
        try
        {
            if (!GameInputReady())
            {
                return;
            }

            InputManager.ActiveInput(owner);
        }
        catch (Exception e)
        {
            Log.Error(e, "[MouseAimControl] Failed to restore mouse aiming");
        }
    }

    private static void ReportError(Exception e)
    {
        Log.Error(e, "[MouseAimControl] Mouse aiming control failed");
    }
}

/// <summary>
/// The SDK holder object of <see cref="MouseAimControl"/>: active, in the scene, and running the per-frame prune of dead owners.
/// Holds no state of its own.
/// </summary>
internal sealed class MouseAimHolder : MonoBehaviour
{
    private void Update()
    {
        MouseAimControl.PruneDeadOwners();
    }
}
