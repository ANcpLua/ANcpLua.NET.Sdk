# .NET Architecture Lint Plugin Design

## Overview

A Claude Code plugin that enforces .NET build patterns (Version.props, CPM, single-owner imports) through deterministic script detection and prompt-based remediation.

## Problem Statement

AI agents lose context and break centralized version management patterns:
- Hardcode versions instead of using `$(VariableName)`
- Import Version.props from wrong locations
- Replace symlinks with regular files
- Bypass CPM with inline PackageReference versions

## Solution

Hybrid approach:
- **Deterministic script** detects violations (no false positives)
- **Claude hook** triggers after MSBuild file edits
- **Prompt instructions** guide fixes

## Architecture

```
User edits .props/.csproj
        ↓
PostToolUse hook triggers
        ↓
lint-dotnet.sh runs
        ↓
    ┌───┴───┐
    │       │
  CLEAN   VIOLATIONS
    │       │
    ↓       ↓
 Proceed  Claude fixes before commit
```

## Rules

### Rule A: No Hardcoded Versions in Directory.Packages.props

**Detection:**
```bash
grep -n 'PackageVersion.*Version="[^$]' Directory.Packages.props
```

**Violation:**
```xml
<PackageVersion Include="Serilog" Version="3.1.1"/>
```

**Fix:**
```xml
<PackageVersion Include="Serilog" Version="$(SerilogVersion)"/>
```
And ensure `<SerilogVersion>3.1.1</SerilogVersion>` exists in Version.props.

**Naming convention:** Mirror AL0017 analyzer logic:
- Known packages: Use `PackageToVariableMap` (grouped strategy)
- Unknown packages: `Some.Package.Name` → `SomePackageNameVersion`

---

### Rule B: Version.props Import Owners

**Allowed importers:**
| File | Purpose |
|------|---------|
| `Directory.Packages.props` | CPM-enabled projects |
| `eng/Directory.Build.props` | CPM-disabled projects |

**Detection:**
```bash
find . -name "*.props" -exec grep -l 'Import.*Version\.props' {} \;
# Filter: only Directory.Packages.props and eng/Directory.Build.props allowed
```

**Violation example:**
```
src/Directory.Build.props:3
  <Import Project="$(MSBuildThisFileDirectory)../Version.props"/>
```

**Fix:** Delete the import. Only the designated owners should import Version.props.

---

### Rule C: Version.props Symlink Integrity

**Pattern:**
```
ANcpLua.NET.Sdk/src/common/Version.props  ← Source of truth
    ↑ symlink
ANcpLua.Analyzers/Version.props
    ↑ symlink
ANcpLua.Roslyn.Utilities/Version.props
```

**Detection:**
```bash
if [[ -e "Version.props" && ! -L "Version.props" ]]; then
  # Check if this is source repo (has src/common/Version.props)
  if [[ ! -f "src/common/Version.props" ]]; then
    echo "RULE_C violation: Expected symlink"
  fi
fi
```

**Violations:**
- Version.props is a regular file (not symlink) in consumer repo
- Symlink exists but is broken

**Fix:** Recreate symlink, never copy the file.

---

### Rule G: CPM Enforcement (No Inline Versions)

**Detection:**
```bash
grep -rn 'PackageReference.*Version=' --include="*.csproj" | grep -v 'VersionOverride'
```

**Violation:**
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3"/>
```

**Fix:**
1. Remove `Version` attribute from csproj
2. Add to Directory.Packages.props:
   ```xml
   <PackageVersion Include="Newtonsoft.Json" Version="$(NewtonsoftJsonVersion)"/>
   ```
3. Ensure variable exists in Version.props

---

## Plugin Structure

```
dotnet-architecture-lint/
├── plugin.json
├── lint-dotnet.sh
├── hooks/
│   └── post-msbuild-edit.yml
├── skills/
│   └── lint-dotnet.md
└── data/
    └── package-variable-map.json  (optional)
```

## Implementation Files

### lint-dotnet.sh

```bash
#!/bin/bash
# Deterministic .NET architecture linter
# Output format: RULE_X|file|details
# Exit codes: 0 = clean, 1 = violations

set -e
REPO_ROOT="${1:-.}"
VIOLATIONS=()

# Rule A: Hardcoded versions
DPP="$REPO_ROOT/Directory.Packages.props"
if [[ -f "$DPP" ]]; then
  HARDCODED=$(grep -n 'PackageVersion.*Version="[^$]' "$DPP" 2>/dev/null || true)
  if [[ -n "$HARDCODED" ]]; then
    VIOLATIONS+=("RULE_A:$DPP")
    echo "RULE_A|$DPP|$HARDCODED"
  fi
fi

# Rule B: Unauthorized imports
ALLOWED_OWNERS=("Directory.Packages.props" "Directory.Build.props")
while IFS= read -r -d '' file; do
  if grep -q 'Import.*Version\.props' "$file" 2>/dev/null; then
    BASENAME=$(basename "$file")
    DIRNAME=$(dirname "$file")
    # Allow Directory.Packages.props anywhere
    # Allow eng/Directory.Build.props specifically
    if [[ "$BASENAME" == "Directory.Packages.props" ]]; then
      continue
    elif [[ "$BASENAME" == "Directory.Build.props" && "$DIRNAME" == *"/eng" ]]; then
      continue
    else
      LINE=$(grep -n 'Import.*Version\.props' "$file" | head -1)
      VIOLATIONS+=("RULE_B:$file")
      echo "RULE_B|$file|$LINE"
    fi
  fi
