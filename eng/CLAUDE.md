# CLAUDE.md - eng/

SDK implementation: props, targets, and injectable code.

## Directory Map

```
eng/
├── MSBuild/              # Core .props/.targets (SDK entry points)
├── LegacySupport/        # Polyfill source files (netstandard2.0)
├── Extensions/           # Optional helpers (FakeLogger, SourceGen, Comparers)
├── Shared/               # Always-injected code (Throw)
└── ANcpSdk.AspNetCore.*/ # Web SDK service defaults (source generator)
```

## Core Wiring Flows

### 1. Polyfill Injection (netstandard2.0 support)

```
LegacySupport.props         → Sets switches (InjectIndexRange, InjectTimeProviderPolyfill, etc.)
       ↓
LegacySupport.targets       → Reads switches, conditionally adds <Compile Include="..."/>
       ↓
eng/LegacySupport/**/*.cs   → Actual polyfill source files
```

Key files:
- `MSBuild/LegacySupport.props` - switch definitions with defaults
- `MSBuild/LegacySupport.targets` - conditional file injection
- `LegacySupport/*/` - one folder per polyfill (IndexRange, TimeProvider, etc.)

### 2. Analyzer Injection

```
Common.targets              → Adds PackageReference to ANcpLua.Analyzers
       ↓
Condition: IncludeANcpLuaAnalyzers != false
```

Key files:
- `MSBuild/Common.targets` - analyzer package injection
- `MSBuild/BannedSymbols.txt` - banned API list (legacy time APIs, Newtonsoft, etc.)

### 3. Test Project Detection (MTP auto-config)

```
Testing.props               → Detects xunit.v3.mtp-v2 package reference
       ↓
Sets OutputType=Exe, TestingPlatform=true
```

Key files:
- `MSBuild/Testing.props` - MTP detection and property setting
- Detection: looks for `xunit.v3.mtp-v2` in package references

### 4. Shared Code Injection

```
Shared.props                → Reads InjectSharedThrow (default: true)
       ↓
Shared.targets              → Adds <Compile Include="Throw.cs"/>
       ↓
eng/Shared/Throw/Throw.cs   → Guard clause utilities
```

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

## Documentation

Full reference: https://ancplua.io/sdk/overview
