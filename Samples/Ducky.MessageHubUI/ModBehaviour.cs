using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Logging;
using Ducky.Sdk.ModBehaviours;

namespace Ducky.MessageHubUI;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        // 尝试使用代理注册客户端并发送消息

        var rootCmd = new RootCommand();
        {
            foreach (var t in rootCmd.Options)
            {
                if (t is HelpOption defaultHelp)
                {
                    t.Action = new CustomHelpAction((HelpAction)defaultHelp.Action!);
                }
            }
        }
        var modIdArgument = new Argument<string>("modId");
        {
            var onlineCmd = new Command("online", "Notify that the terminal is online");
            onlineCmd.Arguments.Add(modIdArgument);
            rootCmd.Add(onlineCmd);
            onlineCmd.SetAction(result =>
            {
                Log.Info("Terminal is online. ModId: {ModId}", result.GetValue(modIdArgument)!);
            });
        }
        {
            var offlineCmd = new Command("offline", "Notify that the terminal is offline");
            offlineCmd.Arguments.Add(modIdArgument);
            rootCmd.Add(offlineCmd);
            offlineCmd.SetAction(result =>
            {
                Log.Info("Terminal is offline. ModId: {ModId}", result.GetValue(modIdArgument)!);
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
                Log.Info($"Terminal message: {message}");
            });
        }

        Contract.ModBusTerminal.Connect(async (id, message) =>
        {
            Log.Info("Received message from terminal. ModId: {ModId}, Message: {Message}", id, message);
            var parseResult = CommandLineParser.Parse(rootCmd, message);
            var re = await parseResult.InvokeAsync();
            if (parseResult.Action is CustomHelpAction act)
            {
                Log.Info("Sending help text to terminal for ModId: {ModId}", id);
                await Contract.ModBusTerminal.SendTo(id, act.HelpText);
            }

            Log.Info("Command executed with result: {Result}", re);
        });
    }

    protected override void ModDisabled()
    {
    }
}

internal class CustomHelpAction : SynchronousCommandLineAction
{
    private readonly HelpAction _defaultHelp;

    public CustomHelpAction(HelpAction action) => _defaultHelp = action;

    public string HelpText { get; private set; } = null!;

    public override int Invoke(ParseResult parseResult)
    {
        using var output = new StringWriter();
        var helpContext = new HelpContext(_defaultHelp.Builder,
            parseResult.CommandResult.Command,
            output);
        _defaultHelp.Builder.Write(helpContext);
        HelpText = output.ToString();
        Log.Info("Help requested:\n{HelpText}", output.ToString());
        return 0;
    }
}
