# qyl.protocol

@../../.claude/instructions/architecture.md
@../../.claude/instructions/patterns.md

## Scope

OTLP protocol handling. Converts proto messages to Qyl.Core models. gRPC services receive telemetry.

## Layer Enforcement

<allowed_dependencies>
- Qyl.Core (generated models)
- qyl.telemetry
- Grpc.AspNetCore
- Grpc.Tools
- Google.Protobuf
- Microsoft.AspNetCore.App (FrameworkReference)
</allowed_dependencies>

<forbidden_dependencies>
You ***must not*** add references to:
- qyl.collector
- qyl.sdk
- qyl.mcp
- DuckDB
- Any storage abstraction

You will be penalized for adding forbidden dependencies.
</forbidden_dependencies>

## Ownership

<owned_files>
| Directory | Purpose |
|-----------|---------|
| `Protos/` | OTLP proto definitions |
| `Services/` | gRPC service implementations |
| `Http/` | HTTP protocol handler |
| `Conversion/` | Proto → Qyl.Core conversion |
| `Abstractions/` | Interfaces for collector to implement |
</owned_files>

## Code Patterns

<conversion_pattern>
```csharp
public static class OtlpConverter
{
    public static SpanData ToSpanData(
        this Opentelemetry.Proto.Trace.V1.Span proto,
        Resource resource)
    {
        // Use telemetry's extractor - do NOT duplicate
        GenAiExtractor.Extract(
            proto.Attributes.ToKeyValuePairs(),
            out var providerName,
            out var requestModel,
            out var inputTokens,
            out var outputTokens);

        return new SpanData
        {
            TraceId = proto.TraceId.ToHexString(),
            SpanId = proto.SpanId.ToHexString(),
            Name = proto.Name,
            GenAiProviderName = providerName,
            GenAiRequestModel = requestModel,
            GenAiInputTokens = inputTokens,
            GenAiOutputTokens = outputTokens,
        };
    }
}
```
</conversion_pattern>

<abstraction_pattern>
```csharp
// Interfaces that collector implements
public interface ITelemetryProcessor
{
    ValueTask ProcessSpansAsync(ReadOnlyMemory<SpanData> spans, CancellationToken ct);
    ValueTask ProcessLogsAsync(ReadOnlyMemory<LogData> logs, CancellationToken ct);
}
```
</abstraction_pattern>

## Forbidden Actions

- Do NOT store data (no DB access)
- Do NOT implement business logic beyond conversion
- Do NOT reference collector directly (use abstractions)
- Do NOT create extractors (use telemetry's GenAiExtractor)

## Validation

Before commit:
- [ ] No storage code added
- [ ] Uses GenAiExtractor from telemetry
- [ ] All conversions return Qyl.Core models
- [ ] No collector reference
