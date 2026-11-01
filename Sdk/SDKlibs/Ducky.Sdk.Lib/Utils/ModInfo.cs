using System.IO;
using System.Reflection;

namespace Ducky.Sdk.Utils;

/// <summary>
/// Identity of the running mod: its name, id, folder, assets path and whether it is a hot reload Core.
/// Every value is per mod, because each mod has its own copy of the SDK source.
/// </summary>
public static class ModInfo
{
    /// <summary>The mod name, from a <c>ModNameAttribute</c> or derived from the assembly name.</summary>
    public static string Name => Helper.GetModName();

    /// <summary>The mod id: <c>steam.&lt;digits&gt;</c> for a Workshop folder, <c>local.&lt;folder&gt;</c> otherwise.</summary>
    public static ModId Id => Helper.GetModId();

    /// <summary>
    /// Full path of the folder that holds the running mod, or null when it is unknown. For a hot reload Core this is
    /// the folder of the loaded shim assembly of the same mod. Never throws.
    /// </summary>
    public static string? Folder => Helper.GetModFolderPath();

    /// <summary>
    /// The <c>assets</c> directory inside <see cref="Folder"/>, or null when the folder is unknown.
    /// O1 (resolved): the build pipeline does not create an <c>assets</c> subdirectory. <c>DeployModLib.CopyAssets</c>
    /// (Sdk/Ducky.Sdk.Build/Libs/DeployModLib.cs) copies the CONTENTS of <c>AssetsDir</c> (default
    /// <c>&lt;project&gt;/assets</c>, Ducky.Sdk.props) into the mod folder root, with
    /// <c>GetTargetModDirectory</c> = <c>&lt;ModsDirectory&gt;/&lt;ModName&gt;</c>. So a mod deployed by the default
    /// pipeline has no <c>&lt;Folder&gt;/assets</c>. This follows the spec (sdk-mod-info); see the change's design.md
    /// Open Questions for the decision on this mismatch.
    /// </summary>
    public static string? AssetsPath => AssetsPathFor(Folder);

    /// <summary>The <c>assets</c> path under a mod folder, or null when the folder is unknown. Pure.</summary>
    internal static string? AssetsPathFor(string? folder) =>
        string.IsNullOrEmpty(folder) ? null : Path.Combine(folder!, "assets");

    /// <summary>
    /// True only when the running assembly is a development hot reload Core named <c>&lt;ModName&gt;.Core.&lt;stamp&gt;</c>.
    /// Computed once from the assembly name.
    /// </summary>
    public static bool IsHotReloadCore => IsHotReloadCoreAssembly;

    private static readonly bool IsHotReloadCoreAssembly =
        HotReloadNames.IsCoreAssemblyName(typeof(ModInfo).Assembly.GetName().Name);
}
