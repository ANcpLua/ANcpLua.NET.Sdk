# Instrumentation Validation Specs

**Status:** PENDING VALIDATION
**Date:** 2026-01-16
**Purpose:** Validate auto-instrumentation features before documenting as production-ready

---

## Spec 1: Database Auto-Instrumentation

### Description

Validate that ADO.NET `DbCommand.Execute*` calls are automatically instrumented with OpenTelemetry spans when using
`ANcpLua.NET.Sdk.Web`.

### Prerequisites

- Project using `ANcpLua.NET.Sdk.Web`
- Reference to a database provider (e.g., `DuckDB.NET.Data`)
- OTLP exporter or in-memory exporter for verification

### Test Scenario

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/db-test", async () =>
{
    using var connection = new DuckDBConnection("Data Source=:memory:");
    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = "SELECT 42 as answer";
    var result = await command.ExecuteScalarAsync();

    return Results.Ok(new { answer = result });
});

app.Run();
```

### Definition of Done

| # | Criterion                                                                    | Validated | Evidence                |
|---|------------------------------------------------------------------------------|-----------|-------------------------|
| 1 | `DbIntercepts.g.cs` is generated in `obj/` folder                            | ☐         | File exists             |
| 2 | Generated file contains `[InterceptsLocation]` for `ExecuteScalarAsync` call | ☐         | Grep output             |
| 3 | Span with name `db.query` is emitted when endpoint is called                 | ☐         | OTLP/console output     |
| 4 | Span has `db.system.name = "duckdb"`                                         | ☐         | Span attributes         |
| 5 | Span has `db.operation.name = "ExecuteScalar"`                               | ☐         | Span attributes         |
| 6 | Span has `db.query.text = "SELECT 42 as answer"`                             | ☐         | Span attributes         |
| 7 | Error spans are emitted with exception details on failure                    | ☐         | Force error, check span |

### Validation Command

```bash
# Build and check for generated file
dotnet build -v detailed 2>&1 | grep -i "DbIntercepts"

# Run with console exporter
OTEL_TRACES_EXPORTER=console dotnet run

# Call endpoint
curl http://localhost:5000/db-test
```

---

## Spec 2: GenAI Auto-Instrumentation

### Description

Validate that GenAI SDK calls (OpenAI, Anthropic, etc.) are automatically instrumented with OpenTelemetry spans.

### Prerequisites

- Project using `ANcpLua.NET.Sdk.Web`
- Reference to `OpenAI` NuGet package
- Valid API key or mock server

### Test Scenario

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/genai-test", async () =>
{
    var client = new OpenAI.Chat.ChatClient("gpt-4o-mini", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

    var messages = new List<OpenAI.Chat.ChatMessage>
    {
        new OpenAI.Chat.UserChatMessage("Say hello in one word")
    };

    var result = await client.CompleteChatAsync(messages);

    return Results.Ok(new { response = result.Value.Content[0].Text });
});

app.Run();
```

### Definition of Done

| # | Criterion                                                                   | Validated | Evidence                |
|---|-----------------------------------------------------------------------------|-----------|-------------------------|
| 1 | `GenAiIntercepts.g.cs` is generated in `obj/` folder                        | ☐         | File exists             |
| 2 | Generated file contains `[InterceptsLocation]` for `CompleteChatAsync` call | ☐         | Grep output             |
| 3 | Span with operation `chat openai` is emitted                                | ☐         | OTLP/console output     |
| 4 | Span has `gen_ai.system = "openai"`                                         | ☐         | Span attributes         |
| 5 | Span has `gen_ai.operation.name = "chat"`                                   | ☐         | Span attributes         |
| 6 | Span has `gen_ai.request.model` (if extractable)                            | ☐         | Span attributes         |
| 7 | Span has `gen_ai.usage.input_tokens` after successful call                  | ☐         | Span attributes         |
| 8 | Span has `gen_ai.usage.output_tokens` after successful call                 | ☐         | Span attributes         |
| 9 | Error spans are emitted with exception on API failure                       | ☐         | Force error, check span |

### Validation Command

```bash
# Build and check for generated file
dotnet build -v detailed 2>&1 | grep -i "GenAiIntercepts"

# Run with console exporter
OTEL_TRACES_EXPORTER=console OPENAI_API_KEY=sk-xxx dotnet run

# Call endpoint
curl http://localhost:5000/genai-test
```

---

## Spec 3: [OTel] Attribute Tag Generation

### Description

Validate that the `[OTel]` attribute generates `Activity.SetTag()` extension methods.

### Prerequisites

- Project using `ANcpLua.NET.Sdk.Web`
- Record type with `[OTel]` attributes

