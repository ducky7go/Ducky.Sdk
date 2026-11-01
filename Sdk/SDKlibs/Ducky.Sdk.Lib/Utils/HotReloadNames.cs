using System;
using System.Text.RegularExpressions;

namespace Ducky.Sdk.Utils;

/// <summary>
/// Naming of hot reload Core assemblies. A development hot reload build names its Core <c>&lt;ModName&gt;.Core.&lt;stamp&gt;</c>,
/// where the stamp is a UTC timestamp with millisecond precision (yyyyMMddHHmmssfff). Release builds never match.
/// </summary>
internal static class HotReloadNames
{
    internal const string CoreInfix = ".Core.";

    private static readonly Regex CoreNamePattern = new(@"^(?<mod>.+)\.Core\.(?<stamp>\d{17})$", RegexOptions.CultureInvariant);

    /// <summary>True when the assembly name is a hot reload Core name, for example "Ducky.Sample.Core.20261031210000123".</summary>
    internal static bool IsCoreAssemblyName(string? assemblyName) =>
        !string.IsNullOrEmpty(assemblyName) && CoreNamePattern.IsMatch(assemblyName!);

    /// <summary>The mod name a Core assembly belongs to, or the name unchanged when it is not a Core name.</summary>
    internal static string ToModName(string assemblyName)
    {
        var match = CoreNamePattern.Match(assemblyName);
        return match.Success ? match.Groups["mod"].Value : assemblyName;
    }
}
