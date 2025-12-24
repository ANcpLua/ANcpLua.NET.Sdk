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
# Build
pwsh -Command './build.ps1 "-p:Version=1.2.0"'      # Release
pwsh -Command './build.ps1 "-p:Version=999.9.9"'    # Testing

# Test
./run-tests.sh              # Full (~5 min)
./run-tests.sh --quick      # Fast (~1 min)
./run-tests.sh --filter "ClassName"
```

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

## SourceGen Helpers

When `InjectSourceGenHelpers=true`:
- `EquatableArray<T>` - Value-equal ImmutableArray wrapper (critical for caching)
- `SymbolExtensions` - HasAttribute, GetAttribute, IsOrInheritsFrom
- `SyntaxExtensions` - GetMethodName, HasModifier, IsPartial
- `SemanticModelExtensions` - IsConstant, GetConstantValueOrDefault
- `LocationInfo/DiagnosticInfo` - Cache-safe diagnostic patterns

**Why embedded?** Source generators cannot reference NuGet packages. Standard industry pattern.

**For non-generators:** Reference `ANcpLua.Roslyn.Utilities` NuGet directly.

## Pending Work

See `MIGRATION-PLAN.md` - consolidating utilities:

**Phase 1:** Move SDK utilities → Roslyn.Utilities (single source of truth) ✅ COMPLETE
**Phase 2:** SDK embeds FROM Roslyn.Utilities (delete duplicates)
**Phase 3:** Validate

**⚠️ Anti-Pattern:** Do NOT copy TO SDK first (increases duplication)

**⚠️ C# 14 Issue:** Roslyn.Utilities uses experimental `extension(Type)` syntax. When embedding, convert to traditional `this Type` methods.

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
