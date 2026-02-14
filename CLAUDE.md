# CLAUDE.md - ANcpLua.NET.Sdk

Opinionated MSBuild SDK providing standardized defaults, policy enforcement, and analyzer injection for .NET projects.

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
    |       +-> SourceGenerators.props  # Source generator/analyzer configuration
    |
    +-> Enforcement.props          # BANNED packages policy
    +-> DeterminismAndSourceLink.props  # Reproducible builds

Sdk.targets
    |
    +-> Microsoft.NET.Sdk[.Web]    # Base SDK targets
    +-> ItemMetadata.targets       # Auto-apply item metadata
    +-> Deduplication.targets      # Remove duplicate items
    +-> ArtifactStaging.targets    # Output organization
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
| `PolySharp`              | Polyfills in separate package              | Use `ANcpLua.Roslyn.Utilities.Polyfills` |
| `FluentAssertions`       | License concerns                           | Use `AwesomeAssertions`              |
| `Microsoft.NET.Test.Sdk` | Only when MTP enabled                      | MTP doesn't need VSTest              |
| `DisableTransitiveProjectReferences` | Breaks CPM          | Use `CentralPackageTransitivePinningEnabled` |

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
2. Injects `xunit.v3.mtp-v2`
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
tests/
  ANcpLua.Sdk.Tests/  # SDK behavior tests
tools/
  SdkGenerator/       # Generates Sdk.props/Sdk.targets
  ConfigFilesGenerator/ # Generates editorconfig
eng/                  # Build infrastructure
```
