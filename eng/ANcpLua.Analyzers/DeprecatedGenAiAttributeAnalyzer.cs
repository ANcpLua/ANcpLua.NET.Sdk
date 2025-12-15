// Licensed under MIT.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers;

/// <summary>
///     Analyzer that detects usage of deprecated OTel GenAI attribute names.
///     Enforces OTel 1.38 semantic conventions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DeprecatedGenAiAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "QYL0002";

    private const string Category = "Naming";

    private static readonly LocalizableString Title =
        "Deprecated GenAI attribute";

    private static readonly LocalizableString MessageFormat =
        "'{0}' is deprecated. Use '{1}' instead (OTel 1.38).";

    private static readonly LocalizableString Description =
        "OpenTelemetry 1.38 renamed several GenAI attributes. Use the new names.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        true,
        Description,
        "https://opentelemetry.io/docs/specs/semconv/gen-ai/");

    // Deprecated → Current mapping (OTel 1.38)
    private static readonly ImmutableDictionary<string, string> DeprecatedAttributes =
        ImmutableDictionary.CreateRange([
            new KeyValuePair<string, string>("gen_ai.system", "gen_ai.provider.name"),
            new KeyValuePair<string, string>("gen_ai.usage.prompt_tokens", "gen_ai.usage.input_tokens"),
            new KeyValuePair<string, string>("gen_ai.usage.completion_tokens", "gen_ai.usage.output_tokens"),
            new KeyValuePair<string, string>("gen_ai.request.max_tokens", "gen_ai.request.max_output_tokens")
        ]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literal = (LiteralExpressionSyntax)context.Node;

        if (literal.Token.Value is not string value)
            return;

        if (DeprecatedAttributes.TryGetValue(value, out var replacement))
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                literal.GetLocation(),
                value,
                replacement);

            context.ReportDiagnostic(diagnostic);
        }
    }
}