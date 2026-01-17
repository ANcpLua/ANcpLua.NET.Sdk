# ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister

Incremental source generator for ASP.NET Core service defaults and instrumentation.

## Overview

This generator provides compile-time interception of:

- **Build() calls** - Auto-registers service defaults
- **GenAI SDK calls** - OpenTelemetry spans for AI providers
- **Database calls** - OpenTelemetry spans for ADO.NET
- **[OTel] attributes** - Activity.SetTag() extension methods

## Generated Files

| File                     | Purpose                                    |
|--------------------------|--------------------------------------------|
| `Intercepts.g.cs`        | WebApplicationBuilder.Build() interception |
| `GenAiIntercepts.g.cs`   | GenAI SDK method interceptors              |
| `DbIntercepts.g.cs`      | DbCommand.Execute* interceptors            |
| `OTelTagExtensions.g.cs` | SetTagsFrom* extension methods             |

## Architecture

```
Models/
├── ProviderRegistry.cs      ← SSOT for all provider definitions
├── GenAiInvocationInfo.cs   ← GenAI call site data
├── DbInvocationInfo.cs      ← Database call site data
├── OTelTagInfo.cs           ← [OTel] attribute data
├── InterceptionData.cs      ← Build() interception data
└── ProviderInfo.cs          ← Detected provider info

Analyzers/
├── ProviderDetector.cs      ← Detects providers via assembly references
├── GenAiCallSiteAnalyzer.cs ← Finds GenAI SDK invocations
├── DbCallSiteAnalyzer.cs    ← Finds DbCommand invocations
└── OTelTagAnalyzer.cs       ← Finds [OTel] attributes

Emitters/
├── GenAiInterceptorEmitter.cs ← Emits GenAI interceptors
├── DbInterceptorEmitter.cs    ← Emits DB interceptors
└── OTelTagsEmitter.cs         ← Emits SetTag extensions
```

## Supported Providers

### GenAI

| Provider     | Assembly                     | Provider ID    |
|--------------|------------------------------|----------------|
| OpenAI       | `OpenAI`                     | `openai`       |
| Anthropic    | `Anthropic.SDK`              | `anthropic`    |
| Azure OpenAI | `Azure.AI.OpenAI`            | `azure_openai` |
| Ollama       | `OllamaSharp`                | `ollama`       |
| Google AI    | `Mscc.GenerativeAI`          | `google_ai`    |
| Vertex AI    | `Google.Cloud.AIPlatform.V1` | `vertex_ai`    |

### Database

| Provider   | Assembly                          | Provider ID  |
|------------|-----------------------------------|--------------|
| DuckDB     | `DuckDB.NET.Data`                 | `duckdb`     |
| SQLite     | `Microsoft.Data.Sqlite`           | `sqlite`     |
| PostgreSQL | `Npgsql`                          | `postgresql` |
| MySQL      | `MySqlConnector`                  | `mysql`      |
| SQL Server | `Microsoft.Data.SqlClient`        | `mssql`      |
| Oracle     | `Oracle.ManagedDataAccess`        | `oracle`     |
| Firebird   | `FirebirdSql.Data.FirebirdClient` | `firebird`   |

## Adding New Providers

1. Add provider definition to `Models/ProviderRegistry.cs`
2. For database providers, also update `DbInstrumentation.MapTypeNameToDbSystem`
3. Run tests to verify detection

## Implementation Notes

- Targets `netstandard2.0` for Roslyn compatibility
- Uses C# interceptors (`[InterceptsLocation]`)
- Incremental generator with tracking names for debugging
- Provider detection via `Compilation.ReferencedAssemblyNames`