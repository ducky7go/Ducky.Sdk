using Ducky.EntranceMod.Common;
using Ducky.Sdk.Localizations;
using Ducky.Sdk.Logging;

namespace Ducky.EntranceMod
{
    public class ModBehaviour : MyModBase
    {
        protected override void ModEnabled()
        {
            Log.Info("Ducky.EntranceMod Mod Enabled" + L.UI.NiceWelcomeMessage);
        }

        protected override void ModDisabled()
        {
            Log.Info("Ducky.EntranceMod Mod Disabled");
        }
    }
}
