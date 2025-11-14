using Ducky.Sdk.Localizations;
using Ducky.Sdk.Logging;
using Ducky.Sdk.ModBehaviours;
using Ducky.Sdk.Options;

namespace Ducky.SingleProject;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        Log.Info("Ducky.SingleProject Mod Enabled" + L.UI.NiceWelcomeMessage);
        var randomKey = UnityEngine.Random.Range(1, 10000);
        ModOptions.ForName.LoadConfig("RandomKey", randomKey);
        Log.Info("Loaded RandomKey from ModOptions.ForName: {RandomKey}", randomKey);

        ModOptions.ForName.SaveConfig("RandomKey", randomKey + 1);
        Log.Info("Saved RandomKey to ModOptions.ForName: {RandomKey}", randomKey + 1);
    }

    protected override void ModDisabled()
    {
        Log.Info("Ducky.SingleProject Mod Disabled");
    }
}
