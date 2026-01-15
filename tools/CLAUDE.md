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

## When to Run

- **SdkGenerator**: After modifying SDK import structure or adding new SDK flavors
- **ConfigFilesGenerator**: After updating analyzer package versions in `Version.props`

## Implementation Notes

Both tools:
- Auto-detect repo root via `.git` folder traversal
- Use `Meziantou.Framework.FullPath` for path handling
- Are standalone executables (not part of SDK package)
