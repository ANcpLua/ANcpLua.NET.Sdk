// Licensed under MIT.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers;

/// <summary>
///     Analyzer that detects usage of the lock keyword and suggests using Lock.EnterScope() instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LockKeywordAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "QYL0001";

    private const string Category = "Threading";

    private static readonly LocalizableString Title =
        "Avoid lock keyword";

    private static readonly LocalizableString MessageFormat =
        "Use 'using (_lock.EnterScope())' instead of 'lock(obj)'. The Lock class provides better performance in .NET 9+.";

    private static readonly LocalizableString Description =
        "The lock keyword should be replaced with the Lock class from System.Threading. " +
        "Use 'private readonly Lock _lock = new();' and 'using (_lock.EnterScope()) { ... }'.";

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

        var diagnostic = Diagnostic.Create(
            Rule,
            lockStatement.LockKeyword.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }
}