



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

}