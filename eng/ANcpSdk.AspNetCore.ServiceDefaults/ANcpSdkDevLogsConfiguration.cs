namespace ANcpSdk.AspNetCore.ServiceDefaults;

public sealed class ANcpSdkDevLogsConfiguration
{
    public bool Enabled { get; set; } = true;
    public string RoutePattern { get; set; } = "/api/dev-logs";
    public bool EnableInProduction { get; set; }
}
