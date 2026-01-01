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
- **DevLogs**: Frontend console log bridge for unified debugging (Development only)

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
    options.DevLogs.Enabled = true; // Default: true in Development
    options.OpenTelemetry.ConfigureTracing = tracing => tracing.AddSource("MyApp");
});
```

## DevLogs - Frontend Console Bridge

Captures browser `console.log/warn/error` and sends to server logs. Enabled by default in Development.

**Add to your HTML** (only served in Development):
```html
<script src="/dev-logs.js"></script>
```

**All frontend logs appear in server output with `[BROWSER]` prefix:**
```
info: DevLogEntry[0] [BROWSER] User clicked button
warn: DevLogEntry[0] [BROWSER] Deprecated API called
error: DevLogEntry[0] [BROWSER] Failed to fetch data
```

**Configuration:**
```csharp
options.DevLogs.Enabled = true;           // Default: true
options.DevLogs.RoutePattern = "/api/dev-logs"; // Default
options.DevLogs.EnableInProduction = false;     // Default: false
```

## Auto-Registration

When used with `ANcpLua.NET.Sdk.Web`, service defaults are auto-registered via source generation.
Opt-out: `<AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>`

## License

MIT
