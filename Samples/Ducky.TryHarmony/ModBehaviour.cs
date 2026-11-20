using Duckov.Buffs;
using Duckov.Utilities;
using Ducky.Sdk.Utils;
using Serilog;
using Steamworks;
using Log = Ducky.Sdk.Logging.Log;

namespace Ducky.TryHarmony;

public class ModBehaviour : ModBehaviourBase
{
    public SteamAPICall_t _handle;
    private HarmonyLib.Harmony? _harmony;
    private int _buffId;

    protected override void ModEnabled()
    {
        // A hot reload Core is loaded from bytes and has no Location: log to the game's data folder then.
        var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var logFolder = (string.IsNullOrEmpty(location) ? null : Path.GetDirectoryName(location))
                        ?? Application.persistentDataPath;
        Serilog.Log.Logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(Path.Combine(logFolder, "Ducky.TryHarmony.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var modName = Helper.GetModName();
        // An id of its own for this copy of the mod, so another copy (or another mod) that picks the mod name cannot share
        // it, and UnpatchAll(id) removes only what this copy applied.
        var harmonyId = Ducky.Sdk.Utils.PatchIds.For(modName, "Patches", System.Guid.NewGuid());
        var harmony = new HarmonyLib.Harmony(harmonyId);
        try
        {
            harmony.PatchAll();
            _harmony = harmony;
        }
        catch (System.Exception ex)
        {
            // PatchAll may have applied some patches before it failed: take them back and say so in the log.
            harmony.UnpatchAll(harmonyId);
            Ducky.Sdk.Utils.PatchHealth.Fail(harmonyId, ex, "[TryHarmony] could not apply its patches");
        }

        _buffId = Contract.Buffs.RegisterBuff<DoNothingBuff>(buff =>
        {
            buff.SetDisplayName(LK.UI.DoNothingBuffName)
                .SetDescription(LK.UI.DoNothingBuffDescription)
                .SetIcon(GameplayDataSettings.Buffs.BaseBuff.Icon)
                .SetLimitedLifeTime(true)
                .SetTotalLifeTime(60)
                .SetExclusiveTag(Buff.BuffExclusiveTags.NotExclusive);
        });

        Log.Info("{ModName} Mod Enabled", modName);

        SceneLoader.onAfterSceneInitialize += SceneLoader_onAfterSceneInitialize;
    }

    private void SceneLoader_onAfterSceneInitialize(SceneLoadingContext obj)
    {
        if (obj.sceneName == GameplayDataSettings.SceneManagement.BaseScene.Name)
        {
            var main = LevelManager.Instance.MainCharacter;
            if (main != null)
            {
                main.AddBuff(Contract.Buffs.CreateBuffInstance(_buffId));
            }
        }
    }

    protected override void ModDisabled()
    {
        // ModEnabled may have failed before the patch existed (a hot reload Core that could not start).
        _harmony?.UnpatchAll(_harmony.Id);
        _harmony = null;
        var modName = Helper.GetModName();
        SceneLoader.onAfterSceneInitialize -= SceneLoader_onAfterSceneInitialize;
        Log.Info("{ModName} Mod Disabled", modName);
    }
}

public class DoNothingBuff : Buff
{
    protected override void OnNotifiedOutOfTime()
    {
        base.OnNotifiedOutOfTime();
        Log.Info("DoNothingBuff time out");
    }
}
