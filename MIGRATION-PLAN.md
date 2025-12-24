# Migration Plan: Roslyn Utilities Consolidation

**Date:** 2024-12-24
**Status:** Phase 1 Complete
**Goal:** Single source of truth in `ANcpLua.Roslyn.Utilities`, SDK embeds for generator compatibility

---

## Current State (From Analysis)

### Critical Duplicates

| File | SDK Version | Roslyn.Utilities Version | Resolution |
|------|-------------|--------------------------|------------|
| `EquatableArray.cs` | `readonly record struct` (~50 lines) | `readonly struct` (~200 lines, more features) | **Use Roslyn.Utilities** ✅ |
| `SourceProductionContextExtensions.cs` | `AddSourceWithHeader()`, `ReportDiagnostic(DiagnosticInfo)` | `ReportException()`, `ToDiagnostic()` | **MERGED** ✅ |
| `AnalyzerConfigOptionsProviderExtensions.cs` | 3 methods (MSBuild helpers) | 7 methods (comprehensive) | **MERGED** ✅ |

### SDK-Only (Moved TO Roslyn.Utilities) ✅

| File | Key Methods |
|------|-------------|
| `SymbolExtensions.cs` | `HasAttribute`, `GetAttribute`, `IsOrInheritsFrom`, `ImplementsInterface`, `GetMethod`, `GetProperty` |
| `SyntaxExtensions.cs` | `GetMethodName`, `HasModifier`, `IsPartial`, `IsPrimaryConstructorType` |
| `SemanticModelExtensions.cs` | `IsConstant`, `AllConstant`, `GetConstantValueOrDefault` |
| `CompilationExtensions.cs` | `HasLanguageVersionAtLeastEqualTo`, `HasAccessibleTypeWithMetadataName` |
| `DiagnosticInfo.cs` | Cache-safe diagnostic representation |
| `LocationInfo.cs` | Cache-safe location (Path, Span, LineSpan) |
| `EquatableMessageArgs.cs` | Value-equal diagnostic message args |
| `FileExtensions.cs` | `WriteIfChanged` |

### Roslyn.Utilities-Only (SDK Currently Lacks)

