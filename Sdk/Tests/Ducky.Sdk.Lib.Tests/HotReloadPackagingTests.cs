using System.Diagnostics;
using Ducky.Sdk.Lib.Tests.Runner;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// MSBuild-level checks of the hot reload opt-in matrix (default, opt-in, Release plus opt-in, IsModLib plus opt-in),
/// the stamped Core name, and the release guard in Ducky.Sdk.Validation.targets. They evaluate the SDK's own props and
/// targets with a small probe project, so no game or package is needed.
/// </summary>
public class HotReloadPackagingTests
{
    private string _root = null!;
    private string _probe = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("msbuild probe");
        _probe = Path.Combine(_root, "Ducky.Probe");
        Directory.CreateDirectory(_probe);
        var sdk = FindSdkDirectory();
        File.WriteAllText(Path.Combine(_probe, "Probe.csproj"), $"""
            <Project>
              <PropertyGroup>
                <ModName>Ducky.Probe</ModName>
              </PropertyGroup>
              <Import Project="{Path.Combine(sdk, "Ducky.Sdk.props")}" />
              <Import Project="{Path.Combine(sdk, "Ducky.Sdk.Orchestration.targets")}" />
              <Import Project="{Path.Combine(sdk, "Ducky.Sdk.Validation.targets")}" />
              <Target Name="PrintActive" DependsOnTargets="InitializeDuckySdkProperties">
                <Message Importance="high" Text="ACTIVE=$(DuckyHotReloadActive)" />
              </Target>
              <Target Name="PrintStamp" DependsOnTargets="DuckyHotReloadPrepare">
                <Message Importance="high" Text="ASSEMBLY=$(AssemblyName)" />
                <Message Importance="high" Text="INTERMEDIATE=@(IntermediateAssembly->'%(Filename)%(Extension)')" />
              </Target>
            </Project>
            """);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    /// <summary>The SDK package folder (the one with Ducky.Sdk.props), found from the test assembly upwards.</summary>
    private static string FindSdkDirectory()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Sdk", "SDKlibs", "Ducky.Sdk");
            if (File.Exists(Path.Combine(candidate, "Ducky.Sdk.props")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Ducky.Sdk.props not found above the test directory.");
    }

    private static (int exitCode, string output) Run(string projectFile, params string[] args)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("msbuild");
        psi.ArgumentList.Add(projectFile);
        psi.ArgumentList.Add("-nologo");
        psi.ArgumentList.Add("-v:m");
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.Result + stderr.Result);
    }

    private string Probe => Path.Combine(_probe, "Probe.csproj");

    private string ActiveFor(params string[] args)
    {
        var (exit, output) = Run(Probe, ["-t:PrintActive", .. args]);
        exit.ShouldBe(0, output);
        var line = output.Split('\n').FirstOrDefault(l => l.Contains("ACTIVE="));
        return line!.Trim().Split("ACTIVE=")[1];
    }

    [Test]
    public void Default_IsOff()
    {
        ActiveFor("-p:Configuration=Debug").ShouldBe("false");
    }

    [Test]
    public void OptIn_InDebug_IsOn()
    {
        ActiveFor("-p:Configuration=Debug", "-p:DuckyHotReload=true").ShouldBe("true");
    }

    [Test]
    public void OptIn_InRelease_IsIgnoredWithAWarning()
    {
        var (exit, output) = Run(Probe, ["-t:PrintActive", "-p:Configuration=Release", "-p:DuckyHotReload=true"]);

        exit.ShouldBe(0, output);
        output.ShouldContain("ACTIVE=false");
        output.ShouldContain("DuckyHotReload is ignored in the Release configuration");
    }

    [Test]
    public void OptIn_ForIsModLib_IsIgnoredWithAWarning()
    {
        var (exit, output) = Run(Probe, ["-t:PrintActive", "-p:Configuration=Debug", "-p:DuckyHotReload=true", "-p:IsModLib=true"]);

        exit.ShouldBe(0, output);
        output.ShouldContain("ACTIVE=false");
        output.ShouldContain("DuckyHotReload is ignored for library projects");
    }

    [Test]
    public void Stamp_DiffersBetweenBuildsAndIsInTheCoreName()
    {
        var first = StampedName();
        Thread.Sleep(20);
        var second = StampedName();

        first.ShouldNotBe(second);
        System.Text.RegularExpressions.Regex.IsMatch(first, @"^Ducky\.Probe\.Core\.\d{17}$").ShouldBeTrue(first);
        System.Text.RegularExpressions.Regex.IsMatch(second, @"^Ducky\.Probe\.Core\.\d{17}$").ShouldBeTrue(second);
    }

    private string StampedName()
    {
        var (exit, output) = Run(Probe, ["-t:PrintStamp", "-p:Configuration=Debug", "-p:DuckyHotReload=true"]);
        exit.ShouldBe(0, output);
        var line = output.Split('\n').First(l => l.Contains("ASSEMBLY="));
        var name = line.Trim().Split("ASSEMBLY=")[1];
        output.ShouldContain($"INTERMEDIATE={name}.dll");
        return name;
    }

    [Test]
    public void ReleaseGuard_FailsWhenDevelopmentArtifactsArePresent()
    {
        var mods = Path.Combine(_root, "Mods");
        var modDir = Path.Combine(mods, "Ducky.Probe");
        Directory.CreateDirectory(modDir);
        File.WriteAllText(Path.Combine(modDir, "hotreload.enabled"), "");

        var (exit, output) = Run(Probe, ["-t:ValidateHotReloadPackageInput", $"-p:ModsDirectory={mods}{Path.DirectorySeparatorChar}"]);

        exit.ShouldNotBe(0);
        output.ShouldContain("Hot reload development artifacts found");
    }

    [Test]
    public void ReleaseGuard_PassesForAFolderWithoutDevelopmentArtifacts()
    {
        var mods = Path.Combine(_root, "Mods");
        var modDir = Path.Combine(mods, "Ducky.Probe");
        Directory.CreateDirectory(modDir);
        File.WriteAllText(Path.Combine(modDir, "Ducky.Probe.dll"), "release");

        var (exit, output) = Run(Probe, ["-t:ValidateHotReloadPackageInput", $"-p:ModsDirectory={mods}{Path.DirectorySeparatorChar}"]);

        exit.ShouldBe(0, output);
    }

    [Test]
    public void ReleaseGuard_FailsForAStampedCoreInTheMainOutput()
    {
        var mods = Path.Combine(_root, "Mods");
        var outDir = Path.Combine(_root, "out");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "Ducky.Probe.Core.20261031210000123.dll"), "core");

        var (exit, output) = Run(Probe, ["-t:ValidateHotReloadPackageInput", $"-p:ModsDirectory={mods}{Path.DirectorySeparatorChar}",
            $"-p:OutDir={outDir}{Path.DirectorySeparatorChar}"]);

        exit.ShouldNotBe(0);
        output.ShouldContain("Ducky.Probe.Core.20261031210000123.dll");
    }
}
