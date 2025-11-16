using Cysharp.Threading.Tasks;
using Ducky.Sdk.ModBehaviours;
using UnityEngine;

namespace Ducky.MessageHubUI;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        TerminalUIProtocol.Instance.StartAsync().Forget();

        // 获取或创建 Canvas
        var canvas = FindObjectOfType<Canvas>();

        // 创建主界面
        var mainView = TerminalMainView.Create(canvas);

        // 创建触发器（连接到主界面）
        var handler = TerminalHandlerView.Create(canvas, mainView);

        // 设置双向引用，使主界面能控制触发区域
        mainView.SetHandlerView(handler);
    }

    protected override void ModDisabled()
    {
    }
}
