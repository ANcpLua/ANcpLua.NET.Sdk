# CLAUDE.md - ANcpLua.NET.Sdk

Custom MSBuild SDK providing opinionated defaults, polyfills, and analyzers for .NET projects.

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
| **Upstream dependencies** | ANcpLua.Roslyn.Utilities.Sources (see Directory.Packages.props) |
| **Downstream consumers** | ANcpLua.Analyzers, qyl, all SDK users |
| **Version.props** | SOURCE (canonical) |
| **Auto-sync** | SENDS to Analyzers via GitHub Action |

---

## Build Commands

```bash
# Build
dotnet build

# Pack (creates NuGet packages)
pwsh ./build.ps1 -Version <major.minor.patch>

# Test
dotnet test
```

## Published Packages

| Package | Description |
|---------|-------------|
| `ANcpLua.NET.Sdk` | Main SDK (`Sdk="ANcpLua.NET.Sdk"`) |
| `ANcpLua.NET.Sdk.Test` | Test projects with xUnit v3 MTP |
| `ANcpLua.NET.Sdk.Web` | Web projects with ASP.NET Core |

## Key Files

```
src/
├── common/
│   ├── Version.props          ← SOURCE OF TRUTH for all versions
│   ├── Common.props           ← LangVersion, Nullable, Analyzers
│   ├── Common.targets         ← Analyzer package injection
│   ├── LegacySupport.props    ← Polyfill switches
│   ├── LegacySupport.targets  ← Polyfill file injection
│   ├── Shared.props           ← Utility switches
│   └── BannedSymbols.txt      ← API enforcement
├── Sdk/
│   ├── Sdk.props              ← SDK entry point
│   └── Sdk.targets
└── Testing/
    └── Testing.props          ← xUnit v3 MTP auto-injection
```

## Features Provided to Consumers

- **Polyfills:** Index/Range, IsExternalInit, StringExtensions (netstandard2.0)
- **Analyzers:** ANcpLua.Analyzers, Meziantou.Analyzer, BannedApiAnalyzers
- **BannedSymbols:** DateTime.Now, Newtonsoft.Json, object locks
- **LangVersion:** Forces `latest`
- **Nullable:** Enabled by default
- **Deterministic:** Reproducible builds

## Version.props Auto-Sync

When `src/common/Version.props` changes:
1. GitHub Action triggers
2. PR created in ANcpLua.Analyzers
3. Merge updates Analyzers versions

**Workflow:** `.github/workflows/sync-versions.yml`

## NuGet Feeds

```xml
<packageSources>
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  <add key="dotnet-tools" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" />
</packageSources>
```

The `dotnet-tools` feed is required for beta versions of `Microsoft.CodeAnalysis.Testing`.
