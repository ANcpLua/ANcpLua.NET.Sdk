# CLAUDE.md - AI Assistant Instructions for ANcpLua.NET.Sdk

## CRITICAL: BRANDING - READ THIS FIRST

This SDK was forked from Meziantou's SDK. **ALL legacy "Meziantou" branding has been replaced.**

### NEVER USE THESE (Legacy Names):
```
Meziantou
meziantou
MEZIANTOU
UseMeziantouConventions
MeziantouServiceDefaultsOptions
Meziantou.AspNetCore.ServiceDefaults
Meziantou.Sdk.Name
```

### ALWAYS USE THESE (Current Names):
| Purpose | Correct Value |
|---------|---------------|
| Author | `ANcpLua` |
| SDK Metadata Key | `ANcpLua.Sdk.Name` |
| ServiceDefaults Namespace | `ANcpSdk.AspNetCore.ServiceDefaults` |
| ServiceDefaults Options | `ANcpSdkServiceDefaultsOptions` |
| Conventions Method | `UseANcpSdkConventions()` |

### Use Constants From:
```csharp
// ALWAYS reference this file for branding strings:
tests/ANcpLua.Sdk.Tests/Infrastructure/SdkBrandingConstants.cs
```

**History:** 7 hours lost debugging hardcoded legacy names (2024-12-16). NEVER hardcode branding strings.

---

## Project Structure

```
ANcpLua.NET.Sdk/
├── src/                    # SDK source (nuspec, props, targets)
│   ├── common/             # Shared MSBuild files
│   ├── configuration/      # Editorconfig, banned symbols
│   └── Sdk/                # SDK entry points
├── eng/                    # Engineering tools
│   ├── ANcpLua.Analyzers/  # Custom Roslyn analyzers
│   ├── ANcpSdk.AspNetCore.ServiceDefaults/  # ASP.NET conventions
│   ├── LegacySupport/      # Polyfills for older TFMs
│   ├── MSBuild/            # MSBuild extensions
│   └── Shared/             # Shared source files
└── tests/                  # Test projects
```

## Build Commands

```bash
# Build all packages
dotnet pack src/ANcpLua.NET.Sdk.csproj -c Release

# Run tests
dotnet test tests/ANcpLua.Sdk.Tests/ANcpLua.Sdk.Tests.csproj

# Build analyzer
dotnet build eng/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release
```

## Test Infrastructure

Tests use `PackageFixture` to build SDK packages and `ProjectBuilder` to create test projects.

Key files:
- `tests/ANcpLua.Sdk.Tests/Helpers/PackageFixture.cs` - Builds SDK packages
- `tests/ANcpLua.Sdk.Tests/Helpers/ProjectBuilder.cs` - Creates test projects
- `tests/ANcpLua.Sdk.Tests/Infrastructure/SdkBrandingConstants.cs` - **BRANDING CONSTANTS**

## Before Completing Any Task

1. **Run tests:** `dotnet test`
2. **Check for legacy names:** `grep -r "Meziantou" --include="*.cs" tests/`
3. **Update this file if needed**
