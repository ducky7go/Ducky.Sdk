using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Ducky.Sdk.Analyser;

/// <summary>
/// Code fix provider that replaces Unity Debug calls with Ducky.Sdk.Logging.Log calls.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnityDebugCodeFixProvider)), Shared]
public class UnityDebugCodeFixProvider : CodeFixProvider
{
    private const string LogNamespace = "Ducky.Sdk.Logging";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        UnityDebugAnalyzer.DiagnosticIdLog,
        UnityDebugAnalyzer.DiagnosticIdLogWarning,
        UnityDebugAnalyzer.DiagnosticIdLogError,
        UnityDebugAnalyzer.DiagnosticIdLogException,
        UnityDebugAnalyzer.DiagnosticIdLogFormat
    ];

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the invocation expression identified by the diagnostic
        var invocation = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .First();

        if (invocation == null)
        {
            return;
        }

        // Determine the replacement method name based on diagnostic ID
        var replacementMethod = diagnostic.Id switch
        {
            UnityDebugAnalyzer.DiagnosticIdLog => "Info",
            UnityDebugAnalyzer.DiagnosticIdLogWarning => "Warn",
            UnityDebugAnalyzer.DiagnosticIdLogError => "Error",
            UnityDebugAnalyzer.DiagnosticIdLogException => "ErrorException",
            UnityDebugAnalyzer.DiagnosticIdLogFormat => "InfoFormat",
            _ => null
        };

        if (replacementMethod == null)
        {
            return;
        }

        // Register a code action that will invoke the fix
        var title = $"Replace with Log.{replacementMethod}";
        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => ReplaceDebugCallAsync(
                    context.Document,
                    invocation,
                    replacementMethod,
                    diagnostic.Id,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> ReplaceDebugCallAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        string replacementMethod,
        string diagnosticId,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        // Create the new invocation expression
        InvocationExpressionSyntax newInvocation;

        if (diagnosticId == UnityDebugAnalyzer.DiagnosticIdLogException)
        {
            // Special handling for LogException: Log.Error(exception, message, args)
            // This promotes the structured logging pattern with exception as first parameter
            newInvocation = CreateLogExceptionReplacement(invocation);
        }
        else
        {
            // Standard replacement: Log.MethodName(args)
            // Preserves template parameters for structured logging
            newInvocation = CreateStandardReplacement(invocation, replacementMethod);
        }

        // Add formatting annotation
        newInvocation = newInvocation.WithAdditionalAnnotations(Formatter.Annotation);

        // Replace the old invocation with the new one
        var newRoot = root.ReplaceNode(invocation, newInvocation);

        // Add using directive if not already present
        newRoot = AddUsingDirectiveIfNeeded(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }

    private static InvocationExpressionSyntax CreateStandardReplacement(
        InvocationExpressionSyntax invocation,
        string replacementMethod)
    {
        // Create: Log.MethodName
        var logAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Log"),
            SyntaxFactory.IdentifierName(replacementMethod));

        // Keep the same arguments to preserve structured logging template parameters
        // e.g., Debug.Log("User {0} logged in", userId) -> Log.Info("User {0} logged in", userId)
        return SyntaxFactory.InvocationExpression(
            logAccess,
            invocation.ArgumentList);
    }

    private static InvocationExpressionSyntax CreateLogExceptionReplacement(
        InvocationExpressionSyntax invocation)
    {
        // Use the structured logging pattern: Log.Error(exception, message, args)
        // Debug.LogException(exception) -> Log.Error(exception, "Exception occurred")
        // Debug.LogException(exception, context) -> Log.Error(exception, "Exception from {Context}", context)

        var arguments = invocation.ArgumentList.Arguments;

        // Create: Log.Error (using Error instead of ErrorException for cleaner API)
        var logAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Log"),
            SyntaxFactory.IdentifierName("Error"));

        ArgumentListSyntax newArgumentList;

        if (arguments.Count >= 2)
        {
            // Debug.LogException(exception, context)
            // -> Log.Error(exception, "Exception from {Context}", context)
            var exceptionArg = arguments[0];
            var contextArg = arguments[1];

            var messageArg = SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Exception from {Context}")));

            newArgumentList = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[] { exceptionArg, messageArg, contextArg }));
        }
        else if (arguments.Count >= 1)
        {
            // Debug.LogException(exception)
            // -> Log.Error(exception, "Exception occurred")
            var exceptionArg = arguments[0];

            var messageArg = SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Exception occurred")));

            newArgumentList = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[] { exceptionArg, messageArg }));
        }
        else
        {
            // Fallback: keep original arguments
            newArgumentList = invocation.ArgumentList;
        }

        return SyntaxFactory.InvocationExpression(logAccess, newArgumentList);
    }

    private static SyntaxNode AddUsingDirectiveIfNeeded(SyntaxNode root)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return root;
        }

        // Check if using directive already exists
        var hasUsingDirective = compilationUnit.Usings.Any(u =>
            u.Name?.ToString() == LogNamespace);

        if (hasUsingDirective)
        {
            return root;
        }

        // Create new using directive
        var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName(LogNamespace))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // Add using directive to the compilation unit
        var newCompilationUnit = compilationUnit.AddUsings(usingDirective);

        return newCompilationUnit;
    }
}
