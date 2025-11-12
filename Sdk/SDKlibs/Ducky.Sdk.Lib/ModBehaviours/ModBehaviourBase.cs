using System;
using Duckov.Modding;
using Ducky.Sdk.Assemblies;
using Ducky.Sdk.GameApis;
using Ducky.Sdk.Localizations;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using SodaCraft.Localizations;
using UnityEngine;

namespace Ducky.Sdk.ModBehaviours;

public abstract class ModBehaviourBase : ModBehaviour
{
    protected void OnEnable()
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
            L.Instance.SetLanguage(LocalizationManager.CurrentLanguage);
            Log.Debug("Set localization language to: {Language}", LocalizationManager.CurrentLanguage);
            BuffRegistrator.Instance.EnsureBuffIdRegion(); // ensure BuffRegistrator is initialized
            Log.Debug("Ensured BuffRegistrator is initialized.");
            ModEnabled();
        }
        catch (Exception e)
        {
            Log.Current.Error(e, "Error enabling mod behaviour: {ModBehaviour}", GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// mod enabled, override to add your logic, apply patches, etc.
    /// </summary>
    protected abstract void ModEnabled();

    protected void OnDisable()
    {
        Log.Debug("Disabling mod behaviour: {ModBehaviour}", GetType().Name);
        try
        {
            ModDisabled();
        }
        catch (Exception e)
        {
            Log.Current.Error(e, "Error disabling mod behaviour: {ModBehaviour}", GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// mod disabled, override to remove your logic, unpatch, etc.
    /// </summary>
    protected abstract void ModDisabled();
}
