using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Ducky.Sdk.Analyser;

/// <summary>
/// Reports Unity API use that is unsafe off the main thread:
/// <c>DUCKY006</c> inside a delegate passed to <c>Task.Run</c> or <c>UniTask.RunOnThreadPool</c>, and
/// <c>DUCKY007</c> inside a static constructor or a static field or property initializer (which can run on any thread
/// on first use). Known limits: helper methods and method groups called from those places are not followed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnityThreadingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticIdThreadPool = "DUCKY006";
    public const string DiagnosticIdStaticInitializer = "DUCKY007";

    private const string Category = "Usage";

    private const string TaskType = "System.Threading.Tasks.Task";
    private const string UniTaskType = "Cysharp.Threading.Tasks.UniTask";
    private const string MainThreadType = "Ducky.Sdk.Threading.MainThread";
    private const string UnityObjectType = "UnityEngine.Object";

    /// <summary>Unity static classes whose members must run on the main thread.</summary>
    private static readonly ImmutableHashSet<string> MainThreadStaticClasses = ImmutableHashSet.Create(
        "UnityEngine.Application",
        "UnityEngine.Screen",
        "UnityEngine.Resources",
        "UnityEngine.Time",
        "UnityEngine.Physics",
        "UnityEngine.Physics2D",
        "UnityEngine.QualitySettings",
        "UnityEngine.Cursor",
        "UnityEngine.Graphics",
        "UnityEngine.SceneManagement.SceneManager");

    /// <summary>Unity types that are safe off the main thread, even though they live in UnityEngine.</summary>
    private static readonly ImmutableHashSet<string> MainThreadSafeTypes = ImmutableHashSet.Create(
        "UnityEngine.Debug",
        "UnityEngine.Mathf");

    private static readonly DiagnosticDescriptor RuleThreadPool = new DiagnosticDescriptor(
        DiagnosticIdThreadPool,
        title: "Unity API used off the main thread",
        messageFormat:
        "'{0}' is a Unity API used inside a thread-pool delegate; run it on the main thread instead (await UniTask.SwitchToMainThread() or MainThread.InvokeAsync)",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "Unity APIs must run on the main thread. Work started by Task.Run or UniTask.RunOnThreadPool must switch to the main thread (await UniTask.SwitchToMainThread()) or hand the call to MainThread.InvokeAsync before touching Unity.");

    private static readonly DiagnosticDescriptor RuleStaticInitializer = new DiagnosticDescriptor(
        DiagnosticIdStaticInitializer,
        title: "Unity object accessed in a static initializer",
        messageFormat:
        "'{0}' is a Unity API used in a static constructor or static initializer; a failure there poisons the type for the whole session, so create it lazily on the main thread instead",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
        "Static constructors and static field or property initializers can run on any thread on first use. Access to Unity objects there must move into a lazily created instance on the main thread.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(RuleThreadPool, RuleStaticInitializer);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeThreadPoolCall, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeStaticConstructor, SyntaxKind.ConstructorDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeStaticField, SyntaxKind.FieldDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeStaticProperty, SyntaxKind.PropertyDeclaration);
    }

    /// <summary>
    /// The shared test: true when the symbol is a member of a type that must be used on the main thread. Those are
    /// types derived from <c>UnityEngine.Object</c> (GameObject, Transform, MonoBehaviour subclasses, game types such as
    /// LevelManager) and the denylisted Unity static classes. Structs (Vector3, Color), <c>Debug</c> and <c>Mathf</c> are
    /// not flagged.
    /// </summary>
    internal static bool IsMainThreadOnly(ISymbol symbol)
    {
        var type = symbol.ContainingType;
        if (type == null || type.TypeKind == TypeKind.Struct)
        {
            return false;
        }

        var name = type.ToDisplayString();
        if (MainThreadSafeTypes.Contains(name))
        {
            return false;
        }

        if (MainThreadStaticClasses.Contains(name))
        {
            return true;
        }

        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.ToDisplayString() == UnityObjectType)
            {
                return true;
            }
        }

        return false;
    }

    private static void AnalyzeThreadPoolCall(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol method) ||
            !IsThreadPoolRun(method))
        {
            return;
        }

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            var body = argument.Expression switch
            {
                ParenthesizedLambdaExpressionSyntax lambda => (SyntaxNode)lambda.Body,
                SimpleLambdaExpressionSyntax lambda => lambda.Body,
                AnonymousMethodExpressionSyntax anonymous => anonymous.Block,
                _ => null,
            };
            if (body != null)
            {
                ReportInThreadPoolBody(context, body);
            }
        }
    }

    private static bool IsThreadPoolRun(IMethodSymbol method)
    {
        var containing = method.ContainingType?.ToDisplayString();
        return (containing == TaskType && method.Name == "Run") ||
               (containing == UniTaskType && method.Name == "RunOnThreadPool");
    }

    private static void ReportInThreadPoolBody(SyntaxNodeAnalysisContext context, SyntaxNode body)
    {
        var cancellationToken = context.CancellationToken;
        int? switchedEnd = null;
        var excludedSpans = new List<TextSpan>();

        foreach (var node in body.DescendantNodesAndSelf())
        {
            if (node is AwaitExpressionSyntax awaitExpression && IsSwitchToMainThread(context, awaitExpression.Expression) &&
                (switchedEnd == null || awaitExpression.Span.End < switchedEnd.Value))
            {
                switchedEnd = awaitExpression.Span.End;
            }

            if (node is InvocationExpressionSyntax call)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(call, cancellationToken).Symbol as IMethodSymbol;
                // Arguments of MainThread.Post/InvokeAsync already run on the main thread. Arguments of a nested
                // thread-pool call are analyzed by that call itself.
                if (symbol != null && (IsMainThreadHandOff(symbol) || IsThreadPoolRun(symbol)))
                {
                    excludedSpans.AddRange(call.ArgumentList.Arguments.Select(a => a.Span));
                }
            }
        }

        foreach (var node in body.DescendantNodesAndSelf())
        {
            if (switchedEnd.HasValue && node.SpanStart >= switchedEnd.Value)
            {
                continue; // after await UniTask.SwitchToMainThread()
            }

            if (excludedSpans.Any(span => span.Contains(node.Span)))
            {
                continue;
            }

            var symbol = SymbolOfUsage(context, node);
            if (symbol != null && IsMainThreadOnly(symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleThreadPool, node.GetLocation(), DisplayName(symbol)));
            }
        }
    }

    private static bool IsSwitchToMainThread(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
    {
        return expression is InvocationExpressionSyntax call &&
               context.SemanticModel.GetSymbolInfo(call, context.CancellationToken).Symbol is IMethodSymbol method &&
               method.ContainingType?.ToDisplayString() == UniTaskType &&
               method.Name == "SwitchToMainThread";
    }

    private static bool IsMainThreadHandOff(IMethodSymbol method)
    {
        return method.ContainingType?.ToDisplayString() == MainThreadType &&
               (method.Name == "Post" || method.Name == "InvokeAsync");
    }

    /// <summary>The member a node uses or creates, when it runs code (a member access or an object creation).</summary>
    private static ISymbol? SymbolOfUsage(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        if (node is not MemberAccessExpressionSyntax && node is not ObjectCreationExpressionSyntax &&
            node is not ImplicitObjectCreationExpressionSyntax)
        {
            return null;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol;
        return symbol is IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol ? symbol : null;
    }

    private static string DisplayName(ISymbol symbol)
    {
        var typeName = symbol.ContainingType?.Name ?? string.Empty;
        return symbol is IMethodSymbol { MethodKind: MethodKind.Constructor }
            ? "new " + typeName
            : typeName + "." + symbol.Name;
    }

    private static void AnalyzeStaticConstructor(SyntaxNodeAnalysisContext context)
    {
        var constructor = (ConstructorDeclarationSyntax)context.Node;
        if (!constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return;
        }

        SyntaxNode? root = constructor.Body;
        root ??= constructor.ExpressionBody?.Expression;
        if (root != null)
        {
            ReportInStaticInitializer(context, root);
        }
    }

    private static void AnalyzeStaticField(SyntaxNodeAnalysisContext context)
    {
        var field = (FieldDeclarationSyntax)context.Node;
        if (!field.Modifiers.Any(SyntaxKind.StaticKeyword) || field.Modifiers.Any(SyntaxKind.ConstKeyword))
        {
            return;
        }

        foreach (var variable in field.Declaration.Variables)
        {
            if (variable.Initializer != null)
            {
                ReportInStaticInitializer(context, variable.Initializer.Value);
            }
        }
    }

    private static void AnalyzeStaticProperty(SyntaxNodeAnalysisContext context)
    {
        var property = (PropertyDeclarationSyntax)context.Node;
        if (!property.Modifiers.Any(SyntaxKind.StaticKeyword) || property.Initializer == null)
        {
            return;
        }

        // Only the initializer runs during static construction; a getter runs on each access.
        ReportInStaticInitializer(context, property.Initializer.Value);
    }

    private static void ReportInStaticInitializer(SyntaxNodeAnalysisContext context, SyntaxNode root)
    {
        // Lambdas and local functions do not run during static construction, so they are not entered.
        var nodes = root.DescendantNodesAndSelf(node =>
            node is not LambdaExpressionSyntax &&
            node is not AnonymousMethodExpressionSyntax &&
            node is not LocalFunctionStatementSyntax);

        foreach (var node in nodes)
        {
            var symbol = SymbolOfUsage(context, node);
            if (symbol != null && IsMainThreadOnly(symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(RuleStaticInitializer, node.GetLocation(), DisplayName(symbol)));
            }
        }
    }
}
