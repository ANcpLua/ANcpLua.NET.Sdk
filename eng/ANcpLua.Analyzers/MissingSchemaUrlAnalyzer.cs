// Licensed under MIT.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers;

/// <summary>
/// QYL0003: Detects OpenTelemetry configurations that don't set the schema URL.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingSchemaUrlAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "QYL0003";

    private const string Category = "OpenTelemetry";

    private static readonly LocalizableString Title =
        "Missing telemetry schema URL";

    private static readonly LocalizableString MessageFormat =
        "OpenTelemetry resource configuration should include 'telemetry.schema_url' attribute";

    private static readonly LocalizableString Description =
        "Setting the schema URL allows collectors and backends to understand which version of " +
        "semantic conventions your telemetry uses, enabling automatic normalization.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://opentelemetry.io/docs/specs/otel/schemas/");

    // Known builder types that should have schema URL set
    private static readonly HashSet<string> ResourceConfigMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "ConfigureResource",
        "SetResourceBuilder",
        "AddResource",
        "WithResource",
        "ConfigureOpenTelemetry",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Get method name
        var methodName = GetMethodName(invocation);
        if (methodName is null || !ResourceConfigMethods.Contains(methodName))
            return;

        // Check if this looks like an OTel builder call
        // We look for builder patterns like builder.ConfigureResource(...)
        if (!IsLikelyOtelBuilderCall(invocation))
            return;

        // Check if schema URL is set in the lambda/delegate argument
        var hasSchemaUrl = CheckForSchemaUrl(invocation);

        if (hasSchemaUrl) return;
        // Report on the method name
        var location = GetMethodLocation(invocation);
        var diagnostic = Diagnostic.Create(Rule, location);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsLikelyOtelBuilderCall(InvocationExpressionSyntax invocation)
    {
        // Check if it's a member access (builder.ConfigureResource)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        // Check if the receiver looks like a builder (contains "Builder" or common patterns)
        var receiverText = memberAccess.Expression.ToString().ToLowerInvariant();
        return receiverText.Contains("builder") ||
               receiverText.Contains("tracer") ||
               receiverText.Contains("meter") ||
               receiverText.Contains("logger") ||
               receiverText.Contains("otel") ||
               receiverText.Contains("opentelemetry");
    }

    private static bool CheckForSchemaUrl(InvocationExpressionSyntax invocation)
    {
        // Search for "telemetry.schema_url" or "schema" in arguments
        var descendants = invocation.DescendantNodes();

        foreach (var node in descendants)
        {
            if (node is LiteralExpressionSyntax literal)
            {
                var value = literal.Token.ValueText;
                if (value.Contains("schema", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("telemetry.schema_url", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("opentelemetry.io/schemas", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Check for AddAttribute("telemetry.schema_url", ...) or SetSchemaUrl(...)
            if (node is not InvocationExpressionSyntax nestedInvocation) continue;
            var nestedMethod = GetMethodName(nestedInvocation);
            if (nestedMethod?.Contains("Schema", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetMethodLocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            // Return the location of just the method name
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.GetLocation(),
            IdentifierNameSyntax identifier => identifier.GetLocation(),
            _ => invocation.GetLocation()
        };
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
}
