# CLAUDE.md - eng/

Build engineering infrastructure and shared source files for the SDK.

## Directory Structure

```
eng/
  Extensions/           # Optional injectable helpers
    Comparers/          # StringOrdinalComparer
    FakeLogger/         # Test logging helpers
    SourceGen/          # Source generator utilities
  LegacySupport/        # Polyfill source files (netstandard2.0)
  MSBuild/              # MSBuild polyfills and utilities
    Polyfills/          # MSBuild-level polyfills
  Shared/               # Always-injected utilities
    Throw/              # Guard clause utilities
```

## Polyfill Injection Flow

```
LegacySupport.props (src/Build/Common/)
    |
    +-> Defines switches: InjectIndexRangeOnLegacy, InjectTimeProviderPolyfill, etc.
    +-> All default to 'false' (opt-in)
    |
    v
LegacySupport.targets (src/Build/Common/)
    |
    +-> Expands bundles (InjectAllPolyfillsOnLegacy -> individual switches)
    +-> Computes TFM needs: _NeedsNet5Polyfills, _NeedsNet7Polyfills, etc.
    +-> Conditionally adds: <Compile Include="$(SharedSourcePath)Polyfills/..."/>
    |
    v
src/shared/Polyfills/**/*.cs
    |
    +-> Actual polyfill implementations with #if guards
```

## Analyzer Injection Flow

```
GlobalPackages.props (src/Build/Common/)
    |
    +-> CPM enabled: GlobalPackageReference (immutable)
    +-> CPM disabled: PackageReference fallback
    |
    +-> ANcpLua.Analyzers
    +-> Microsoft.CodeAnalysis.BannedApiAnalyzers
    +-> JonSkeet.RoslynAnalyzers
    +-> Microsoft.Sbom.Targets (if IsPackable=true)
    +-> AwesomeAssertions.Analyzers (if IsTestProject=true)
```

## Test Project Detection Flow

```
Testing.props (src/Testing/)
    |
    +-> Property-time detection:
    |   - IsTestProject: Name ends with .Tests/.Test OR directory contains 'tests'
    |   - IsIntegrationTestProject: Directory contains 'Integration' or 'E2E'
    |   - IsAnalyzerTestProject: Name contains 'Analyzer'
    |
    +-> When IsTestProject=true:
        - OutputType=Exe (MTP)
        - Injects xunit.v3.mtp-v2, AwesomeAssertions
        - Adds global usings: Xunit, AwesomeAssertions
        - Configures TRX reporting
    |
    v
_AncpLuaDetectIntegrationTestReferences (target)
    |
    +-> Target-time detection: references to .Web/.Api projects
```

## Shared Code Injection Flow

```
Common.props (src/Build/Common/)
    |
    +-> InjectSharedThrow defaults to 'true'
    |
    v
LegacySupport.targets
    |
    +-> When InjectSharedThrow=true:
        - Adds <Compile Include="$(SharedSourcePath)Throw/Throw.cs"/>
        - Adds <Using Include="Microsoft.Shared.Diagnostics"/>
```

## Decision Guide: Where to Edit

| Task                              | Location                                              |
|-----------------------------------|-------------------------------------------------------|
| Add new polyfill                  | 1. Create src/shared/Polyfills/NewPolyfill/*.cs       |
|                                   | 2. Add switch in src/Build/Common/LegacySupport.props |
|                                   | 3. Add conditional in src/Build/Common/LegacySupport.targets |
| Ban new API                       | src/Config/BannedSymbols.txt                          |
| Add new analyzer package          | src/Build/Common/GlobalPackages.props                 |
| Modify Throw helpers              | src/shared/Throw/Throw.cs                             |
| Add new injectable extension      | 1. Create src/shared/Extensions/NewExt/*.cs           |
|                                   | 2. Add switch in LegacySupport.props                  |
|                                   | 3. Add conditional in LegacySupport.targets           |
| Change test detection logic       | src/Testing/Testing.props                             |
| Add new SDK property default      | src/Build/Common/Common.props                         |
| Add policy enforcement            | src/Build/Enforcement/Enforcement.props               |

## Key Files Reference

| File                              | Responsibility                                    |
|-----------------------------------|---------------------------------------------------|
| `src/Build/Common/Version.props`  | All package version constants                     |
| `src/Build/Common/Common.props`   | Core SDK defaults (LangVersion, Nullable, etc.)   |
| `src/Build/Common/GlobalPackages.props` | Analyzer injection via GlobalPackageReference|
| `src/Build/Common/LegacySupport.props` | Polyfill switch definitions                  |
| `src/Build/Common/LegacySupport.targets` | Polyfill conditional injection             |
| `src/Build/Enforcement/Enforcement.props` | Banned package detection                  |
| `src/Testing/Testing.props`       | Test project detection and configuration          |
| `src/Config/BannedSymbols.txt`    | Banned API list for BannedApiAnalyzers            |
