# CLAUDE.md - ANcpLua.NET.Sdk

Opinionated MSBuild SDK providing standardized defaults, policy enforcement, polyfills, and analyzer injection for .NET projects.

## SDK Variants

| Package              | Base SDK              | Purpose                            |
|----------------------|-----------------------|------------------------------------|
| ANcpLua.NET.Sdk      | Microsoft.NET.Sdk     | Standard .NET libraries/apps       |
| ANcpLua.NET.Sdk.Web  | Microsoft.NET.Sdk.Web | ASP.NET Core web projects          |
| ANcpLua.NET.Sdk.Test | Microsoft.NET.Sdk     | Test projects (xUnit v3 MTP)       |

## Import Chain

```
Sdk.props
    |
    +-> Version.props              # Package version constants
    +-> GlobalPackages.props       # GlobalPackageReference (analyzers, SBOM)
    |       |
    |       +-> ANcpLua.Analyzers, BannedApiAnalyzers, JonSkeet.RoslynAnalyzers
    |       +-> Microsoft.Sbom.Targets (if IsPackable=true)
    |       +-> AwesomeAssertions.Analyzers (if IsTestProject=true)
    |
    +-> Microsoft.NET.Sdk[.Web]    # Base SDK import
    |
    +-> ItemDefaults.props         # AdditionalFiles, InternalsVisibleTo
    +-> InternalsVisibleTo.props   # Simplified IVT syntax
    +-> TerminalLogger.props       # Terminal logger configuration
    +-> BuildCheck.props           # MSBuild BuildCheck integration
    |
    +-> Common.props               # Core defaults (see below)
    |       |
    |       +-> ContinuousIntegrationBuild.props
    |       +-> LegacySupport.props  # Polyfill switch definitions
    |
    +-> Enforcement.props          # BANNED packages policy
    +-> DeterminismAndSourceLink.props  # Reproducible builds
    +-> Testing.props              # Test project detection (ANcpLua.NET.Sdk, ANcpLua.NET.Sdk.Test only)

Sdk.targets
    |
    +-> Microsoft.NET.Sdk[.Web]    # Base SDK targets
    +-> ItemMetadata.targets       # Auto-apply item metadata
    +-> Deduplication.targets      # Remove duplicate items
    +-> ArtifactStaging.targets    # Output organization
    +-> LegacySupport.targets      # Polyfill file injection
```

## Auto-Detected Properties

| Property                   | Detection Logic                                                          |
|----------------------------|--------------------------------------------------------------------------|
| `IsTestProject`            | Name ends with `.Tests`/`.Test` OR directory contains `tests`            |
| `IsIntegrationTestProject` | Directory contains `Integration` or `E2E` OR references `.Web`/`.Api`    |
| `IsAnalyzerTestProject`    | Name contains `Analyzer` or `Analyzers`                                  |
| `_ANcpLuaInGitRepo`        | `.git` folder found via `GetDirectoryNameOfFileAbove`                    |

## Core Defaults (Common.props)

| Property                              | Value      | Notes                                      |
|---------------------------------------|------------|--------------------------------------------|
| `LangVersion`                         | `latest`   | Always use latest C# features              |
| `Nullable`                            | `enable`   | NRTs enabled by default                    |
| `ImplicitUsings`                      | `enable`   | Global usings enabled                      |
| `Deterministic`                       | `true`     | Reproducible builds                        |
| `EnableNETAnalyzers`                  | `true`     | .NET analyzers enabled                     |
| `AnalysisLevel`                       | `latest-all` | All analysis rules                       |
| `TreatWarningsAsErrors`               | `true`     | In CI or Release builds                    |
| `ManagePackageVersionsCentrally`      | `true`     | CPM required                               |
| `CentralPackageTransitivePinningEnabled` | `true`  | Transitive pinning enabled                 |

## Banned Packages (Policy Enforcement)

| Package                  | Reason                                     | Alternative                          |
|--------------------------|--------------------------------------------|--------------------------------------|
| `PolySharp`              | SDK provides polyfills                     | Use `Inject*OnLegacy` properties     |
| `FluentAssertions`       | License concerns                           | Use `AwesomeAssertions`              |
| `Microsoft.NET.Test.Sdk` | Only when MTP enabled                      | MTP doesn't need VSTest              |
| `DisableTransitiveProjectReferences` | Breaks CPM          | Use `CentralPackageTransitivePinningEnabled` |

## Polyfill Opt-In

All polyfills are **opt-in** (default: `false`). Set in your project file:

```xml
<PropertyGroup>
  <!-- Individual polyfills -->
  <InjectIndexRangeOnLegacy>true</InjectIndexRangeOnLegacy>
  <InjectIsExternalInitOnLegacy>true</InjectIsExternalInitOnLegacy>
  <InjectRequiredMemberOnLegacy>true</InjectRequiredMemberOnLegacy>
  <InjectCallerAttributesOnLegacy>true</InjectCallerAttributesOnLegacy>
  <InjectUnreachableExceptionOnLegacy>true</InjectUnreachableExceptionOnLegacy>
  <InjectStringExtensionsPolyfill>true</InjectStringExtensionsPolyfill>
  <InjectTimeProviderPolyfill>true</InjectTimeProviderPolyfill>

  <!-- OR use bundle for all polyfills -->
  <InjectAllPolyfillsOnLegacy>true</InjectAllPolyfillsOnLegacy>
</PropertyGroup>
```

