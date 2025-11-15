using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.ModBehaviours;

namespace Ducky.MessageHubClient;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        // 尝试使用代理注册客户端并发送消息
        UniTask.RunOnThreadPool(async () =>
        {
            await Contract.ModBusClient.Connect((terminal, id, message, toTerminal) =>
            {
                return UniTask.CompletedTask;
            });
            // await Contract.ModBusClient.SendToTerminal("help");
            // await Contract.ModBusClient.SendToTerminal("/?");
        });
    }

    protected override void ModDisabled()
    {
    }
}
