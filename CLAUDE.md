# CLAUDE.md

Guidance for Claude Code when working with this repository.

## Project Overview

**ANcpLua.NET.Sdk** is an opinionated MSBuild SDK providing better developer experience than plain Microsoft.NET.Sdk:
- Banned API enforcement (RS0030) + ANcpLua.Analyzers (AL0001-AL0013)
- Polyfills for modern .NET features on legacy TFMs
- Embedded source helpers (Throw.IfNull, SourceGen utilities)
- ASP.NET Core service defaults (OpenTelemetry, Health Checks, Resilience)

**Current Version:** 1.2.0

## Package Ecosystem

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ANcpLua.Roslyn.Utilities                         │
│                     (Public NuGet, netstandard2.0)                      │
│  SINGLE SOURCE OF TRUTH for Roslyn utilities                            │
│  Path: /Users/ancplua/ANcpLua.Roslyn.Utilities                          │
└─────────────────────────────────────────────────────────────────────────┘
                    │                               │
                    ▼                               ▼
┌───────────────────────────────────┐   ┌─────────────────────────────────┐
│ ANcpLua.Roslyn.Utilities.Testing  │   │ ANcpLua.NET.Sdk (this repo)     │
│ (net10.0, NuGet reference)        │   │ (embeds source for generators)  │
└───────────────────────────────────┘   └─────────────────────────────────┘
                                                    │
                                        ┌───────────┴───────────┐
                                        ▼                       ▼
                            ┌───────────────────┐   ┌───────────────────┐
                            │ ANcpLua.NET.Sdk   │   │ ANcpLua.NET.Sdk   │
                            │ .Web              │   │ .Test             │
                            └───────────────────┘   └───────────────────┘
```

| Package | Purpose |
|---------|---------|
| `ANcpLua.NET.Sdk` | Base SDK for libraries, console, workers |
| `ANcpLua.NET.Sdk.Web` | Web SDK with auto-registered ServiceDefaults |
| `ANcpLua.NET.Sdk.Test` | Test SDK with xUnit configuration |
| `ANcpLua.Analyzers` | Code analyzers (AL0001-AL0013) |
| `ANcpLua.Roslyn.Utilities` | **Single source** for Roslyn utilities |

**Architecture Rule:** Roslyn.Utilities owns all Roslyn extensions. SDK embeds from it for generator compatibility.

## Build & Test

```bash
# Build (fully automated - syncs submodule + transforms + packs)
pwsh ./build.ps1 -Version 1.3.0    # Release
pwsh ./build.ps1 -Version 999.9.9  # Testing

# Test
dotnet test tests/ANcpLua.Sdk.Tests/
```

## Automation (ZERO MANUAL STEPS REQUIRED)

**Everything is automated. Don't remind Claude about these - they happen automatically.**

### What's Automated

| Automation | Trigger | What Happens |
|------------|---------|--------------|
| **Submodule sync** | `build.ps1` | Auto-syncs Roslyn.Utilities to latest |
| **Source transform** | `build.ps1` | Auto-regenerates embedded source |
| **Dependabot** | Daily/Weekly | Creates PRs for package updates |
| **Submodule PRs** | Daily | GitHub Action creates PR when Roslyn.Utilities updates |

### build.ps1 Does Everything

```bash
pwsh ./build.ps1 -Version 1.3.0
```

This single command:
1. ✅ Fetches latest Roslyn.Utilities submodule
2. ✅ Auto-syncs if behind
3. ✅ Transforms source (namespace, visibility, preprocessor)
4. ✅ Cleans old .nupkg files
5. ✅ Generates Version.props
6. ✅ Packs all 5 NuGet packages

**No need to manually run transform scripts or sync submodules.**

### Dependabot Configuration

`.github/dependabot.yml` handles:
- **NuGet packages** - Weekly on Monday, grouped by category
- **GitHub Actions** - Weekly updates
- **Git submodules** - Daily sync (Roslyn.Utilities)

### Cross-Repo Dependency Flow

```
ANcpLua.Roslyn.Utilities (push to main)
         │
         ▼ (Dependabot: gitsubmodule daily)
