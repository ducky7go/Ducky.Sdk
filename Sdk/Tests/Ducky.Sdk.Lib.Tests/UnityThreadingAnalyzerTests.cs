using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ducky.Sdk.Analyser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <c>DUCKY006</c> and <c>DUCKY007</c>, run on inline source through <c>CompilationWithAnalyzers</c>. The Unity,
/// <c>Task</c>/<c>UniTask</c> and <c>MainThread</c> types are small stubs with the real names.
/// </summary>
public class UnityThreadingAnalyzerTests
{
    private const string Stubs = """
        namespace UnityEngine
        {
            public class Object { }
            public class Component : Object { }
            public class Transform : Component { }
            public class GameObject : Object
            {
                public GameObject() { }
                public GameObject(string name) { }
                public static GameObject Find(string name) => null;
                public T AddComponent<T>() where T : Component => null;
            }
            public static class Application
            {
                public static string dataPath => "";
            }
            public static class Debug
            {
                public static void Log(string message) { }
            }
            public static class Mathf
            {
                public static float Sin(float f) => 0f;
            }
            public struct Vector3
            {
                public float x;
            }
        }
        namespace Cysharp.Threading.Tasks
        {
            public static class UniTask
            {
                public static System.Threading.Tasks.Task SwitchToMainThread() => System.Threading.Tasks.Task.CompletedTask;
                public static System.Threading.Tasks.Task RunOnThreadPool(System.Action action) => System.Threading.Tasks.Task.CompletedTask;
            }
        }
        namespace Ducky.Sdk.Threading
        {
            public static class MainThread
            {
                public static void Post(System.Action action) { }
                public static System.Threading.Tasks.Task InvokeAsync(System.Action action) => System.Threading.Tasks.Task.CompletedTask;
            }
        }
        """;

