# ANcpLua.NET.Sdk

MSBuild SDK with opinionated defaults for .NET projects. Inspired by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk).

## SDKs

| SDK | Base SDK | Use For |
|-----|----------|---------|
| `ANcpLua.NET.Sdk` | `Microsoft.NET.Sdk` | Libraries, Console Apps, Workers, Unit Tests |
| `ANcpLua.NET.Sdk.Web` | `Microsoft.NET.Sdk.Web` | Web APIs, ASP.NET Core, Integration Tests |

## Installation

Replace your SDK reference:

```xml
<!-- Libraries / Console / Workers -->
<Project Sdk="ANcpLua.NET.Sdk/1.1.0">

<!-- Web APIs / ASP.NET Core -->
<Project Sdk="ANcpLua.NET.Sdk.Web/1.1.0">
```

Or use `global.json` for centralized version management:

```json
{
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "1.1.0",
    "ANcpLua.NET.Sdk.Web": "1.1.0"
  }
}
```

Then reference without version:

```xml
<Project Sdk="ANcpLua.NET.Sdk">
```

## Working Features

### Banned API Analyzer (RS0030)

Enforces best practices via `BannedSymbols.txt`:

| Banned                                 | Use Instead                       |
|----------------------------------------|-----------------------------------|
| `DateTime.Now/UtcNow`                  | `TimeProvider.System.GetUtcNow()` |
| `DateTimeOffset.Now/UtcNow`            | `TimeProvider.System.GetUtcNow()` |
| `ArgumentNullException.ThrowIfNull`    | Native .NET 6+ API (polyfilled) |
| `Enumerable.Any(predicate)`            | `List<T>.Exists()`                |
| `Enumerable.FirstOrDefault(predicate)` | `List<T>.Find()`                  |
| `InvariantCulture` comparisons         | `Ordinal`                         |
| `System.Tuple`                         | `ValueTuple`                      |
| `Math.Round` (no rounding mode)        | Overload with `MidpointRounding`  |
| Local time file APIs                   | UTC variants                      |
| Newtonsoft.Json                        | System.Text.Json                  |

### ANcpLua.Analyzers (Bundled)

| Rule    | Description                                              |
|---------|----------------------------------------------------------|
| QYL0001 | `lock` keyword → Use `Lock.EnterScope()` (.NET 9+)       |
| QYL0002 | Deprecated OTel GenAI attributes → Use OTel 1.38 names   |

### Extensions (Opt-in)

| Property                       | Description                                                  | Default |
|--------------------------------|--------------------------------------------------------------|---------|
| `GenerateClaudeMd`             | Auto-generates `CLAUDE.md` linking to repo root              | `false` |
| `InjectSharedThrow`            | Injects `Throw.IfNull()` guard clause helper                 | `false` |
| `InjectStringOrdinalComparer`  | Injects internal `StringOrdinalComparer`                     | `false` |
| `InjectFakeLogger`             | Injects `FakeLoggerExtensions` (requires `FakeLogCollector`) | `false` |
| `InjectSourceGenHelpers`       | Injects Roslyn symbol extensions                             | `false` |
| `InjectCommonComparers`        | Injects common comparers                                     | `false` |
| `EnableANcpLuaExtensions`      | Enables all general extensions                               | `false` |
| `EnableANcpLuaSharedResources` | Enables shared engineering resources                         | `false` |
| `EnableANcpLuaSharedWorkflows` | Enables shared workflows                                     | `false` |

### Polyfills (Opt-in for Legacy TFMs)

| Property                                 | Description                                                         | Default |
|------------------------------------------|---------------------------------------------------------------------|---------|
| `InjectLockPolyfill`                     | Injects `System.Threading.Lock` (net8.0 backport)                   | `false` |
| `InjectTimeProviderPolyfill`             | Injects `System.TimeProvider`                                       | `false` |
| `InjectIndexRangeOnLegacy`               | Injects `Index` and `Range` types                                   | `false` |
| `InjectIsExternalInitOnLegacy`           | Injects `IsExternalInit` (for records)                              | `false` |
| `InjectTrimAttributesOnLegacy`           | Injects trimming attributes (e.g. `DynamicallyAccessedMembers`)     | `false` |
| `InjectNullabilityAttributesOnLegacy`    | Injects nullability attributes (e.g. `AllowNull`)                   | `false` |
| `InjectRequiredMemberOnLegacy`           | Injects `RequiredMemberAttribute`                                   | `false` |
| `InjectCompilerFeatureRequiredOnLegacy`  | Injects `CompilerFeatureRequiredAttribute`                          | `false` |
| `InjectCallerAttributesOnLegacy`         | Injects `CallerArgumentExpressionAttribute`                         | `false` |
| `InjectUnreachableExceptionOnLegacy`     | Injects `UnreachableException`                                      | `false` |
| `InjectExperimentalAttributeOnLegacy`    | Injects `ExperimentalAttribute`                                     | `false` |
| `InjectParamCollectionOnLegacy`          | Injects `ParamCollectionAttribute`                                  | `false` |
| `InjectStackTraceHiddenOnLegacy`         | Injects `StackTraceHiddenAttribute`                                 | `false` |
| `InjectThrowPolyfillsOnLegacy`           | Injects C# 14 ThrowPolyfills (extension members)                    | `false` |

### C# 14 Extension Member Polyfills

Modern throw helpers using C# 14 `extension(Type)` syntax. Available on all targets:

```csharp
// .NET 6+ style APIs - work everywhere!
ArgumentNullException.ThrowIfNull(myArg);
ArgumentException.ThrowIfNullOrEmpty(myString);
ArgumentException.ThrowIfNullOrWhiteSpace(myString);
ArgumentOutOfRangeException.ThrowIfNegative(count);
ArgumentOutOfRangeException.ThrowIfZero(count);
ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
ArgumentOutOfRangeException.ThrowIfGreaterThan(value, max);
ArgumentOutOfRangeException.ThrowIfLessThan(value, min);
ObjectDisposedException.ThrowIf(isDisposed, this);
```

## Configuration

| Property                      | Default     | Description                           |
|-------------------------------|-------------|---------------------------------------|
| `GenerateClaudeMd`            | `false`     | Generate CLAUDE.md for AI assistants  |
| `IncludeDefaultBannedSymbols` | `true`      | Include BannedSymbols.txt             |
| `BannedNewtonsoftJsonSymbols` | `true`      | Ban Newtonsoft.Json                   |
| `EnableDefaultTestSettings`   | `true`      | Auto-configure test runner            |
| `EnableCodeCoverage`          | `true` (CI) | Enable coverage                       |

### Web Service Defaults (Auto-Registered for Web Projects)

When using `Microsoft.NET.Sdk.Web`, the SDK automatically adds Aspire 13.0-compatible service defaults:

| Feature | Description |
|---------|-------------|
| **OpenTelemetry** | Logging, Metrics (ASP.NET, HTTP, Runtime), Tracing with OTLP export |
| **Health Checks** | `/health` (readiness) and `/alive` (liveness) endpoints |
| **Service Discovery** | Microsoft.Extensions.ServiceDiscovery enabled |
| **HTTP Resilience** | Standard resilience handlers with retries and circuit breakers |

Opt-out: `<AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>`

## License

MIT

---

## Credits

- Inspired by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk) by Gérald Barré
- ServiceDefaults pattern from [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
