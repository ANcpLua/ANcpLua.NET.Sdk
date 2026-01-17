using System.Diagnostics.CodeAnalysis;
using ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Analyzers;

/// <summary>
///     Analyzes syntax to find ADO.NET DbCommand method invocations to intercept.
/// </summary>
internal static class DbCallSiteAnalyzer
{
    /// <summary>
    ///     The DbCommand base type metadata name.
    /// </summary>
    private const string DbCommandTypeName = "System.Data.Common.DbCommand";

    /// <summary>
    ///     Method patterns to intercept on DbCommand and derived types.
    /// </summary>
    private static readonly Dictionary<string, (DbCommandMethod Method, bool IsAsync)> _methodPatterns = new(StringComparer.Ordinal)
    {
        ["ExecuteReader"] = (DbCommandMethod.ExecuteReader, false),
        ["ExecuteReaderAsync"] = (DbCommandMethod.ExecuteReader, true),
        ["ExecuteNonQuery"] = (DbCommandMethod.ExecuteNonQuery, false),
        ["ExecuteNonQueryAsync"] = (DbCommandMethod.ExecuteNonQuery, true),
        ["ExecuteScalar"] = (DbCommandMethod.ExecuteScalar, false),
        ["ExecuteScalarAsync"] = (DbCommandMethod.ExecuteScalar, true)
    };

    /// <summary>
    ///     Checks if the syntax node could potentially be a DbCommand method call.
    /// </summary>
    public static bool IsPotentialDbCall(SyntaxNode node, CancellationToken _) =>
        node.IsKind(SyntaxKind.InvocationExpression);

    /// <summary>
    ///     Transforms a potential DbCommand call to invocation info if it matches known patterns.
    /// </summary>
    public static DbInvocationInfo? TransformToDbInvocation(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (IsGeneratedFile(context.Node.SyntaxTree.FilePath))
            return null;

        if (!TryGetInvocationOperation(context, cancellationToken, out var invocation))
            return null;

        if (!TryMatchDbCommandMethod(invocation, context.SemanticModel.Compilation, out var method, out var isAsync, out var concreteType))
            return null;

        var interceptLocation = context.SemanticModel.GetInterceptableLocation(
            (InvocationExpressionSyntax)context.Node,
            cancellationToken);

        if (interceptLocation is null)
            return null;

        return new DbInvocationInfo(
            OrderKey: FormatOrderKey(context.Node),
            Method: method,
            IsAsync: isAsync,
            ConcreteCommandType: concreteType,
            InterceptableLocation: interceptLocation);
    }

    private static bool IsGeneratedFile(string filePath) =>
        filePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
        filePath.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetInvocationOperation(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out IInvocationOperation? invocation)
    {
        if (context.SemanticModel.GetOperation(context.Node, cancellationToken)
            is IInvocationOperation op)
        {
            invocation = op;
            return true;
        }

        invocation = null;
        return false;
    }

    private static bool TryMatchDbCommandMethod(
        IInvocationOperation invocation,
        Compilation compilation,
        out DbCommandMethod method,
        out bool isAsync,
        out string? concreteType)
    {
        method = default;
        isAsync = false;
        concreteType = null;

        var methodName = invocation.TargetMethod.Name;

        if (!_methodPatterns.TryGetValue(methodName, out var pattern))
            return false;

        var containingType = invocation.TargetMethod.ContainingType;
        if (containingType is null)
            return false;

        var dbCommandType = compilation.GetTypeByMetadataName(DbCommandTypeName);
        if (dbCommandType is null)
            return false;

        if (!IsOrDerivesFrom(containingType, dbCommandType))
            return false;

        method = pattern.Method;
        isAsync = pattern.IsAsync;

        if (!SymbolEqualityComparer.Default.Equals(containingType, dbCommandType))
            concreteType = containingType.ToDisplayString();

        return true;
    }

    private static bool IsOrDerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        if (SymbolEqualityComparer.Default.Equals(type, baseType))
            return true;

        var current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static string FormatOrderKey(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        var start = span.StartLinePosition;
        return $"{node.SyntaxTree.FilePath}:{start.Line}:{start.Character}";
    }
}
