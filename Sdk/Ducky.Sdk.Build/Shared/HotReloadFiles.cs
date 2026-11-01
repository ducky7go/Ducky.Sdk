#nullable enable

using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// File names and locations of the development hot reload outputs (opt-in, see docs/hot-reload.md).
/// A Core file is named ModName.Core.&lt;stamp&gt;.dll, the stamp being a UTC timestamp yyyyMMddHHmmssfff.
/// </summary>
internal static class HotReloadFiles
{
    /// <summary>The SDK's Harmony, shared by every Core load and never merged into one.</summary>
    internal const string HarmonyFileName = "0Harmony.dll";

    /// <summary>Marker file: the shim watches for new Core files only while it exists.</summary>
    internal const string MarkerFileName = "hotreload.enabled";

    /// <summary>Folder under obj/ that holds the generated shim source and the compiled shim.</summary>
    internal const string ShimFolderName = "ducky-hotreload";

    /// <summary>Folder under the mod folder where dependencies are copied when ILRepack is disabled.</summary>
    internal const string DependencyFolderName = "Dependency";

    /// <summary>
    /// True for the Core file of this mod and any build: "ModName.Core.&lt;17-digit stamp&gt;.dll".
    /// Does not match the shim or a Core of another mod.
    /// </summary>
    internal static bool IsCoreFileOf(string? modName, string fileName)
    {
        if (string.IsNullOrEmpty(modName))
        {
            return false;
        }

        var pattern = "^" + Regex.Escape(modName!) + @"\.Core\.\d{17}\.dll$";
        return Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    internal static string ShimOutputDirectory(BuildContext context) =>
        Path.Combine(context.BaseIntermediateOutputPath, ShimFolderName);

    /// <summary>The generated shim source: the template with ModName substituted.</summary>
    internal static string ShimSourcePath(BuildContext context) =>
        Path.Combine(ShimOutputDirectory(context), $"{context.ModName}.Shim.g.cs");

    /// <summary>The compiled shim, named <c>ModName.dll</c>: the assembly the game loads.</summary>
    internal static string ShimAssemblyPath(BuildContext context) =>
        Path.Combine(ShimOutputDirectory(context), $"{context.ModName}.dll");
}
