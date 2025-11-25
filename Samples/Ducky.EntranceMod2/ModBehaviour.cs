using Ducky.Sdk.Logging;
using Ducky.Sdk.ModBehaviours;

namespace Ducky.EntranceMod2;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        Log.Info("Mod Enabled");
    }

    protected override void ModDisabled()
    {
        Log.Info("Mod Disabled");
    }
}
