# CLAUDE.md - ANcpLua.NET.Sdk

Custom MSBuild SDK providing opinionated defaults, polyfills, and analyzers for .NET projects.

## Current Package Versions

| Package | Version |
|---------|---------|
| ANcpLua.NET.Sdk | 1.6.28 |
| ANcpLua.Roslyn.Utilities | 1.18.3 |
| ANcpLua.Roslyn.Utilities.Sources | 1.18.3 |
| ANcpLua.Roslyn.Utilities.Testing | 1.18.3 |
| ANcpLua.Analyzers | 1.10.2 |
| Roslyn | 5.0.0 |
| OpenTelemetry | 1.15.0 |
| Microsoft.Extensions | 10.2.0 |
| AspNetCore | 10.0.2 |
| xunit.v3 | 3.2.2 |

## Ecosystem Position

```
LAYER 0: ANcpLua.Roslyn.Utilities  <-- UPSTREAM (no SDK dependency!)
         | publishes .Sources
LAYER 1: ANcpLua.NET.Sdk           <-- YOU ARE HERE (SOURCE OF TRUTH)
         | auto-syncs Version.props
LAYER 2: ANcpLua.Analyzers         <-- DOWNSTREAM (uses SDK)
         | consumed by
LAYER 3: qyl, other projects       <-- END USERS
```

### This Repo: LAYER 1 (Source of Truth)

| Property                  | Value                                                           |
|---------------------------|-----------------------------------------------------------------|
| **Upstream dependencies** | ANcpLua.Roslyn.Utilities.Sources (see Directory.Packages.props) |
| **Downstream consumers**  | ANcpLua.Analyzers, qyl, all SDK users                           |
| **Version.props**         | SOURCE (canonical)                                              |
| **Auto-sync**             | SENDS to Analyzers via GitHub Action                            |

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

| Package                | Description                        |
|------------------------|------------------------------------|
| `ANcpLua.NET.Sdk`      | Main SDK (`Sdk="ANcpLua.NET.Sdk"`) |
| `ANcpLua.NET.Sdk.Test` | Test projects with xUnit v3 MTP    |
| `ANcpLua.NET.Sdk.Web`  | Web projects with ASP.NET Core     |

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

eng/
├── ANcpSdk.AspNetCore.ServiceDefaults/           ← Runtime library (net10.0)
│   └── Instrumentation/                          ← OTel instrumentation helpers
└── ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/ ← Source generator (netstandard2.0)
    └── Models/ProviderRegistry.cs                ← SSOT for provider definitions
```

## Features Provided to Consumers

- **Polyfills:** Index/Range, IsExternalInit, StringExtensions (netstandard2.0)
- **Analyzers:** ANcpLua.Analyzers, Meziantou.Analyzer, BannedApiAnalyzers
- **BannedSymbols:** Legacy time APIs, legacy JSON libraries, object locks
- **LangVersion:** Forces `latest`
- **Nullable:** Enabled by default
- **Deterministic:** Reproducible builds

### Web SDK Additional Features

- **Auto-instrumentation:** GenAI (OpenAI, Anthropic, etc.) and Database (Npgsql, etc.) calls
- **[OTel] attribute:** Compile-time Activity.SetTag() extension generation
- **Service defaults:** OpenTelemetry, health endpoints, HTTP resilience

## Version.props Auto-Sync

When `src/common/Version.props` changes:

1. GitHub Action triggers
2. PR created in ANcpLua.Analyzers
3. Merge updates Analyzers versions

**Workflow:** `.github/workflows/sync-versions.yml`

## Publishing to NuGet

Uses **NuGet Trusted Publishing** via GitHub Actions – no API keys needed.

```bash
# 1. Pack locally
pwsh ./build.ps1 -Version X.Y.Z

# 2. Commit and push
git add . && git commit -m "Release X.Y.Z" && git push

# 3. Create release tag (triggers nuget-publish.yml)
git tag vX.Y.Z && git push origin vX.Y.Z
```

**Workflow:** `.github/workflows/nuget-publish.yml`
**Trusted Publisher:** GitHubActions → ANcpLua/ANcpLua.NET.Sdk

## NuGet Feeds

```xml
<packageSources>
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```