### Test Scenario

```csharp
// Models/ChatRequest.cs
using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;

public record ChatRequest(
    [OTel("gen_ai.request.model")] string Model,
    [OTel("gen_ai.request.max_tokens")] int? MaxTokens,
    [OTel("gen_ai.request.temperature", SkipIfNull = false)] double? Temperature);

// Program.cs
app.MapPost("/otel-test", (ChatRequest request) =>
{
    using var activity = Activity.Current;

    // This method should be generated
    activity.SetTagsFromChatRequest(request);

    return Results.Ok();
});
```

### Definition of Done

| # | Criterion                                                                    | Validated | Evidence                  |
|---|------------------------------------------------------------------------------|-----------|---------------------------|
| 1 | `OTelTagExtensions.g.cs` is generated in `obj/` folder                       | ☐         | File exists               |
| 2 | Generated file contains `SetTagsFromChatRequest` extension method            | ☐         | Grep output               |
| 3 | Method sets `gen_ai.request.model` tag                                       | ☐         | Generated code inspection |
| 4 | Method skips `gen_ai.request.max_tokens` when null (SkipIfNull=true default) | ☐         | Generated code inspection |
| 5 | Method sets `gen_ai.request.temperature` even when null (SkipIfNull=false)   | ☐         | Generated code inspection |
| 6 | Code compiles and `SetTagsFromChatRequest` is callable                       | ☐         | Build succeeds            |
| 7 | Tags appear in span when endpoint is called                                  | ☐         | OTLP/console output       |

### Validation Command

```bash
# Build and check for generated file
dotnet build -v detailed 2>&1 | grep -i "OTelTagExtensions"

# Inspect generated file
cat obj/Debug/net10.0/generated/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/OTelTagExtensions.g.cs
```

---

## Spec 4: ActivitySource Subscription

### Description

Validate that `ANcpSdk.GenAi` and `ANcpSdk.Db` ActivitySources are subscribed to by the service defaults.

### Prerequisites

- Project using `ANcpLua.NET.Sdk.Web`
- OTLP or console exporter

### Test Scenario

```csharp
// Verify ActivitySources are registered
// The service defaults should call: .AddSource("ANcpSdk.*")

// Manual verification
var genAiSource = new ActivitySource("ANcpSdk.GenAi");
var dbSource = new ActivitySource("ANcpSdk.Db");

// These should create activities if listeners are subscribed
using var genAiActivity = genAiSource.StartActivity("test-genai");
using var dbActivity = dbSource.StartActivity("test-db");
```

### Definition of Done

| # | Criterion                                                             | Validated | Evidence        |
|---|-----------------------------------------------------------------------|-----------|-----------------|
| 1 | `ANcpSdkServiceDefaults.cs` contains `.AddSource("ANcpSdk.*")`        | ☐         | Code inspection |
| 2 | Starting activity on `ANcpSdk.GenAi` source creates non-null activity | ☐         | Runtime check   |
| 3 | Starting activity on `ANcpSdk.Db` source creates non-null activity    | ☐         | Runtime check   |
| 4 | Activities are exported to OTLP/console                               | ☐         | Exporter output |
| 5 | Custom sources can be added via `ConfigureTracing` callback           | ☐         | Code test       |

### Validation Command

```bash
# Check service defaults source
grep -r "AddSource" eng/ANcpSdk.AspNetCore.ServiceDefaults/

# Run app and verify spans appear
OTEL_TRACES_EXPORTER=console dotnet run
```

---

## Overall Validation Status

| Spec | Feature                       | Status    | Blocker                 |
|------|-------------------------------|-----------|-------------------------|
| 1    | Database Auto-Instrumentation | ⏳ PENDING | Not integrated into qyl |
| 2    | GenAI Auto-Instrumentation    | ⏳ PENDING | Not integrated into qyl |
| 3    | [OTel] Attribute              | ⏳ PENDING | Not integrated into qyl |
| 4    | ActivitySource Subscription   | ⏳ PENDING | Not integrated into qyl |

## Next Steps

1. Create minimal test project in qyl that uses `ANcpLua.NET.Sdk.Web`
2. Add DuckDB and OpenAI references
3. Verify generated files appear in `obj/`
4. Run with OTLP exporter to Jaeger/Aspire Dashboard
5. Capture evidence for each Definition of Done criterion
6. Update docs only after validation passes

## Notes

- Design doc claims 85% complete but Phase 5 (Tests) is 0%
- No dedicated instrumentation tests exist yet
- SDK tests (296/296) pass but don't cover instrumentation
- Documentation should be marked as **preview/experimental** until validated