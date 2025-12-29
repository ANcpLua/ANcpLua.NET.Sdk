# ANcpLua.NET.Sdk

MSBuild SDK with opinionated defaults for .NET projects. Inspired
by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk).

## SDKs

| SDK                   | Base SDK                | Use For                                      |
|-----------------------|-------------------------|----------------------------------------------|
| `ANcpLua.NET.Sdk`     | `Microsoft.NET.Sdk`     | Libraries, Console Apps, Workers, Unit Tests |
| `ANcpLua.NET.Sdk.Web` | `Microsoft.NET.Sdk.Web` | Web APIs, ASP.NET Core, Integration Tests    |

## Installation

Replace your SDK reference:

```xml
<!-- Libraries / Console / Workers -->
<Project Sdk="ANcpLua.NET.Sdk/1.2.0"></Project>

<!-- Web APIs / ASP.NET Core -->
<Project Sdk="ANcpLua.NET.Sdk.Web/1.2.0"></Project>
```

Or use `global.json` for centralized version management:

```json
{
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "1.2.0",
    "ANcpLua.NET.Sdk.Web": "1.2.0"
  }
}
```

Then reference without version:

```xml

<Project Sdk="ANcpLua.NET.Sdk"></Project>
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

See [ANcpLua.Analyzers](https://nuget.org/packages/ANcpLua.Analyzers) for all 13 rules.

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
| `InjectSourceGenHelpers`      | Roslyn source generator utilities ([details](eng/Extensions/SourceGen/README.md)) | `false` |

<details>
<summary><b>SourceGen Helpers</b> - what's included</summary>

- `EquatableArray<T>` - IEquatable wrapper for ImmutableArray (incremental generator caching)
- `DiagnosticInfo` / `DiagnosticsExtensions` - Simplified diagnostic creation
- `SymbolExtensions` - `HasAttribute`, `GetAttribute`, `IsOrInheritsFrom`, `ImplementsInterface`, `GetMethod`, `GetProperty`
- `SyntaxExtensions` - `GetMethodName`, `GetIdentifierName`, `HasModifier`
- `SemanticModelExtensions` - `IsConstant`, `AllConstant`, `GetConstantValueOrDefault`
- `CompilationExtensions` - `IsCSharp9OrGreater`, `IsCSharp10OrGreater`, `IsCSharp11OrGreater`
- `SyntaxValueProvider` helpers - `ForClassesWithAttribute`, `ForMethodsWithAttribute`
- `EnumerableExtensions` - `SelectManyOrEmpty`, `OrEmpty`, `ToImmutableArrayOrEmpty`, `HasDuplicates`
- `FileExtensions` - `WriteIfChangedAsync` (avoid unnecessary file writes)
- `LocationInfo`, `EquatableMessageArgs` - Equatable records for caching

**Note:** For analyzers, CLI tools, or test projects, use [ANcpLua.Roslyn.Utilities](https://nuget.org/packages/ANcpLua.Roslyn.Utilities) NuGet package instead. The embedded source approach is specifically for source generators which cannot easily reference NuGet packages.

</details>

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
| `InjectThrowPolyfillsOnLegacy`          | Injects C# 14 ThrowPolyfills (extension members)                | `false` |

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

Opt-out: `<AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>`

## Running Tests Correctly

The test suite simulates an environment where the SDK is packed and consumed. To avoid performance issues caused by parallel tests trying to repack the SDK simultaneously, use the following command:

```bash
# 1. Ensure artifacts are built (if running for the first time)
# Note: The test fixture will auto-build if artifacts/nuget is empty,
# but for optimal performance, set up the environment variables.

# 2. Run tests pointing to the artifacts directory
export CI=true
export NUGET_DIRECTORY="$(pwd)/artifacts"
dotnet test tests/ANcpLua.Sdk.Tests
```

## License

MIT

---

## Credits

- Inspired by [Meziantou.NET.Sdk](https://github.com/meziantou/Meziantou.NET.Sdk) by Gérald Barré
- ServiceDefaults pattern from [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
