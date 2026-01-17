# Instrumentation Generator Design

**Date:** 2026-01-16
**Status:** Approved → Implementation ~85% Complete
**Author:** Claude + ancplua

## Overview

A source generator that automatically instruments ADO.NET and GenAI SDK calls with OpenTelemetry spans. Zero-config
telemetry for DuckDB, SQLite, Npgsql, OpenAI, Anthropic, and Ollama.

## Architecture

```yaml
instrumentation_pipeline:
  name: ANcpSdk Instrumentation Pipeline

  compile_time:
    component: AutoRegister Generator
    responsibilities:
      - scan_package_references:
          targets: [OpenAI, DuckDB, Anthropic, Ollama, etc.]
          method: Compilation.ReferencedAssemblyNames
      - find_call_sites:
          genai: [ChatClient.Complete*, AnthropicClient.Messages.CreateAsync]
          database: [DbCommand.Execute*]
      - emit_interceptors:
          strategy: One per method signature
          attributes: Multiple [InterceptsLocation] on same method
    outputs:
      - GenAiIntercepts.g.cs
      - DbIntercepts.g.cs
      - OTelTagExtensions.g.cs
      - Intercepts.g.cs

  runtime:
    component: ServiceDefaults Library
    instrumentation_classes:
      - GenAiInstrumentation
      - DbInstrumentation
    activity_sources:
      - name: ANcpSdk.GenAi
        kind: Client
      - name: ANcpSdk.Db
        kind: Client
    semantic_conventions:
      genai: gen_ai.*
      database: db.*

  otel_integration:
    subscription: .AddSource("ANcpSdk.*")
    exporters: Configured by ServiceDefaults (OTLP, Console, etc.)
```

## Key Decisions

```yaml
decisions:
  - decision: Scope
    choice: GenAI + ADO.NET
    rationale: gRPC already has AddGrpcClientInstrumentation()

  - decision: Provider detection
    choice: Compile-time assembly scan
    rationale: No runtime reflection, dead code elimination

  - decision: Interceptor granularity
    choice: One per method signature
    rationale: Multiple [InterceptsLocation] on same method

  - decision: Runtime library
    choice: Separate testable helpers
    rationale: NuGet update path, proper debugging

  - decision: Package structure
    choice: Extend existing ServiceDefaults
    rationale: Zero new dependencies for users

  - decision: Provider definitions
    choice: ProviderRegistry as SSOT
    rationale: Single source of truth eliminates duplication
```

## Supported Providers (v1)

```yaml
providers:
  genai:
    - id: openai
      package: OpenAI
      assembly: OpenAI
      primary_type: OpenAI.OpenAIClient
      operations:
        - name: chat
          methods: [CompleteChat, CompleteChatAsync]
        - name: embeddings
          methods: [GenerateEmbeddings, GenerateEmbeddingsAsync]
      token_usage:
        input: Usage.InputTokenCount
        output: Usage.OutputTokenCount

    - id: anthropic
      package: Anthropic.SDK
      assembly: Anthropic.SDK
      primary_type: Anthropic.AnthropicClient
      operations:
        - name: chat
          methods: [Messages.CreateAsync]
      token_usage:
        input: Usage.InputTokens
        output: Usage.OutputTokens

    - id: azure_openai
      package: Azure.AI.OpenAI
      assembly: Azure.AI.OpenAI
      primary_type: Azure.AI.OpenAI.AzureOpenAIClient
      operations:
        - name: chat
          methods: [CompleteChat, CompleteChatAsync]
      token_usage:
        input: Usage.InputTokenCount
        output: Usage.OutputTokenCount

    - id: ollama
      package: OllamaSharp
      assembly: OllamaSharp
      primary_type: OllamaSharp.OllamaApiClient
      operations:
        - name: chat
          methods: [Chat, ChatAsync]
      token_usage: null  # Ollama doesn't report token counts

    - id: google_ai
      package: Mscc.GenerativeAI
      assembly: Mscc.GenerativeAI
      primary_type: Mscc.GenerativeAI.GenerativeModel
      operations:
        - name: chat
          methods: [GenerateContentAsync]
      token_usage:
        input: UsageMetadata.PromptTokenCount
        output: UsageMetadata.CandidatesTokenCount

    - id: vertex_ai
      package: Google.Cloud.AIPlatform.V1
      assembly: Google.Cloud.AIPlatform.V1
      primary_type: Google.Cloud.AIPlatform.V1.PredictionServiceClient
      operations:
        - name: predict
          methods: [Predict, PredictAsync]
      token_usage: null

  database:
    - id: duckdb
      package: DuckDB.NET.Data
      assembly: DuckDB.NET.Data
      type_contains: DuckDB

    - id: sqlite
      package: Microsoft.Data.Sqlite
      assembly: Microsoft.Data.Sqlite
      type_contains: Sqlite

    - id: postgresql
      package: Npgsql
      assembly: Npgsql
      type_contains: Npgsql

    - id: mysql
      package: MySqlConnector
      assembly: MySqlConnector
      type_contains: MySql

    - id: mssql
      package: Microsoft.Data.SqlClient
      assembly: Microsoft.Data.SqlClient
      type_contains: SqlClient

    - id: oracle
      package: Oracle.ManagedDataAccess
      assembly: Oracle.ManagedDataAccess
      type_contains: Oracle

    - id: firebird
      package: FirebirdSql.Data.FirebirdClient
      assembly: FirebirdSql.Data.FirebirdClient
      type_contains: Firebird
```

