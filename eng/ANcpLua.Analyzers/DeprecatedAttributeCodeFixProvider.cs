// Licensed under MIT.

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

namespace ANcpLua.Analyzers;

/// <summary>
/// Code fix provider for QYL0002 - replaces deprecated attributes with modern equivalents.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DeprecatedAttributeCodeFixProvider))]
[Shared]
public sealed class DeprecatedAttributeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DeprecatedAttributeAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the string literal that triggered the diagnostic
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        // The node could be the literal itself or a parent node
        var literal = node as LiteralExpressionSyntax
            ?? node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();

        if (literal is null)
            return;

        var deprecatedName = literal.Token.ValueText;

        if (!DeprecatedAttributes.Renames.TryGetValue(deprecatedName, out var replacement))
            return;

        // Register the code fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Use '{replacement.Replacement}' instead",
                createChangedDocument: c => ReplaceAttributeAsync(
                    context.Document, literal, replacement.Replacement, c),
                equivalenceKey: $"UseModernAttribute_{replacement.Replacement}"),
            diagnostic);
    }

    private static async Task<Document> ReplaceAttributeAsync(
        Document document,
        LiteralExpressionSyntax oldLiteral,
        string newAttributeName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
            return document;

        // Create the new string literal with the replacement attribute name
        var newLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(newAttributeName))
            .WithTriviaFrom(oldLiteral);

        // Replace the old node with the new one
        var newRoot = root.ReplaceNode(oldLiteral, newLiteral);

        return document.WithSyntaxRoot(newRoot);
    }
}
