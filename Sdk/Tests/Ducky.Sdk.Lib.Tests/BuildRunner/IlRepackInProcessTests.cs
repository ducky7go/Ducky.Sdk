using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Merges a two-assembly fixture with ILRepack.Lib in process, using the arguments BuildILRepackArgs builds,
/// and checks the merged types and exports.
/// </summary>
public class IlRepackInProcessTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp()
    {
        // A space in the directory name covers "paths with spaces".
        _dir = Path.Combine(Path.GetTempPath(), "ducky ilrepack", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Test]
    public void RunRepack_MergesTwoAssembliesWithTheSameArgumentsAsTheCli()
    {
        var main = Path.Combine(_dir, "Alpha.dll");
        var dependency = Path.Combine(_dir, "Beta.dll");
        var output = Path.Combine(_dir, "merged out", "Fixture.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        WriteFixture(main, "Ducky.Fixture.Alpha", "Run");
        WriteFixture(dependency, "Ducky.Fixture.Beta", "Go");

        var args = ILRepackAssembliesLib.BuildILRepackArgs(main, [dependency], output, NewContext());
        var (exitCode, log) = ILRepackAssembliesLib.RunRepack(args);

        exitCode.ShouldBe(0, log);
        File.Exists(output).ShouldBeTrue();
        File.Exists(Path.ChangeExtension(output, ".pdb")).ShouldBeFalse("/ndebug writes no pdb");

        var merged = ReadTypes(output);
        // The main assembly's public type stays public; the dependency's type is internalized by /internalize.
        merged["Ducky.Fixture.Alpha"].ShouldBe("public");
        merged["Ducky.Fixture.Beta"].ShouldBe("internal");
        merged.Keys.ShouldBe(["Ducky.Fixture.Alpha", "Ducky.Fixture.Beta"], ignoreOrder: true);
    }

    [Test]
    public void RunRepack_ReportsFailureForMissingInput()
    {
        var args = ILRepackAssembliesLib.BuildILRepackArgs(Path.Combine(_dir, "Missing.dll"), [],
            Path.Combine(_dir, "out.dll"), NewContext());

        var (exitCode, output) = ILRepackAssembliesLib.RunRepack(args);

        exitCode.ShouldNotBe(0);
        output.ShouldContain("Missing.dll");
    }

    private BuildContext NewContext() => new()
    {
        ProjectDir = _dir,
        Configuration = "Debug",
        TargetFramework = "netstandard2.1",
        IntermediateOutputPath = _dir + "/",
        BaseIntermediateOutputPath = _dir + "/",
        OutputPath = _dir + "/",
        ModName = "Fixture",
    };

    /// <summary>Writes a tiny assembly with one public type and one public method.</summary>
    private static void WriteFixture(string path, string typeName, string methodName)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var builder = new PersistedAssemblyBuilder(new AssemblyName(name), typeof(object).Assembly);
        var module = builder.DefineDynamicModule(name);
        var type = module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);
        var method = type.DefineMethod(methodName, MethodAttributes.Public, typeof(void), Type.EmptyTypes);
        method.GetILGenerator().Emit(OpCodes.Ret);
        type.CreateType();
        builder.Save(path);
    }

    /// <summary>Type full name to "public" or "internal", read from metadata without loading the assembly.</summary>
    private static Dictionary<string, string> ReadTypes(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var types = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var handle in reader.TypeDefinitions)
        {
            var definition = reader.GetTypeDefinition(handle);
            var ns = reader.GetString(definition.Namespace);
            var name = reader.GetString(definition.Name);
            if (name == "<Module>")
            {
                continue;
            }

            var visibility = (definition.Attributes & TypeAttributes.VisibilityMask) is TypeAttributes.Public
                ? "public"
                : "internal";
            types[string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}"] = visibility;
        }

        return types;
    }
}