## File Structure

```yaml
file_structure:
  eng/:
    ANcpSdk.AspNetCore.ServiceDefaults/:
      target_framework: net10.0
      files:
        - path: ANcpSdkServiceDefaults.cs
          status: updated
          change: Added ActivitySources subscription
        - path: Instrumentation/ActivitySources.cs
          status: created
        - path: Instrumentation/SemanticConventions.cs
          status: created
        - path: Instrumentation/OTelAttribute.cs
          status: created (bonus)
        - path: Instrumentation/GenAi/GenAiInstrumentation.cs
          status: created
        - path: Instrumentation/GenAi/TokenUsage.cs
          status: created
        - path: Instrumentation/Db/DbInstrumentation.cs
          status: created

    ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/:
      target_framework: netstandard2.0
      files:
        Models/:
          - path: ProviderRegistry.cs
            status: created (bonus - SSOT)
          - path: ProviderDefinition.cs
            status: created (bonus)
          - path: GenAiInvocationInfo.cs
            status: created
          - path: DbInvocationInfo.cs
            status: created
          - path: ProviderInfo.cs
            status: created
          - path: OTelTagInfo.cs
            status: created (bonus)
          - path: InterceptionData.cs
            status: updated
          - path: InterceptionMethodKind.cs
            status: updated
        Analyzers/:
          - path: ProviderDetector.cs
            status: created
          - path: GenAiCallSiteAnalyzer.cs
            status: created
          - path: DbCallSiteAnalyzer.cs
            status: created
          - path: OTelTagAnalyzer.cs
            status: created (bonus)
        Emitters/:
          - path: GenAiInterceptorEmitter.cs
            status: created
          - path: DbInterceptorEmitter.cs
            status: created
          - path: OTelTagsEmitter.cs
            status: created (bonus)
```

## Generated Code Example

### User Code

```csharp
var client = new ChatClient("gpt-4o", apiKey);
var result = await client.CompleteChatAsync(messages);  // Line 42
```

### Generated Interceptor

```csharp
// <auto-generated/>
namespace ANcpSdk.Generated
{
    file static class GenAiInterceptors
    {
        // Signature: CompleteChatAsync(IEnumerable<ChatMessage>, CancellationToken)
        // Intercepts:
        //   - Program.cs:42
        [InterceptsLocation(1, "...")]
        public static Task<ChatCompletion> CompleteChatAsync_0(
            this ChatClient client,
            IEnumerable<ChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            return GenAiInstrumentation.ExecuteAsync(
                provider: "openai",
                operation: "chat",
                model: client.Model,
                execute: () => client.CompleteChatAsync(messages, cancellationToken),
                extractUsage: static r => new TokenUsage(r.Usage.InputTokenCount, r.Usage.OutputTokenCount));
        }
    }
}
```

## Bonus: OTel Attribute System

### User Code

```csharp
using ANcpSdk.AspNetCore.ServiceDefaults.Instrumentation;

public record ChatRequest(
    [OTel("gen_ai.request.model")] string Model,
    [OTel("gen_ai.request.max_tokens")] int? MaxTokens,
    [OTel("gen_ai.request.temperature", SkipIfNull = false)] double? Temperature);
```

### Generated Extension

