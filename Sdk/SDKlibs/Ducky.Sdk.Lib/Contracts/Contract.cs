using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// Contracts for various SDK functionalities
/// </summary>
public static class Contract
{
    /// <summary>
    /// Buffs contract singleton instance
    /// </summary>
    public static BuffsContract Buffs => BuffsContract.Instance;

    /// <summary>
    /// Mod Terminal Client contract singleton instance for the current mod
    /// </summary>
    public static ModTerminalClientContract ModTerminalClient =>
        ModTerminalClientContract.GetOrCreate(Helper.GetModId());
}
