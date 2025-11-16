using System.CommandLine;
using System.CommandLine.Help;

namespace Ducky.Sdk.Contracts.CommandLine;

/// <summary>
/// Mod 专用的 RootCommand，重写了 Help 选项的行为，将帮助信息发送到 Mod 终端
/// </summary>
public class ModRootCommand : RootCommand
{
    public ModRootCommand(string description = "") : base(description)
    {
        foreach (var t in Options)
        {
            if (t is HelpOption defaultHelp)
            {
                t.Action = new ModHelpAction((HelpAction)defaultHelp.Action!);
            }
        }
    }
}
