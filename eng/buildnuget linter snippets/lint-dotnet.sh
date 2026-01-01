#!/bin/bash
# dotnet-architecture-linter: Validates MSBuild/NuGet architecture
# Exit code 0 = clean, 1 = violations found

REPO_ROOT="${1:-.}"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Use a temp file to track violations across subshells
VIOLATION_FILE=$(mktemp)
echo "0" > "$VIOLATION_FILE"

add_violation() {
    local current=$(cat "$VIOLATION_FILE")
    echo $((current + 1)) > "$VIOLATION_FILE"
}

get_violations() {
    cat "$VIOLATION_FILE"
}

echo "════════════════════════════════════════════════════════════════"
echo "  .NET Architecture Linter"
echo "════════════════════════════════════════════════════════════════"
echo ""

# ─────────────────────────────────────────────────────────────────────
# Rule A: No hardcoded versions in Directory.Packages.props
# ─────────────────────────────────────────────────────────────────────
echo "📋 Rule A: Checking for hardcoded versions in Directory.Packages.props..."

DPP="$REPO_ROOT/Directory.Packages.props"
if [[ -f "$DPP" ]]; then
    # Find PackageVersion with Version="X.Y.Z" (not Version="$(xxx)")
    grep -nE 'PackageVersion.*Version="[0-9]' "$DPP" 2>/dev/null | while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            LINE_NUM=$(echo "$line" | cut -d: -f1)
            CONTENT=$(echo "$line" | cut -d: -f2-)
            
            # Extract package name
            PACKAGE=$(echo "$CONTENT" | grep -oP 'Include="\K[^"]+' 2>/dev/null || echo "Unknown")
            
            echo -e "${RED}❌ RULE A VIOLATION${NC}: Directory.Packages.props:$LINE_NUM"
            echo "   $CONTENT"
            echo ""
            echo "   Package '$PACKAGE' has hardcoded version."
            echo "   FIX: Use Version=\"\$(${PACKAGE//\.}Version)\" and define in Version.props"
            echo ""
            add_violation
        fi
    done
else
    echo -e "${YELLOW}⚠️  Directory.Packages.props not found${NC}"
fi

# ─────────────────────────────────────────────────────────────────────
# Rule B: Version.props single owner
# ─────────────────────────────────────────────────────────────────────
echo "📋 Rule B: Checking Version.props import ownership..."

# Track if Directory.Packages.props has the import
DPP_HAS_IMPORT="false"

# Find all .props and .targets files
while IFS= read -r file; do
    if [[ -n "$file" && -f "$file" ]]; then
        # Check if file imports Version.props
        if grep -q 'Import.*Version\.props' "$file" 2>/dev/null; then
            BASENAME=$(basename "$file")
            RELPATH="${file#$REPO_ROOT/}"
            
            if [[ "$BASENAME" == "Directory.Packages.props" ]]; then
                DPP_HAS_IMPORT="true"
            else
                # Unauthorized import
                LINE_INFO=$(grep -n 'Import.*Version\.props' "$file" | head -1)
                LINE_NUM=$(echo "$LINE_INFO" | cut -d: -f1)
                
                echo -e "${RED}❌ RULE B VIOLATION${NC}: $RELPATH:$LINE_NUM"
                echo "   $(grep 'Import.*Version\.props' "$file" | head -1 | xargs)"
                echo ""
                echo "   Version.props must ONLY be imported by Directory.Packages.props (single owner)."
                echo "   This duplicate import causes variable resolution failures during NuGet restore."
                echo ""
                echo "   FIX: Delete this Import line. Directory.Packages.props owns Version.props import."
                echo ""
                add_violation
            fi
        fi
    fi
done < <(find "$REPO_ROOT" -type f \( -name "*.props" -o -name "*.targets" \) \
    -not -path "*/obj/*" \
    -not -path "*/bin/*" \
    -not -path "*/.git/*" \
    -not -path "*/node_modules/*" \
    2>/dev/null)

# Check if Directory.Packages.props exists but doesn't have the import
if [[ -f "$DPP" && "$DPP_HAS_IMPORT" == "false" ]]; then
    echo -e "${RED}❌ RULE B VIOLATION${NC}: Directory.Packages.props missing Version.props import"
    echo "   Directory.Packages.props must import Version.props to define package versions."
    echo ""
    echo "   FIX: Add to Directory.Packages.props:"
    echo "   <Import Project=\"\$(MSBuildThisFileDirectory)src/common/Version.props\"/>"
    echo ""
    add_violation
fi

# ─────────────────────────────────────────────────────────────────────
# Rule G: No PackageReference with Version attribute
# ─────────────────────────────────────────────────────────────────────
echo "📋 Rule G: Checking for inline PackageReference versions..."

while IFS= read -r file; do
    if [[ -n "$file" && -f "$file" ]]; then
        # Find PackageReference with Version= (not VersionOverride=)
        grep -nE 'PackageReference.*Version=' "$file" 2>/dev/null | grep -v 'VersionOverride' | while IFS= read -r line; do
            if [[ -n "$line" ]]; then
                LINE_NUM=$(echo "$line" | cut -d: -f1)
                CONTENT=$(echo "$line" | cut -d: -f2-)
                RELPATH="${file#$REPO_ROOT/}"
                
                # Extract package name
                PACKAGE=$(echo "$CONTENT" | grep -oP 'Include="\K[^"]+' 2>/dev/null || echo "Unknown")
                
                echo -e "${RED}❌ RULE G VIOLATION${NC}: $RELPATH:$LINE_NUM"
                echo "   $CONTENT"
                echo ""
                echo "   Projects must use Central Package Management, not inline versions."
                echo ""
                echo "   FIX:"
                echo "   1. Add to Directory.Packages.props:"
                echo "      <PackageVersion Include=\"$PACKAGE\" Version=\"\$(${PACKAGE//\.}Version)\"/>"
                echo "   2. Add to Version.props:"
                echo "      <${PACKAGE//\.}Version>X.Y.Z</${PACKAGE//\.}Version>"
                echo "   3. Change csproj to:"
                echo "      <PackageReference Include=\"$PACKAGE\"/>"
                echo ""
                add_violation
            fi
        done
    fi
done < <(find "$REPO_ROOT" -type f -name "*.csproj" \
    -not -path "*/obj/*" \
    -not -path "*/bin/*" \
    -not -path "*/.git/*" \
    2>/dev/null)

# ─────────────────────────────────────────────────────────────────────
# Summary
# ─────────────────────────────────────────────────────────────────────
echo "════════════════════════════════════════════════════════════════"

TOTAL_VIOLATIONS=$(get_violations)
rm -f "$VIOLATION_FILE"

if [[ "$TOTAL_VIOLATIONS" -eq 0 ]]; then
    echo -e "${GREEN}✅ All rules passed${NC} - safe to commit"
    exit 0
else
    echo -e "${RED}❌ $TOTAL_VIOLATIONS violation(s) found${NC}"
    echo ""
    echo "⛔ DO NOT commit or push until all violations are fixed."
    echo "   Fix each violation, run 'dotnet build', then re-run this linter."
    exit 1
fi
