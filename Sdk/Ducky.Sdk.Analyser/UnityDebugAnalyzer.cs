using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ducky.Sdk.Analyser;

/// <summary>
/// Analyzer that detects Unity Debug logging calls and suggests replacing them
/// with Ducky.Sdk.Logging.Log methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnityDebugAnalyzer : DiagnosticAnalyzer
{
    // Diagnostic IDs
    public const string DiagnosticIdLog = "DUCKY001";
    public const string DiagnosticIdLogWarning = "DUCKY002";
    public const string DiagnosticIdLogError = "DUCKY003";
    public const string DiagnosticIdLogException = "DUCKY004";
    public const string DiagnosticIdLogFormat = "DUCKY005";

    private const string Category = "Usage";

    // Diagnostic descriptors
    private static readonly DiagnosticDescriptor RuleLog = new DiagnosticDescriptor(
        DiagnosticIdLog,
        title: "Use Ducky.Sdk.Logging.Log instead of UnityEngine.Debug.Log",
        messageFormat: "Replace 'Debug.Log' with 'Log.Info' from Ducky.Sdk.Logging",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Unity Debug.Log calls should be replaced with Ducky.Sdk.Logging.Log.Info for better logging infrastructure.");

    private static readonly DiagnosticDescriptor RuleLogWarning = new DiagnosticDescriptor(
        DiagnosticIdLogWarning,
        title: "Use Ducky.Sdk.Logging.Log instead of UnityEngine.Debug.LogWarning",
        messageFormat: "Replace 'Debug.LogWarning' with 'Log.Warn' from Ducky.Sdk.Logging",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Unity Debug.LogWarning calls should be replaced with Ducky.Sdk.Logging.Log.Warn for better logging infrastructure.");

    private static readonly DiagnosticDescriptor RuleLogError = new DiagnosticDescriptor(
        DiagnosticIdLogError,
        title: "Use Ducky.Sdk.Logging.Log instead of UnityEngine.Debug.LogError",
        messageFormat: "Replace 'Debug.LogError' with 'Log.Error' from Ducky.Sdk.Logging",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Unity Debug.LogError calls should be replaced with Ducky.Sdk.Logging.Log.Error for better logging infrastructure.");

    private static readonly DiagnosticDescriptor RuleLogException = new DiagnosticDescriptor(
        DiagnosticIdLogException,
        title: "Use Ducky.Sdk.Logging.Log instead of UnityEngine.Debug.LogException",
        messageFormat: "Replace 'Debug.LogException' with 'Log.ErrorException' from Ducky.Sdk.Logging",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Unity Debug.LogException calls should be replaced with Ducky.Sdk.Logging.Log.ErrorException for better logging infrastructure.");

    private static readonly DiagnosticDescriptor RuleLogFormat = new DiagnosticDescriptor(
        DiagnosticIdLogFormat,
        title: "Use Ducky.Sdk.Logging.Log instead of UnityEngine.Debug.LogFormat",
        messageFormat: "Replace 'Debug.LogFormat' with 'Log.InfoFormat' from Ducky.Sdk.Logging",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Unity Debug.LogFormat calls should be replaced with Ducky.Sdk.Logging.Log.InfoFormat for better logging infrastructure.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(RuleLog, RuleLogWarning, RuleLogError, RuleLogException, RuleLogFormat);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register action to analyze invocation expressions
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a member access expression (e.g., Debug.Log)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        // Get the symbol information
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        // Check if the method belongs to UnityEngine.Debug
        if (methodSymbol.ContainingType?.ToString() != "UnityEngine.Debug")
        {
            return;
        }

        // Determine which Debug method is being called and report appropriate diagnostic
        var methodName = methodSymbol.Name;
        DiagnosticDescriptor? rule = methodName switch
        {
            "Log" => RuleLog,
            "LogWarning" => RuleLogWarning,
            "LogError" => RuleLogError,
            "LogException" => RuleLogException,
            "LogFormat" => RuleLogFormat,
            _ => null
        };

        if (rule != null)
        {
            var diagnostic = Diagnostic.Create(rule, invocation.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
