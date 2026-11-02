#if DUCKY_SDK_GAMEUI
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace Ducky.Sdk.GameUi;

/// <summary>
/// The only code in the SDK that names the game's <c>ProceduralImage</c> and <c>UniformModifier</c> types. Every method is
/// <c>NoInlining</c>, so a missing or changed type surfaces at the call site in <see cref="GameUiRounded"/>, where it is
/// caught. Signatures use <see cref="Image"/> only, so no caller has to compile against the game's <c>Plugins.dll</c>.
/// Main thread only, like every Unity call.
/// </summary>
internal static class ProceduralImageBridge
{
    /// <summary>True in a build that has the game's <c>Plugins.dll</c> compiled in (the <c>DUCKY_SDK_GAMEUI</c> symbol).</summary>
    internal static bool Compiled => true;

    /// <summary>The existing rounded graphic on <paramref name="target"/>, or null when there is none.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Image? FindFill(GameObject target) => target.GetComponent<ProceduralImage>();

    /// <summary>Adds a solid rounded graphic with a uniform corner radius to <paramref name="target"/>.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Image CreateFill(GameObject target, float radius)
    {
        var image = target.AddComponent<ProceduralImage>();
        SetUniformModifier(image, radius);
        return image;
    }

    /// <summary>Adds a border-only rounded graphic (<paramref name="borderWidth"/> greater than zero) to <paramref name="rimObject"/>.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Image CreateRim(GameObject rimObject, float radius, float borderWidth)
    {
        var image = rimObject.AddComponent<ProceduralImage>();
        image.BorderWidth = borderWidth;
        SetUniformModifier(image, radius);
        return image;
    }

    /// <summary>Sets the corner radius of a rounded graphic returned by <see cref="FindFill"/>.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void SetRadius(Image graphic, float radius)
    {
        if (graphic is ProceduralImage image)
        {
            SetUniformModifier(image, radius);
        }
    }

    /// <summary>Destroys the rounded graphic and its modifier on <paramref name="target"/>, if any (used after a failed or forced fallback).</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Cleanup(GameObject target)
    {
        foreach (var modifier in target.GetComponents<UniformModifier>())
        {
            UnityEngine.Object.DestroyImmediate(modifier);
        }

        foreach (var image in target.GetComponents<ProceduralImage>())
        {
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static void SetUniformModifier(ProceduralImage image, float radius)
    {
        image.ModifierType = typeof(UniformModifier);
        var modifier = image.GetComponent<UniformModifier>();
        if (modifier == null)
        {
            modifier = image.gameObject.AddComponent<UniformModifier>();
        }

        modifier.Radius = radius;
        image.SetVerticesDirty();
    }
}
#else
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ducky.Sdk.GameUi;

/// <summary>
/// Stand-in used when the SDK is built without the game's <c>Plugins.dll</c>. <see cref="GameUiRounded"/> checks
/// <see cref="Compiled"/> first and never calls these members, so they only keep the call sites compiling.
/// </summary>
internal static class ProceduralImageBridge
{
    /// <summary>False: the rounded graphics are not compiled into this build.</summary>
    internal static bool Compiled => false;

    internal static Image? FindFill(GameObject target) => throw NotCompiled();

    internal static Image CreateFill(GameObject target, float radius) => throw NotCompiled();

    internal static Image CreateRim(GameObject rimObject, float radius, float borderWidth) => throw NotCompiled();

    internal static void SetRadius(Image graphic, float radius) => throw NotCompiled();

    internal static void Cleanup(GameObject target) => throw NotCompiled();

    private static Exception NotCompiled() =>
        new InvalidOperationException("The rounded graphics are not compiled into this build (DUCKY_SDK_GAMEUI is not defined).");
}
#endif