### Polyfill Reference

| Property                              | Adds                                       | Required When         |
|---------------------------------------|--------------------------------------------|-----------------------|
| `InjectIndexRangeOnLegacy`            | `Index`, `Range` structs                   | before netcoreapp3.1  |
| `InjectIsExternalInitOnLegacy`        | `IsExternalInit` (records)                 | before net5.0         |
| `InjectRequiredMemberOnLegacy`        | `required` keyword support                 | before net7.0         |
| `InjectCallerAttributesOnLegacy`      | `CallerArgumentExpression`                 | before net6.0         |
| `InjectUnreachableExceptionOnLegacy`  | `UnreachableException`                     | before net7.0         |
| `InjectExperimentalAttributeOnLegacy` | `ExperimentalAttribute`                    | before net8.0         |
| `InjectNullabilityAttributesOnLegacy` | `MaybeNull`, `NotNull`, etc.               | before netcoreapp3.1  |
| `InjectTrimAttributesOnLegacy`        | AOT/Trim attributes                        | before net5.0         |
| `InjectStringExtensionsPolyfill`      | `string.Contains(StringComparison)`        | before netcoreapp2.1  |
| `InjectTimeProviderPolyfill`          | `TimeProvider` abstract class              | before net8.0         |
| `InjectLockPolyfill`                  | `Lock` class                               | before net10.0        |
| `InjectSharedThrow`                   | Guard clause utilities                     | All TFMs (default: true) |

## Extension Opt-In

```xml
<PropertyGroup>
  <!-- Individual extensions -->
  <InjectSharedThrow>true</InjectSharedThrow>          <!-- Guard clauses (enabled by default) -->
  <InjectSourceGenHelpers>true</InjectSourceGenHelpers> <!-- EquatableArray, DiagnosticInfo -->
  <InjectCommonComparers>true</InjectCommonComparers>   <!-- StringOrdinalComparer -->
  <InjectFakeLogger>true</InjectFakeLogger>             <!-- Test logging helpers -->

  <!-- OR use bundle for all extensions -->
  <InjectAllExtensions>true</InjectAllExtensions>
</PropertyGroup>
```

## Analyzer Injection

Analyzers are injected via `GlobalPackageReference` (immutable when CPM enabled):

| Analyzer                               | Purpose                          |
|----------------------------------------|----------------------------------|
| `ANcpLua.Analyzers`                    | Custom code quality rules        |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` | Banned API enforcement       |
| `JonSkeet.RoslynAnalyzers`             | NodaTime best practices          |
| `AwesomeAssertions.Analyzers`          | Test assertion best practices    |

Opt-out: `<DisableANcpLuaAnalyzers>true</DisableANcpLuaAnalyzers>`

## Test Project Configuration

When `IsTestProject=true`, the SDK automatically:

1. Sets `OutputType=Exe` (for MTP)
2. Injects `xunit.v3.mtp-v2` and `Meziantou.Xunit.v3.ParallelTestFramework`
3. Injects `AwesomeAssertions` and its analyzer
4. Adds global usings: `Xunit`, `AwesomeAssertions`
5. Configures TRX reporting: `--report-xunit-trx`

Opt-out of xUnit injection (for TUnit/NUnit/MSTest):
```xml
<PropertyGroup>
  <SkipXunitInjection>true</SkipXunitInjection>
</PropertyGroup>
```

## Roslyn Utilities Integration

For source generators targeting netstandard2.0:

```xml
<PropertyGroup>
  <UseRoslynUtilities>true</UseRoslynUtilities>
</PropertyGroup>
```

This adds `ANcpLua.Roslyn.Utilities.Sources` (embedded) for netstandard2.0 or `ANcpLua.Roslyn.Utilities` (runtime) for newer TFMs.

For analyzer/generator testing:

```xml
<PropertyGroup>
  <UseRoslynUtilitiesTesting>true</UseRoslynUtilitiesTesting>
</PropertyGroup>
```

## Consumer Usage

### global.json

```json
{
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "<version>",
    "ANcpLua.NET.Sdk.Web": "<version>",
    "ANcpLua.NET.Sdk.Test": "<version>"
  }
}
```

### Project File

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Build Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Pack (creates NuGet packages)
pwsh ./build.ps1 -Version <major.minor.patch>
```

## Version.props: Source of Truth

`src/Build/Common/Version.props` defines ALL package versions used across the SDK ecosystem:

- ANcpLua.NET.Sdk (this repo)
- ANcpLua.Roslyn.Utilities (via symlink)
- ANcpLua.Analyzers (via auto-sync GitHub Action)

## Directory Structure

```
src/
  Build/
    Common/           # Core props/targets
    Enforcement/      # Policy enforcement
    Packaging/        # NuGet packaging
  Config/             # EditorConfig, BannedSymbols
  Sdk/                # SDK entry points per variant
  Testing/            # Test SDK infrastructure
  shared/             # Injectable source files
tests/
  ANcpLua.Sdk.Tests/  # SDK behavior tests
tools/
  SdkGenerator/       # Generates Sdk.props/Sdk.targets
  ConfigFilesGenerator/ # Generates editorconfig
eng/                  # Build infrastructure
```