done < <(find "$REPO_ROOT" -name "*.props" -print0 2>/dev/null)

# Rule C: Symlink check
VP="$REPO_ROOT/Version.props"
if [[ -e "$VP" || -L "$VP" ]]; then
  if [[ ! -L "$VP" ]]; then
    # Not a symlink - check if source repo
    if [[ ! -f "$REPO_ROOT/src/common/Version.props" ]]; then
      VIOLATIONS+=("RULE_C:$VP")
      echo "RULE_C|$VP|Expected symlink, found regular file"
    fi
  elif [[ -L "$VP" && ! -e "$VP" ]]; then
    # Broken symlink
    VIOLATIONS+=("RULE_C:$VP")
    echo "RULE_C|$VP|Broken symlink"
  fi
fi

# Rule G: Inline versions in csproj
while IFS= read -r -d '' file; do
  INLINE=$(grep -n 'PackageReference.*Version=' "$file" 2>/dev/null | grep -v 'VersionOverride' || true)
  if [[ -n "$INLINE" ]]; then
    VIOLATIONS+=("RULE_G:$file")
    echo "RULE_G|$file|$INLINE"
  fi
done < <(find "$REPO_ROOT" -name "*.csproj" -print0 2>/dev/null)

# Summary
if [[ ${#VIOLATIONS[@]} -eq 0 ]]; then
  echo "CLEAN|All rules passed"
  exit 0
else
  echo "VIOLATIONS|${#VIOLATIONS[@]} found"
  exit 1
fi
```

### hooks/post-msbuild-edit.yml

```yaml
name: dotnet-architecture-lint
description: Validates MSBuild changes after edits
event: PostToolUse
tools: [Edit, Write]

file_patterns:
  - "*.props"
  - "*.targets"
  - "*.csproj"
  - "global.json"
  - "nuget.config"

prompt: |
  You just modified an MSBuild file. Run the architecture linter:

  ```bash
  bash "${CLAUDE_PLUGIN_ROOT}/lint-dotnet.sh" .
  ```

  **If output contains VIOLATIONS:**

  | Rule | Fix |
  |------|-----|
  | RULE_A | Replace hardcoded version with $(VariableName). Add variable to Version.props if missing. |
  | RULE_B | Remove Import of Version.props. Only Directory.Packages.props or eng/Directory.Build.props may import it. |
  | RULE_C | Version.props must be symlink in consumer repos. Recreate symlink, don't copy. |
  | RULE_G | Remove Version attribute from PackageReference. Use CPM via Directory.Packages.props. |

  **Fix all violations before any git operations.**

  **If CLEAN:** Proceed normally.
```

### skills/lint-dotnet.md

```markdown
---
name: lint-dotnet
description: Run .NET architecture linter on demand
---

# /lint-dotnet

Run the .NET architecture linter to check for violations.

## Execution

```bash
bash "${CLAUDE_PLUGIN_ROOT}/lint-dotnet.sh" .
```

## Output Format

`RULE_X|file|details`

| Rule | Meaning | Fix |
|------|---------|-----|
| RULE_A | Hardcoded version in Directory.Packages.props | Use $(VariableName) |
| RULE_B | Unauthorized Version.props import | Remove import (only DPP or eng/DBP allowed) |
| RULE_C | Version.props not a symlink | Recreate symlink |
| RULE_G | Inline version in .csproj | Remove Version attr, use CPM |

## Clean Output

`CLEAN|All rules passed` = no violations.
```

### plugin.json

```json
{
  "name": "dotnet-architecture-lint",
  "version": "1.0.0",
  "description": "Enforces .NET build patterns: Version.props symlinks, CPM, single-owner imports",
  "author": "ancplua",
  "hooks": ["hooks/post-msbuild-edit.yml"],
  "skills": ["skills/lint-dotnet.md"]
}
```

## Multi-Repo Context

| Repo | Version.props | Role |
|------|---------------|------|
| ANcpLua.NET.Sdk | `src/common/Version.props` | Source of truth |
| ANcpLua.Analyzers | Symlink → SDK | Consumer |
| ANcpLua.Roslyn.Utilities | Symlink → SDK | Consumer |

## Future Enhancements (v2)

- [ ] Auto-fix capability (script generates fix commands)
- [ ] `package-variable-map.json` for smart suggestions
- [ ] PreToolUse hook for "about to edit" warnings
- [ ] Git pre-commit hook integration
- [ ] Cross-repo symlink validation

## What This Catches

Example violation this would have caught:
```
$ ./lint-dotnet.sh .

RULE_B|eng/Directory.Build.props|3: <Import Project="...Version.props"/>
VIOLATIONS|1 found
```

The duplicate import in `eng/Directory.Build.props` (outside allowed owners) causes variable resolution failures during NuGet restore.
