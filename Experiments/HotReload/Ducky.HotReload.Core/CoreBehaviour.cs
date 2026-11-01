using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Ducky.HotReload.Core
{
    /// <summary>What a mod author writes: enable/disable with full cleanup. Created by the shim, never by the game.</summary>
    public class CoreBehaviour : Duckov.Modding.ModBehaviour
    {
        private const string HarmonyId = "ducky.hotreload.core";
        private Harmony? _harmony;
        private int _ticks;

        public static string Marker =>
            typeof(CoreBehaviour).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "Marker")?.Value ?? "?";

        private void OnEnable()
        {
            Probe.ActiveMarker = Marker;
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(typeof(CoreBehaviour).Assembly);
            Probe.Tick += OnTick;
            var go = new GameObject("CoreRunner");
            go.transform.SetParent(transform, false);
            go.AddComponent<CoreRunner>();
            Debug.Log($"[HotReload.Core] OnEnable marker={Marker} asm={typeof(CoreBehaviour).Assembly.GetName().Name}");
        }

        private void OnDisable()
        {
            Probe.Tick -= OnTick;
            _harmony?.UnpatchAll(HarmonyId);
            _harmony = null;
            Probe.ActiveMarker = "none";
            Debug.Log($"[HotReload.Core] OnDisable marker={Marker} (ticks seen: {_ticks})");
        }

        private void OnTick() => _ticks++;
    }

    [HarmonyPatch(typeof(Probe), nameof(Probe.Ping))]
    internal static class ProbePatch
    {
        private static void Postfix() => Probe.Hits++;
    }

    public class CoreRunner : MonoBehaviour
    {
        private void Awake() => Debug.Log($"[HotReload.Core] CoreRunner.Awake marker={CoreBehaviour.Marker}");
        private void OnDestroy() => Debug.Log($"[HotReload.Core] CoreRunner.OnDestroy marker={CoreBehaviour.Marker}");
    }
}
