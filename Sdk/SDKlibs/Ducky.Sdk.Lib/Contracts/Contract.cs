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

    /// <summary>
    /// Debug event API: report events, or record the events of all mods into one file.
    /// </summary>
    public static DebugEventsContract DebugEvents => DebugEventsContract.Instance;

    /// <summary>
    /// HTTP endpoint API: declare endpoints under this mod's id, served by the mod that holds the host role.
    /// </summary>
    public static HttpEndpointsContract Http => HttpEndpointsContract.Instance;

    /// <summary>
    /// HTTP host role: claim it, forward external requests to the endpoint owners and release it.
    /// </summary>
    public static HttpHostContract HttpHost => HttpHostContract.Instance;
}
