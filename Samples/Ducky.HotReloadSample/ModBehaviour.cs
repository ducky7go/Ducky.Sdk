using Duckov.Buffs;
using HarmonyLib;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.GameEvents;
using Ducky.Sdk.Threading;
using Ducky.Sdk.Utils;
using Ducky.Sdk.ModBehaviours;
using SodaCraft.Localizations;

namespace Ducky.HotReloadSample;

/// <summary>
/// Hot reload validation mod (task 6.1). Uses everything a real mod uses: a Harmony patch, an event subscription,
/// a child GameObject, a Buff, a localized string and the MessageHub host (started by ModBehaviourBase).
/// ModDisabled undoes the first four; ModBehaviourBase does the rest in hot reload builds.
/// The shared SDK pieces are exercised too: level and scene-phase handlers through <c>Subscriptions</c> (released by
/// the SDK, not by ModDisabled), a <c>MainThread.Post</c> round trip from a worker thread, and <c>ModInfo</c>.
/// </summary>
public class ModBehaviour : ModBehaviourBase
{
    public const string LocKey = "HotReloadSample_Name";
    private const string UiName = "HotReloadSampleUi";
    private const string HarmonyId = "ducky.hotreloadsample";

    private static Harmony? _harmony;
    private static int _subscribed;
    private static int _levelRuns;
    private static int _phaseRuns;
    private static int _postRuns;
    private GameObject? _child;
    private GameObject? _ui;
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

        _ui = new GameObject(UiName, typeof(Canvas));
        _ui.transform.SetParent(transform, false);

        _buffId = Contract.Buffs.RegisterBuff<SampleBuff>(_ => { });
        LocalizationManager.SetOverrideText(LocKey, "sample " + Marker);

        // Released by Subscriptions after ModDisabled(), on disable and on hot reload.
        Subscriptions.Add(LevelEvents.OnInitialized(OnLevelInitialized));
        Subscriptions.Add(SceneEvents.OnPhaseChanged(OnPhaseChanged));

        // Worker thread -> MainThread.Post -> the pump runs it on the main thread in a later frame.
        System.Threading.Tasks.Task.Run(() => MainThread.Post(() => System.Threading.Interlocked.Increment(ref _postRuns)));
    }

    protected override void ModDisabled()
    {
        _harmony?.UnpatchAll(HarmonyId);
        _harmony = null;

        SceneLoader.onAfterSceneInitialize -= OnSceneInitialized;
        _subscribed = 0;

        if (_child != null) Destroy(_child);
        _child = null;

        if (_ui != null) Destroy(_ui);
        _ui = null;

        LocalizationManager.RemoveOverrideText(LocKey);
    }

    private void OnSceneInitialized(SceneLoadingContext context)
    {
        // Intentionally empty: the subscription is what the validation counts.
    }

    private void OnLevelInitialized()
    {
        System.Threading.Interlocked.Increment(ref _levelRuns);
    }

    private void OnPhaseChanged(ScenePhase previous, ScenePhase next)
    {
        System.Threading.Interlocked.Increment(ref _phaseRuns);
    }

    /// <summary>
    /// Handlers of this Core on a game event, found through the event's backing delegate and filtered by the declaring
    /// assembly. -1 when the reflection fails.
    /// </summary>
    private static int CountOwnHandlers(System.Type type, string eventName)
    {
        try
        {
            var field = type.GetField(eventName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (!(field?.GetValue(null) is System.Delegate handlers))
            {
                return 0;
            }

            var own = typeof(ModBehaviour).Assembly;
            return handlers.GetInvocationList().Count(d => d.Method.DeclaringType?.Assembly == own);
        }
        catch (System.Exception)
        {
            return -1;
        }
    }

    /// <summary>
    /// Read by the shim after every unload and load (Report). Counts what this Core still holds.
    /// </summary>
    public static string HotReloadReport()
    {
        var patches = _harmony?.GetPatchedMethods().Count() ?? 0;
        var loc = LocalizationManager.TryGetOverrideText(LocKey, out var text) ? text : "none";
        var ui = UnityEngine.Object.FindObjectsOfType<Canvas>().Count(c => c != null && c.gameObject.name == UiName);
        var holderName = $"{Helper.GetModId()}_BuffsContractHolder"; // this mod's holder only; other mods have their own
        var holders = UnityEngine.Object.FindObjectsOfType<Transform>().Count(t => t.name == holderName);
        var pumpName = $"{ModInfo.Id}_MainThreadPump";
        var pumps = UnityEngine.Object.FindObjectsOfType<Transform>().Count(t => t.name == pumpName);
        var levelHandlers = CountOwnHandlers(typeof(LevelManager), "OnLevelInitialized");
        var phaseHandlers = CountOwnHandlers(typeof(UnityEngine.SceneManagement.SceneManager), "activeSceneChanged");
        return $"samplePatches={patches} sampleSubscriptions={_subscribed} sampleUi={ui} sampleHolders={holders} sampleLocalization={loc} " +
               $"sampleCore={ModInfo.IsHotReloadCore} sampleModId={ModInfo.Id} sampleLevelHandlers={levelHandlers} samplePhaseHandlers={phaseHandlers} " +
               $"sampleLevelRuns={_levelRuns} samplePhaseRuns={_phaseRuns} samplePostRuns={_postRuns} samplePumps={pumps}";
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
