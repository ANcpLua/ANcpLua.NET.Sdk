# SourceGen Helpers

Roslyn symbol extensions and helpers for Source Generators.

## Architecture

The SDK provides source generator helpers from **two sources**:

| Source | Purpose |
|--------|---------|
| `ANcpLua.Roslyn.Utilities.Sources` NuGet | Core utilities (~27 files) - embedded as internal types |
| `eng/Extensions/SourceGen/` | SDK-specific extensions (2 files) |

### Why Source-Only Packages?

Source generators **cannot easily reference external NuGet packages** at design-time. The Roslyn compiler loads generators in a constrained environment where package dependencies often fail to resolve.

**This is the industry-standard pattern** used by Microsoft and the .NET community:

| Use Case | Distribution | Why |
|----------|--------------|-----|
| Source generator projects | **Source-only package** (`InjectSourceGenHelpers`) | Generators can't easily use NuGet packages |
| Analyzers, CLI tools, tests | **ANcpLua.Roslyn.Utilities** NuGet package | Normal projects can reference packages |

## Usage

To use these helpers in your source generator project:

```xml
<PropertyGroup>
  <InjectSourceGenHelpers>true</InjectSourceGenHelpers>
</PropertyGroup>
```

This will:
1. Reference `ANcpLua.Roslyn.Utilities.Sources` package (embeds as internal types)
2. Include SDK-specific extension files
3. Define the `ANCPLUA_SOURCEGEN_HELPERS` constant

## What Gets Injected

When `InjectSourceGenHelpers=true`, you get access to:

### From ANcpLua.Roslyn.Utilities.Sources

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

## Related Packages

- [`ANcpLua.Roslyn.Utilities`](https://nuget.org/packages/ANcpLua.Roslyn.Utilities) - For analyzers, CLI tools, tests
- [`ANcpLua.Roslyn.Utilities.Sources`](https://nuget.org/packages/ANcpLua.Roslyn.Utilities.Sources) - Source-only for generators
