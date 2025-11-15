namespace Ducky.Sdk.Contracts;

public class Contract
{
    public static BuffsContract Buffs => BuffsContract.Instance;
    public static ModBusClientContract ModBusClient => ModBusClientContract.Instance;
    public static ModBusTerminalContract ModBusTerminal => ModBusTerminalContract.Instance;
}
