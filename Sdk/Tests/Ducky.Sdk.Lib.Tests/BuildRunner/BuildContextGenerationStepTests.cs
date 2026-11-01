namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Checks the ported ContextJsonBuild.csx step against a golden file produced by the legacy script
/// (Golden/ducky-build-context.golden.json, with the sample root replaced by __ROOT__ and isCIEnvironment left out).
/// </summary>
public class BuildContextGenerationStepTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        // A space in the directory name covers the "paths with spaces" requirement.
        _root = Path.Combine(Path.GetTempPath(), "ducky runner gen", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Samples", "Ducky.EntranceMod"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string ProjectDir => Path.Combine(_root, "Samples", "Ducky.EntranceMod");

    /// <summary>The 19 property values the GenerateBuildContext target writes, same values as the golden run.</summary>
    private IEnumerable<string> PropertyLines()
    {
        var p = ProjectDir;
        var values = new[]
        {
            p, "Ducky.EntranceMod", "Debug", "netstandard2.1", "", "",
            Path.Combine(p, "assets"), Path.Combine(p, "assets"), "true", "true", "false", "true", "true", "false",
            Path.Combine(p, "obj", "Debug", "netstandard2.1") + "/",
            Path.Combine(p, "obj") + "/",
            Path.Combine(p, "bin", "Debug", "netstandard2.1") + "/",
            Path.Combine(_root, "Managed") + "/",
            Path.Combine(_root, "artifacts", "Mods") + "/",
        };
        return values.Select((value, index) => $"{index + 1}={value}");
    }

    private string WriteProperties()
    {
        var path = Path.Combine(ProjectDir, "obj", BuildContextGenerationStep.PropertiesFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, PropertyLines());
        return path;
    }

    private static string Normalize(string json) => json
        .Replace("\r\n", "\n")
        .Replace('\\', '/')
        .Split('\n')
        .Where(line => !line.Contains("\"isCIEnvironment\""))
        .Aggregate(string.Empty, (acc, line) => acc + line + "\n");

    private static string ReadGolden(string root)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "ducky-build-context.golden.json"));
        return Normalize(text.Replace("__ROOT__", root.Replace('\\', '/')));
    }

    [Test]
    public void Execute_WritesJsonMatchingLegacyGoldenOutput()
    {
        var propertiesPath = WriteProperties();
        var jsonPath = Path.Combine(ProjectDir, "obj", "ducky-build-context.json");

        var exit = BuildContextGenerationStep.Execute(propertiesPath, jsonPath);

        exit.ShouldBe(0);
        Normalize(File.ReadAllText(jsonPath)).ShouldBe(ReadGolden(_root));
    }

    [Test]
    public void Execute_OutputDeserializesThroughBuildContextFromJson()
    {
        var jsonPath = Path.Combine(ProjectDir, "obj", "ducky-build-context.json");
        BuildContextGenerationStep.Execute(WriteProperties(), jsonPath).ShouldBe(0);

        var context = BuildContext.FromJson(File.ReadAllText(jsonPath));

        context.ShouldNotBeNull();
        context!.ModName.ShouldBe("Ducky.EntranceMod");
        context.ProjectDir.ShouldBe(ProjectDir);
        context.EnableILRepack.ShouldBeTrue();
        context.DeployMod.ShouldBeTrue();
        context.IsModLib.ShouldBeFalse();
    }

    [Test]
    public void Execute_KeepsSemicolonsInLocalizationAssetsDirAsOneValue()
    {
        var propertiesPath = WriteProperties();
        var lines = File.ReadAllLines(propertiesPath)
            .Select(line => line.StartsWith("8=", StringComparison.Ordinal) ? "8=/a dir;/b dir" : line);
        File.WriteAllLines(propertiesPath, lines);
        var jsonPath = Path.Combine(ProjectDir, "obj", "ducky-build-context.json");

        BuildContextGenerationStep.Execute(propertiesPath, jsonPath).ShouldBe(0);

        BuildContext.FromJson(File.ReadAllText(jsonPath))!.LocalizationAssetsDir.ShouldBe("/a dir;/b dir");
    }

    [Test]
    public void Execute_FailsWithActionableMessageWhenPropertyFileIsMissing()
    {
        var missing = Path.Combine(ProjectDir, "obj", BuildContextGenerationStep.PropertiesFileName);

        var (exit, output) = CaptureConsoleError(() =>
            BuildContextGenerationStep.Execute(missing, Path.Combine(ProjectDir, "obj", "ducky-build-context.json")));

        exit.ShouldBe(1);
        output.ShouldContain(missing);
        output.ShouldContain("Rebuild the project");
    }

    private static (int exit, string output) CaptureConsoleError(Func<int> action)
    {
        var original = Console.Error;
        using var writer = new StringWriter();
        Console.SetError(writer);
        try
        {
            var exit = action();
            return (exit, writer.ToString());
        }
        finally
        {
            Console.SetError(original);
        }
    }
}
