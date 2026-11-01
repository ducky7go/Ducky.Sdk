using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Duckov.Buffs;
using Duckov.Utilities;
using SodaCraft.Localizations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ducky.HotReload
{
    /// <summary>
    /// Task 1.1 probe: counts the game-side state that a ModBehaviourBase mod leaves behind, and removes it through the
    /// game's own APIs (to show which APIs can remove each kind). Lives in the shim, so it survives every Core reload.
    /// </summary>
    public static class GameStateProbe
    {
        public const string BuffTypeName = "ProbeBuff";
        public const string LocKey = "HotReloadProbe_Name";
        private const string HostGoName = "ModHttpV1";
        private const string HolderName = "local.Ducky.HotReload_BuffsContractHolder"; // this mod's holder (Helper.GetModId + suffix)

        /// <summary>Buff list of the game (GameplayDataSettings.Buffs, private field allBuffs).</summary>
        private static List<Buff> AllBuffs()
        {
            var data = GameplayDataSettings.Buffs;
            var field = data.GetType().GetField("allBuffs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (List<Buff>)field.GetValue(data);
        }

        private static Transform[] AllTransforms() => Object.FindObjectsOfType<Transform>();

        /// <summary>Compact state line appended to the REPORT line.</summary>
        public static string Describe()
        {
            var buffs = AllBuffs().Count(b => b != null && b.GetType().Name == BuffTypeName);
            var registrars = AllTransforms().Count(t => t.name == "BuffRegistrar_" + BuffTypeName);
            var hosts = Object.FindObjectsOfType<MonoBehaviour>().Count(m => m != null && m.GetType().Name == "ModHttpV1");
            var holders = AllTransforms().Count(t => t.name == HolderName);
            var loc = LocalizationManager.TryGetOverrideText(LocKey, out var text) ? text : "none";
            return $"probeBuffs={buffs} probeRegistrars={registrars} hostComponents={hosts} buffHolders={holders} loc={loc}";
        }

        /// <summary>Removes every probe registration through game APIs. Returns a line describing what was removed.</summary>
        public static string RemoveAll()
        {
            var removedLoc = LocalizationManager.RemoveOverrideText(LocKey);

            var list = AllBuffs();
            var probeBuffs = list.Where(b => b != null && b.GetType().Name == BuffTypeName).ToList();
            foreach (var buff in probeBuffs)
            {
                list.Remove(buff);
                Object.Destroy(buff.gameObject);
            }

            var hostGos = Object.FindObjectsOfType<MonoBehaviour>()
                .Where(m => m != null && m.GetType().Name == "ModHttpV1")
                .Select(m => m.gameObject).Distinct().ToList();
            foreach (var go in hostGos) Object.Destroy(go);

            var holders = AllTransforms().Where(t => t.name == HolderName)
                .Select(t => t.gameObject).ToList();
            foreach (var go in holders) Object.Destroy(go);

            return $"removed locRemoved={removedLoc} buffs={probeBuffs.Count} hosts={hostGos.Count} holders={holders.Count}";
        }
    }
}
