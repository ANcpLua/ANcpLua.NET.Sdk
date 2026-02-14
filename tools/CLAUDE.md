# CLAUDE.md - tools/

Build-time code generators for SDK maintenance. These are standalone executables, not part of the SDK package.

## Tools

### SdkGenerator

Generates `Sdk.props` and `Sdk.targets` files for each SDK package variant.

```bash
dotnet run --project tools/SdkGenerator
```

**Input:** Reads SDK structure from `src/Build/` directory
**Output:** `src/Sdk/ANcpLua.NET.Sdk/Sdk.props`, `src/Sdk/ANcpLua.NET.Sdk.Web/Sdk.props`, etc.

The generator creates the import chain for each SDK variant, ensuring:
- Correct base SDK import (Microsoft.NET.Sdk vs Microsoft.NET.Sdk.Web)
- Proper ordering of props/targets imports
- Variant-specific features (Testing.props only for non-Web SDKs)

### ConfigFilesGenerator

Generates configuration files from analyzer packages:

```bash
dotnet run --project tools/ConfigFilesGenerator
```

**Output:**
- `src/Config/Analyzer.*.editorconfig` - All analyzer rule IDs for explicit severity control
- `src/Config/BannedSymbols.*.txt` - Banned symbols from library public API surfaces

## When to Run Each Tool

| Tool                 | Run When                                                    |
|----------------------|-------------------------------------------------------------|
| SdkGenerator         | After modifying SDK import structure or adding new variants |
| ConfigFilesGenerator | After updating analyzer package versions in Version.props   |

## Implementation Notes

All tools:
- Auto-detect repo root via `.git` folder traversal
- Use `Meziantou.Framework.FullPath` for path handling
- Target net10.0
- Are excluded from SDK package (standalone executables)

## Adding a New Tool

1. Create new project in `tools/<ToolName>/`
2. Reference common utilities if needed
3. Add to solution file
4. Document purpose and usage in this file
