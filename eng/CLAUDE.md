# CLAUDE.md - eng/

SDK implementation: props, targets, and injectable code.

## Directory Map

```
eng/
├── MSBuild/              # Core .props/.targets (SDK entry points)
├── LegacySupport/        # Polyfill source files (netstandard2.0)
├── Extensions/           # Optional helpers (FakeLogger, SourceGen, Comparers)
├── Shared/               # Always-injected code (Throw)
└── ANcpSdk.AspNetCore.*/ # Web SDK service defaults (source generator)
```

## Core Wiring Flows

### 1. Polyfill Injection (netstandard2.0 support)

```
LegacySupport.props         → Sets switches (InjectIndexRange, InjectTimeProviderPolyfill, etc.)
       ↓
LegacySupport.targets       → Reads switches, conditionally adds <Compile Include="..."/>
       ↓
eng/LegacySupport/**/*.cs   → Actual polyfill source files
```

Key files:

- `MSBuild/LegacySupport.props` - switch definitions with defaults
- `MSBuild/LegacySupport.targets` - conditional file injection
- `LegacySupport/*/` - one folder per polyfill (IndexRange, TimeProvider, etc.)

### 2. Analyzer Injection

```
Common.targets              → Adds PackageReference to ANcpLua.Analyzers
       ↓
Condition: IncludeANcpLuaAnalyzers != false
```

Key files:

- `MSBuild/Common.targets` - analyzer package injection
- `MSBuild/BannedSymbols.txt` - banned API list (legacy time APIs, Newtonsoft, etc.)

### 3. Test Project Detection (MTP auto-config)

```
Testing.props               → Detects xunit.v3.mtp-v2 package reference
       ↓
Sets OutputType=Exe, TestingPlatform=true
```

Key files:

- `MSBuild/Testing.props` - MTP detection and property setting
- Detection: looks for `xunit.v3.mtp-v2` in package references

### 4. Shared Code Injection

```
Shared.props                → Reads InjectSharedThrow (default: true)
       ↓
Shared.targets              → Adds <Compile Include="Throw.cs"/>
       ↓
eng/Shared/Throw/Throw.cs   → Guard clause utilities
```

Optional injections:

- `InjectSourceGenHelpers=true` → eng/Extensions/SourceGen/*
- `InjectFakeLogger=true` → eng/Extensions/FakeLogger/*

## Decision Guide

| Task                             | Where to Edit                                                                                                            |
|----------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| Add new polyfill                 | Create `eng/LegacySupport/NewPolyfill/`, add switch in `LegacySupport.props`, add conditional in `LegacySupport.targets` |
| Ban new API                      | Add to `MSBuild/BannedSymbols.txt`                                                                                       |
| Add new analyzer package         | Edit `Common.targets` PackageReference section                                                                           |
| Modify Throw helpers             | Edit `eng/Shared/Throw/Throw.cs`                                                                                         |
| Add new injectable extension     | Create folder in `eng/Extensions/`, add switch in `Shared.props`, add conditional in `Shared.targets`                    |
| Modify ServiceDefaults           | See `eng/ANcpSdk.AspNetCore.ServiceDefaults/` and `eng/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/`                 |
| Add new instrumentation provider | Add to `ProviderRegistry.cs`, update `DbInstrumentation.MapTypeNameToDbSystem` if DB                                     |

## 5. ServiceDefaults Auto-Registration (Web SDK)

The Web SDK includes a source generator that automatically instruments method calls at compile time.

### Architecture

```
ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/  ← Source Generator (netstandard2.0)
├── Models/
│   ├── ProviderRegistry.cs      ← SSOT for all provider definitions
│   ├── GenAiInvocationInfo.cs   ← GenAI call site model
│   ├── DbInvocationInfo.cs      ← Database call site model
│   └── OTelTagInfo.cs           ← [OTel] attribute model
├── Analyzers/
│   ├── ProviderDetector.cs      ← Detects referenced providers
│   ├── GenAiCallSiteAnalyzer.cs ← Finds GenAI SDK calls
│   ├── DbCallSiteAnalyzer.cs    ← Finds DbCommand calls
│   └── OTelTagAnalyzer.cs       ← Finds [OTel] attributes
└── Emitters/
    ├── GenAiInterceptorEmitter.cs ← Generates GenAI interceptors
    ├── DbInterceptorEmitter.cs    ← Generates DB interceptors
    └── OTelTagsEmitter.cs         ← Generates SetTag extensions

ANcpSdk.AspNetCore.ServiceDefaults/  ← Runtime Library (net10.0)
└── Instrumentation/
    ├── ActivitySources.cs       ← Centralized ActivitySource definitions
    ├── SemanticConventions.cs   ← OTel semantic convention constants
    ├── OTelAttribute.cs         ← [OTel] marker attribute
    ├── GenAi/
    │   ├── GenAiInstrumentation.cs ← Execute/ExecuteAsync wrappers
    │   └── TokenUsage.cs           ← Token count record
    └── Db/
        └── DbInstrumentation.cs    ← DbCommand wrappers
```

### Generator Pipelines

The `ServiceDefaultsSourceGenerator` has 4 pipelines:

| Pipeline                 | Output                   | Purpose                                |
|--------------------------|--------------------------|----------------------------------------|
| Build() interception     | `Intercepts.g.cs`        | Auto-registers service defaults        |
| GenAI instrumentation    | `GenAiIntercepts.g.cs`   | Wraps OpenAI/Anthropic/etc. calls      |
| Database instrumentation | `DbIntercepts.g.cs`      | Wraps DbCommand.Execute* calls         |
| OTel tags                | `OTelTagExtensions.g.cs` | Generates Activity.SetTag() extensions |

### ProviderRegistry (SSOT)

All provider definitions live in `ProviderRegistry.cs`:

```csharp
// GenAI providers
OpenAi, Anthropic, AzureOpenAi, Ollama, GoogleAi, VertexAi

// Database providers
DuckDb, SqliteMicrosoft, SqliteSystem, PostgreSql, MySqlConnector,
MySqlData, SqlServerMicrosoft, SqlServerSystem, Oracle, Firebird
```

Each provider includes:

- `ProviderId` - OTel semantic convention value
- `AssemblyName` - For compile-time detection
- `TypeContains` - For runtime type matching
- `TokenUsage` - Property paths for token extraction (GenAI only)

### [OTel] Attribute Usage

Mark properties with semantic convention names:

```csharp
public record ChatRequest(
    [OTel("gen_ai.request.model")] string Model,
    [OTel("gen_ai.request.max_tokens")] int? MaxTokens);
```

Generated extension method:

```csharp
activity.SetTagsFromChatRequest(request);
```

## Documentation

Full reference: https://ancplua.mintlify.app/sdk/overview