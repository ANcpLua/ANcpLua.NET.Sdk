# CLAUDE.md - src/

SDK packaging and MSBuild infrastructure.

## Directory Map

```
src/
├── common/               # Shared .props/.targets (Version.props is SOURCE OF TRUTH)
├── configuration/        # Auto-generated .editorconfig + BannedSymbols
├── Enforcement/          # Version enforcement targets
├── Packaging/            # NuGet packaging infrastructure
├── Sdk/                  # Generated Sdk.props/Sdk.targets per SDK flavor
├── Shared/               # Injectable code (Throw helpers)
└── Testing/              # Test SDK-specific targets
```

## SDK Packages

| Package              | Base SDK              | Purpose                      |
|----------------------|-----------------------|------------------------------|
| ANcpLua.NET.Sdk      | Microsoft.NET.Sdk     | Standard .NET projects       |
| ANcpLua.NET.Sdk.Web  | Microsoft.NET.Sdk.Web | ASP.NET Core projects        |
| ANcpLua.NET.Sdk.Test | Microsoft.NET.Sdk     | Test projects (xUnit v3 MTP) |

## Key Files

| File                              | Purpose                                            |
|-----------------------------------|----------------------------------------------------|
| `common/Version.props`            | **SOURCE OF TRUTH** for all package versions       |
| `common/Common.props`             | LangVersion, Nullable, analyzer injection switches |
| `common/Common.targets`           | Package injection, CLAUDE.md generation            |
| `common/LegacySupport.props`      | Polyfill switches (InjectIndexRange, etc.)         |
| `common/LegacySupport.targets`    | Conditional polyfill file injection                |
| `configuration/BannedSymbols.txt` | Legacy time APIs, object locks, etc.               |

## Build Flow

```
Sdk.props (per SDK)
    ↓ imports
common/Common.props (switches, defaults)
    ↓ imports
common/LegacySupport.props (polyfill switches)

Sdk.targets (per SDK)
    ↓ imports
common/Common.targets (package injection, generation)
    ↓ imports
common/LegacySupport.targets (polyfill injection)
```

## Regenerating SDK Files

```bash
# Run from repo root
dotnet run --project tools/SdkGenerator
```

This regenerates `Sdk.props` and `Sdk.targets` in `src/Sdk/*/`.