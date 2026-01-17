using Microsoft.CodeAnalysis.CSharp;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister;

internal sealed record InterceptionData(
    string OrderKey,
    InterceptionMethodKind Kind,
    InterceptableLocation InterceptableLocation);
