using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

/// <summary>
///     Configuration options for OpenTelemetry instrumentation.
/// </summary>
public sealed class ANcpSdkOpenTelemetryConfiguration
{
    /// <summary>
    ///     Gets or sets a delegate to configure OpenTelemetry logging.
    /// </summary>
    public Action<OpenTelemetryLoggerOptions>? ConfigureLogging { get; set; }

    /// <summary>
    ///     Gets or sets a delegate to configure OpenTelemetry metrics.
    /// </summary>
    public Action<MeterProviderBuilder>? ConfigureMetrics { get; set; }

    /// <summary>
    ///     Gets or sets a delegate to configure OpenTelemetry tracing.
    /// </summary>
    public Action<TracerProviderBuilder>? ConfigureTracing { get; set; }
}
