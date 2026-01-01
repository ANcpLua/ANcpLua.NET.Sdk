#!/bin/bash
# Git pre-push hook for .NET architecture validation
# Install: cp pre-push.sh .git/hooks/pre-push && chmod +x .git/hooks/pre-push

set -e

REPO_ROOT=$(git rev-parse --show-toplevel)
LINTER="$REPO_ROOT/.claude/skills/dotnet-architecture-linter/scripts/lint-dotnet.sh"

# Check if linter exists
if [[ ! -f "$LINTER" ]]; then
    # Try alternate location
    LINTER="$REPO_ROOT/scripts/lint-dotnet.sh"
fi

if [[ ! -f "$LINTER" ]]; then
    echo "⚠️  Linter not found, skipping architecture check"
    exit 0
fi

echo "🔍 Running .NET architecture linter before push..."
echo ""

if ! bash "$LINTER" "$REPO_ROOT"; then
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "❌ PUSH BLOCKED: Architecture violations detected"
    echo "════════════════════════════════════════════════════════════════"
    echo ""
    echo "Fix all violations listed above, then try again."
    echo ""
    exit 1
fi

echo ""
echo "✅ Architecture check passed, proceeding with push..."
