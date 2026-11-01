using System;

namespace Ducky.Sdk.Utils;

/// <summary>
/// Harmony patch identifiers that are unique to one compiled copy of the SDK. Every mod carries its own copy, so two mods
/// that patch the same method get two identifiers, and unpatching one never removes the other's patch. Touches no Unity
/// object.
/// </summary>
internal static class PatchIds
{
    /// <summary>The identifier for one feature in one copy: "&lt;assembly&gt;.&lt;feature&gt;.&lt;instance guid&gt;".</summary>
    internal static string For(string assemblyName, string feature, Guid instance) =>
        assemblyName + "." + feature + "." + instance.ToString("N");
}
