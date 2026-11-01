namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Regression tests for the dotnet-script build helpers (SDKlibs/scripts), compiled into this project.
/// </summary>
public class BuildScriptsTests
{
    private static BuildContext NewContext(string? managedDirectory) => new()
    {
        ProjectDir = "/proj",
        Configuration = "Release",
        TargetFramework = "netstandard2.1",
        IntermediateOutputPath = "obj/",
        BaseIntermediateOutputPath = "obj/",
        OutputPath = "bin/",
        ManagedDirectory_Explicit = managedDirectory
    };

    [Test]
    public void BuildILRepackArgs_KeepsPathsWithSpacesAsSingleUnquotedArguments()
    {
        var main = Path.Combine("/games/Escape from Duckov/mods/My Mod", "My Mod.dll");
        var dep = Path.Combine("/games/Escape from Duckov/mods/My Mod", "Dep One.dll");
        var output = "/tmp/out dir/My Mod.dll";

        var args = ILRepackAssembliesLib.BuildILRepackArgs(main, [dep], output,
            NewContext("/games/Escape from Duckov/Duckov_Data/Managed"));

        args.ShouldContain($"/out:{output}");
        args.ShouldContain(main);
        args.ShouldContain(Path.GetFullPath(dep));
        args.ShouldContain("/lib:/games/Escape from Duckov/mods/My Mod");
        args.ShouldContain("/lib:/games/Escape from Duckov/Duckov_Data/Managed");
        // Quotes are added by the process launcher, never embedded in the arguments themselves.
        args.ShouldAllBe(a => !a.Contains('"'));
    }

    [Test]
    public void BuildILRepackArgs_OmitsEmptyManagedDirectory()
    {
        var args = ILRepackAssembliesLib.BuildILRepackArgs("/a/Main.dll", [], "/tmp/Main.dll", NewContext(null));

        args.ShouldNotContain(a => a == "/lib:");
        args.ShouldContain("/lib:/a");
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ExecuteCommand_PassesEachArgumentIntact()
    {
        var (exitCode, output) = BuildUtils.ExecuteCommand("sh", Path.GetTempPath(),
            "-c", "printf '%s|' \"$@\"", "sh", "plain", "with space", "/out:/tmp/a b/c.dll");

        exitCode.ShouldBe(0);
        output.ShouldBe("plain|with space|/out:/tmp/a b/c.dll|");
    }

    [Test]
    [Platform(Include = "Linux,MacOsX")]
    public void ExecuteCommand_DoesNotDeadlockOnLargeStderr()
    {
        // ~1 MB on stderr (> pipe buffer) followed by a little stdout.
        var task = Task.Run(() => BuildUtils.ExecuteCommand("sh", Path.GetTempPath(),
            "-c", "head -c 1048576 /dev/zero | tr '\\0' 'x' >&2; echo done"));

        task.Wait(TimeSpan.FromSeconds(30)).ShouldBeTrue("ExecuteCommand blocked on a full stderr pipe");
        task.Result.exitCode.ShouldBe(0);
        task.Result.output.ShouldStartWith("done");
    }

    [Test]
    public void ExecuteCommand_ReportsMissingToolWithInstallHint()
    {
        var (exitCode, output) = BuildUtils.ExecuteCommand("definitely-not-a-real-tool-xyz", Path.GetTempPath());

        exitCode.ShouldNotBe(0);
        output.ShouldContain("definitely-not-a-real-tool-xyz");
        output.ShouldContain("PATH");
    }
}
