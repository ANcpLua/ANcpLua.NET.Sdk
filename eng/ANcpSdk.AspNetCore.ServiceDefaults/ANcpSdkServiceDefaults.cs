using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

public static partial class ANcpSdkServiceDefaults
{
    private const string DevLogsScript = """
                                         (function() {
                                             const endpoint = '{{ROUTE}}';
                                             const send = (level, args) => {
                                                 try {
                                                     fetch(endpoint, {
                                                         method: 'POST',
                                                         headers: { 'Content-Type': 'application/json' },
                                                         body: JSON.stringify({ level, message: Array.from(args).map(String).join(' '), timestamp: new Date().toISOString() })
                                                     }).catch(() => {});
                                                 } catch {}
                                             };
                                             ['log', 'info', 'warn', 'error', 'debug', 'trace'].forEach(level => {
                                                 const orig = console[level];
                                                 console[level] = function(...args) { orig.apply(console, args); send(level, args); };
                                             });
                                             console.log('[DevLogs] Frontend logging bridge active');
                                         })();
                                         """;

    public static TBuilder TryUseANcpSdkConventions<TBuilder>(this TBuilder builder,
        Action<ANcpSdkServiceDefaultsOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        return builder.Services.Any(static service => service.ServiceType == typeof(ANcpSdkServiceDefaultsOptions))
            ? builder
            : builder.UseANcpSdkConventions(configure);
    }

    public static TBuilder UseANcpSdkConventions<TBuilder>(this TBuilder builder,
        Action<ANcpSdkServiceDefaultsOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        var options = new ANcpSdkServiceDefaultsOptions();
        configure?.Invoke(options);

        if (options.ValidateDependencyContainersOnStartup)
            builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            }));

        builder.Services.Configure<KestrelServerOptions>(static serverOptions =>
        {
            serverOptions.AddServerHeader = false;
        });

        builder.Services.TryAddSingleton<IStartupFilter>(new ValidationStartupFilter());
        builder.Services.TryAddSingleton(options);

        if (options.AntiForgery.Enabled) builder.Services.AddAntiforgery();

        builder.ConfigureOpenTelemetry(options);
        builder.AddDefaultHealthChecks();

        if (options.OpenApi.Enabled) builder.Services.AddOpenApi(options.OpenApi.ConfigureOpenApi ?? (static _ => { }));

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(static http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
            http.ConfigureHttpClient(static (serviceProvider, client) =>
            {
                var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(
                        hostEnvironment.ApplicationName,
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString()));
            });
        });

        builder.Services.Configure<JsonOptions>(jsonOptions =>
            ConfigureJsonOptions(jsonOptions.JsonSerializerOptions, options));
        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(jsonOptions =>
            ConfigureJsonOptions(jsonOptions.SerializerOptions, options));

        builder.Services.AddProblemDetails();

#if NET10_0_OR_GREATER
        builder.Services.AddValidation();
#endif

        return builder;
    }

    private static void ConfigureJsonOptions(JsonSerializerOptions jsonOptions, ANcpSdkServiceDefaultsOptions options)
    {
        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.RespectNullableAnnotations = true;
        jsonOptions.RespectRequiredConstructorParameters = true;
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.ConfigureJsonOptions?.Invoke(jsonOptions);
    }

    private static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder,
        ANcpSdkServiceDefaultsOptions options)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            options.OpenTelemetry.ConfigureLogging?.Invoke(logging);
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(x =>
            {
                var name = builder.Environment.ApplicationName;
                var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
                x.AddService(name, serviceVersion: version);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("ANcpSdk.*");

                options.OpenTelemetry.ConfigureMetrics?.Invoke(metrics);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(static aspNetCoreTraceInstrumentationOptions =>
                    {
                        aspNetCoreTraceInstrumentationOptions.EnableAspNetCoreSignalRSupport = true;
                        aspNetCoreTraceInstrumentationOptions.Filter = static context =>
                            context.Request.Path != "/health" && context.Request.Path != "/alive";
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("ANcpSdk.*");

                options.OpenTelemetry.ConfigureTracing?.Invoke(tracing);
            });

        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter) builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }

    private static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static void MapANcpSdkDefaultEndpoints(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<ANcpSdkServiceDefaultsOptions>();
        if (options.MapCalled)
            return;

        options.MapCalled = true;

        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = options.ForwardedHeaders.ForwardedHeaders
        };
#pragma warning disable ASPDEPR005
        forwardedHeadersOptions.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
#if NET10_0_OR_GREATER
        forwardedHeadersOptions.KnownIPNetworks.Clear();
#endif
        forwardedHeadersOptions.KnownProxies.Clear();

        app.UseForwardedHeaders(forwardedHeadersOptions);

        if (options.Https.Enabled) app.UseHttpsRedirection();

        var environment = app.Services.GetRequiredService<IWebHostEnvironment>();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", true);

            if (options.Https is { Enabled: true, HstsEnabled: true }) app.UseHsts();
        }

        if (options.AntiForgery.Enabled) app.UseAntiforgery();

        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = static r => r.Tags.Contains("live")
        });

        if (options.StaticAssets.Enabled)
        {
            var staticAssetsManifestPath = $"{environment.ApplicationName}.staticwebassets.endpoints.json";
            staticAssetsManifestPath = !Path.IsPathRooted(staticAssetsManifestPath)
                ? Path.Combine(AppContext.BaseDirectory, staticAssetsManifestPath)
                : staticAssetsManifestPath;

            if (File.Exists(staticAssetsManifestPath)) app.MapStaticAssets();
        }

        if (options.OpenApi.Enabled) app.MapOpenApi(options.OpenApi.RoutePattern).CacheOutput();

        if (options.DevLogs.Enabled && (app.Environment.IsDevelopment() || options.DevLogs.EnableInProduction))
            app.MapDevLogsEndpoint(options);
    }

    private static void MapDevLogsEndpoint(this IEndpointRouteBuilder app, ANcpSdkServiceDefaultsOptions options)
    {
        app.MapPost(options.DevLogs.RoutePattern, static (DevLogEntry entry, ILogger<DevLogEntry> logger) =>
        {
            var logLevel = entry.Level?.ToLowerInvariant() switch
            {
                "error" => LogLevel.Error,
                "warn" or "warning" => LogLevel.Warning,
                "debug" => LogLevel.Debug,
                "trace" => LogLevel.Trace,
                _ => LogLevel.Information
            };
            logger.LogBrowserMessage(logLevel, entry.Message ?? string.Empty);
            return Results.Ok();
        }).ExcludeFromDescription();

        app.MapGet("/dev-logs.js", () =>
        {
            var script = DevLogsScript.Replace("{{ROUTE}}", options.DevLogs.RoutePattern);
            return Results.Content(script, "application/javascript");
        }).ExcludeFromDescription();
    }

    [LoggerMessage(Message = "[BROWSER] {Message}")]
    private static partial void LogBrowserMessage(this ILogger logger, LogLevel level, string message);
}

internal sealed record DevLogEntry(string? Level, string? Message);