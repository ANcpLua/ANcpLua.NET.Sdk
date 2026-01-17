namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.Models;

/// <summary>
///     Information about a type member decorated with [OTel] attribute.
/// </summary>
internal sealed record OTelTagInfo(
    string ContainingTypeName,
    string MemberName,
    string MemberTypeName,
    string AttributeName,
    bool SkipIfNull,
    bool IsNullable)
{
    public string OrderKey => $"{ContainingTypeName}.{MemberName}";
}
