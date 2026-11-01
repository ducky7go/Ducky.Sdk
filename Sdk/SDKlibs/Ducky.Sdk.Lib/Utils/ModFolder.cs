using System;

namespace Ducky.Sdk.Utils;

/// <summary>
/// The folder the running mod was installed in, in every loading mode. A normal load uses the assembly's own location.
/// A hot reload Core has no location, so the folder comes from the loaded hot reload shim of the same mod, which the game
/// loads from its file (see <see cref="Helper.GetModFolderPath"/>).
/// </summary>
public static class ModFolder
{
    /// <summary>The folder of the running mod.</summary>
    /// <exception cref="InvalidOperationException">No folder is known. The call never returns an empty or guessed path.</exception>
    public static string Get() => Require(Helper.GetModFolderPath());

    /// <summary>Returns <paramref name="folder"/> when it is known and throws a descriptive error when it is not.</summary>
    internal static string Require(string? folder)
    {
        if (string.IsNullOrEmpty(folder))
        {
            throw new InvalidOperationException(
                "The mod folder is unknown: the running assembly has no file location, and no loaded assembly of this mod " +
                "(the hot reload shim) has one either. Load the mod from its folder, or start it through the hot reload shim.");
        }

        return folder!;
    }
}
