using System.Linq;
using System.Reflection;
using Duckov.Buffs;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.ModBehaviours;
using Ducky.Sdk.Utils;
using SodaCraft.Localizations;
using UnityEngine;

namespace Ducky.HotReload.Core
{
    /// <summary>Buff type of the probe. Registered through the SDK, so the game sees a Buff with a registrar GameObject.</summary>
    public class ProbeBuff : Buff
    {
    }

    /// <summary>
    /// Probe mod for task 1.1: registers a Buff, a localized string and (through ModBehaviourBase) the MessageHub host.
    /// It deliberately does NOT clean up in ModDisabled: the probe measures what the game keeps after an unload.
    /// </summary>
    public class CoreBehaviour : ModBehaviourBase
    {
        public static string Marker =>
            typeof(CoreBehaviour).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "Marker")?.Value ?? "?";

        protected override void ModEnabled()
        {
            Debug.Log($"[HotReload.Core] modId={Helper.GetModId()}");
            var id = Contract.Buffs.RegisterBuff<ProbeBuff>(_ => { });
            LocalizationManager.SetOverrideText(GameStateProbe.LocKey, "probe-" + Marker);
            Debug.Log($"[HotReload.Core] ModEnabled marker={Marker} buffId={id}");
        }

        protected override void ModDisabled()
        {
            Debug.Log($"[HotReload.Core] ModDisabled marker={Marker} (no cleanup: probe)");
        }
    }
}