```csharp
// <auto-generated/>
internal static class OTelTagExtensions
{
    public static void SetTagsFromChatRequest(this Activity? activity, ChatRequest? value)
    {
        if (activity is null || value is null) return;

        activity.SetTag("gen_ai.request.model", value.Model);
        if (value.MaxTokens is not null)
            activity.SetTag("gen_ai.request.max_tokens", value.MaxTokens);
        activity.SetTag("gen_ai.request.temperature", value.Temperature);
    }
}
```

## Runtime Library

### GenAiInstrumentation.cs

```csharp
public static class GenAiInstrumentation
{
    public static async Task<TResponse> ExecuteAsync<TResponse>(
        string provider,
        string operation,
        string? model,
        Func<Task<TResponse>> execute,
        Func<TResponse, TokenUsage>? extractUsage = null)
    {
        using var activity = ActivitySources.GenAi.StartActivity(
            $"{operation} {provider}",
            ActivityKind.Client);

        if (activity is null)
            return await execute();

        activity.SetTag(SemanticConventions.GenAi.System, provider);
        activity.SetTag(SemanticConventions.GenAi.OperationName, operation);

        if (model is not null)
            activity.SetTag(SemanticConventions.GenAi.RequestModel, model);

        try
        {
            var response = await execute();

            if (extractUsage is not null)
            {
                var usage = extractUsage(response);
                activity.SetTag(SemanticConventions.GenAi.InputTokens, usage.InputTokens);
                activity.SetTag(SemanticConventions.GenAi.OutputTokens, usage.OutputTokens);
            }

            return response;
        }
        catch (Exception ex)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.RecordException(ex);
            throw;
        }
    }
}
```

### DbInstrumentation.cs

```csharp
public static class DbInstrumentation
{
    private static readonly ConcurrentDictionary<Type, string> s_dbSystemCache = new();

    public static async Task<DbDataReader> ExecuteReaderAsync(
        DbCommand command,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartDbActivity(command, "ExecuteReader");

        try
        {
            return await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    // Note: Activity covers command execution, not reader iteration.
    // This matches OTel semantic conventions for db.query spans.

    private static string GetDbSystem(DbConnection? connection)
    {
        if (connection is null)
            return "unknown";

        return s_dbSystemCache.GetOrAdd(connection.GetType(), static type =>
        {
            var name = type.FullName ?? type.Name;
            return MapTypeNameToDbSystem(name);
        });
    }

    internal static string MapTypeNameToDbSystem(string typeName) =>
        ProviderRegistry.DatabaseProviders
            .FirstOrDefault(p => typeName.Contains(p.TypeContains, StringComparison.OrdinalIgnoreCase))
            ?.ProviderId ?? "unknown";
}
```

## Risks & Mitigations

```yaml
risks:
  - risk: SDK API changes break interceptors (OpenAI 3.0, Anthropic 2.0)
    mitigation: Version ranges in templates; CI tests against multiple SDK versions

  - risk: Interceptors add compile time
    mitigation: Analyzers use fast predicate filters; incremental generator with caching

  - risk: User wants to disable for specific call sites
    mitigation: "Future: MSBuild properties for opt-out (InstrumentGenAi, InstrumentDb)"

  - risk: Token extraction fails for custom response types
    mitigation: extractUsage is nullable; graceful degradation (span still created)

  - risk: DbCommand.CommandText is null/empty
    mitigation: Emit tag only if non-empty; no exception

  - risk: Infinite recursion from intercepting generated code
    mitigation: Skip *.g.cs files in analyzer predicate

  - risk: Duplicate provider definitions across files
    mitigation: ProviderRegistry as SSOT (implemented)
```

## Out of Scope (v1)

```yaml
out_of_scope:
  - item: Streaming responses (ChatCompletionStreaming)
    reason: Complex lifetime management

  - item: GenAI prompt/response content capture
    reason: Privacy concerns, opt-in for v2

  - item: Metrics (only tracing in v1)
    reason: Add counters/histograms in v2

  - item: Custom provider extensibility
    reason: Users can't add their own patterns yet

  - item: gRPC instrumentation
    reason: OTel already provides AddGrpcClientInstrumentation()
```

## Implementation Phases

### Phase 1: Runtime Library ✅ COMPLETE

