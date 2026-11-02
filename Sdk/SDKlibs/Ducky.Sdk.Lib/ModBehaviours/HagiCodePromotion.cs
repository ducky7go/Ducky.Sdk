using System;
using UnityEngine;

namespace Ducky.Sdk.ModBehaviours;

/// <summary>
/// The one-line HagiCode promotion written to the game log each time a mod behaviour is enabled.
/// </summary>
/// <remarks>
/// Written with <c>UnityEngine.Debug.Log</c> so it reaches <c>Player.log</c> without a logging provider mod.
/// The write is compiled out when the mod sets <c>DuckyDisablePromotion=true</c> (defines
/// <c>DUCKY_SDK_NO_PROMOTION</c>, see <c>Ducky.Sdk.props</c>).
/// </remarks>
internal static class HagiCodePromotion
{
#if !DUCKY_SDK_NO_PROMOTION
    /// <summary>The promotion text: one physical line that names the SDK and carries the HagiCode URL.</summary>
    /// <remarks>Inside the guard so that an opted-out mod's DLL does not contain the text at all.</remarks>
    internal const string Message = "[Ducky.Sdk] Built with Ducky.Sdk. Discover HagiCode: https://www.hagicode.com";
#endif

    /// <summary>
    /// Sends <see cref="Message"/> once to <paramref name="sink"/>, or to <c>Debug.Log</c> when no sink is given.
    /// Never throws: a failing log write must not stop the mod from enabling.
    /// </summary>
    /// <param name="sink">Receives the message. Null means <c>UnityEngine.Debug.Log</c>.</param>
    internal static void Write(Action<string>? sink = null)
    {
#if !DUCKY_SDK_NO_PROMOTION
        try
        {
            if (sink != null)
            {
                sink(Message);
            }
            else
            {
                Debug.Log(Message);
            }
        }
        catch (Exception)
        {
            // The promotion line is optional; swallow the failure so mod startup is unaffected.
        }
#endif
    }
}