ANcpLua.NET.Sdk/eng/submodules/Roslyn.Utilities
         │
         ▼ (build.ps1 auto-transforms)
eng/.generated/SourceGen/
         │
         ▼
SDK packages include embedded source
```

**When Roslyn.Utilities updates:**
1. Dependabot creates PR to update submodule
2. Merge PR
3. Next `build.ps1` auto-transforms the new source

**When ANcpLua.Analyzers updates its dependency on Roslyn.Utilities:**
1. Dependabot creates PR in Analyzers repo
2. Merge PR
3. Done

## Directory Structure

```
src/
├── Sdk/ANcpLua.NET.Sdk/       # SDK entry points
├── common/
│   ├── Common.targets         # Injection logic
│   ├── LegacySupport.targets  # Polyfill + SourceGen injection
│   └── Version.props          # Auto-generated
└── configuration/
    └── BannedSymbols.txt

eng/
├── ANcpSdk.AspNetCore.ServiceDefaults/      # Runtime package
├── Extensions/
│   ├── SourceGen/             # Embedded Roslyn helpers
│   ├── Comparers/             # StringOrdinalComparer
│   └── FakeLogger/            # Test helpers
├── Shared/Throw/              # Guard clauses
└── LegacySupport/             # Polyfills

tests/ANcpLua.Sdk.Tests/
├── Helpers/                   # PackageFixture, ProjectBuilder
└── Infrastructure/            # SdkBrandingConstants
```

## Key MSBuild Properties

### Auto-Enabled
| Property | Description |
|----------|-------------|
| `InjectSharedThrow` | Throw.IfNull() helpers |
| `IncludeDefaultBannedSymbols` | RS0030 banned symbols |
| `BanNewtonsoftJsonSymbols` | Ban Newtonsoft.Json |

### Opt-In
| Property | Description |
|----------|-------------|
| `InjectSourceGenHelpers` | Roslyn utilities for generators |
| `InjectStringOrdinalComparer` | StringOrdinalComparer |
| `InjectFakeLogger` | FakeLoggerExtensions |
| `InjectLockPolyfill` | System.Threading.Lock |
| `InjectTimeProviderPolyfill` | System.TimeProvider |

## Test Project vs MTP Detection

**CRITICAL:** These are TWO SEPARATE concerns - don't confuse them!

### 1. Test Project Detection (Broad)
```
IsTestProject=true → Imports Tests.targets (implicit usings, test defaults)
```

Triggers: Any test framework package (xunit, xunit.v3, NUnit, MSTest.TestFramework, TUnit)

**Users must set `IsTestProject=true` explicitly** in their csproj. Package-based auto-detection runs too late (MSBuild limitation).

### 2. MTP Detection (Strict)
```
UseMicrosoftTestingPlatform=true → OutputType=Exe, skip Microsoft.NET.Test.Sdk, inject MTP extensions
```

**Only these signals trigger MTP:**

| Signal | Type |
|--------|------|
| `TUnit` package | Always MTP |
| `xunit.v3.mtp-v1` / `xunit.v3.mtp-v2` | Explicit MTP |
| `Microsoft.Testing.Extensions.*` packages | Explicit MTP |
| `EnableNUnitRunner=true` | Explicit opt-in |
| `EnableMSTestRunner=true` | Explicit opt-in |
| `UseMicrosoftTestingPlatform=true` | Explicit property |

**NOT MTP signals (VSTest by default):**
- Plain `xunit.v3` (ambiguous - don't assume!)
- `NUnit` alone
- `MSTest.TestFramework` alone
- `xunit` (v2)

### MSBuild Implementation

```xml
<!-- Property-based detection works during import -->
<PropertyGroup>
  <_UsesMTP Condition="'$(UseMicrosoftTestingPlatform)' == 'true'
                       OR '$(EnableNUnitRunner)' == 'true'">true</_UsesMTP>