```yaml
phase: 1
name: Runtime Library
status: complete
items:
  - file: Instrumentation/ActivitySources.cs
    status: done
  - file: Instrumentation/SemanticConventions.cs
    status: done
  - file: Instrumentation/GenAi/TokenUsage.cs
    status: done
  - file: Instrumentation/GenAi/GenAiInstrumentation.cs
    status: done
  - file: Instrumentation/Db/DbInstrumentation.cs
    status: done
  - file: ANcpSdkServiceDefaults.cs (update)
    status: done
    change: Added .AddSource("ANcpSdk.*")
```

### Phase 2: Generator Infrastructure ✅ COMPLETE

```yaml
phase: 2
name: Generator Infrastructure
status: complete
items:
  - file: Models/GenAiInvocationInfo.cs
    status: done
  - file: Models/DbInvocationInfo.cs
    status: done
  - file: Models/ProviderInfo.cs
    status: done
  - file: Analyzers/ProviderDetector.cs
    status: done
  - file: Analyzers/GenAiCallSiteAnalyzer.cs
    status: done
  - file: Analyzers/DbCallSiteAnalyzer.cs
    status: done
```

### Phase 3: Emitters ✅ COMPLETE

```yaml
phase: 3
name: Emitters
status: complete
items:
  - file: Emitters/GenAiInterceptorEmitter.cs
    status: done
  - file: Emitters/DbInterceptorEmitter.cs
    status: done
```

### Phase 4: Generator Integration ✅ COMPLETE

```yaml
phase: 4
name: Generator Integration
status: complete
items:
  - file: ServiceDefaultsSourceGenerator.cs
    status: done
    pipelines:
      - name: Build() interception
        output: Intercepts.g.cs
      - name: GenAI instrumentation
        output: GenAiIntercepts.g.cs
      - name: Database instrumentation
        output: DbIntercepts.g.cs
      - name: OTel tags (bonus)
        output: OTelTagExtensions.g.cs
```

### Bonus Phase: SSOT & OTel Tags ✅ COMPLETE

```yaml
phase: bonus
name: SSOT & OTel Tags
status: complete
items:
  - file: Models/ProviderRegistry.cs
    status: done
    purpose: Single source of truth for all provider definitions
  - file: Models/ProviderDefinition.cs
    status: done
  - file: Models/OTelTagInfo.cs
    status: done
  - file: Instrumentation/OTelAttribute.cs
    status: done
  - file: Analyzers/OTelTagAnalyzer.cs
    status: done
  - file: Emitters/OTelTagsEmitter.cs
    status: done
```

### Phase 5: Tests ⏳ PENDING

```yaml
phase: 5
name: Tests
status: pending
note: Existing SDK tests pass (296/296), but dedicated instrumentation tests not yet written
items:
  - file: GenAiInterceptorGeneratorTests.cs
    status: pending
  - file: DbInterceptorGeneratorTests.cs
    status: pending
  - file: GenAiInstrumentationTests.cs
    status: pending
  - file: DbInstrumentationTests.cs
    status: pending
  - file: EndToEndTracingTests.cs
    status: pending
```

## Progress Summary

```yaml
progress:
  overall: 85%
  phases:
    - phase: 1
      name: Runtime Library
      completion: 100%
    - phase: 2
      name: Generator Infrastructure
      completion: 100%
    - phase: 3
      name: Emitters
      completion: 100%
    - phase: 4
      name: Generator Integration
      completion: 100%
    - phase: bonus
      name: SSOT & OTel Tags
      completion: 100%
    - phase: 5
      name: Dedicated Tests
      completion: 0%

  test_status:
    sdk_tests: 296 passed, 0 failed, 2 skipped
    dedicated_instrumentation_tests: not yet implemented
```

## User Experience

### Before (Manual)

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

### After (Automatic)

```csharp
var client = new ChatClient("gpt-4o", apiKey);
var result = await client.CompleteChatAsync(messages);  // Spans emitted automatically
```

## Testing Strategy

Uses `ANcpLua.Roslyn.Utilities.Testing`:

```csharp
using var result = await Test<ServiceDefaultsSourceGenerator>.Run(engine => engine
    .WithSource(source)
    .WithReference(typeof(ChatClient).Assembly)
    .WithReference(typeof(ANcpSdkServiceDefaults).Assembly));

result
    .Produces("GenAiInterceptors.g.cs")
    .File("GenAiInterceptors.g.cs", content =>
    {
        Assert.Contains("[InterceptsLocation(", content);
        Assert.Contains("GenAiInstrumentation.ExecuteAsync(", content);
    })
    .IsCached()
    .IsClean();
```