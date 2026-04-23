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
    +-> Version.props              # Package version constants (source of truth)
    +-> GlobalPackages.props       # GlobalPackageReference (analyzers, SBOM)
    +-> Microsoft.NET.Sdk[.Web]    # Base SDK import
    +-> Enforcement.props          # Policy enforcement
    +-> DeterminismAndSourceLink.props  # Reproducible builds

Common.props  (via CustomBeforeDirectoryBuildProps)
    |
    +-> ContinuousIntegrationBuild.props  # CI detection
    +-> SourceGenerators.props            # Auto-pin Roslyn 5.0.0 for generators

Common.targets  (via BeforeMicrosoftNETSdkTargets)
    |
    +-> Tests.targets               # Test framework injection (IsTestProject)
    +-> Npm.targets                 # Opt-in npm integration
    +-> VersionEnforcement.targets  # AL0018 import check
```

## Auto-Detected Properties

| Property                   | Detection Logic                                                          |
|----------------------------|--------------------------------------------------------------------------|
| `IsTestProject`            | `ANcpLuaSdkName == ANcpLua.NET.Sdk.Test`, or root Directory.Build.targets name match |
| `ANcpLuaSingleFileApp`     | `FileBasedProgram == true` (.NET 10+ `#:sdk` directive)                   |
| `_IsSourceGeneratorProject` | Project name contains `generator` or `analyzer` (case-insensitive), or `IsRoslynComponent=true`, or `IsSourceGenerator=true` |

## Core Defaults (Common.props)

| Property                              | Value        | Notes                                    |
|---------------------------------------|--------------|------------------------------------------|
| `LangVersion`                         | `latest`     | Always use latest C# features            |
| `Nullable`                            | `enable`     | NRTs enabled by default                  |
| `ImplicitUsings`                      | `enable`     | Global usings enabled                    |
| `Deterministic`                       | `true`       | Reproducible builds                      |
| `EnableNETAnalyzers`                  | `true`       | .NET analyzers enabled                   |
| `AnalysisLevel`                       | `latest-all` | All analysis rules                       |
| `TreatWarningsAsErrors`               | `true`       | In CI or Release builds                  |
| `ManagePackageVersionsCentrally`      | `true`       | CPM required                             |
| `CentralPackageTransitivePinningEnabled` | `true`    | Transitive pinning enabled               |

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
| `AwesomeAssertions.Analyzers`          | Test assertion best practices (test projects only) |

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

## Source Generator Roslyn Pin

Projects whose name contains `generator` or `analyzer` get
`Microsoft.CodeAnalysis.CSharp` pinned to `SourceGeneratorRoslynVersion` (default
`5.0.0`) via `VersionOverride`, keeping CPM enabled. Override:

```xml
<PropertyGroup>
  <SourceGeneratorRoslynVersion>4.11.0</SourceGeneratorRoslynVersion>
</PropertyGroup>
```

## First-Party Library References

The SDK does NOT pin or auto-inject `ANcpLua.Roslyn.Utilities`, `ANcpLua.Agents`,
or `ANcpLua.Analyzers`. Consumers reference them like any other NuGet dep.

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="ANcpLua.Roslyn.Utilities.Sources" Version="$(ANcpLuaRoslynUtilitiesVersion)"/>

<!-- consumer csproj (netstandard2.0) -->
<PackageReference Include="ANcpLua.Roslyn.Utilities.Sources" PrivateAssets="all"/>
```

For runtime consumers (net10.0+) use `ANcpLua.Roslyn.Utilities` instead of `.Sources`.

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

`src/Build/Common/Version.props` defines ALL package versions used across the SDK
ecosystem.

## Directory Structure

```
src/
  Build/
    Common/           # Core props/targets
    Enforcement/      # Policy enforcement, determinism, version checks
  Config/
    Analyzers/        # Analyzer editorconfig
    BannedSymbols/    # BannedApiAnalyzers txt files
    Style/            # EditorConfig code style
  Sdk/                # SDK entry points per variant
    ANcpLua.NET.Sdk/
    ANcpLua.NET.Sdk.Test/
    ANcpLua.NET.Sdk.Web/
tests/
  ANcpLua.Sdk.Tests/  # SDK behavior tests
tools/
  SdkGenerator/       # Generates Sdk.props/Sdk.targets
  ConfigFilesGenerator/ # Generates editorconfig
```
