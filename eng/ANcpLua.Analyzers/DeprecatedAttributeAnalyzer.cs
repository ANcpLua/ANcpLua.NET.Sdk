// Licensed under MIT.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers;

/// <summary>
/// QYL0002: Detects usage of deprecated OpenTelemetry semantic convention attributes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DeprecatedAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "QYL0002";

    private const string Category = "OpenTelemetry";

    private static readonly LocalizableString Title =
        "Deprecated semantic convention attribute";

    private static readonly LocalizableString MessageFormat =
        "'{0}' is deprecated since schema v{1}. Use '{2}' instead.";

    private static readonly LocalizableString Description =
        "This attribute name has been deprecated in the OpenTelemetry semantic conventions. " +
        "Using deprecated attributes may cause issues with telemetry backends that expect modern attribute names.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://opentelemetry.io/docs/specs/semconv/");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Analyze string literals
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literal = (LiteralExpressionSyntax)context.Node;
        var value = literal.Token.ValueText;

        if (string.IsNullOrEmpty(value))
            return;

        // Check if this string is a deprecated attribute name
        if (!DeprecatedAttributes.Renames.TryGetValue(value, out var replacement))
            return;

        // Check if it's used in a telemetry context
        if (!IsInTelemetryContext(literal))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            literal.GetLocation(),
            value,
            replacement.Version,
            replacement.Replacement);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsInTelemetryContext(SyntaxNode node)
    {
        // Walk up the syntax tree to find context
        var current = node.Parent;

        while (current is not null)
        {
            switch (current)
            {
                // Check for indexer access like attributes["key"] or tags["key"]
                case ElementAccessExpressionSyntax elementAccess:
                    var identifier = GetIdentifierName(elementAccess.Expression);
                    if (identifier is not null && IsLikelyTelemetryContainer(identifier))
                    {
                        return true;
                    }
                    break;

                // Check for method invocations like SetAttribute, AddTag
                case InvocationExpressionSyntax invocation:
                    var methodName = GetMethodName(invocation);
                    if (methodName is not null && DeprecatedAttributes.AttributeKeyPatterns.Any(p =>
                        methodName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                    break;

                // Check for object initializers
                case InitializerExpressionSyntax initializer:
                    if (initializer.Parent is ObjectCreationExpressionSyntax creation)
                    {
                        var typeName = creation.Type.ToString();
                        if (typeName.Contains("Tag") || typeName.Contains("Attribute") ||
                            typeName.Contains("KeyValuePair"))
                        {
                            return true;
                        }
                    }
                    break;

                // Check for dictionary initializers
                case AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax }:
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsLikelyTelemetryContainer(string identifier)
    {
        // Common variable names for telemetry attribute containers
        var lowerIdentifier = identifier.ToLowerInvariant();
        return lowerIdentifier.Contains("attribute") ||
               lowerIdentifier.Contains("tag") ||
               lowerIdentifier.Contains("attr") ||
               lowerIdentifier == "attrs";
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    private static string? GetIdentifierName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }
}