| File | Key Methods |
|------|-------------|
| `StringExtensions.cs` | `ToParameterName()` (C# keyword escaping!), `ToPropertyName()`, `ExtractNamespace()` |
| `AttributeDataExtensions.cs` | `GetNamedArgument()`, `GetGenericTypeArgument()` |
| `ConvertExtensions.cs` | `ToBoolean()`, `ToEnum<T>()` from TypedConstant |
| `IncrementalValuesProviderExtensions.cs` | `WhereNotNull()`, `CollectAsEquatableArray()`, `SelectAndReportExceptions()` |
| `Models/FileWithName.cs` | (Name, Text) pair for generated source |
| `Models/ResultWithDiagnostics.cs` | Result + `EquatableArray<Diagnostic>` |

### Non-Duplicated (Keep Separate)

| File | Location | Notes |
|------|----------|-------|
| `EnumerableExtensions.cs` | Both | **MERGED** ✅ |
| `SyntaxValueProvider*.cs` | Both | Different approaches - MERGE pending |
| `DiagnosticReportingExtensions.cs` | ANcpLua.Analyzers only | Keep in Analyzers |

---

## Architecture (Target State)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        ANcpLua.Roslyn.Utilities                         │
│                     (Public NuGet, netstandard2.0)                      │
│  SINGLE SOURCE OF TRUTH for all Roslyn utilities                        │
│  ────────────────────────────────────────────────────────────────────── │
│  EquatableArray<T> │ SymbolExtensions │ SyntaxExtensions │ Models      │
│  StringExtensions │ AttributeDataExtensions │ Pipeline helpers         │
│  LocationInfo │ DiagnosticInfo │ Cache-safe patterns                   │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
┌───────────────────────────────────────┐   ┌─────────────────────────────┐
│ ANcpLua.Roslyn.Utilities.Testing      │   │ ANcpLua.NET.Sdk             │
│ (net10.0)                             │   │ (MSBuild SDK)               │
│ ──────────────────────────────────────│   │ ─────────────────────────── │
│ References via NuGet                  │   │ Embeds source via           │
│                                       │   │ InjectSourceGenHelpers=true │
└───────────────────────────────────────┘   └─────────────────────────────┘
```

---

## Phase 1: Consolidate INTO Roslyn.Utilities ✅ COMPLETE

### Step 1.1: Add SDK utilities to Roslyn.Utilities ✅

| SDK File | Target in Roslyn.Utilities | Status |
|----------|---------------------------|--------|
| `SymbolExtensions.cs` | `SymbolExtensions.cs` | ✅ |
| `SyntaxExtensions.cs` | `SyntaxExtensions.cs` | ✅ |
| `SemanticModelExtensions.cs` | `SemanticModelExtensions.cs` | ✅ |
| `CompilationExtensions.cs` | `CompilationExtensions.cs` | ✅ |
| `LocationInfo.cs` | `Models/LocationInfo.cs` | ✅ |
| `DiagnosticInfo.cs` | `Models/DiagnosticInfo.cs` | ✅ |
| `EquatableMessageArgs.cs` | `Models/EquatableMessageArgs.cs` | ✅ |
| `FileExtensions.cs` | `FileExtensions.cs` | ✅ |

### Step 1.2: Merge duplicates ✅

- **SourceProductionContextExtensions.cs** - MERGED ✅
- **AnalyzerConfigOptionsProviderExtensions.cs** - MERGED ✅
- **EnumerableExtensions.cs** - MERGED ✅
- **EquatableArray.cs** - Using Roslyn.Utilities version ✅

### Step 1.3: Validate ✅

Build succeeded: `dotnet build` - 0 errors, 0 warnings

---

## Phase 2: Update SDK to Embed FROM Roslyn.Utilities

### Step 2.1: Delete SDK duplicates

Remove from `eng/Extensions/SourceGen/`:
- `EquatableArray.cs`
- `SymbolExtensions.cs`
- `SyntaxExtensions.cs`
- `SemanticModelExtensions.cs`
- `CompilationExtensions.cs`
- `LocationInfo.cs`
- `DiagnosticInfo.cs`
- `EquatableMessageArgs.cs`
- `FileExtensions.cs`
- `SourceProductionContextExtensions.cs`
- `AnalyzerConfigOptionsProviderExtensions.cs`
- `EnumerableExtensions.cs`

### Step 2.2: Add submodule (or build-time copy)

```bash
git submodule add <repo-url> submodules/Roslyn.Utilities
```

### Step 2.3: Update LegacySupport.targets

Embed source with transformations:
1. Add `#if ANCPLUA_SOURCEGEN_HELPERS` guard
2. Change namespace to `ANcpLua.SourceGen`
3. Change `public` → `internal`
4. Convert C# 14 `extension(Type)` → traditional `this Type`

---

## Phase 3: Validation

- [x] Roslyn.Utilities contains ALL utilities
- [ ] No duplicates between repos
- [ ] `InjectSourceGenHelpers=true` works
- [ ] Tests pass: `./run-tests.sh --quick`

---

## Key Patterns to Preserve

### Value Equality for Caching
```csharp
IncrementalValuesProvider<EquatableArray<T>> // ✅ Proper caching
IncrementalValuesProvider<ImmutableArray<T>> // ❌ Reference equality
```

### Cache-Safe Diagnostics
```csharp
record struct LocationInfo(string Path, TextSpan Span, LinePositionSpan LineSpan);
record struct DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo Location, EquatableMessageArgs Args);
```

### Forbidden Types (Never Cache)
`ISymbol`, `Compilation`, `SemanticModel`, `SyntaxNode`, `Location`

---

## Files to KEEP in SDK

| Location | Purpose |
|----------|---------|
| `eng/Shared/Throw/` | Guard clause helpers |
| `eng/Extensions/Comparers/` | StringOrdinalComparer |
| `eng/Extensions/FakeLogger/` | Test logging helpers |
| `eng/LegacySupport/` | Polyfills |
| `src/configuration/BannedSymbols.txt` | Best practices |

---

## Source Locations

| Item | Path |
|------|------|
| SDK | `/Users/ancplua/ANcpLua.NET.Sdk/` |
| Roslyn.Utilities | `/Users/ancplua/ANcpLua.Roslyn.Utilities/` |
| Analyzers | `/Users/ancplua/RiderProjects/ANcpLua.Analyzers/` |
