# qyl.telemetry

@../../.claude/instructions/architecture.md
@../../.claude/instructions/patterns.md

## Scope

Lowest-level library. Contains OTel constants and GenAI extraction logic. All other qyl.* projects depend on this.

## Layer Enforcement

<allowed_dependencies>
- Qyl.Core (generated models)
- OpenTelemetry.Api
- System.* BCL
</allowed_dependencies>

<forbidden_dependencies>
You ***must not*** add references to:
- qyl.protocol
- qyl.collector
- qyl.sdk
- qyl.mcp
- ASP.NET
- gRPC
- DuckDB
- Any HTTP client

You will be penalized for adding forbidden dependencies.
</forbidden_dependencies>

## Ownership

<owned_files>
| File | Purpose | Consumers |
|------|---------|-----------|
| `GenAiAttributes.cs` | Attribute name constants | ALL projects |
| `GenAiExtractor.cs` | SINGLE extraction logic | protocol, collector |
| `DeprecationMap.cs` | OTel deprecated→current | extractor |
| `QylAttributes.cs` | qyl-specific attributes | ALL projects |
</owned_files>

This project owns GenAiExtractor. Do NOT create extractors in other projects.

## Code Pattern

<extractor_pattern>
```csharp
public static class GenAiExtractor
{
    public static void Extract(
        ReadOnlySpan<KeyValuePair<string, object?>> attributes,
        out string? providerName,
        out string? requestModel,
        out long inputTokens,
        out long outputTokens)
    {
        providerName = null;
        requestModel = null;
        inputTokens = 0;
        outputTokens = 0;

        foreach (var attr in attributes)
        {
            var key = DeprecationMap.Normalize(attr.Key);
            switch (key)
            {
                case GenAiAttributes.ProviderName:
                    providerName = attr.Value as string;
                    break;
                case GenAiAttributes.RequestModel:
                    requestModel = attr.Value as string;
                    break;
                case GenAiAttributes.InputTokens:
                    inputTokens = Convert.ToInt64(attr.Value);
                    break;
                case GenAiAttributes.OutputTokens:
                    outputTokens = Convert.ToInt64(attr.Value);
                    break;
            }
        }
    }
}
```
</extractor_pattern>

## Validation

Before commit:
- [ ] No forbidden dependencies added
- [ ] GenAiExtractor is the ONLY extractor
- [ ] All attribute constants use correct OTel 1.38 names
- [ ] Deprecated attributes handled in DeprecationMap
