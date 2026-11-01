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
        var harmony = new HarmonyLib.Harmony(modName);
        harmony.PatchAll();
        _harmony = harmony;

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
