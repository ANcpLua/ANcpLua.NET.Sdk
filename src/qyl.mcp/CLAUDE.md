# qyl.mcp

@../../.claude/instructions/architecture.md
@../../.claude/instructions/patterns.md

## Scope

MCP server for AI assistants. Provides tools to query telemetry data. Calls collector via HTTP.

## Layer Enforcement

<allowed_dependencies>
- Qyl.Core (generated models)
- qyl.telemetry
- ModelContextProtocol SDK
- Microsoft.Extensions.Hosting
- System.Net.Http
</allowed_dependencies>

<forbidden_dependencies>
You ***must not*** add references to:
- qyl.protocol (MCP doesn't need gRPC)
- qyl.collector (calls via HTTP, not direct reference)
- qyl.sdk
- DuckDB

You will be penalized for adding forbidden dependencies.
</forbidden_dependencies>

## Communication Pattern

<http_only>
MCP server communicates with collector via HTTP only. Do NOT reference collector project.

```csharp
public class QylApiClient
{
    private readonly HttpClient _http;
    
    public async Task<SpanData[]> GetSpansAsync(string traceId, CancellationToken ct)
    {
        return await _http.GetFromJsonAsync<SpanData[]>(
            $"/api/v1/traces/{traceId}/spans",
            TelemetryJsonContext.Default.SpanDataArray,
            ct) ?? [];
    }
}
```
</http_only>

## Tool Pattern

<tool_implementation>
```csharp
[McpTool("query_traces")]
[Description("Search traces by service name or time range")]
public class QueryTracesTool
{
    [McpParameter("service")]
    [Description("Filter by service name")]
    public string? ServiceName { get; set; }
    
    [McpParameter("since_minutes")]
    [Description("Traces from last N minutes")]
    public int SinceMinutes { get; set; } = 60;
    
    public async Task<QueryResult> ExecuteAsync(QylApiClient client, CancellationToken ct)
    {
        var traces = await client.QueryTracesAsync(ServiceName, SinceMinutes, ct);
        return new QueryResult { Traces = traces };
    }
}
```
</tool_implementation>

## AOT Registration

<aot_requirement>
```csharp
[JsonSerializable(typeof(SpanData[]))]
[JsonSerializable(typeof(QueryResult))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class TelemetryJsonContext : JsonSerializerContext { }
```

MCP server is Native AOT. Register all types.
</aot_requirement>

## Forbidden Actions

- Do NOT reference collector project directly
- Do NOT include DuckDB
- Do NOT create complex tools (AI works better with simple, focused tools)
- Do NOT skip AOT registration

## Validation

Before commit:
- [ ] Calls collector via HTTP only
- [ ] No collector project reference
- [ ] All types in TelemetryJsonContext
- [ ] Tools are simple and focused
