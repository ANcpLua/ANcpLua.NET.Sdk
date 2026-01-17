



using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
/// Extension methods for <see cref="SyntaxValueProvider"/> to simplify common patterns.
/// </summary>
internal static class SyntaxValueProviderExtensions
{
    /// <summary>
    ///     Creates a provider for class declarations with a specific attribute.
    /// </summary>
    /// <param name="provider">The <see cref="SyntaxValueProvider"/> to extend.</param>
    /// <param name="attributeName">
    ///     The name of the attribute to filter by. Can be specified with or without
    ///     the "Attribute" suffix (e.g., "Serializable" or "SerializableAttribute").
    /// </param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{T}"/> that produces <see cref="ClassDeclarationSyntax"/>
    ///     nodes for classes decorated with the specified attribute.
    /// </returns>
    public static IncrementalValuesProvider<ClassDeclarationSyntax> ForClassesWithAttribute(
        this SyntaxValueProvider provider,
        string attributeName)
    {
        return provider.CreateSyntaxProvider(
            predicate: (node, _) => IsClassWithAttribute(node, attributeName),
            transform: (ctx, _) => (ClassDeclarationSyntax)ctx.Node);
    }

    /// <summary>
    ///     Creates a provider for method declarations with a specific attribute.
    /// </summary>
    /// <param name="provider">The <see cref="SyntaxValueProvider"/> to extend.</param>
    /// <param name="attributeName">
    ///     The name of the attribute to filter by. Can be specified with or without
    ///     the "Attribute" suffix (e.g., "Test" or "TestAttribute").
    /// </param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{T}"/> that produces <see cref="MethodDeclarationSyntax"/>
    ///     nodes for methods decorated with the specified attribute.
    /// </returns>
    public static IncrementalValuesProvider<MethodDeclarationSyntax> ForMethodsWithAttribute(
        this SyntaxValueProvider provider,
        string attributeName)
    {
        return provider.CreateSyntaxProvider(
            predicate: (node, _) => IsMethodWithAttribute(node, attributeName),
            transform: (ctx, _) => (MethodDeclarationSyntax)ctx.Node);
    }

    /// <summary>
    ///     Creates a provider that filters by attribute and transforms with semantic model access.
    /// </summary>
    /// <typeparam name="T">The type of value produced by the transform function.</typeparam>
    /// <param name="provider">The <see cref="SyntaxValueProvider"/> to extend.</param>
    /// <param name="fullyQualifiedMetadataName">
    ///     The fully qualified metadata name of the attribute (e.g., "System.SerializableAttribute").
    /// </param>
    /// <param name="predicate">
    ///     A function to filter syntax nodes before semantic analysis. This is called first
    ///     for performance, as it does not require the semantic model.
    /// </param>
    /// <param name="transform">
    ///     A function that transforms matching nodes using the <see cref="GeneratorAttributeSyntaxContext"/>,
    ///     which provides access to both the syntax node and its semantic model.
    /// </param>
    /// <returns>
    ///     An <see cref="IncrementalValuesProvider{T}"/> that produces values of type <typeparamref name="T"/>
    ///     for nodes matching both the predicate and attribute.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is a pass-through to the built-in Roslyn method, provided for API consistency
    ///         and to enable future SDK-specific enhancements.
    ///     </para>
    /// </remarks>
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