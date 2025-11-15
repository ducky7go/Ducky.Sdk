using Ducky.Sdk.Logging;
using Ducky.Sdk.ModBehaviours;

namespace Ducky.MessageHubHost;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        Log.Info("Ducky.MessageHubHost Mod Enabled");
        if (ModBusV1.Instance == null)
        {
            Log.Info("ModBusV1 instance not found, creating new GameObject and component.");
            var go = new UnityEngine.GameObject(ModBusV1.HubGameObjectName);
            go.AddComponent<ModBusV1>();
        }
        if (ModBusV1.Instance != null)
        {
            Log.Info("ModBusV1 instance found, activating hub.");
            ModBusV1.Instance.Active();
        }
    }

    protected override void ModDisabled()
    {
        Log.Info("Ducky.SingleProject Mod Disabled");
    }
}
