# qyl.sdk

@../../.claude/instructions/architecture.md
@../../.claude/instructions/patterns.md

## Scope

NuGet package for end-users. Adds qyl observability to ASP.NET applications. Must be lightweight.

## Layer Enforcement

<allowed_dependencies>
- Qyl.Core (generated models)
- qyl.telemetry
- OpenTelemetry.Api
- OpenTelemetry.Extensions.Hosting
- Microsoft.AspNetCore.App (FrameworkReference)
- Microsoft.Extensions.AI
</allowed_dependencies>

<forbidden_dependencies>
You ***must not*** add references to:
- qyl.protocol (user doesn't need gRPC internals)
- qyl.collector (user runs separate collector)
- DuckDB
- Grpc.*
- Google.Protobuf

You will be penalized for adding forbidden dependencies. This is a lightweight SDK.
</forbidden_dependencies>

## Public API

<api_surface>
Keep public API minimal. Only these should be public:

```csharp
// Entry points
public static class QylServiceCollectionExtensions
{
    public static IServiceCollection AddQyl(this IServiceCollection services, Action<QylOptions>? configure = null);
}

public static class QylApplicationBuilderExtensions
{
    public static IApplicationBuilder UseQyl(this IApplicationBuilder app);
}

// Configuration
public class QylOptions
{
    public string CollectorEndpoint { get; set; } = "http://localhost:5100";
    public string ServiceName { get; set; } = "unknown";
    public bool EnableGenAiCapture { get; set; } = true;
}
```
</api_surface>

## User Experience

<usage_example>
```csharp
// User's Program.cs - this is the target experience
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddQyl(options =>
{
    options.CollectorEndpoint = "http://localhost:5100";
    options.ServiceName = "my-app";
});

var app = builder.Build();
app.UseQyl();
app.Run();
```
</usage_example>

## Forbidden Actions

- Do NOT expose internal types
- Do NOT add heavy dependencies
- Do NOT reference collector implementation
- Do NOT break backward compatibility without major version

## Validation

Before commit:
- [ ] No forbidden dependencies
- [ ] Public API is minimal
- [ ] Options validate correctly
- [ ] Works without collector (graceful degradation)
