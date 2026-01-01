#!/bin/bash
# MSBuild Architecture Linter Hook
# Triggers after Edit/Write on MSBuild files
# Exit 2 = feed stderr back to Claude

set -euo pipefail

# Read hook input
input=$(cat)
tool_name=$(echo "$input" | jq -r '.tool_name // ""')

# Get the file path from tool input
if [[ "$tool_name" == "Edit" ]]; then
    file_path=$(echo "$input" | jq -r '.tool_input.file_path // ""')
elif [[ "$tool_name" == "Write" ]]; then
    file_path=$(echo "$input" | jq -r '.tool_input.file_path // ""')
else
    exit 0
fi

# Check if it's an MSBuild file
case "$file_path" in
    *.props|*.targets|*.csproj|*.slnx|nuget.config|global.json)
        # MSBuild file - run linter
        ;;
    *)
        # Not an MSBuild file - skip
        exit 0
        ;;
esac

# Run the linter
REPO_ROOT="${CLAUDE_PROJECT_DIR:-.}"
LINTER_SCRIPT="$REPO_ROOT/scripts/lint-dotnet.sh"

if [[ ! -f "$LINTER_SCRIPT" ]]; then
    echo "Linter script not found at $LINTER_SCRIPT" >&2
    exit 0
fi

# Capture linter output
LINT_OUTPUT=$("$LINTER_SCRIPT" "$REPO_ROOT" 2>&1) || true
LINT_EXIT=$?

if [[ "$LINT_EXIT" -ne 0 ]]; then
    # Violations found - feed back to Claude
    cat >&2 <<EOF
{
  "systemMessage": "MSBuild Architecture Linter detected violations after editing $file_path. You MUST fix these violations before continuing or committing.

$LINT_OUTPUT

REQUIRED ACTIONS:
1. Fix each violation shown above
2. Re-run 'dotnet build' to verify
3. Do NOT commit until linter passes

DO NOT ask the user what to do. DO NOT propose alternatives. Fix the violations NOW."
}
EOF
    exit 2
fi

# Clean - output success
echo "MSBuild linter: All rules passed for $file_path"
exit 0
