using Microsoft.AspNetCore.HttpOverrides;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

/// <summary>
///     Configuration options for forwarded headers middleware.
/// </summary>
public sealed class ANcpSdkForwardedHeadersConfiguration
{
    /// <summary>
    ///     Gets or sets the forwarded headers to process.
    ///     <value>The default is <see cref="ForwardedHeaders.XForwardedFor"/> | <see cref="ForwardedHeaders.XForwardedProto"/> | <see cref="ForwardedHeaders.XForwardedHost"/>.</value>
    /// </summary>
    public ForwardedHeaders ForwardedHeaders { get; set; } =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
}
