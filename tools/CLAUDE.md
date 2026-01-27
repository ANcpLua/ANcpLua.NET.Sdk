# CLAUDE.md - tools/

Build-time code generators for SDK maintenance.

## Tools

### SdkGenerator

Generates `Sdk.props` and `Sdk.targets` files for each SDK package flavor.

```bash
dotnet run --project tools/SdkGenerator
```

**Output:** `src/Sdk/ANcpLua.NET.Sdk/Sdk.props`, `src/Sdk/ANcpLua.NET.Sdk.Web/Sdk.props`, etc.

### ConfigFilesGenerator

Generates configuration files from analyzer packages:

1. `.editorconfig` files with all analyzer rule IDs (for explicit severity control)
2. `BannedSymbols.*.txt` files from banned library public API surfaces

```bash
dotnet run --project tools/ConfigFilesGenerator
```

**Output:** `src/configuration/Analyzer.*.editorconfig`, `src/configuration/BannedSymbols.*.txt`

### SchemaGenerator

Generates JSON schemas for SDK configuration and validation.

```bash
dotnet run --project tools/SchemaGenerator
```

### SemconvGenerator

Generates OTel Semantic Conventions from `@opentelemetry/semantic-conventions` NPM package for TypeScript, C#, and DuckDB.

```bash
cd tools/SemconvGenerator
npm run generate      # All outputs
npm run deploy:all    # Copy to target locations
npm run ci            # Generate + validate (CI enforcement)
```

**Source:** `@opentelemetry/semantic-conventions` v1.39.0

**Outputs:**
- `output/semconv.ts` → `qyl/src/qyl.dashboard/src/lib/semconv.ts`
- `output/SemanticConventions.g.cs` → `eng/ANcpSdk.AspNetCore.ServiceDefaults/Instrumentation/`
- `output/promoted-columns.sql` (DuckDB column definitions)

**CI Validation:** `npm run ci` fails if outputs differ from deployed files.

## When to Run

- **SdkGenerator**: After modifying SDK import structure or adding new SDK flavors
- **ConfigFilesGenerator**: After updating analyzer package versions in `Version.props`
- **SchemaGenerator**: After modifying SDK configuration options
- **SemconvGenerator**: After updating `@opentelemetry/semantic-conventions` version or modifying prefix filters

## Implementation Notes

All tools:
- Auto-detect repo root via `.git` folder traversal
- Use `Meziantou.Framework.FullPath` for path handling
- Are standalone executables (not part of SDK package)
