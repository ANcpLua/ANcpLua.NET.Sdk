# CLAUDE.md

## Quick Reference

```bash
# Build & Pack
pwsh ./build.ps1 -Version 1.3.15

# Test (MTP, not VSTest)
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj

# Filter tests
dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj --filter-method "*SomeTest*"

# Lint MSBuild files
./scripts/lint-dotnet.sh .
```

## SDK Usage

```xml
<!-- Library/Console/Worker -->
<Project Sdk="ANcpLua.NET.Sdk/1.3.15" />

<!-- Web API (auto-registers ServiceDefaults) -->
<Project Sdk="ANcpLua.NET.Sdk.Web/1.3.15" />

<!-- Tests (auto-injects xunit, assertions, fixtures) -->
<Project Sdk="ANcpLua.NET.Sdk.Test/1.3.15" />
```

## What Gets Auto-Injected

| SDK | Auto-Injected |
|-----|---------------|
| Base | Throw.IfNull, BannedSymbols, Polyfills |
| Web | OpenTelemetry, HealthChecks, Resilience, DevLogs |
| Test | xunit.v3.mtp-v2, AwesomeAssertions, FakeLogger |

**Integration tests** (detected by `Integration/` or `E2E/` folder, or refs `.Web`/`.Api`):
→ Microsoft.AspNetCore.Mvc.Testing + base classes

**Analyzer tests** (detected by `*Analyzer*` name):
→ Analyzer.Testing + CodeFix.Testing + fixtures

**GitHub Actions** (detected by `GITHUB_ACTIONS=true`):
→ GitHubActionsTestLogger

## Samples (Use These as Reference)

| What | Sample File |
|------|-------------|
| Test fixture setup | `tests/ANcpLua.Sdk.Tests/Helpers/ProjectBuilder.cs` |
| Build result parsing | `tests/ANcpLua.Sdk.Tests/Helpers/BuildResult.cs` |
| Integration test base | `eng/Shared/IntegrationTestBase.cs` |
| Analyzer test fixture | `eng/Shared/AnalyzerTest.cs` |
| FakeLogger extensions | `eng/Extensions/FakeLogger/FakeLoggerExtensions.cs` |
| Banned APIs list | `src/configuration/BannedSymbols.txt` |
| MTP detection logic | `src/common/MtpDetection.targets` |
| Test injection logic | `src/Testing/Testing.props` |

## Opt-In Features

```xml
<PropertyGroup>
  <InjectSourceGenHelpers>true</InjectSourceGenHelpers>   <!-- Roslyn utilities -->
  <InjectXunitLogger>true</InjectXunitLogger>             <!-- ITestOutputHelper → ILogger -->
  <InjectCompilerHelper>true</InjectCompilerHelper>       <!-- Source generator testing -->
  <SkipXunitInjection>true</SkipXunitInjection>           <!-- For TUnit/NUnit/MSTest -->
</PropertyGroup>
```

## Directory Structure

```
src/Sdk/           → SDK entry points (Sdk.props, Sdk.targets)
src/common/        → Shared MSBuild logic (Version.props, Tests.targets)
src/Testing/       → Test project detection & injection
eng/Shared/        → Embedded source files (fixtures, helpers)
eng/Extensions/    → FakeLogger, SourceGen helpers
tests/             → SDK validation tests
```

---

## MSBuild Linter (Auto-Enforced)

The linter runs automatically after editing `.props`, `.targets`, `.csproj` files.

**Rules:**
- **A**: No hardcoded versions in Directory.Packages.props
- **B**: Version.props single owner (Directory.Packages.props)
- **G**: No inline `Version="X.Y.Z"` in PackageReference

**Manual run:** `./scripts/lint-dotnet.sh .`

---

## Absolute Rules

### 1. Version.props is append-only
Add/update variables only. Never delete or restructure.

### 2. Use CPM variables
```xml
<!-- ❌ --> <PackageVersion Include="Serilog" Version="4.3.0"/>
<!-- ✅ --> <PackageVersion Include="Serilog" Version="$(SerilogVersion)"/>
```

### 3. Never reorder imports
Import order in `.props`/`.targets` is load-bearing.

### 4. One MSBuild file per edit
Edit → Build → Verify → Then edit another.

### 5. Banned patterns
- `RunAnalyzers=false`
- `Version="X.Y.Z"` in PackageReference
- `NoWarn` for new warnings

### 6. When in doubt, ask
These rules override helpfulness. Ask before breaking them.

---

## MTP vs VSTest

This repo uses **MTP** (xunit.v3.mtp-v2). VSTest syntax doesn't work.

| VSTest | MTP |
|--------|-----|
| `--filter "Name~Foo"` | `--filter-method "*Foo*"` |
| `--logger "trx"` | `--report-xunit-trx` |

---

## CI Workflow

```yaml
- uses: actions/checkout@v6
- uses: actions/setup-dotnet@v5
- run: mkdir -p artifacts
- run: pwsh ./build.ps1 -Version ${{ version }}
- run: dotnet test --project tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj
```

Order matters. `artifacts/` must exist before restore.
