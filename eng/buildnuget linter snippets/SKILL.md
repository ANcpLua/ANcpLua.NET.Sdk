---
name: dotnet-architecture-linter
description: |
  MSBuild/NuGet/CPM architecture validator. MUST be triggered:
  (1) Before committing changes to .props, .targets, .csproj, nuget.config, global.json
  (2) When user runs /lint-dotnet
  (3) After ANY MSBuild-related file edit
  
  Catches: hardcoded versions, duplicate imports, CPM bypass, single-owner violations.
  Prevents CI failures by validating locally before push.
---

# .NET Architecture Linter

Validates MSBuild/NuGet architecture before changes break CI.

## Quick Start

```bash
# Run validation
./scripts/lint-dotnet.sh .

# Or on Windows
pwsh ./scripts/lint-dotnet.ps1 .
```

## Rules

| Rule | Catches | Severity |
|------|---------|----------|
| A | Hardcoded versions in Directory.Packages.props | ERROR |
| B | Version.props imported outside single owner | ERROR |
| G | PackageReference with inline Version attribute | ERROR |

## Rule Details

### Rule A: No Hardcoded Versions

```xml
<!-- ❌ VIOLATION -->
<PackageVersion Include="Serilog" Version="3.1.1"/>

<!-- ✅ CORRECT -->
<PackageVersion Include="Serilog" Version="$(SerilogVersion)"/>
```

Version variables must be defined in `Version.props`.

### Rule B: Single Owner for Version.props

Only `Directory.Packages.props` may import `Version.props`.

```xml
<!-- ❌ VIOLATION: eng/Directory.Build.props -->
<Import Project="...Version.props"/>

<!-- ✅ CORRECT: Only in Directory.Packages.props -->
<Import Project="$(MSBuildThisFileDirectory)src/common/Version.props"/>
```

If multiple files import Version.props, NuGet restore timing causes empty variables → version 1.0.0 fallback → CI failure.

### Rule G: CPM Enforcement

Projects must not bypass Central Package Management:

```xml
<!-- ❌ VIOLATION -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3"/>

<!-- ✅ CORRECT -->
<PackageReference Include="Newtonsoft.Json"/>
```

## When to Run

**MANDATORY before:**
- `git commit` of any .props/.targets/.csproj
- `git push`

**Automatic triggers:**
- After editing Directory.Packages.props
- After editing Directory.Build.props
- After editing any .csproj
- After editing Version.props
- After editing nuget.config or global.json

## On Violation

1. **DO NOT** commit or push
2. Fix each violation using suggested fix
3. Run `dotnet build` to verify
4. Re-run linter until exit code 0
5. Only then proceed with git operations

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All rules pass - safe to commit |
| 1 | Violations found - fix before commit |

## Integration with CLAUDE.md

Add to your CLAUDE.md:

```markdown
## ⛔ BEFORE ANY COMMIT TO MSBUILD FILES

Run: `./scripts/lint-dotnet.sh .`

If exit code ≠ 0:
1. DO NOT commit
2. Fix violations shown
3. Re-run until clean
```
