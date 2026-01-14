# Documentation Restructure Design

## Goal

Consolidate 26 scattered README/CLAUDE.md files into a focused AI-first structure that eliminates maintenance burden while preserving useful context.

## Current State

| Location | Files | Issue |
|----------|-------|-------|
| Root | CLAUDE.md, README.md | Good - keep as-is |
| tests/ | 2 CLAUDE.md files | Redundant intermediate layer |
| eng/**/ | 23 READMEs | Inconsistent, often outdated, duplicates ANcpLua.io |

## Target State

```
CLAUDE.md                              # Keep (ecosystem position, build commands)
README.md                              # Keep (NuGet quick-start for humans)
eng/CLAUDE.md                          # NEW (4 core flows + decision guide)
tests/ANcpLua.Sdk.Tests/CLAUDE.md      # Keep (test patterns, helpers)
```

## Files to Delete (24 total)

### Intermediate layer (1)
- `tests/CLAUDE.md`

### eng/ READMEs (23)
- `eng/Extensions/README.md`
- `eng/Extensions/Comparers/README.md`
- `eng/Extensions/FakeLogger/README.md`
- `eng/Extensions/SourceGen/README.md`
- `eng/LegacySupport/README.md`
- `eng/LegacySupport/DiagnosticAttributes/README.md`
- `eng/LegacySupport/Diagnostics/README.md`
- `eng/LegacySupport/Exceptions/README.md`
- `eng/LegacySupport/Experimental/README.md`
- `eng/LegacySupport/IndexRange/README.md`
- `eng/LegacySupport/IsExternalInit/README.md`
- `eng/LegacySupport/LanguageFeatures/README.md`
- `eng/LegacySupport/NullabilityAttributes/README.md`
- `eng/LegacySupport/TimeProvider/README.md`
- `eng/LegacySupport/TrimAttributes/README.md`
- `eng/MSBuild/README.md`
- `eng/MSBuild/Polyfills/README.md`
- `eng/Shared/README.md`
- `eng/Shared/CodeTests/README.md`
- `eng/Shared/Throw/README.md`
- `eng/ANcpSdk.AspNetCore.ServiceDefaults/README.md`
- `eng/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/README.md`

---

## New File: eng/CLAUDE.md

### Content Structure

```markdown
# CLAUDE.md - eng/

SDK implementation: props, targets, and injectable code.

## Directory Map

eng/
├── MSBuild/              # Core .props/.targets (SDK entry points)
├── LegacySupport/        # Polyfill source files (netstandard2.0)
├── Extensions/           # Optional helpers (FakeLogger, SourceGen, Comparers)
├── Shared/               # Always-injected code (Throw)
└── ANcpSdk.AspNetCore.*/ # Web SDK service defaults (source generator)

## Core Wiring Flows

### 1. Polyfill Injection (netstandard2.0 support)

LegacySupport.props         → Sets switches (InjectIndexRange, InjectTimeProviderPolyfill, etc.)
       ↓
LegacySupport.targets       → Reads switches, conditionally adds <Compile Include="..."/>
       ↓
eng/LegacySupport/**/*.cs   → Actual polyfill source files

Key files:
- `MSBuild/LegacySupport.props` - switch definitions with defaults
- `MSBuild/LegacySupport.targets` - conditional file injection
- `LegacySupport/*/` - one folder per polyfill (IndexRange, TimeProvider, etc.)

### 2. Analyzer Injection

Common.targets              → Adds PackageReference to ANcpLua.Analyzers
       ↓
Condition: !IncludeANcpLuaAnalyzers=false

Key files:
- `MSBuild/Common.targets` - analyzer package injection
- `MSBuild/BannedSymbols.txt` - banned API list (legacy time APIs, Newtonsoft, etc.)

### 3. Test Project Detection (MTP auto-config)

Testing.props               → Detects xunit.v3.mtp-v2 package reference
       ↓
Sets OutputType=Exe, TestingPlatform=true

Key files:
- `MSBuild/Testing.props` - MTP detection and property setting
- Detection: looks for `xunit.v3.mtp-v2` in package references

### 4. Shared Code Injection

Shared.props                → Reads InjectSharedThrow (default: true)
       ↓
Shared.targets              → Adds <Compile Include="Throw.cs"/>
       ↓
eng/Shared/Throw/Throw.cs   → Guard clause utilities

Optional injections:
- `InjectSourceGenHelpers=true` → eng/Extensions/SourceGen/*
- `InjectFakeLogger=true` → eng/Extensions/FakeLogger/*

## Decision Guide

| Task | Where to Edit |
|------|---------------|
| Add new polyfill | Create `eng/LegacySupport/NewPolyfill/`, add switch in `LegacySupport.props`, add conditional in `LegacySupport.targets` |
| Ban new API | Add to `MSBuild/BannedSymbols.txt` |
| Add new analyzer package | Edit `Common.targets` PackageReference section |
| Modify Throw helpers | Edit `eng/Shared/Throw/Throw.cs` |
| Add new injectable extension | Create folder in `eng/Extensions/`, add switch in `Shared.props`, add conditional in `Shared.targets` |
| Modify ServiceDefaults | See `eng/ANcpSdk.AspNetCore.ServiceDefaults/` and `eng/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/` |

## External Documentation

Full reference: https://ancplua.io/content/sdk/
```

---

## Implementation Steps

1. Create `eng/CLAUDE.md` with content above
2. Delete 24 files listed above
3. Verify build still works (READMEs shouldn't affect build)
4. Commit with message describing consolidation

## Rationale

- **Less doc = less drift**: 1 file to maintain instead of 24
- **CLAUDE.md as index, not mirror**: tells Claude where and why, source files tell what
- **ANcpLua.io stays canonical**: humans go there, no duplication
- **Claude reads source anyway**: detailed implementation is in .props/.targets files
