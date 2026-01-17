using System.Diagnostics.CodeAnalysis;
using ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Analyzers;

/// <summary>
///     Analyzes syntax to find GenAI SDK method invocations to intercept.
/// </summary>
internal static class GenAiCallSiteAnalyzer
{
    /// <summary>
    ///     Known GenAI method patterns to intercept.
    ///     Key: containing type prefix, Value: (method name, operation name, is async).
    /// </summary>
    private static readonly Dictionary<string, (string MethodName, string Operation, bool IsAsync)[]> _methodPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OpenAI.Chat.ChatClient"] =
        [
            ("CompleteChatAsync", "chat", true),
            ("CompleteChat", "chat", false)
        ],
        ["OpenAI.Embeddings.EmbeddingClient"] =
        [
            ("GenerateEmbeddingsAsync", "embeddings", true),
            ("GenerateEmbeddings", "embeddings", false)
        ],

        ["Anthropic.AnthropicClient"] =
        [
            ("CreateMessageAsync", "chat", true),
            ("CreateMessage", "chat", false)
        ],

        ["OllamaSharp.OllamaApiClient"] =
        [
            ("ChatAsync", "chat", true),
            ("GenerateEmbeddingsAsync", "embeddings", true)
        ],

        ["Azure.AI.OpenAI.OpenAIClient"] =
        [
            ("GetChatCompletionsAsync", "chat", true),
            ("GetChatCompletions", "chat", false),
            ("GetEmbeddingsAsync", "embeddings", true),
            ("GetEmbeddings", "embeddings", false)
        ]
    };

    /// <summary>
    ///     Checks if the syntax node could potentially be a GenAI method call.
    /// </summary>
    public static bool IsPotentialGenAiCall(SyntaxNode node, CancellationToken _) =>
        node.IsKind(SyntaxKind.InvocationExpression);

    /// <summary>
    ///     Transforms a potential GenAI call to invocation info if it matches known patterns.
    /// </summary>
    public static GenAiInvocationInfo? TransformToGenAiInvocation(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (IsGeneratedFile(context.Node.SyntaxTree.FilePath))
            return null;

        if (!TryGetInvocationOperation(context, cancellationToken, out var invocation))
            return null;

        if (!TryMatchGenAiMethod(invocation, out var provider, out var operation, out var isAsync))
            return null;

        var interceptLocation = context.SemanticModel.GetInterceptableLocation(
            (InvocationExpressionSyntax)context.Node,
            cancellationToken);

        if (interceptLocation is null)
            return null;

        var method = invocation.TargetMethod;
        var model = TryExtractModelName(invocation);

        return new GenAiInvocationInfo(
            OrderKey: FormatOrderKey(context.Node),
            Provider: provider,
            Operation: operation,
            Model: model,
            ContainingTypeName: method.ContainingType.ToDisplayString(),
            MethodName: method.Name,
            IsAsync: isAsync,
            ReturnTypeName: method.ReturnType.ToDisplayString(),
            ParameterTypes: method.Parameters.Select(static p => p.Type.ToDisplayString()).ToList(),
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

    private static bool TryMatchGenAiMethod(
        IInvocationOperation invocation,
        [NotNullWhen(true)] out string? provider,
        [NotNullWhen(true)] out string? operation,
        out bool isAsync)
    {
        provider = null;
        operation = null;
        isAsync = false;

        var containingType = invocation.TargetMethod.ContainingType;
        if (containingType is null)
            return false;

        var typeName = containingType.ToDisplayString();
        var methodName = invocation.TargetMethod.Name;

        foreach (var kvp in _methodPatterns)
        {
            var typePrefix = kvp.Key;
            var methods = kvp.Value;

            if (!typeName.StartsWith(typePrefix, StringComparison.Ordinal))
                continue;

            foreach (var methodPattern in methods)
            {
                if (methodName != methodPattern.MethodName)
                    continue;

                provider = ProviderDetector.GetGenAiProviderId(typeName) ?? "unknown";
                operation = methodPattern.Operation;
                isAsync = methodPattern.IsAsync;
                return true;
            }
        }

        return false;
    }

    private static string? TryExtractModelName(IInvocationOperation invocation)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Name is not ("model" or "modelId" or "deploymentName"))
                continue;

            if (argument.Value.ConstantValue is { HasValue: true, Value: string modelValue })
                return modelValue;
        }

        return null;
    }

    private static string FormatOrderKey(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        var start = span.StartLinePosition;
        return $"{node.SyntaxTree.FilePath}:{start.Line}:{start.Character}";
    }
}
