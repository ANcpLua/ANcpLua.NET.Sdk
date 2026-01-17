using Microsoft.CodeAnalysis.CSharp;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;

/// <summary>
///     Represents an ADO.NET DbCommand method invocation to be intercepted.
/// </summary>
internal sealed record DbInvocationInfo(
    string OrderKey,
    DbCommandMethod Method,
    bool IsAsync,
    string? ConcreteCommandType,
    InterceptableLocation InterceptableLocation);

/// <summary>
///     The ADO.NET DbCommand methods that can be intercepted.
/// </summary>
internal enum DbCommandMethod
{
    ExecuteReader,
    ExecuteNonQuery,
    ExecuteScalar
}
