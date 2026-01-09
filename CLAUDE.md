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

## ⚠️ Common CI Errors

### SDK Version Not Found
```
error: Unable to find package ANcpLua.NET.Sdk with version (= X.X.X)
```

**Cause:** global.json references SDK version not yet published to NuGet.

**Fix Options:**
1. **Downgrade:** Change global.json to latest published version
2. **Publish:** Tag and push in SDK repo: `git tag vX.X.X && git push --tags`

**Prevention:** Always publish SDK BEFORE syncing version to downstream repos.

### Release Order (CRITICAL!)
```
1. Roslyn.Utilities → publish to NuGet
2. SDK → update Version.props → publish to NuGet
3. THEN sync Version.props to Analyzers
4. Analyzers → can now build
```
