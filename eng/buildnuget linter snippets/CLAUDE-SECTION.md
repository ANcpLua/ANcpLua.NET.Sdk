# ══════════════════════════════════════════════════════════════════════════════
# ADD THIS SECTION TO YOUR CLAUDE.md
# ══════════════════════════════════════════════════════════════════════════════

## ⛔ ABSOLUTE RULES — MSBuild/NuGet Architecture

### Before ANY commit to MSBuild files:

```bash
./scripts/lint-dotnet.sh .
```

If exit code ≠ 0: **STOP. DO NOT COMMIT.**

### Files that trigger mandatory linting:
- `*.props`
- `*.targets`
- `*.csproj`
- `nuget.config`
- `global.json`
- `Version.props`

### Rule Violations = STOP

| Rule | Violation | Action |
|------|-----------|--------|
| A | Hardcoded version in Directory.Packages.props | Use `$(VariableName)` |
| B | Version.props imported outside Directory.Packages.props | Delete unauthorized import |
| G | PackageReference with inline Version | Remove Version, use CPM |

### Single Owner Principle

**Version.props** must only be imported by `Directory.Packages.props`.

```xml
<!-- ✅ ONLY HERE -->
<!-- Directory.Packages.props -->
<Import Project="$(MSBuildThisFileDirectory)src/common/Version.props"/>

<!-- ❌ NEVER IN -->
<!-- eng/Directory.Build.props, Directory.Build.props, or any other file -->
```

### Why This Matters

NuGet restore evaluates files in a different order than MSBuild build.
Multiple imports → timing issues → empty variables → version 1.0.0 → CI failure.

One import. One owner. Zero CI failures.

### Error → Root Cause Map

| Error Pattern | Root Cause | Fix |
|---------------|------------|-----|
| `version 1.0.0 was resolved` | `$(XxxVersion)` empty | Check Version.props import |
| `NU1015: no version specified` | Missing PackageVersion | Add to Directory.Packages.props |
| `SDK cannot be found` | Upstream restore failed | Fix restore errors first |
| `MSB4011: cannot be imported again` | Duplicate import | Single owner principle |
| `72+ warnings` | Upstream variable issue | Trace to ONE root cause |

### One MSBuild File Per Edit Session

1. Edit ONE .props/.targets/.csproj file
2. Run `dotnet build`
3. Verify zero errors
4. Only then edit another MSBuild file

Never batch-edit MSBuild files. The cascade effects are invisible until CI fails.
