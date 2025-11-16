using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.Contracts.CommandLine;

/// <summary>
/// Mod 专用的异步命令行操作，将结果发送到 Mod 终端
/// </summary>
/// <param name="action"></param>
public class ModAsynchronousCommandLineAction(Func<ParseResult, UniTask<string>> action) :
    AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var result = await action.Invoke(parseResult);
        Log.Debug("ModAsynchronousCommandLineAction result: " + result);
        if (!string.IsNullOrEmpty(result))
        {
            Log.Info("Sending result to terminal...");
            try
            {
                await Contract.ModTerminalClient.ShowToTerminal(result);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to send result to terminal");
                throw;
            }
        }

        return 0;
    }
}