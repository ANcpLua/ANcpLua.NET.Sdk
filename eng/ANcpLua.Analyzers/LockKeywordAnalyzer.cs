// Licensed under MIT.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers;

/// <summary>
///     Analyzer that detects usage of the lock keyword on non-Lock types.
///     In .NET 9+, lock(Lock) is valid and preferred - only warn on lock(object).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LockKeywordAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "QYL0001";

    private const string Category = "Threading";
    private const string LockTypeName = "System.Threading.Lock";

    private static readonly LocalizableString Title =
        "Avoid lock keyword on non-Lock types";

    private static readonly LocalizableString MessageFormat =
        "Use 'Lock _lock = new()' with 'lock(_lock)' instead of 'lock(object)'. The Lock class provides better performance in .NET 9+.";

    private static readonly LocalizableString Description =
        "The lock keyword on object/non-Lock types should be replaced with the Lock class from System.Threading. " +
        "Use 'private readonly Lock _lock = new();' and 'lock(_lock) { ... }'. " +
        "Note: lock(Lock) is the correct modern pattern and will not trigger this warning.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        true,
        Description,
        "https://learn.microsoft.com/en-us/dotnet/api/system.threading.lock");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
    }

    private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
    {
        var lockStatement = (LockStatementSyntax)context.Node;

        // Get the type of the lock expression
        var lockExpressionType =
            context.SemanticModel.GetTypeInfo(lockStatement.Expression, context.CancellationToken).Type;

        // If locking on System.Threading.Lock, this is the correct modern pattern - don't warn
        if (lockExpressionType is not null &&
            string.Equals(lockExpressionType.ToDisplayString(), LockTypeName, StringComparison.Ordinal))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            lockStatement.LockKeyword.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}