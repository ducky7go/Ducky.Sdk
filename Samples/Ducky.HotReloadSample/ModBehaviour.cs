using Duckov.Buffs;
using HarmonyLib;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.ModBehaviours;
using SodaCraft.Localizations;

namespace Ducky.HotReloadSample;

/// <summary>
/// Hot reload validation mod (task 6.1). Uses everything a real mod uses: a Harmony patch, an event subscription,
/// a child GameObject, a Buff, a localized string and the MessageHub host (started by ModBehaviourBase).
/// ModDisabled undoes the first four; ModBehaviourBase does the rest in hot reload builds.
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    public const string LocKey = "HotReloadSample_Name";
    private const string HarmonyId = "ducky.hotreloadsample";

    private static Harmony? _harmony;
    private static int _subscribed;
    private GameObject? _child;
    private int _buffId;

    public static string Marker => typeof(ModBehaviour).Assembly.GetName().Name ?? "?";

    protected override void ModEnabled()
    {
        _harmony = new Harmony(HarmonyId);
        _harmony.PatchAll(typeof(ModBehaviour).Assembly);

        SceneLoader.onAfterSceneInitialize += OnSceneInitialized;
        _subscribed = 1;

        _child = new GameObject("HotReloadSampleChild");
        _child.transform.SetParent(transform, false);

        _buffId = Contract.Buffs.RegisterBuff<SampleBuff>(_ => { });
        LocalizationManager.SetOverrideText(LocKey, "sample " + Marker);
    }

    protected override void ModDisabled()
    {
        _harmony?.UnpatchAll(HarmonyId);
        _harmony = null;

        SceneLoader.onAfterSceneInitialize -= OnSceneInitialized;
        _subscribed = 0;

        if (_child != null) Destroy(_child);
        _child = null;

        LocalizationManager.RemoveOverrideText(LocKey);
    }

    private void OnSceneInitialized(SceneLoadingContext context)
    {
        // Intentionally empty: the subscription is what the validation counts.
    }

    /// <summary>
    /// Read by the shim after every unload and load (Report). Counts what this Core still holds.
    /// </summary>
    public static string HotReloadReport()
    {
        var patches = _harmony?.GetPatchedMethods().Count() ?? 0;
        var loc = LocalizationManager.TryGetOverrideText(LocKey, out var text) ? text : "none";
        return $"samplePatches={patches} sampleSubscriptions={_subscribed} sampleLocalization={loc}";
    }
}

/// <summary>The sample's Buff. Registered through Contract.Buffs; the SDK removes it in a hot reload build.</summary>
public class SampleBuff : Buff
{
}

/// <summary>Patch with an empty prefix, so the game behaves the same and the patch is still a live Harmony patch.</summary>
[HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.LoadMainMenu))]
internal static class SampleLoadMainMenuPatch
{
    private static void Prefix()
    {
    }
}
