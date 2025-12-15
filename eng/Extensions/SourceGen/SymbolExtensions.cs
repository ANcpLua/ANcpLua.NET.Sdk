// Copyright (c) ANcpLua. All rights reserved.
// Licensed under the MIT License.

#if ANCPLUA_SOURCEGEN_HELPERS
using Microsoft.CodeAnalysis;

namespace ANcpLua.SourceGen;

/// <summary>
/// Extension methods for Roslyn symbol types.
/// </summary>
internal static class SymbolExtensions
{
    /// <summary>
    /// Gets the fully qualified name of a type symbol (global::Namespace.Type format).
    /// </summary>
    public static string GetFullyQualifiedName(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Gets the metadata name of a type symbol (Namespace.Type format).
    /// </summary>
    public static string GetMetadataName(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }

    /// <summary>
    /// Checks if a type symbol has a specific attribute.
    /// </summary>
    public static bool HasAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the first attribute matching the specified name, or null.
    /// </summary>
    public static AttributeData? GetAttribute(this ISymbol symbol, string fullyQualifiedAttributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == fullyQualifiedAttributeName)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a type is or inherits from a specific base type.
    /// </summary>
    public static bool IsOrInheritsFrom(this ITypeSymbol? symbol, string fullyQualifiedTypeName)
    {
        while (symbol is not null)
        {
            if (symbol.ToDisplayString() == fullyQualifiedTypeName)
            {
                return true;
            }

            symbol = symbol.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Checks if a type implements a specific interface.
    /// </summary>
    public static bool ImplementsInterface(this ITypeSymbol symbol, string fullyQualifiedInterfaceName)
    {
        foreach (var iface in symbol.AllInterfaces)
        {
            if (iface.ToDisplayString() == fullyQualifiedInterfaceName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the containing namespace as a string, or empty if global.
    /// </summary>
    public static string GetNamespaceName(this ISymbol symbol)
    {
        var ns = symbol.ContainingNamespace;
        return ns.IsGlobalNamespace ? string.Empty : ns.ToDisplayString();
    }
}

#endif