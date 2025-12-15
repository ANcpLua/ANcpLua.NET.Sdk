using System.Text.Json;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

public sealed class ANcpSdkServiceDefaultsOptions
{
    internal bool MapCalled { get; set; }

    public bool ValidateDependencyContainersOnStartup { get; set; } = true;

    public ANcpSdkHttpsConfiguration Https { get; } = new();
    public ANcpSdkOpenApiConfiguration OpenApi { get; } = new();
    public ANcpSdkOpenTelemetryConfiguration OpenTelemetry { get; } = new();
    public Action<JsonSerializerOptions>? ConfigureJsonOptions { get; set; }
    public ANcpSdkAntiForgeryConfiguration AntiForgery { get; } = new();
    public ANcpSdkStaticAssetsConfiguration StaticAssets { get; } = new();
    public ANcpSdkForwardedHeadersConfiguration ForwardedHeaders { get; } = new();
}