#!/bin/bash
# Stop Guard Hook
# Prevents Claude from stopping if MSBuild linter has violations
# Exit 2 with decision=block = force Claude to continue

set -euo pipefail

REPO_ROOT="${CLAUDE_PROJECT_DIR:-.}"
LINTER_SCRIPT="$REPO_ROOT/scripts/lint-dotnet.sh"

# Check if there are any modified MSBuild files
MODIFIED_MSBUILD=$(git -C "$REPO_ROOT" diff --name-only 2>/dev/null | grep -E '\.(props|targets|csproj|slnx)$|nuget\.config|global\.json' || true)
STAGED_MSBUILD=$(git -C "$REPO_ROOT" diff --cached --name-only 2>/dev/null | grep -E '\.(props|targets|csproj|slnx)$|nuget\.config|global\.json' || true)

# If no MSBuild files modified, allow stop
if [[ -z "$MODIFIED_MSBUILD" && -z "$STAGED_MSBUILD" ]]; then
    echo '{"decision": "approve"}'
    exit 0
fi

# Run linter if MSBuild files were touched
if [[ -f "$LINTER_SCRIPT" ]]; then
    LINT_OUTPUT=$("$LINTER_SCRIPT" "$REPO_ROOT" 2>&1) || true
    LINT_EXIT=$?

    if [[ "$LINT_EXIT" -ne 0 ]]; then
        # Violations found - block stop
        cat >&2 <<EOF
{
  "decision": "block",
  "reason": "MSBuild linter violations must be fixed before completing",
  "systemMessage": "STOP BLOCKED: You modified MSBuild files but linter found violations. You MUST fix them before stopping.

$LINT_OUTPUT

Fix all violations above, then you may complete the task. DO NOT ask the user - just fix them."
}
EOF
        exit 2
    fi
fi

# All clean
echo '{"decision": "approve", "reason": "MSBuild linter passed"}'
exit 0
