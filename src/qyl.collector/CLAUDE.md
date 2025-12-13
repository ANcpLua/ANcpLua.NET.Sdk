# qyl.collector

@../../.claude/instructions/architecture.md
@../../.claude/instructions/patterns.md
@../../.claude/instructions/workflows.md

## Scope

Backend service. Receives telemetry via protocol layer, stores in DuckDB, serves REST API and SSE.

## Layer Enforcement

<allowed_dependencies>
- Qyl.Core (generated models)
- qyl.telemetry
- qyl.protocol
- DuckDB.NET.Data
- Microsoft.AspNetCore (full)
- System.IO.Hashing
</allowed_dependencies>

<forbidden_dependencies>
You ***must not*** add references to:
- qyl.sdk
- qyl.mcp
- qyl.dashboard

You will be penalized for adding forbidden dependencies.
</forbidden_dependencies>

## Ownership

<owned_files>
| File/Directory | Purpose | Notes |
|----------------|---------|-------|
| `Storage/DuckDbSchema.cs` | SINGLE schema definition | Authoritative |
| `Storage/DuckDbStore.cs` | Storage implementation | Uses schema from DuckDbSchema |
| `Query/SessionQueryService.cs` | SINGLE aggregation logic | SQL-based |
| `Api/` | REST endpoints | |
| `Realtime/` | SSE streaming | |
</owned_files>

## Single Source Rules

<schema_source>
`DuckDbSchema.cs` is the SINGLE schema definition. Do NOT define schema elsewhere.

```csharp
public static class DuckDbSchema
{
    public const string Version = "2.0.0";
    
    public static readonly string CreateSpansTable = """
        CREATE TABLE IF NOT EXISTS spans (
            trace_id              VARCHAR(32) NOT NULL,
            span_id               VARCHAR(16) NOT NULL,
            parent_span_id        VARCHAR(16),
            name                  VARCHAR NOT NULL,
            kind                  UTINYINT,
            start_time_unix_nano  BIGINT NOT NULL,
            end_time_unix_nano    BIGINT NOT NULL,
            duration_ns           BIGINT GENERATED ALWAYS AS (end_time_unix_nano - start_time_unix_nano),
            status_code           UTINYINT,
            "gen_ai.provider.name"        VARCHAR,
            "gen_ai.request.model"        VARCHAR,
            "gen_ai.usage.input_tokens"   BIGINT,
            "gen_ai.usage.output_tokens"  BIGINT,
            attributes            MAP(VARCHAR, VARCHAR),
            PRIMARY KEY (trace_id, span_id)
        );
        """;
}
```
</schema_source>

<aggregation_source>
`SessionQueryService.cs` is the SINGLE aggregation implementation. Do NOT create additional aggregators.

All aggregation uses SQL queries against DuckDB. Do NOT use in-memory aggregation.
</aggregation_source>

## AOT Registration

<aot_requirement>
Register ALL API response types in `QylSerializerContext`:

```csharp
[JsonSerializable(typeof(SpanData))]
[JsonSerializable(typeof(SpanData[]))]
[JsonSerializable(typeof(SessionSummary))]
[JsonSerializable(typeof(TraceData))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class QylSerializerContext : JsonSerializerContext { }
```

If you add a new endpoint returning a new type and do not register it, Native AOT build will fail.
</aot_requirement>

## Forbidden Actions

- Do NOT create duplicate schema (use DuckDbSchema.cs)
- Do NOT create duplicate aggregators (use SessionQueryService)
- Do NOT use LINQ in DuckDB write paths
- Do NOT skip AOT registration for new API types
- Do NOT create manual model classes (use Qyl.Core)

## Validation

Before commit:
- [ ] DuckDbSchema.cs is only schema definition
- [ ] SessionQueryService is only aggregator
- [ ] All API types in QylSerializerContext
- [ ] Lock class used for synchronization
- [ ] TimeProvider used for time access
