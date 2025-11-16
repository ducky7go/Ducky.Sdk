using System.CommandLine.Help;
using System.IO;

namespace Ducky.Sdk.Contracts.CommandLine;

public class ModHelpAction(HelpAction defaultHelp) :
    ModAsynchronousCommandLineAction(async p =>
    {
        await using var output = new StringWriter();
        var helpContext = new HelpContext(defaultHelp.Builder,
            p.CommandResult.Command,
            output);
        defaultHelp.Builder.Write(helpContext);
        return output.ToString();
    });
