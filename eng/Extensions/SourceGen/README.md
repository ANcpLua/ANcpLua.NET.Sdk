# SourceGen Helpers

Roslyn symbol extensions and helpers for Source Generators.

## Architecture

The SDK embeds source files for source generators from **two sources**:

| Source | Files | Purpose |
|--------|-------|---------|
| `eng/.generated/SourceGen/` | ~27 files | From `ANcpLua.Roslyn.Utilities` submodule (auto-transformed) |
| `eng/Extensions/SourceGen/` | 2 files | SDK-specific extensions |

### Why This Architecture?

Source generators **cannot easily reference external NuGet packages** at design-time. The Roslyn compiler loads generators in a constrained environment where package dependencies often fail to resolve.

**This is the industry-standard pattern** used by Microsoft and the .NET community:

| Use Case | Distribution | Why |
|----------|--------------|-----|
| Source generator projects | **SDK embedded** (`InjectSourceGenHelpers`) | Generators can't easily use NuGet packages |
| Analyzers, CLI tools, tests | **ANcpLua.Roslyn.Utilities** NuGet package | Normal projects can reference packages |

### Build Flow

```
eng/submodules/Roslyn.Utilities/          # Source of truth (git submodule)
        ↓
eng/scripts/Transform-RoslynUtilities.ps1  # Transforms namespace + adds #if guard
        ↓
eng/.generated/SourceGen/                  # Transformed files (gitignored)
        ↓
build.ps1 → dotnet pack                    # Packages into SDK nupkg
        ↓
shared/Extensions/SourceGen/               # In the published nupkg
```

## Usage

To use these helpers in your source generator project:

```xml
<PropertyGroup>
  <InjectSourceGenHelpers>true</InjectSourceGenHelpers>
</PropertyGroup>
```

This will:
1. Define the `ANCPLUA_SOURCEGEN_HELPERS` constant
2. Include all SourceGen helper files from the SDK

## What Gets Injected

When `InjectSourceGenHelpers=true`, you get access to:

### From Roslyn.Utilities (auto-transformed)

- `EquatableArray<T>` - Cache-safe array wrapper for incremental generators
- `DiagnosticFlow` - Fluent diagnostic reporting
- `SymbolExtensions` - Symbol analysis helpers
- `TypeSymbolExtensions` - Type checking utilities
- `InvocationExtensions` - Method invocation matching
- `SyntaxValueProviderExtensions` - Pipeline helpers
- `CodeGeneration` - Code emission utilities
- `HashCombiner` - Hash code generation
- And more (~27 files total)

### SDK-Specific (this directory)

- `DiagnosticsExtensions.cs` - Additional diagnostic helpers
- `SyntaxValueProvider.cs` - Custom syntax providers

## For SDK Maintainers

To update the Roslyn.Utilities files:

```bash
# Update submodule to latest
git submodule update --remote eng/submodules/Roslyn.Utilities

# Rebuild (transform runs automatically)
pwsh build.ps1 -Version 999.9.9
```

For everything else (analyzers, CLI tools, tests), reference the NuGet package:
[`ANcpLua.Roslyn.Utilities`](https://nuget.org/packages/ANcpLua.Roslyn.Utilities)
