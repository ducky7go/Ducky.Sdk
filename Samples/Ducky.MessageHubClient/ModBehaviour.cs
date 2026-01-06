using System.CommandLine.Parsing;
using Ducky.Sdk.Utils;

namespace Ducky.MessageHubClient;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        var modRootCommand = new ModRootCommand("Ducky Message Hub Client");
        // 使用 ModTerminalClientContract 连接
        UniTask.RunOnThreadPool(async () =>
        {
            var client = Contract.ModTerminalClient;
            await client.Connect(async (terminal, id, message, toTerminal) =>
            {
                Log.Info($"Received message from {id}: {message}");
                if (message == "ping")
                {
                    await toTerminal("pong");
                }

                else
                {
                    var parseResult = CommandLineParser.Parse(modRootCommand, message);
                    await parseResult.InvokeAsync();
                }
            });
            var nativeClient = ModHttpV1ClientContract.GetOrCreate(Helper.GetModId());
            await nativeClient.SendTo("someonemissing", "command", "Hello from Ducky.MessageHubClient Mod!");
        });
    }

    protected override void ModDisabled()
    {
    }
}
