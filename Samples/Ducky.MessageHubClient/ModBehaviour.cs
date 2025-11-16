using System.CommandLine.Parsing;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.CommandLine;
using Ducky.Sdk.Logging;
using Ducky.Sdk.ModBehaviours;

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
        });
    }

    protected override void ModDisabled()
    {
    }
}
