// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

#if ANCPLUA_SOURCEGEN_HELPERS
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.SourceGen;

/// <summary>
/// Extension methods for <see cref="SyntaxValueProvider"/> to simplify common patterns.
/// </summary>
internal static class SyntaxValueProviderExtensions
{
    /// <summary>
    /// Creates a provider for class declarations with a specific attribute.
    /// </summary>
    public static IncrementalValuesProvider<ClassDeclarationSyntax> ForClassesWithAttribute(
        this SyntaxValueProvider provider,
        string attributeName)
    {
        return provider.CreateSyntaxProvider(
            predicate: (node, _) => IsClassWithAttribute(node, attributeName),
            transform: (ctx, _) => (ClassDeclarationSyntax)ctx.Node);
    }

    /// <summary>
    /// Creates a provider for method declarations with a specific attribute.
    /// </summary>
    public static IncrementalValuesProvider<MethodDeclarationSyntax> ForMethodsWithAttribute(
        this SyntaxValueProvider provider,
        string attributeName)
    {
        return provider.CreateSyntaxProvider(
            predicate: (node, _) => IsMethodWithAttribute(node, attributeName),
            transform: (ctx, _) => (MethodDeclarationSyntax)ctx.Node);
    }

    /// <summary>
    /// Creates a provider that filters by attribute and transforms with semantic model.
    /// </summary>
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
        this SyntaxValueProvider provider,
        string fullyQualifiedMetadataName,
        System.Func<SyntaxNode, CancellationToken, bool> predicate,
        System.Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform)
    {
        return provider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName,
            predicate,
            transform);
    }

    private static bool IsClassWithAttribute(SyntaxNode node, string attributeName)
    {
        return node is ClassDeclarationSyntax classDecl &&
               HasAttributeWithName(classDecl.AttributeLists, attributeName);
    }

    private static bool IsMethodWithAttribute(SyntaxNode node, string attributeName)
    {
        return node is MethodDeclarationSyntax methodDecl &&
               HasAttributeWithName(methodDecl.AttributeLists, attributeName);
    }

    private static bool HasAttributeWithName(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
    {
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name == attributeName ||
                    name == attributeName + "Attribute" ||
                    name.EndsWith("." + attributeName) ||
                    name.EndsWith("." + attributeName + "Attribute"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

#endif