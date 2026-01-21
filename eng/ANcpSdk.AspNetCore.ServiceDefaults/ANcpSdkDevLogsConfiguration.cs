namespace ANcpSdk.AspNetCore.ServiceDefaults;

/// <summary>
///     Configuration options for the developer logging endpoint.
/// </summary>
public sealed class ANcpSdkDevLogsConfiguration
{
    /// <summary>
    ///     Gets or sets a value indicating whether the developer logging endpoint is enabled.
    ///     <value>The default value is <see langword="true"/>.</value>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets the route pattern for the logging endpoint.
    ///     <value>The default value is <c>"/api/dev-logs"</c>.</value>
    /// </summary>
    public string RoutePattern { get; set; } = "/api/dev-logs";

    /// <summary>
    ///     Gets or sets a value indicating whether the logging endpoint is enabled in production environments.
    ///     <value>The default value is <see langword="false"/>.</value>
    /// </summary>
    public bool EnableInProduction { get; set; }
}
