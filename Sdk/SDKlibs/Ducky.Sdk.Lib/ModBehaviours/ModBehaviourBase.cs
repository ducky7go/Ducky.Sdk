using System;
using System.Collections.Concurrent;
using System.Linq;
using Duckov.Modding;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.ModProtocols;
using Ducky.Sdk.Localizations;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using SodaCraft.Localizations;
using UnityEngine;

namespace Ducky.Sdk.ModBehaviours;

public abstract class ModBehaviourBase : ModBehaviour
{
    private static readonly ConcurrentDictionary<Type, EnableState> EnableStates = new();

    /// <summary>
    /// Gets or sets whether this mod should enable MessageHub host functionality.
    /// Default is true. Set to false to opt out of automatic host startup.
    /// </summary>
    protected virtual bool EnableMessageHubHost { get; set; } = true;

    /// <summary>
    /// Gets whether this mod instance is currently acting as the MessageHub host.
    /// </summary>
    public bool IsMessageHubHost => MessageHubManager.GetCurrentHost() != null;

    protected void OnEnable()
    {
        var state = GetStateForType();
        if (state.IsEnabled)
        {
            Debug.LogWarning(
                $"[ModBehaviourBase] Mod behaviour for {GetType().Name} is already enabled. Skipping OnEnable.");
            return;
        }

        lock (state.Lock)
        {
            if (state.IsEnabled)
            {
                Debug.LogWarning(
                    $"[ModBehaviourBase] Mod behaviour {GetType().Name} is already enabled inside lock. Skipping OnEnable.");
                return;
            }

            state.IsEnabled = true;

            try
            {
                EnableCore();
                enabled = true;
            }
            catch
            {
                state.IsEnabled = false;
                // EnableCore may have failed after attaching shared resources; undo them.
                ReleaseSharedResources();
                throw;
            }
        }

        void EnableCore()
        {
            try
            {
                var modName = Helper.GetModName();
                Debug.Log($"[ModBehaviourBase] Retrieved mod name: {modName}");
                Log.Current = LogProvider.GetLogger(modName);
                if (Log.Current == LogProvider.NoOpLogger.Instance)
                {
                    Debug.LogWarning("[ModBehaviourBase] Failed to get a valid logger. Using NoOpLogger.");
                }

                Log.Debug("Enabling mod behaviour: {ModBehaviour}", GetType().Name);
                L.Instance.Attach(); // re-subscribe after a previous disable
                L.Instance.SetLanguage(LocalizationManager.CurrentLanguage);
                Log.Debug("Set localization language to: {Language}", LocalizationManager.CurrentLanguage);
                BuffsContract.Instance.EnsureBuffIdRegion(); // ensure BuffRegistrator is initialized
                Log.Debug("Ensured BuffRegistrator is initialized.");

                // Initialize MessageHub host if enabled
                InitializeMessageHubHost();

                ModEnabled();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error enabling mod behaviour: {ModBehaviour}", GetType().Name);
                throw;
            }
        }
    }

    /// <summary>
    /// mod enabled, override to add your logic, apply patches, etc.
    /// </summary>
    protected abstract void ModEnabled();

    protected void OnDisable()
    {
        Log.Debug("Disabling mod behaviour: {ModBehaviour}", GetType().Name);
        var state = GetStateForType();
        try
        {
            ModDisabled();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error disabling mod behaviour: {ModBehaviour}", GetType().Name);
            throw;
        }
        finally
        {
            lock (state.Lock)
            {
                state.IsEnabled = false;
            }

            enabled = false;
            ReleaseSharedResources();
        }
    }

    /// <summary>
    /// Undo what enabling attached to process-wide state, so a disabled mod stops reacting to game events.
    /// Skipped while another <see cref="ModBehaviourBase"/> subclass in this mod is still enabled.
    /// The MessageHub host is intentionally left running: it is shared by all mods and outlives any one of them.
    /// Never throws, so it cannot mask the exception that triggered the teardown.
    /// </summary>
    private static void ReleaseSharedResources()
    {
        try
        {
            if (EnableStates.Values.Any(s => s.IsEnabled))
            {
                return;
            }

            L.Instance.Detach();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ModBehaviourBase] Failed to release shared resources: {e}");
        }
    }

    /// <summary>
    /// mod disabled, override to remove your logic, unpatch, etc.
    /// </summary>
    protected abstract void ModDisabled();

    /// <summary>
    /// Initialize MessageHub host if enabled and no host is already running
    /// </summary>
    private void InitializeMessageHubHost()
    {
        if (!EnableMessageHubHost)
        {
            Log.Debug("MessageHub host functionality is disabled for this mod");
            return;
        }

        try
        {
            // Check if host is already running
            if (MessageHubManager.IsHostRunning())
            {
                Log.Debug("MessageHub host is already running, connecting as client");
                return;
            }

            // Start new host
            var host = MessageHubManager.StartHost();
            if (host != null)
            {
                Log.Info("This mod has started the MessageHub host");
            }
            else
            {
                Log.Warn("Failed to start MessageHub host");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing MessageHub host");
        }
    }

    private EnableState GetStateForType()
    {
        var type = GetType();
        return EnableStates.GetOrAdd(type, _ => new EnableState());
    }

    private sealed class EnableState
    {
        public readonly object Lock = new();
        public bool IsEnabled;
    }
}
