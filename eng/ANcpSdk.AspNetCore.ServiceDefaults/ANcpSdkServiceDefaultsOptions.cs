using System.Text.Json;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

/// <summary>
///     Options for configuring ANcpSdk default conventions and behaviors.
/// </summary>
public sealed class ANcpSdkServiceDefaultsOptions
{
    internal bool MapCalled { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to validate dependency injection containers on startup.
    ///     <value>The default value is <see langword="true"/>.</value>
    /// </summary>
    public bool ValidateDependencyContainersOnStartup { get; set; } = true;

    /// <summary>
    ///     Gets the configuration options for HTTPS.
    /// </summary>
    public ANcpSdkHttpsConfiguration Https { get; } = new();

    /// <summary>
    ///     Gets the configuration options for OpenAPI.
    /// </summary>
    public ANcpSdkOpenApiConfiguration OpenApi { get; } = new();

    /// <summary>
    ///     Gets the configuration options for OpenTelemetry.
    /// </summary>
    public ANcpSdkOpenTelemetryConfiguration OpenTelemetry { get; } = new();

    /// <summary>
    ///     Gets or sets a delegate to configure <see cref="JsonSerializerOptions"/>.
    ///     <para>
    ///         These options are applied to both minimal APIs and controller-based APIs.
    ///     </para>
    /// </summary>
    public Action<JsonSerializerOptions>? ConfigureJsonOptions { get; set; }

    /// <summary>
    ///     Gets the configuration options for Anti-Forgery.
    /// </summary>
    public ANcpSdkAntiForgeryConfiguration AntiForgery { get; } = new();

    /// <summary>
    ///     Gets the configuration options for static assets.
    /// </summary>
    public ANcpSdkStaticAssetsConfiguration StaticAssets { get; } = new();

    /// <summary>
    ///     Gets the configuration options for forwarded headers.
    /// </summary>
    public ANcpSdkForwardedHeadersConfiguration ForwardedHeaders { get; } = new();

    /// <summary>
    ///     Gets the configuration options for developer logging.
    /// </summary>
    public ANcpSdkDevLogsConfiguration DevLogs { get; } = new();
}
