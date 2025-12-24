# ANcpSdk.AspNetCore.ServiceDefaults

Opinionated service defaults for ASP.NET Core applications, inspired by .NET Aspire.

## Features

- **OpenTelemetry**: Logging, metrics (ASP.NET Core, HTTP, Runtime), tracing with OTLP export
- **Health Checks**: `/health` (readiness) and `/alive` (liveness) endpoints
- **Service Discovery**: Microsoft.Extensions.ServiceDiscovery enabled
- **HTTP Resilience**: Standard resilience handlers with retries and circuit breakers
- **JSON Configuration**: CamelCase naming, enum converters, nullable annotations
- **Security**: Forwarded headers, HTTPS redirect, HSTS, antiforgery
- **OpenAPI**: Optional OpenAPI document generation

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseANcpSdkConventions();

var app = builder.Build();
app.MapANcpSdkDefaultEndpoints();
app.Run();
```

## Configuration

```csharp
builder.UseANcpSdkConventions(options =>
{
    options.Https.Enabled = true;
    options.OpenApi.Enabled = true;
    options.AntiForgery.Enabled = false;
    options.OpenTelemetry.ConfigureTracing = tracing => tracing.AddSource("MyApp");
});
```

## Auto-Registration

When used with `ANcpLua.NET.Sdk.Web`, service defaults are auto-registered via source generation.
Opt-out: `<AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>`

## License

MIT
