using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

public sealed class ANcpSdkOpenTelemetryConfiguration
{
    public Action<OpenTelemetryLoggerOptions>? ConfigureLogging { get; set; }
    public Action<MeterProviderBuilder>? ConfigureMetrics { get; set; }
    public Action<TracerProviderBuilder>? ConfigureTracing { get; set; }
}
