using Microsoft.CodeAnalysis.CSharp;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;

/// <summary>
///     Represents a GenAI SDK method invocation to be intercepted.
/// </summary>
internal sealed record GenAiInvocationInfo(
    string OrderKey,
    string Provider,
    string Operation,
    string? Model,
    string ContainingTypeName,
    string MethodName,
    bool IsAsync,
    string ReturnTypeName,
    IReadOnlyList<string> ParameterTypes,
    InterceptableLocation InterceptableLocation);
