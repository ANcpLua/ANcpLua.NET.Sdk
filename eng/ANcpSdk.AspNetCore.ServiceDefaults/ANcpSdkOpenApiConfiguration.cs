using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.OpenApi;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

public sealed class ANcpSdkOpenApiConfiguration
{
    public bool Enabled { get; set; } = true;
    public Action<OpenApiOptions>? ConfigureOpenApi { get; set; }

    [StringSyntax("Route")] public string RoutePattern { get; set; } = "/openapi/{documentName}.json";
}