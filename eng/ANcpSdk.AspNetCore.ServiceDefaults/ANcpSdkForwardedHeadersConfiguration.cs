using Microsoft.AspNetCore.HttpOverrides;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

public sealed class ANcpSdkForwardedHeadersConfiguration
{
    public ForwardedHeaders ForwardedHeaders { get; set; } = 
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
}