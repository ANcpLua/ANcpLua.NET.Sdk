namespace ANcpSdk.AspNetCore.ServiceDefaults;

/// <summary>
///     Configuration options for static asset serving.
/// </summary>
public sealed class ANcpSdkStaticAssetsConfiguration
{
    /// <summary>
    ///     Gets or sets a value indicating whether static assets should be served.
    ///     <value>The default value is <see langword="true"/>.</value>
    /// </summary>
    public bool Enabled { get; set; } = true;
}
