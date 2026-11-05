#if DUCKY_SDK_HARMONY
using System;
using Ducky.Sdk.Logging;
using HarmonyLib;

namespace Ducky.Sdk.Utils;

/// <summary>
/// Installs and removes one group of Harmony patches under a patch id. A failed install takes back whatever it applied before
/// it failed, so no half-installed patch stays in the game. Removal unpatches by id, so it needs no stored instance.
/// </summary>
internal static class HarmonyPatches
{
    /// <summary>
    /// Runs <paramref name="patch"/> against a Harmony instance for <paramref name="id"/>. When it throws, the patches made under
    /// that id are removed, <paramref name="failure"/> is logged, and false is returned.
    /// </summary>
    public static bool Install(string id, Action<Harmony> patch, string failure)
    {
        var harmony = new Harmony(id);
        try
        {
            patch(harmony);
            return true;
        }
        catch (Exception ex)
        {
            harmony.UnpatchAll(id); // never leave half of it in place
            Log.Error(ex, failure);
            return false;
        }
    }

    /// <summary>Removes every patch installed under <paramref name="id"/>.</summary>
    public static void Uninstall(string id) => new Harmony(id).UnpatchAll(id);
}
#endif
