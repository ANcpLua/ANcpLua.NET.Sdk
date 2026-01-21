# ANcpSdk.AspNetCore.ServiceDefaults

Runtime library for ASP.NET Core service defaults and OpenTelemetry instrumentation.

## Overview

Provides:

- **Service defaults configuration** - OpenTelemetry, health checks, resilience
- **Instrumentation runtime** - Activity wrappers for GenAI and database calls
- **[OTel] attribute** - Marker for compile-time tag generation

## Architecture

```
ANcpSdk.AspNetCore.ServiceDefaults/
├── ANcpSdkServiceDefaults.cs          <- Main service defaults registration
├── ANcpSdkServiceDefaultsOptions.cs   <- Configuration options
├── ANcpSdkOpenTelemetryConfiguration.cs
├── ANcpSdkOpenApiConfiguration.cs
├── ANcpSdkHttpsConfiguration.cs
├── ANcpSdkForwardedHeadersConfiguration.cs
├── ANcpSdkDevLogsConfiguration.cs
├── ANcpSdkAntiForgeryConfiguration.cs
├── ANcpSdkStaticAssetsConfiguration.cs
├── ValidationStartupFilter.cs
└── Instrumentation/
    ├── ActivitySources.cs       <- Centralized ActivitySource definitions
    ├── SemanticConventions.cs   <- OTel semantic convention constants
    ├── OTelAttribute.cs         <- [OTel] marker attribute
    ├── GenAi/
    │   ├── GenAiInstrumentation.cs <- Execute/ExecuteAsync wrappers
    │   └── TokenUsage.cs           <- Token count record
    └── Db/
        └── DbInstrumentation.cs    <- DbCommand wrappers
```

## Instrumentation

### GenAI

Wraps AI SDK calls with OpenTelemetry spans:

```csharp
// Called by generated interceptors
GenAiInstrumentation.ExecuteAsync(
    provider: "openai",
    operation: "chat",
    model: "gpt-4",
    execute: () => client.CompleteChatAsync(messages),
    extractUsage: r => new TokenUsage(r.Usage.InputTokenCount, r.Usage.OutputTokenCount));
```

Emits spans with:

- `gen_ai.system` - Provider name
- `gen_ai.operation.name` - Operation type
- `gen_ai.request.model` - Model name
- `gen_ai.usage.input_tokens` - Input token count
- `gen_ai.usage.output_tokens` - Output token count

### Database

Wraps `DbCommand` methods with OpenTelemetry spans:

```csharp
// Called by generated interceptors
DbInstrumentation.ExecuteReaderAsync(command, cancellationToken);
```

Emits spans with:

- `db.system.name` - Database system (postgresql, mysql, etc.)
- `db.operation.name` - Operation type (ExecuteReader, ExecuteNonQuery, etc.)
- `db.query.text` - SQL command text
- `db.namespace` - Database name

## [OTel] Attribute

Mark properties for automatic tag generation:

```csharp
using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;

public record ChatRequest(
    [OTel("gen_ai.request.model")] string Model,
    [OTel("gen_ai.request.max_tokens")] int? MaxTokens,
    [OTel("gen_ai.request.temperature", SkipIfNull = false)] double? Temperature);
```

The source generator creates extension methods:

```csharp
// Generated
activity.SetTagsFromChatRequest(request);
```

## ActivitySources

| Source          | Description               |
|-----------------|---------------------------|
| `ANcpSdk.GenAi` | GenAI SDK instrumentation |
| `ANcpSdk.Db`    | Database instrumentation  |

Infrastructure wiring in `ANcpSdkServiceDefaults.cs`:

```csharp
.AddSource("ANcpSdk.*")  // Subscribes to all ANcpSdk sources
```

## Semantic Conventions

Constants in `SemanticConventions.cs` follow OpenTelemetry standards:

```csharp
SemanticConventions.GenAi.System       // "gen_ai.system"
SemanticConventions.GenAi.RequestModel // "gen_ai.request.model"
SemanticConventions.Db.SystemName      // "db.system.name"
SemanticConventions.Db.QueryText       // "db.query.text"
```

## Adding New Instrumentation

1. Add `ActivitySource` to `ActivitySources.cs`
2. Add semantic conventions to `SemanticConventions.cs`
3. Create instrumentation class in appropriate subfolder
4. Subscribe to source in `ANcpSdkOpenTelemetryConfiguration.cs`