</PropertyGroup>

<!-- Package-based detection MUST be in a Target (MSBuild limitation) -->
<Target Name="_DetectTestFrameworksAndMTP" BeforeTargets="BeforeBuild">
  <!-- @(PackageReference->AnyHaveMetadataValue()) only works in Targets -->
  <PropertyGroup>
    <_UsesMTP Condition="@(PackageReference->AnyHaveMetadataValue('Identity', 'TUnit')) == 'true'">true</_UsesMTP>
  </PropertyGroup>
</Target>
```

**Why Target?** `@(PackageReference->...)` syntax fails in PropertyGroup conditions (MSB4099 error). Items aren't populated during initial import phase.

### Safety Guard

Target `_ValidateMTPConfiguration` emits `ANCPSDK001` warning if:
- `UseMicrosoftTestingPlatform=true` AND `OutputType=Library`

### What Gets Injected

| Condition | Packages Injected |
|-----------|-------------------|
| `UseMicrosoftTestingPlatform=true` | CrashDump, HangDump, CodeCoverage, TrxReport, HotReload, Retry |
| `UseMicrosoftTestingPlatform!=true` | Microsoft.NET.Test.Sdk (VSTest) |
| GitHub Actions + MTP | GitHubActionsTestLogger 3.x |
| GitHub Actions + VSTest | GitHubActionsTestLogger 2.4.1 |

## SourceGen Helpers (Roslyn.Utilities Embedding)

Source generators cannot reference NuGet packages at runtime, so utilities must be embedded as source.

### Submodule Location
```
eng/submodules/Roslyn.Utilities/  ← Git submodule (ANcpLua.Roslyn.Utilities repo)
```

### Transformation Process
```bash
# Run before `dotnet pack`
pwsh eng/scripts/Transform-RoslynUtilities.ps1
```

**Transformations applied:**
| Original | Transformed |
|----------|-------------|
| `namespace ANcpLua.Roslyn.Utilities` | `namespace ANcpLua.SourceGen` |
| `public static class` | `internal static class` |
| (none) | Wrapped in `#if ANCPLUA_SOURCEGEN_HELPERS` |

**Output:** `eng/.generated/SourceGen/` (gitignored, regenerated on build)

### What Gets Embedded
When `InjectSourceGenHelpers=true`:
- `EquatableArray<T>` - Value-equal ImmutableArray wrapper (critical for caching)
- `SymbolExtensions` - HasAttribute, GetAttribute, IsOrInheritsFrom
- `SyntaxExtensions` - GetMethodName, HasModifier, IsPartial
- `SemanticModelExtensions` - IsConstant, GetConstantValueOrDefault
- `LocationInfo/DiagnosticInfo` - Cache-safe diagnostic patterns

### SDK-Specific Extensions
Files in `eng/Extensions/SourceGen/` are SDK-specific and NOT from the submodule:
- Additional helpers specific to SDK use cases

**For non-generators:** Reference `ANcpLua.Roslyn.Utilities` NuGet directly instead of embedding.

## Critical Patterns

### Value Equality for Caching
```csharp
// ✅ Correct
IncrementalValuesProvider<EquatableArray<T>>

// ❌ Wrong - reference equality breaks caching
IncrementalValuesProvider<ImmutableArray<T>>
```

### Cache-Safe Diagnostics
```csharp
// ✅ Extract primitives
record struct LocationInfo(string Path, TextSpan Span, LinePositionSpan LineSpan);

// ❌ Never cache these
Location, ISymbol, Compilation, SemanticModel, SyntaxNode
```

## Critical Files

| File | Purpose |
|------|---------|
| `src/common/Common.targets` | Main injection logic |
| `src/common/LegacySupport.targets` | Polyfill + SourceGen injection |
| `tests/.../SdkBrandingConstants.cs` | Branding source of truth |
| `tests/.../ProjectBuilder.cs` | Test project builder |
