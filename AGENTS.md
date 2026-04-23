# AGENTS.md — ANcpLua.NET.Sdk

Opinionated MSBuild SDK providing standardized defaults, policy enforcement, and analyzer injection for .NET projects. `CLAUDE.md` symlinks here.

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

Common.targets  (via BeforeMicrosoftNETSdkTargets)
    |
    +-> SourceGenerators.targets    # Auto-pin RoslynVersion for generators
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
`$(RoslynVersion)` from `Version.props`) via `VersionOverride`, keeping CPM
enabled. Override the version:

```xml
<PropertyGroup>
  <SourceGeneratorRoslynVersion>4.11.0</SourceGeneratorRoslynVersion>
</PropertyGroup>
```

### Opt-out: self-describe the Roslyn reference (SDK 3.2.0+)

The implicit injection is fragile under IDE language servers (Rider, Roslyn LSP,
OmniSharp): if the LSP evaluates `Sdk.props` before `Common.props` imports take
effect, `Microsoft.CodeAnalysis.*` disappears from the compilation even though
`dotnet build` succeeds. Consumers hitting this can opt out of the implicit
injection and declare the reference explicitly:

```xml
<Project Sdk="ANcpLua.NET.Sdk">
  <PropertyGroup>
    <DisableImplicitRoslynPackageReference>true</DisableImplicitRoslynPackageReference>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp"
                      VersionOverride="$(SourceGeneratorRoslynVersion)"
                      PrivateAssets="all" />
  </ItemGroup>
</Project>
```

The opt-out flag is a no-op on SDK <= 3.1.0 (which always injects the reference);
consumers on older SDKs that need the opt-out must either upgrade to 3.2.0+ or
use `<PackageReference Remove="Microsoft.CodeAnalysis.CSharp"/>` as a bridge
before their explicit `Include`.

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
    Common/                          # Core props/targets
    Enforcement/                     # Policy, determinism, version checks
  Config/                            # Flat since v3.3.0 — was nested
    Analyzer.*.editorconfig          # per-analyzer rule tuning
    BannedSymbols.txt                # BannedApiAnalyzers lists
    BannedSymbols.NewtonsoftJson.txt
    ANcpLua.NET.Sdk.*.editorconfig   # SDK-variant-specific style
    Global.editorconfig              # SDK global (is_global=true)
    CodingStyle.editorconfig
    NamingConvention.editorconfig
    Compiler.editorconfig
    GeneratedFiles.editorconfig
    default.runsettings
  Sdk/                               # SDK entry points per variant
    ANcpLua.NET.Sdk/
    ANcpLua.NET.Sdk.Test/
    ANcpLua.NET.Sdk.Web/
tests/
  ANcpLua.Sdk.Tests/                 # SDK behavior tests
tools/
  SdkGenerator/                      # Generates Sdk.props/Sdk.targets
  ConfigFilesGenerator/              # Generates Config/*.editorconfig
```

**Packing discipline**: nuspec `<file>` entries must be **enumerated**, not
globbed (`<file src="Config/*"/>` expanded to zero matches under Windows pack
agents in v3.3.0 — see `src/*.nuspec` for the enumerated pattern as of v3.3.1).

## Release workflow notes

- Git tags use `v`-prefix (`v3.3.1`). CI's `gh release create` creates the tag
  with `v` prefix so it matches the human/CLI push convention — no more duplicate
  bare `3.x.x` tags. Historical duplicates before v3.3.1 (`v3.1.0` vs `3.1.0`,
  `v3.2.0` vs `3.2.0`) are left in place; only the workflow going forward is fixed.
- `Must Publish Packages` gate checks `'*.nuspec' 'src/**/*' 'tests/**/*'`. Pre-3.3.1
  runs omitted `tests/**/*`, so test-only fixes silently skipped publish (deploy
  job still reported "success" with zero steps executed). If you edit ONLY a
  workflow file or docs, `has_changes` goes false by design — touch a file under
  one of the watched paths to force a publish.
