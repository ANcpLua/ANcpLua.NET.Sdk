# CLAUDE.md - ANcpLua.NET.Sdk

MSBuild SDK for opinionated .NET project configuration.

## 🏗️ Ecosystem Position

```
LAYER 0: ANcpLua.Roslyn.Utilities  ← UPSTREAM (no SDK dependency!)
         ↓ publishes .Sources
LAYER 1: ANcpLua.NET.Sdk           ← YOU ARE HERE (SOURCE OF TRUTH)
         ↓ auto-syncs Version.props
LAYER 2: ANcpLua.Analyzers         ← DOWNSTREAM (uses SDK)
         ↓ consumed by
LAYER 3: qyl, other projects       ← END USERS
```

### This Repo: LAYER 1 (Source of Truth)

| Property | Value |
|----------|-------|
| **Upstream dependencies** | ANcpLua.Roslyn.Utilities.Sources |
| **Downstream consumers** | ANcpLua.Analyzers, qyl, all SDK consumers |
| **Version.props** | SOURCE - all versions defined here |
| **Auto-sync** | SENDS to Analyzers (symlink/copy) |

---

## Build Commands

```bash
# Build SDK
./build.ps1

# Test
dotnet test tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj

# Pack (creates .nupkg in artifacts/)
./build.ps1 -Pack
```

## Structure

```
src/
  common/           # Shared props/targets
    Version.props   # ⭐ SINGLE SOURCE OF TRUTH for all versions
    Common.props    # Core SDK behavior
  Enforcement/      # Determinism, SourceLink
  Shared/           # Polyfills, shared code
tests/
  ANcpLua.Sdk.Tests/      # SDK behavior tests
  ANcpLua.Sdk.Canary/     # Integration tests
```

## Key Features

- Auto-injects ANcpLua.Analyzers
- Central Package Management (CPM) support
- Polyfills for netstandard2.0 (StringExtensions, etc.)
- Deterministic builds + SourceLink
- MTP (Microsoft Testing Platform) auto-detection

## Banned APIs (enforced by BannedApiAnalyzers)

See `src/Enforcement/BannedSymbols.txt` for full list.
Use `TimeProvider` instead of legacy time APIs.
Use `System.Text.Json` instead of Newtonsoft.

## Version.props Variables

All package versions are centralized in `src/common/Version.props`:

```xml
<RoslynVersion>5.0.0</RoslynVersion>
<XunitV3Version>3.2.1</XunitV3Version>
<AwesomeAssertionsVersion>9.3.0</AwesomeAssertionsVersion>
<!-- ... etc -->
```

Downstream repos (Analyzers) should copy/symlink this file.
