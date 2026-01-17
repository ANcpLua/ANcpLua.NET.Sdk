# Mintlify Docs Update Prompt

## Documentation Site

**Base URL:** https://ancplua.mintlify.app/sdk/

**Existing Pages:**

- https://ancplua.mintlify.app/sdk/overview
- https://ancplua.mintlify.app/sdk/variants
- https://ancplua.mintlify.app/sdk/service-defaults
- https://ancplua.mintlify.app/sdk/testing

## Issues Found

### 1. Testing Page - Missing MTP Details

The testing page mentions "xUnit v3 with Microsoft Testing Platform" but:

- No link to MTP documentation
- No explanation of EXE requirement
- No MTP CLI syntax examples
- Missing: SDK auto-detection of `xunit.v3.mtp-v2` package

**Add this content:**

```yaml
mtp_requirements:
  output_type: Exe  # Required for MTP
  auto_detection: SDK detects xunit.v3.mtp-v2 and sets OutputType=Exe automatically

mtp_cli_syntax:
  correct: "dotnet run -- --filter \"FullyQualifiedName~MyTest\""
  incorrect: "dotnet test --filter \"FullyQualifiedName~MyTest\""
  note: MTP uses dotnet run with -- separator, NOT dotnet test

mtp_documentation:
  official: "https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-overview"
  xunit_v3: "https://xunit.net/docs/getting-started/v3/cmdline"
```

### 2. Service Defaults Page - Missing Auto-Instrumentation

The page documents OpenTelemetry but NOT the auto-instrumentation feature. Add:

```yaml
auto_instrumentation:
  description: Zero-config OTel spans for GenAI and database calls
  mechanism: C# interceptors via source generation

  genai_providers:
    - id: openai
      package: OpenAI
      spans: [gen_ai.system, gen_ai.request.model, gen_ai.usage.*]
    - id: anthropic
      package: Anthropic.SDK
    - id: azure_openai
      package: Azure.AI.OpenAI
    - id: ollama
      package: OllamaSharp
    - id: google_ai
      package: Mscc.GenerativeAI
    - id: vertex_ai
      package: Google.Cloud.AIPlatform.V1

  database_providers:
    - id: duckdb
      package: DuckDB.NET.Data
      spans: [db.system.name, db.operation.name, db.query.text]
    - id: sqlite
      package: Microsoft.Data.Sqlite
    - id: postgresql
      package: Npgsql
    - id: mysql
      package: MySqlConnector
    - id: mssql
      package: Microsoft.Data.SqlClient
    - id: oracle
      package: Oracle.ManagedDataAccess
    - id: firebird
      package: FirebirdSql.Data.FirebirdClient

  activity_sources:
    - ANcpSdk.GenAi
    - ANcpSdk.Db
```

### 3. Missing [OTel] Attribute Documentation

Add new section to service-defaults or create instrumentation page:

```yaml
otel_attribute:
  purpose: Automatic Activity.SetTag() generation from properties
  namespace: ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation

  usage_example: |
    public record ChatRequest(
        [OTel("gen_ai.request.model")] string Model,
        [OTel("gen_ai.request.max_tokens")] int? MaxTokens,
        [OTel("gen_ai.request.temperature", SkipIfNull = false)] double? Temperature);

    // Generated:
    activity.SetTagsFromChatRequest(request);

  properties:
    - name: Name
      type: string
      required: true
      description: OTel semantic convention name
    - name: SkipIfNull
      type: bool
      default: true
      description: Skip tag if value is null
```

### 4. Overview Page - Add Auto-Instrumentation Feature

Update the Web SDK features list:

```yaml
web_sdk_features:
  existing:
    - OpenTelemetry (logging, metrics, tracing)
    - Health endpoints (/health, /alive)
    - HTTP resilience via Polly
    - DevLogs (browser-to-server logging)

  add:
    - "Auto-instrumentation for GenAI SDKs (OpenAI, Anthropic, Ollama, etc.)"
    - "Auto-instrumentation for ADO.NET (DuckDB, SQLite, PostgreSQL, etc.)"
    - "[OTel] attribute for custom span tags"
```

## Source Files for Reference

```
/Users/ancplua/ANcpLua.NET.Sdk/
├── docs/plans/2026-01-16-instrumentation-generator-design.md  ← Full design doc (YAML format)
├── eng/
│   ├── ANcpSdk.AspNetCore.ServiceDefaults/
│   │   ├── README.md
│   │   └── Instrumentation/
│   │       ├── ActivitySources.cs
│   │       ├── SemanticConventions.cs
│   │       ├── OTelAttribute.cs
│   │       ├── GenAi/GenAiInstrumentation.cs
│   │       └── Db/DbInstrumentation.cs
│   └── ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/
│       ├── README.md
│       └── Models/ProviderRegistry.cs  ← SSOT for all providers
└── README.md
```

## User Experience Examples

### GenAI - Before (Manual)

```csharp
var client = new ChatClient("gpt-4o", apiKey);

using var activity = ActivitySource.StartActivity("chat.completions");
activity?.SetTag("gen_ai.system", "openai");
activity?.SetTag("gen_ai.request.model", "gpt-4o");

try
{
    var result = await client.CompleteChatAsync(messages);
    activity?.SetTag("gen_ai.usage.input_tokens", result.Usage.InputTokenCount);
    activity?.SetTag("gen_ai.usage.output_tokens", result.Usage.OutputTokenCount);
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

### GenAI - After (Zero-Config)

```csharp
var client = new ChatClient("gpt-4o", apiKey);
var result = await client.CompleteChatAsync(messages);  // Spans emitted automatically
```

### Database - Before (Manual)

```csharp
using var activity = ActivitySource.StartActivity("db.query");
activity?.SetTag("db.system.name", "duckdb");
activity?.SetTag("db.query.text", command.CommandText);

try
{
    var reader = await command.ExecuteReaderAsync();
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

### Database - After (Zero-Config)

```csharp
var reader = await command.ExecuteReaderAsync();  // Spans emitted automatically
```

## Testing Page Updates

Add this section about MTP:

```markdown
## Microsoft Testing Platform (MTP)

The Test SDK uses xUnit v3 with Microsoft Testing Platform. Key differences from VSTest:

### EXE Requirement

MTP requires test projects to be executables:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

**The SDK auto-detects this!** When you reference `xunit.v3.mtp-v2`, the SDK automatically sets `OutputType=Exe`.

### CLI Syntax

MTP uses different command syntax than VSTest:

```bash
# MTP (correct for xUnit v3)
dotnet run -- --filter "FullyQualifiedName~MyTest"
dotnet run -- --list-tests

# VSTest (does NOT work with MTP)
dotnet test --filter "FullyQualifiedName~MyTest"
```

The `--` separator passes arguments to the test executable.

### Common Issues

| Error           | Cause                 | Fix                                                   |
|-----------------|-----------------------|-------------------------------------------------------|
| Exit code 8     | Zero tests discovered | Check filter syntax, ensure tests are public          |
| Exit code 5     | Unknown option        | Use MTP syntax, not VSTest                            |
| Tests not found | Missing EXE output    | SDK should auto-set, verify xunit.v3.mtp-v2 reference |

```

## Priority

1. **P0**: Testing page - Add MTP section with CLI syntax and EXE requirement
2. **P0**: Service defaults - Add auto-instrumentation section
3. **P1**: Overview - Update Web SDK features list
4. **P1**: Add [OTel] attribute documentation
5. **P2**: Create dedicated instrumentation reference page