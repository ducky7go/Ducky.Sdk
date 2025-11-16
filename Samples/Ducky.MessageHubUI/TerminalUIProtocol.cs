using System.CommandLine;
using System.CommandLine.Parsing;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.CommandLine;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using R3;
using UnityEngine;

namespace Ducky.MessageHubUI;

public class TerminalUIProtocol : MonoBehaviour
{
    private static TerminalUIProtocol? _instance;
    private static GameObject? _containerGameObject;

    public static TerminalUIProtocol Instance
    {
        get
        {
            if (_instance == null)
            {
                // 创建容器 GameObject（如果不存在）
                if (_containerGameObject == null)
                {
                    _containerGameObject = new GameObject("TerminalUIProtocol");
                    DontDestroyOnLoad(_containerGameObject);
                }

                _instance = _containerGameObject.AddComponent<TerminalUIProtocol>();
                Log.Info("Created TerminalUIProtocol singleton instance");
            }

            return _instance;
        }
    }

    /// <summary>
    /// terminal UI 客户端合约, 用于接收 command 命令
    /// </summary>
    private ModTerminalClientContract? _terminalUI;

    /// <summary>
    /// local terminal 客户端合约, 用于发送命令到 Terminal UI
    /// </summary>
    private ModTerminalClientContract? _terminalClientContract;

    /// <summary>
    /// low level HTTP 客户端合约, 用于发送命令到其他 Mod
    /// </summary>
    private ModHttpV1ClientContract? _modHttpV1ClientContract;

    /// <summary>
    /// 在线客户端列表（不包含 TerminalUI 自身）
    /// </summary>
    private readonly HashSet<string> _onlineModIds = [];

    /// <summary>
    /// 可观测的在线 ModId 列表（不包含 TerminalUI 自身）
    /// UI 可以订阅此属性以响应在线客户端的变化
    /// </summary>
    public ReactiveProperty<IReadOnlyList<string>> OnlineModIds { get; } = new(new List<string>());

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _terminalUI = ModTerminalClientContract.GetOrCreate(ModTerminalClientContract.TerminalUIModId);
        _terminalClientContract = ModTerminalClientContract.GetOrCreate(Helper.GetModId());
        _modHttpV1ClientContract = ModHttpV1ClientContract.GetOrCreate(Helper.GetModId());
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            Log.Info("Destroyed TerminalUIProtocol singleton instance");
        }
    }

    public UniTask SendCommand(string modId, string command)
    {
        if (_modHttpV1ClientContract == null)
        {
            throw new InvalidOperationException("TerminalUIProtocol not initialized. HttpClient is null.");
        }

        return _modHttpV1ClientContract.SendTo(modId, ModTerminalClientContract.ContentTypeCli, command);
    }

    private ModRootCommand CreateCommand()
    {
        var rootCmd = new ModRootCommand();

        var modIdArgument = new Argument<string>("modId");
        {
            var onlineCmd = new Command("online", "Notify that the terminal is online");
            onlineCmd.Arguments.Add(modIdArgument);
            rootCmd.Add(onlineCmd);
            onlineCmd.Action = new ModAsynchronousCommandLineAction(result =>
            {
                var modId = result.GetValue(modIdArgument)!;

                _onlineModIds.Add(modId);
                Log.Info("Terminal is online. ModId: {ModId}, Total online: {Count}", modId, _onlineModIds.Count);

                // 更新可观测属性，触发订阅者更新
                OnlineModIds.Value = _onlineModIds.ToList();
                return UniTask.FromResult($"Mod {modId} is now online.");
            });
        }
        {
            var offlineCmd = new Command("offline", "Notify that the terminal is offline");
            offlineCmd.Arguments.Add(modIdArgument);
            rootCmd.Add(offlineCmd);
            offlineCmd.Action = new ModAsynchronousCommandLineAction(result =>
            {
                var modId = result.GetValue(modIdArgument)!;

                _onlineModIds.Remove(modId);
                Log.Info("Terminal is offline. ModId: {ModId}, Total online: {Count}", modId, _onlineModIds.Count);

                // 更新可观测属性，触发订阅者更新
                OnlineModIds.Value = _onlineModIds.ToList();
                return UniTask.FromResult($"Mod {modId} is now offline.");
            });
        }
        {
            var showCmd = new Command("show", "Show a message on the terminal");
            rootCmd.Add(showCmd);

            var messageArg = new Argument<string>("message");
            showCmd.Arguments.Add(messageArg);
            showCmd.SetAction(result =>
            {
                var message = result.GetValue(messageArg)!;
                if (!string.IsNullOrEmpty(message))
                {
                    // 规范化换行符：将 \r\n 和 \r 统一替换为 \n
                    // 这确保了跨平台的换行符兼容性（Windows、Linux、macOS）
                    var normalizedMessage = message.Replace("\\r\\n", "\n").Replace("\\r", "\n");

                    // 使用线程安全的 PostMessage 方法显示消息
                    // 这个方法可以从任何线程调用
                    if (TerminalMainView.Instance != null)
                    {
                        try
                        {
                            TerminalMainView.Instance.PostMessage(normalizedMessage, MessageType.System);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "Failed to post message to TerminalMainView");
                            throw;
                        }
                    }
                    else
                    {
                        Log.Warn("TerminalMainView instance not found, cannot display message");
                    }
                }

                Log.Info($"Terminal message: {message}");
            });
        }
        return rootCmd;
    }

    public async UniTask StartAsync()
    {
        if (_terminalUI == null)
        {
            throw new InvalidOperationException("TerminalUIProtocol not initialized. TerminalClient is null.");
        }

        if (_modHttpV1ClientContract == null)
        {
            throw new InvalidOperationException("TerminalUIProtocol not initialized. HttpClient is null.");
        }

        if (_terminalClientContract == null)
        {
            throw new InvalidOperationException("TerminalUIProtocol not initialized. TerminalClient is null.");
        }

        await _terminalClientContract.Connect((fromTerminal, s, s1, arg3) => UniTask.CompletedTask);
        // no need to connect modHttpV1ClientContract here since it already connected in _terminalClientContract
        // await _modHttpV1ClientContract.Connect((s, s1, arg3) => UniTask.CompletedTask);

        var command = CreateCommand();
        await _terminalUI.Connect(async (fromTerminal, fromModId, body, message) =>
        {
            Log.Info("Received message from terminal. ModId: {ModId}, Message: {Message}", fromModId, body);
            var parseResult = CommandLineParser.Parse(command, body);
            var re = await parseResult.InvokeAsync();

            Log.Debug(
                re > 0
                    ? "Command execution returned non-zero result: {Result}"
                    : "Command executed successfully with result: {Result}", re);

            Log.Info("Command executed with result: {Result}", re);
        });
    }
}
