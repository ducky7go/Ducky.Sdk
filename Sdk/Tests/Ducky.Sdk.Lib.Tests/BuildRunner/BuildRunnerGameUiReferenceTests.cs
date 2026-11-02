using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Checks the game UI <c>Plugins.dll</c> rules through the SDK's own <c>Ducky.Sdk.props</c>: no reference by default,
/// a compile-only reference (<c>Private=false</c>) and the <c>DUCKY_SDK_GAMEUI</c> symbol with <c>IncludeGameUi=true</c>,
/// and <c>Plugins.dll</c> never copied into the output folder. The evaluation tests use a fake managed folder and do not
/// compile. The compile test uses the repo's <c>Managed/</c> folder and is skipped when <c>Plugins.dll</c> is not there.
/// </summary>
public class BuildRunnerGameUiReferenceTests
{
    private string _root = null!;
    private string _sdk = null!;
    private string _fakeManaged = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("gameui probe");
        _sdk = FindSdkDirectory();
        _fakeManaged = Path.Combine(_root, "managed");
        Directory.CreateDirectory(_fakeManaged);
        File.WriteAllBytes(Path.Combine(_fakeManaged, "Plugins.dll"), []);
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

    private static (int exitCode, string output) RunDotnet(params string[] args)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
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

    /// <summary>A project that imports only the SDK props and prints the Plugins reference and the defines after AddDuckyManagedReferences.</summary>
    private string WriteEvaluationProbe(string managedDirectory, string extraProperties = "")
    {
        var probeDir = Path.Combine(_root, "eval");
        Directory.CreateDirectory(probeDir);
        var project = Path.Combine(probeDir, "Probe.csproj");
        File.WriteAllText(project, $"""
            <Project>
              <PropertyGroup>
                <ManagedDirectory>{managedDirectory}</ManagedDirectory>
                {extraProperties}
              </PropertyGroup>
              <Import Project="{Path.Combine(_sdk, "Ducky.Sdk.props")}" />
              <Target Name="PrintGameUi" DependsOnTargets="AddDuckyManagedReferences">
                <Message Importance="high" Text="PLUGIN=@(Reference->WithMetadataValue('Filename','Plugins')->'%(Private)')" />
                <Message Importance="high" Text="DEFINES=$(DefineConstants)" />
              </Target>
            </Project>
            """);
        return project;
    }

    private (string plugin, string defines) Evaluate(string managedDirectory, string extraProperties = "")
    {
        var project = WriteEvaluationProbe(managedDirectory, extraProperties);
        var (exit, output) = RunDotnet("msbuild", project, "-nologo", "-v:m", "-t:PrintGameUi");
        exit.ShouldBe(0, output);
        var plugin = output.Split('\n').First(l => l.Contains("PLUGIN=")).Trim().Split("PLUGIN=")[1];
        var defines = output.Split('\n').First(l => l.Contains("DEFINES=")).Trim().Split("DEFINES=")[1];
        return (plugin, defines);
    }

    [Test]
    public void DefaultMod_HasNoPluginsReference()
    {
        var (plugin, _) = Evaluate(_fakeManaged + Path.DirectorySeparatorChar);

        plugin.ShouldBe(string.Empty);
    }

    [Test]
    public void DefaultMod_DoesNotDefineTheGameUiSymbol()
    {
        var (_, defines) = Evaluate(_fakeManaged + Path.DirectorySeparatorChar);

        defines.ShouldNotContain("DUCKY_SDK_GAMEUI");
    }

    [Test]
    public void IncludeGameUi_AddsPluginsReferenceWithPrivateFalse()
    {
        var (plugin, defines) = Evaluate(_fakeManaged + Path.DirectorySeparatorChar, "<IncludeGameUi>true</IncludeGameUi>");

        plugin.ShouldBe("false");
        defines.ShouldContain("DUCKY_SDK_GAMEUI");
    }

    [Test]
    public void IncludeGameUi_WithoutTheFile_WarnsAndAddsNoReference()
    {
        var emptyManaged = Path.Combine(_root, "no-plugins");
        Directory.CreateDirectory(emptyManaged);
        var project = WriteEvaluationProbe(emptyManaged + Path.DirectorySeparatorChar, "<IncludeGameUi>true</IncludeGameUi>");

        var (exit, output) = RunDotnet("msbuild", project, "-nologo", "-v:m", "-t:PrintGameUi");

        exit.ShouldBe(0, output);
        output.ShouldContain("IncludeGameUi is true but Plugins.dll was not found");
        output.ShouldNotContain("PLUGIN=false");
    }

    [Test]
    public void IncludeGameUi_CompilesAgainstPluginsWithoutCopyingIt()
    {
        var repoManaged = Path.GetFullPath(Path.Combine(_sdk, "..", "..", "..", "Managed"));
        if (!File.Exists(Path.Combine(repoManaged, "Plugins.dll")))
        {
            Assert.Ignore("Managed/Plugins.dll is not in the repo's Managed folder; the compile check needs the game assemblies.");
        }

        var projectDir = Path.Combine(_root, "compile");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "Probe.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>netstandard2.1</TargetFramework>
                <Nullable>disable</Nullable>
                <EnableGlobalUsing>false</EnableGlobalUsing>
                <ManagedDirectory>{repoManaged + Path.DirectorySeparatorChar}</ManagedDirectory>
                <IncludeGameUi>true</IncludeGameUi>
              </PropertyGroup>
              <Import Project="{Path.Combine(_sdk, "Ducky.Sdk.props")}" />
            </Project>
            """);
        File.WriteAllText(Path.Combine(projectDir, "P.cs"), """
            public static class P
            {
                public static object Use() => typeof(UnityEngine.UI.ProceduralImage.ProceduralImage);
            }
            """);

        var project = Path.Combine(projectDir, "Probe.csproj");
        var (exit, output) = RunDotnet("build", project, "-nologo", "-v:m");
        exit.ShouldBe(0, output);

        var outputDir = Path.Combine(projectDir, "bin", "Debug", "netstandard2.1");
        Directory.GetFiles(projectDir, "Plugins.dll", SearchOption.AllDirectories).ShouldBeEmpty();

        var assembly = Path.Combine(outputDir, "Probe.dll");
        File.Exists(assembly).ShouldBeTrue();
        using var stream = File.OpenRead(assembly);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();

        var definedTypes = metadata.TypeDefinitions.Select(h => metadata.GetString(metadata.GetTypeDefinition(h).Name)).ToList();
        definedTypes.ShouldNotContain("ProceduralImage", "the mod must reference the type, not carry a copy of it");

        var referencedTypes = metadata.TypeReferences.Select(h => metadata.GetString(metadata.GetTypeReference(h).Name)).ToList();
        referencedTypes.ShouldContain("ProceduralImage");

        var referencedAssemblies = metadata.AssemblyReferences.Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name)).ToList();
        referencedAssemblies.ShouldContain("Plugins");
    }
}
