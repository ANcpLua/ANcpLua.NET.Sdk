#!/bin/bash
# apply-polyfills-consolidation.sh
# Applies the polyfills consolidation changes to ANcpLua.NET.Sdk

set -euo pipefail

SDK_ROOT="${SDK_ROOT:-$HOME/ANcpLua.NET.Sdk}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "═══════════════════════════════════════════════════════════════════════════"
echo " ANcpLua.NET.Sdk - Polyfills Consolidation"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""
echo "SDK Root: $SDK_ROOT"
echo ""

# Verify SDK root exists
if [[ ! -d "$SDK_ROOT/src/common" ]]; then
    echo "❌ ERROR: SDK root not found at $SDK_ROOT"
    echo "   Set SDK_ROOT environment variable to your ANcpLua.NET.Sdk directory"
    exit 1
fi

# Backup existing files
BACKUP_DIR="$SDK_ROOT/.backup-$(date +%Y%m%d-%H%M%S)"
echo "📦 Creating backup in $BACKUP_DIR..."
mkdir -p "$BACKUP_DIR/common"
cp "$SDK_ROOT/src/common/LegacySupport.props" "$BACKUP_DIR/common/" 2>/dev/null || true
cp "$SDK_ROOT/src/common/LegacySupport.targets" "$BACKUP_DIR/common/" 2>/dev/null || true
cp "$SDK_ROOT/src/common/Shared.props" "$BACKUP_DIR/common/" 2>/dev/null || true
cp "$SDK_ROOT/src/common/Shared.targets" "$BACKUP_DIR/common/" 2>/dev/null || true
cp "$SDK_ROOT/src/common/Common.props" "$BACKUP_DIR/common/" 2>/dev/null || true
cp "$SDK_ROOT/src/common/Common.targets" "$BACKUP_DIR/common/" 2>/dev/null || true
cp "$SDK_ROOT/src/ANcpLua.NET.Sdk.nuspec" "$BACKUP_DIR/" 2>/dev/null || true
echo "   Backup created"
echo ""

# Copy new files
echo "📝 Applying new files..."

echo "   → Polyfills.props"
cp "$SCRIPT_DIR/Polyfills.props" "$SDK_ROOT/src/common/"

echo "   → Polyfills.targets"
cp "$SCRIPT_DIR/Polyfills.targets" "$SDK_ROOT/src/common/"

echo "   → Shared.props (simplified)"
cp "$SCRIPT_DIR/Shared.props" "$SDK_ROOT/src/common/"

echo "   → Shared.targets (simplified)"
cp "$SCRIPT_DIR/Shared.targets" "$SDK_ROOT/src/common/"

echo "   → LegacySupport.props (deprecated, forwards)"
cp "$SCRIPT_DIR/LegacySupport.props" "$SDK_ROOT/src/common/"

echo "   → LegacySupport.targets (deprecated, forwards)"
cp "$SCRIPT_DIR/LegacySupport.targets" "$SDK_ROOT/src/common/"

echo "   → Common.props (updated imports)"
cp "$SCRIPT_DIR/Common.props" "$SDK_ROOT/src/common/"

echo "   → Common.targets (updated imports)"
cp "$SCRIPT_DIR/Common.targets" "$SDK_ROOT/src/common/"

echo "   → ANcpLua.NET.Sdk.nuspec (updated)"
cp "$SCRIPT_DIR/ANcpLua.NET.Sdk.nuspec" "$SDK_ROOT/src/"

# Ensure StringExtensions.cs exists
if [[ ! -f "$SDK_ROOT/eng/LegacySupport/StringExtensions/StringExtensions.cs" ]]; then
    echo "   → Creating StringExtensions.cs"
    mkdir -p "$SDK_ROOT/eng/LegacySupport/StringExtensions"
    cp "$SCRIPT_DIR/../StringExtensions.cs" "$SDK_ROOT/eng/LegacySupport/StringExtensions/" 2>/dev/null || \
        echo "     ⚠️  StringExtensions.cs not found in script dir, create manually"
fi

echo ""
echo "✅ Files applied successfully!"
echo ""

# Version bump reminder
echo "═══════════════════════════════════════════════════════════════════════════"
echo " NEXT STEPS"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""
echo " 1. Bump version in src/common/Version.props:"
echo "    <ANcpSdkPackageVersion>1.3.27</ANcpSdkPackageVersion>"
echo "    →"
echo "    <ANcpSdkPackageVersion>1.4.0</ANcpSdkPackageVersion>"
echo ""
echo " 2. Build and pack:"
echo "    cd $SDK_ROOT"
echo "    dotnet pack src/ANcpLua.NET.Sdk.csproj -c Release"
echo ""
echo " 3. Test locally:"
echo "    dotnet nuget push artifacts/*.nupkg --source local"
echo "    cd ~/RiderProjects/ANcpLua.Analyzers"
echo "    dotnet nuget locals all --clear"
echo "    dotnet build -c Release"
echo ""
echo " 4. If tests pass, commit and push:"
echo "    git add -A"
echo "    git commit -m 'feat: Consolidate polyfills system (fixes path mismatch)'"
echo "    git push"
echo ""
echo "═══════════════════════════════════════════════════════════════════════════"
