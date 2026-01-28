using System.Diagnostics.CodeAnalysis;
using ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Analyzers;

/// <summary>
///     Analyzes syntax to find invocations of methods decorated with [Traced] attribute.
/// </summary>
internal static class TracedCallSiteAnalyzer
{
    private const string TracedAttributeFullName = "ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation.TracedAttribute";
    private const string TracedTagAttributeFullName = "ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation.TracedTagAttribute";

    /// <summary>
    ///     Checks if the syntax node could potentially be a traced method invocation.
    /// </summary>
    public static bool IsPotentialTracedCall(SyntaxNode node, CancellationToken _) =>
        node.IsKind(SyntaxKind.InvocationExpression);

    /// <summary>
    ///     Transforms a potential traced call to invocation info if it targets a [Traced] method.
    /// </summary>
    public static TracedInvocationInfo? TransformToTracedInvocation(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (AnalyzerHelpers.IsGeneratedFile(context.Node.SyntaxTree.FilePath))
            return null;

        if (!AnalyzerHelpers.TryGetInvocationOperation(context, cancellationToken, out var invocation))
            return null;

        if (!TryGetTracedAttribute(invocation.TargetMethod, context.SemanticModel.Compilation, out var tracedInfo))
            return null;

        // Skip if already intercepted by another generator
        if (AnalyzerHelpers.IsAlreadyIntercepted(context, cancellationToken))
            return null;

        var interceptLocation = context.SemanticModel.GetInterceptableLocation(
            (InvocationExpressionSyntax)context.Node,
            cancellationToken);

        if (interceptLocation is null)
            return null;

        var method = invocation.TargetMethod;
        var tracedTags = ExtractTracedTags(method, context.SemanticModel.Compilation);
        var parameterTypes = method.Parameters.Select(static p => p.Type.ToDisplayString()).ToList();
        var parameterNames = method.Parameters.Select(static p => p.Name).ToList();

        var isStatic = method.IsStatic;
        var isAsync = IsAsyncMethod(method);

        return new TracedInvocationInfo(
            AnalyzerHelpers.FormatOrderKey(context.Node),
            tracedInfo.Value.ActivitySourceName,
            tracedInfo.Value.SpanName ?? method.Name,
            tracedInfo.Value.SpanKind,
            method.ContainingType.ToDisplayString(),
            method.Name,
            isStatic,
            isAsync,
            method.ReturnType.ToDisplayString(),
            parameterTypes,
            parameterNames,
            tracedTags,
            interceptLocation);
    }

    private static bool TryGetTracedAttribute(
        IMethodSymbol method,
        Compilation compilation,
        [NotNullWhen(true)] out (string ActivitySourceName, string? SpanName, string SpanKind)? tracedInfo)
    {
        tracedInfo = null;

        var tracedAttributeType = compilation.GetTypeByMetadataName(TracedAttributeFullName);
        if (tracedAttributeType is null)
            return false;

        foreach (var attribute in method.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, tracedAttributeType))
                continue;

            // Get ActivitySourceName from constructor argument
            if (attribute.ConstructorArguments.Length == 0)
                return false;

            var activitySourceName = attribute.ConstructorArguments[0].Value as string;
            if (string.IsNullOrEmpty(activitySourceName))
                return false;

            string? spanName = null;
            var spanKind = "Internal"; // Default

            // Get named arguments
            foreach (var namedArg in attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "SpanName":
                        spanName = namedArg.Value.Value as string;
                        break;
                    case "Kind":
                        // ActivityKind enum value
                        if (namedArg.Value.Value is int kindValue)
                        {
                            spanKind = kindValue switch
                            {
                                0 => "Internal",
                                1 => "Server",
                                2 => "Client",
                                3 => "Producer",
                                4 => "Consumer",
                                _ => "Internal"
                            };
                        }
                        break;
                }
            }

            tracedInfo = (activitySourceName!, spanName, spanKind);
            return true;
        }

        return false;
    }

    private static IReadOnlyList<TracedTagInfo> ExtractTracedTags(
        IMethodSymbol method,
        Compilation compilation)
    {
        var tracedTagAttributeType = compilation.GetTypeByMetadataName(TracedTagAttributeFullName);
        if (tracedTagAttributeType is null)
            return [];

        var tags = new List<TracedTagInfo>();

        foreach (var parameter in method.Parameters)
        {
            foreach (var attribute in parameter.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, tracedTagAttributeType))
                    continue;

                // Get tag name from constructor argument
                if (attribute.ConstructorArguments.Length == 0)
                    continue;

                var tagName = attribute.ConstructorArguments[0].Value as string;
                if (string.IsNullOrEmpty(tagName))
                    continue;

                var skipIfNull = true; // Default
                foreach (var namedArg in attribute.NamedArguments)
                {
                    if (namedArg.Key == "SkipIfNull" && namedArg.Value.Value is bool skipValue)
                        skipIfNull = skipValue;
                }

                var isNullable = parameter.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                                 parameter.Type.IsReferenceType;

                tags.Add(new TracedTagInfo(
                    parameter.Name,
                    tagName!,
                    skipIfNull,
                    isNullable));
            }
        }

        return tags;
    }

    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        // Check if return type is Task, Task<T>, ValueTask, or ValueTask<T>
        var returnType = method.ReturnType;
        var returnTypeName = returnType.ToDisplayString();

        return returnTypeName.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal) ||
               returnTypeName.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal);
    }
}