    private const string Usings = """
        using System;
        using System.Threading.Tasks;
        using Cysharp.Threading.Tasks;
        using Ducky.Sdk.Threading;
        using UnityEngine;
        """;

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string body)
    {
        var source = Usings + "\n" + Stubs + "\nclass Subject\n{\n" + body + "\n}\n";
        return await AnalyzeSourceAsync(source);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeSourceAsync(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var directory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(directory, "System.Runtime.dll")),
        };
        var compilation = CSharpCompilation.Create(
            "UnityThreadingSubject",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new UnityThreadingAnalyzer()));
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.Where(d => d.Id is "DUCKY006" or "DUCKY007").ToImmutableArray();
    }

    private static void ShouldReport(ImmutableArray<Diagnostic> diagnostics, string id, int count) =>
        diagnostics.Count(d => d.Id == id).ShouldBe(count);

    // DUCKY006: Unity API inside a thread-pool delegate

    [TestCase("GameObject.Find(\"x\");")]
    [TestCase("var go = new GameObject(\"x\");")]
    [TestCase("var s = Application.dataPath;")]
    public async Task ThreadPoolRun_UnityApi_ReportsDucky006(string statement)
    {
        var diagnostics = await AnalyzeAsync($$"""
            public void Run() => Task.Run(() => { {{statement}} });
            """);

        ShouldReport(diagnostics, "DUCKY006", 1);
    }

    [Test]
    public async Task ThreadPoolRun_AddComponent_ReportsDucky006()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run()
            {
                var go = new GameObject();
                Task.Run(() => go.AddComponent<Transform>());
            }
            """);

        ShouldReport(diagnostics, "DUCKY006", 1);
    }

    [Test]
    public async Task UniTaskRunOnThreadPool_UnityApi_ReportsDucky006()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run() => UniTask.RunOnThreadPool(() => { GameObject.Find("x"); });
            """);

        ShouldReport(diagnostics, "DUCKY006", 1);
    }

    [Test]
    public async Task ThreadPoolRun_MathfAndStructs_DoNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run() => Task.Run(() =>
            {
                var angle = Mathf.Sin(1f);
                var v = new Vector3();
                var copy = v.x;
            });
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task ThreadPoolRun_DebugLog_IsLeftToTheDebugRules()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run() => Task.Run(() => { Debug.Log("x"); });
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task StatementsAfterSwitchToMainThread_DoNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run() => Task.Run(async () =>
            {
                await UniTask.SwitchToMainThread();
                var go = GameObject.Find("x");
            });
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task StatementsBeforeSwitchToMainThread_StillReport()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run() => Task.Run(async () =>
            {
                var go = GameObject.Find("x");
                await UniTask.SwitchToMainThread();
            });
            """);

        ShouldReport(diagnostics, "DUCKY006", 1);
    }

    [Test]
    public async Task UnityApiOutsideThreadPoolDelegates_DoesNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run()
            {
                var go = GameObject.Find("x");
                Task.Run(() => { var angle = Mathf.Sin(1f); });
            }
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task NestedMainThreadPost_DoesNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run() => Task.Run(() =>
            {
                MainThread.Post(() => GameObject.Find("x"));
            });
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task NestedMainThreadInvokeAsync_DoesNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            public void Run() => Task.Run(() =>
            {
                MainThread.InvokeAsync(() => GameObject.Find("x"));
            });
            """);

        diagnostics.ShouldBeEmpty();
    }

    // DUCKY007: Unity API in a static constructor or static initializer

    [Test]
    public async Task StaticConstructor_UnityApi_ReportsDucky007()
    {
        var diagnostics = await AnalyzeAsync("""
            static Subject() => _ = GameObject.Find("x");
            """);

        ShouldReport(diagnostics, "DUCKY007", 1);
    }

    [Test]
    public async Task StaticConstructorBlock_UnityApi_ReportsDucky007()
    {
        var diagnostics = await AnalyzeAsync("""
            static Subject()
            {
                var go = new GameObject("x");
            }
            """);

        ShouldReport(diagnostics, "DUCKY007", 1);
    }

    [Test]
    public async Task StaticFieldInitializer_UnityObject_ReportsDucky007()
    {
        var diagnostics = await AnalyzeAsync("""
            private static readonly GameObject Holder = new GameObject("x");
            """);

        ShouldReport(diagnostics, "DUCKY007", 1);
    }

    [Test]
    public async Task StaticPropertyInitializer_UnityObject_ReportsDucky007()
    {
        var diagnostics = await AnalyzeAsync("""
            private static GameObject Holder { get; } = GameObject.Find("x");
            """);

        ShouldReport(diagnostics, "DUCKY007", 1);
    }

    [Test]
    public async Task StaticPropertyGetter_DoesNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            private static GameObject Holder => GameObject.Find("x");
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task InstanceMembers_DoNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            private GameObject holder = new GameObject("x");
            public void Use() => GameObject.Find("y");
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task StaticLambdaInitializer_IsNotRunDuringStaticConstruction_DoesNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            private static readonly System.Func<GameObject> Factory = () => GameObject.Find("x");
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task StaticMathf_DoesNotReport()
    {
        var diagnostics = await AnalyzeAsync("""
            private static readonly float Angle = Mathf.Sin(1f);
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public async Task GeneratedCode_IsSkipped()
    {
        // Generated-code detection reads the marker from the top of the file.
        var source = "// <auto-generated/>\n" + Usings + "\n" + Stubs + """

            class Generated
            {
                static Generated() => _ = GameObject.Find("x");
                public void Run() => Task.Run(() => { GameObject.Find("y"); });
            }
            """;

        var diagnostics = await AnalyzeSourceAsync(source);

        diagnostics.ShouldBeEmpty();
    }

    [Test]
    public void BothRulesAreWarningsEnabledByDefault()
    {
        var analyzer = new UnityThreadingAnalyzer();

        analyzer.SupportedDiagnostics.Length.ShouldBe(2);
        foreach (var descriptor in analyzer.SupportedDiagnostics)
        {
            descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
            descriptor.IsEnabledByDefault.ShouldBeTrue();
        }
    }
}
