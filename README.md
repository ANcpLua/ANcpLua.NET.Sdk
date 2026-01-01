[![NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.NET.Sdk/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-7C3AED)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.NET.Sdk

MSBuild SDK with opinionated defaults for .NET projects. Inspired
by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk).

## SDKs

| SDK                   | Base SDK                | Use For                                      |
|-----------------------|-------------------------|----------------------------------------------|
| `ANcpLua.NET.Sdk`     | `Microsoft.NET.Sdk`     | Libraries, Console Apps, Workers, Unit Tests |
| `ANcpLua.NET.Sdk.Web` | `Microsoft.NET.Sdk.Web` | Web APIs, ASP.NET Core, Integration Tests    |

## Installation

Add a `global.json` to your repo root:

```json
{
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "*",
    "ANcpLua.NET.Sdk.Web": "*"
  }
}
```

Then replace your SDK reference:

```xml
<Project Sdk="ANcpLua.NET.Sdk"></Project>
```

For Web projects:

```xml
<Project Sdk="ANcpLua.NET.Sdk.Web"></Project>
```

## Working Features

### Banned API Analyzer (RS0030)

Enforces best practices via `BannedSymbols.txt`:

| Banned                                 | Use Instead                       |
|----------------------------------------|-----------------------------------|
| `DateTime.Now/UtcNow`                  | `TimeProvider.System.GetUtcNow()` |
| `DateTimeOffset.Now/UtcNow`            | `TimeProvider.System.GetUtcNow()` |
| `ArgumentNullException.ThrowIfNull`    | Native .NET 6+ API (polyfilled)   |
| `Enumerable.Any(predicate)`            | `List<T>.Exists()`                |
| `Enumerable.FirstOrDefault(predicate)` | `List<T>.Find()`                  |
| `InvariantCulture` comparisons         | `Ordinal`                         |
| `System.Tuple`                         | `ValueTuple`                      |
| `Math.Round` (no rounding mode)        | Overload with `MidpointRounding`  |
| Local time file APIs                   | UTC variants                      |
| Newtonsoft.Json                        | System.Text.Json                  |

### ANcpLua.Analyzers (Bundled)

| Rule   | Severity | Description                                          |
|--------|----------|------------------------------------------------------|
| AL0001 | Error    | Prohibit reassignment of primary constructor params  |
| AL0003 | Error    | Don't divide by constant zero                        |
| AL0011 | Warning  | Avoid `lock` on non-Lock types (.NET 9+)             |
| AL0012 | Warning  | Deprecated OTel semantic convention attribute        |
| AL0013 | Info     | Missing telemetry schema URL                         |

