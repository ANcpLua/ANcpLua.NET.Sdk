# CLAUDE.md - src/

SDK source files: MSBuild props/targets, configuration, and injectable code.

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
  Testing/              # Test project infrastructure
    AotTesting/         # AOT/Trim testing MSBuild orchestration
  shared/               # Injectable source files
    Polyfills/          # BCL polyfills for older TFMs
    Extensions/         # Utility extensions (Comparers, CodeFixes)
    Throw/              # Guard clause utilities
```

## SDK Entry Points

Each SDK variant has its own `Sdk.props` and `Sdk.targets` in `src/Sdk/<variant>/`:

| Variant              | Base SDK              | Key Differences                         |
|----------------------|-----------------------|-----------------------------------------|
| ANcpLua.NET.Sdk      | Microsoft.NET.Sdk     | Imports Testing.props                   |
| ANcpLua.NET.Sdk.Web  | Microsoft.NET.Sdk.Web | No Testing.props (web apps aren't tests)|
| ANcpLua.NET.Sdk.Test | Microsoft.NET.Sdk     | Sets `IsTestProject=true` unconditionally|

## Build/ Directory

### Common/

| File                        | Purpose                                               |
|-----------------------------|-------------------------------------------------------|
| `Version.props`             | **SOURCE OF TRUTH** for all package versions          |
| `Common.props`              | Core defaults (LangVersion, Nullable, analyzers)      |
| `Common.targets`            | Build-time targets, CLAUDE.md generation              |
| `GlobalPackages.props`      | GlobalPackageReference for analyzer injection         |
| `LegacySupport.props`       | Polyfill switch definitions (all default to false)    |
| `LegacySupport.targets`     | Conditional polyfill file injection                   |
| `ContinuousIntegrationBuild.props` | CI detection and configuration                 |
| `ItemDefaults.props`        | Default metadata for common item types                |
| `InternalsVisibleTo.props`  | Simplified IVT syntax                                 |
| `TerminalLogger.props`      | Terminal logger configuration                         |
| `BuildCheck.props`          | MSBuild BuildCheck integration                        |
| `Tests.targets`             | Test-specific targets (Microsoft.Extensions.Diagnostics.Testing) |
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

## Testing/ Directory

| File                          | Purpose                                      |
|-------------------------------|----------------------------------------------|
| `Testing.props`               | Test project detection and auto-configuration|
| `AotTesting/AotTesting.props` | AOT/Trim testing infrastructure              |

## shared/ Directory

Injectable source files organized by category:

```
shared/
  Polyfills/
    DiagnosticAttributes/   # NullableAttributes.cs
    Diagnostics/            # StackTraceHiddenAttribute.cs
    Exceptions/             # UnreachableException.cs
    Experimental/           # ExperimentalAttribute.cs
    IndexRange/             # Index.cs, Range.cs
    LanguageFeatures/       # IsExternalInit, RequiredMember, CallerArgumentExpression, ParamCollection
    NullabilityAttributes/  # MemberNotNullAttributes.cs
    StringExtensions/       # StringExtensions.cs (Contains, Replace with StringComparison)
    TimeProvider/           # TimeProvider.cs
    TrimAttributes/         # AOT/Trim attributes
  MSBuild/Polyfills/
    DiagnosticClasses.cs    # ExcludeFromCodeCoverageAttribute polyfill
    Lock.cs                 # Lock class polyfill
  Extensions/
    Comparers/              # StringOrdinalComparer.cs
    CodeFixes/              # CodeFixProviderBase, SyntaxModifierExtensions
    DiagnosticAnalyzerBase.cs  # Base class for Roslyn analyzers
  Throw/
    Throw.cs                # Guard clause utilities (Microsoft.Shared.Diagnostics.Throw)
```

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
4. **Polyfills**: All opt-in via `Inject*OnLegacy` properties
5. **TFM detection**: Use `$(_NeedsNet*Polyfills)` computed properties in LegacySupport.targets
