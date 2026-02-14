# CLAUDE.md - src/

SDK source files: MSBuild props/targets, configuration.

## Directory Structure

```
src/
  Build/
    Common/             # Core .props/.targets files
    Enforcement/        # Policy enforcement targets
    Packaging/          # NuGet packaging infrastructure
  Config/               # EditorConfig files, BannedSymbols.txt
  Sdk/                  # SDK entry points (one per variant)
    ANcpLua.NET.Sdk/
    ANcpLua.NET.Sdk.Web/
    ANcpLua.NET.Sdk.Test/
```

## SDK Entry Points

Each SDK variant has its own `Sdk.props` and `Sdk.targets` in `src/Sdk/<variant>/`:

| Variant              | Base SDK              | Key Differences                         |
|----------------------|-----------------------|-----------------------------------------|
| ANcpLua.NET.Sdk      | Microsoft.NET.Sdk     | Standard .NET libraries/apps            |
| ANcpLua.NET.Sdk.Web  | Microsoft.NET.Sdk.Web | ASP.NET Core web projects               |
| ANcpLua.NET.Sdk.Test | Microsoft.NET.Sdk     | Sets `IsTestProject=true` unconditionally|

## Build/ Directory

### Common/

| File                        | Purpose                                               |
|-----------------------------|-------------------------------------------------------|
| `Version.props`             | **SOURCE OF TRUTH** for all package versions          |
| `Common.props`              | Core defaults (LangVersion, Nullable, analyzers)      |
| `Common.targets`            | Build-time targets, CLAUDE.md generation              |
| `GlobalPackages.props`      | GlobalPackageReference for analyzer injection         |
| `SourceGenerators.props`    | Source generator/analyzer project configuration       |
| `ContinuousIntegrationBuild.props` | CI detection and configuration                 |
| `ItemDefaults.props`        | Default metadata for common item types                |
| `InternalsVisibleTo.props`  | Simplified IVT syntax                                 |
| `TerminalLogger.props`      | Terminal logger configuration                         |
| `BuildCheck.props`          | MSBuild BuildCheck integration                        |
| `Tests.targets`             | Test-specific targets                                 |
| `ItemMetadata.targets`      | Auto-apply metadata to items                          |
| `Deduplication.targets`     | Remove duplicate items                                |
| `ArtifactStaging.targets`   | Output organization                                   |
| `Npm.targets`               | NPM integration for web projects                      |

### Enforcement/

| File                          | Purpose                                        |
|-------------------------------|------------------------------------------------|
| `Enforcement.props`           | BANNED package detection (PolySharp, FluentAssertions, etc.) |
| `DeterminismAndSourceLink.props` | Reproducible builds, git metadata wiring    |
| `VersionEnforcement.targets`  | Version consistency enforcement                |

### Packaging/

| File                   | Purpose                                             |
|------------------------|-----------------------------------------------------|
| `AnalyzersPack.targets`| Layout enforcement for analyzer NuGet packages      |

## Regenerating SDK Files

The SDK entry points (`Sdk.props`, `Sdk.targets`) are generated:

```bash
dotnet run --project tools/SdkGenerator
```

This reads the structure from `src/Build/` and generates appropriate import chains for each SDK variant.

## Key Conventions

1. **Props vs Targets**: Props files set properties/defaults, targets files define build actions
2. **Guard patterns**: Use `Condition="'$(Property)' == ''"` to allow consumer override
3. **GlobalPackageReference**: Used for analyzer injection (immutable when CPM enabled)
4. **Roslyn Utilities**: Use `<UseRoslynUtilities>true</UseRoslynUtilities>` for source generator helpers
5. **Polyfills**: Migrated to `ANcpLua.Roslyn.Utilities.Polyfills` NuGet package