See [ANcpLua.Analyzers](https://nuget.org/packages/ANcpLua.Analyzers) for all 16 rules.

### Extensions (Auto-Enabled by Default)

| Property                      | Description                                     | Default    |
|-------------------------------|-------------------------------------------------|------------|
| `GenerateClaudeMd`            | Auto-generates `CLAUDE.md` linking to repo root | **`true`** |
| `InjectSharedThrow`           | Injects `Throw.IfNull()` guard clause helper    | **`true`** |
| `IncludeDefaultBannedSymbols` | Include BannedSymbols.txt                       | **`true`** |
| `BanNewtonsoftJsonSymbols`    | Ban Newtonsoft.Json direct usage                | **`true`** |

### Extensions (Opt-in)

| Property                      | Description                                                  | Default |
|-------------------------------|--------------------------------------------------------------|---------|
| `InjectStringOrdinalComparer` | Injects internal `StringOrdinalComparer`                     | `false` |
| `InjectFakeLogger`            | Injects `FakeLoggerExtensions` (requires `FakeLogCollector`) | `false` |
| `InjectSourceGenHelpers`      | Roslyn source generator utilities ([details](https://github.com/ANcpLua/ANcpLua.NET.Sdk/blob/main/eng/Extensions/SourceGen/README.md)) | `false` |

**SourceGen Helpers include:** `EquatableArray<T>`, `DiagnosticInfo`, `DiagnosticsExtensions`, `SymbolExtensions`, `SyntaxExtensions`, `SemanticModelExtensions`, `CompilationExtensions`, `SyntaxValueProvider` helpers, `EnumerableExtensions`, `FileExtensions`, `LocationInfo`, `EquatableMessageArgs`

> For analyzers, CLI tools, or test projects, use [ANcpLua.Roslyn.Utilities](https://nuget.org/packages/ANcpLua.Roslyn.Utilities) NuGet package instead.

### Analyzer Test Fixtures (Auto-Injected)

Test projects with "Analyzer" in their name automatically receive:

| Package                                        | Purpose                          |
|------------------------------------------------|----------------------------------|
| `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` | Analyzer test infrastructure   |
| `Microsoft.CodeAnalysis.CSharp.CodeFix.Testing`  | Code fix test infrastructure   |
| `Basic.Reference.Assemblies.Net100`              | .NET 10 reference assemblies   |
| `Basic.Reference.Assemblies.NetStandard20`       | NetStandard 2.0 references     |

**Base classes injected into `ANcpLua.Testing.Analyzers` namespace:**

```csharp
// Analyzer tests
public class MyAnalyzerTests : AnalyzerTest<MyAnalyzer> {
    [Fact]
    public async Task Test() => await VerifyAsync("source code...");
}

// Code fix tests
public class MyCodeFixTests : CodeFixTest<MyAnalyzer, MyCodeFix> {
    [Fact]
    public async Task Test() => await VerifyAsync("source", "fixed");
}

// Code fix with EditorConfig
public class MyConfigTests : CodeFixTestWithEditorConfig<MyAnalyzer, MyCodeFix> {
    [Fact]
    public async Task Test() => await VerifyAsync(
        source, fixed,
        editorConfig: new() { ["my_option"] = "value" });
}
```

**Opt-out:** `<InjectAnalyzerTestFixtures>false</InjectAnalyzerTestFixtures>`

### Polyfills (Opt-in for Legacy TFMs)

| Property                                | Description                                                     | Default |
|-----------------------------------------|-----------------------------------------------------------------|---------|
| `InjectLockPolyfill`                    | Injects `System.Threading.Lock` (net8.0 backport)               | `false` |
| `InjectTimeProviderPolyfill`            | Injects `System.TimeProvider`                                   | `false` |
| `InjectIndexRangeOnLegacy`              | Injects `Index` and `Range` types                               | `false` |
| `InjectIsExternalInitOnLegacy`          | Injects `IsExternalInit` (for records)                          | `false` |
| `InjectTrimAttributesOnLegacy`          | Injects trimming attributes (e.g. `DynamicallyAccessedMembers`) | `false` |
| `InjectNullabilityAttributesOnLegacy`   | Injects nullability attributes (e.g. `AllowNull`)               | `false` |
| `InjectRequiredMemberOnLegacy`          | Injects `RequiredMemberAttribute`                               | `false` |
| `InjectCompilerFeatureRequiredOnLegacy` | Injects `CompilerFeatureRequiredAttribute`                      | `false` |
| `InjectCallerAttributesOnLegacy`        | Injects `CallerArgumentExpressionAttribute`                     | `false` |
| `InjectUnreachableExceptionOnLegacy`    | Injects `UnreachableException`                                  | `false` |
| `InjectExperimentalAttributeOnLegacy`   | Injects `ExperimentalAttribute`                                 | `false` |
| `InjectParamCollectionOnLegacy`         | Injects `ParamCollectionAttribute`                              | `false` |
| `InjectStackTraceHiddenOnLegacy`        | Injects `StackTraceHiddenAttribute`                             | `false` |

## Configuration

| Property                      | Default     | Description                          |
|-------------------------------|-------------|--------------------------------------|
| `GenerateClaudeMd`            | **`true`**  | Generate CLAUDE.md for AI assistants |
| `InjectSharedThrow`           | **`true`**  | Inject Throw.IfNull() guard clauses  |
| `IncludeDefaultBannedSymbols` | **`true`**  | Include BannedSymbols.txt            |
| `BanNewtonsoftJsonSymbols`    | **`true`**  | Ban Newtonsoft.Json direct usage     |
| `EnableDefaultTestSettings`   | `true`      | Auto-configure test runner           |
| `EnableCodeCoverage`          | `true` (CI) | Enable coverage                      |

### Web Service Defaults (Auto-Registered for Web Projects)

When using `Microsoft.NET.Sdk.Web`, the SDK automatically adds Aspire 13.0-compatible service defaults:

| Feature               | Description                                                         |
|-----------------------|---------------------------------------------------------------------|
| **OpenTelemetry**     | Logging, Metrics (ASP.NET, HTTP, Runtime), Tracing with OTLP export |
| **Health Checks**     | `/health` (readiness) and `/alive` (liveness) endpoints             |
| **Service Discovery** | Microsoft.Extensions.ServiceDiscovery enabled                       |
| **HTTP Resilience**   | Standard resilience handlers with retries and circuit breakers      |
| **DevLogs**           | Frontend console log bridge for unified debugging (Development only)|

Opt-out: `<AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>`

### DevLogs - Frontend Console Bridge

Captures browser `console.log/warn/error` and sends to server logs. Enabled by default in Development.

**Add to your HTML** (only served in Development):
```html
<script src="/dev-logs.js"></script>
```

**All frontend logs appear in server output with `[BROWSER]` prefix:**
```
info: DevLogEntry[0] [BROWSER] User clicked button
error: DevLogEntry[0] [BROWSER] Failed to fetch data
```

**Configuration:**
```csharp
builder.UseANcpSdkConventions(options =>
{
    options.DevLogs.Enabled = true;           // Default: true
    options.DevLogs.RoutePattern = "/api/dev-logs"; // Default
    options.DevLogs.EnableInProduction = false;     // Default: false
});
```

**Why this matters:** AI agents can see frontend and backend logs in one place without using browser MCP (which burns tokens and is slow).

## Repository Requirements

This SDK enforces the following repository-level configurations:

### Directory.Packages.props (Required)

```xml
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
</PropertyGroup>
```

### Directory.Build.props (Recommended)

```xml
<PropertyGroup>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <Deterministic>true</Deterministic>
</PropertyGroup>
<PropertyGroup Condition="'$(CI)' == 'true'">
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>
```

**Note:** `LangVersion` and `Nullable` are SDK-owned properties. Place them in `Directory.Build.props`, NOT in individual csproj files.

## Running Tests

```bash
export CI=true
export NUGET_DIRECTORY="$(pwd)/artifacts"
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj
```
