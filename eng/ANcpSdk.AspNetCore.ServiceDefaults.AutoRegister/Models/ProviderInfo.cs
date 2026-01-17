namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;

/// <summary>
///     Information about a detected instrumentation provider package.
/// </summary>
internal sealed record ProviderInfo(
    ProviderCategory Category,
    string ProviderId,
    string AssemblyName,
    string PrimaryTypeName);

/// <summary>
///     Categories of instrumentation providers.
/// </summary>
internal enum ProviderCategory
{
    GenAi,
    Database
}
