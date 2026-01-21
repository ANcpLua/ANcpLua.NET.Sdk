using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.OpenApi;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

/// <summary>
///     Configuration options for OpenAPI document generation and serving.
/// </summary>
public sealed class ANcpSdkOpenApiConfiguration
{
    /// <summary>
    ///     Gets or sets a value indicating whether OpenAPI support is enabled.
    ///     <value>The default value is <see langword="true"/>.</value>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets a delegate to configure <see cref="OpenApiOptions"/>.
    /// </summary>
    public Action<OpenApiOptions>? ConfigureOpenApi { get; set; }

    /// <summary>
    ///     Gets or sets the route pattern for the OpenAPI JSON document.
    ///     <value>The default value is <c>"/openapi/{documentName}.json"</c>.</value>
    /// </summary>
    [StringSyntax("Route")] public string RoutePattern { get; set; } = "/openapi/{documentName}.json";
}
