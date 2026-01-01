#!/bin/bash
# canary.sh - Fast SDK validation (<10 seconds)
#
# USAGE:
#   ./canary.sh           # Quick validation (build + test)
#   ./canary.sh --build   # Build only (fastest)
#   ./canary.sh --test    # Test only (requires prior build)
#   ./canary.sh --diag    # With diagnostics output
#   ./canary.sh --binlog  # Generate binlog for debugging

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/ANcpLua.Sdk.Canary.csproj"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Timer
START_TIME=$(date +%s)

print_header() {
    echo ""
    echo "═══════════════════════════════════════════════════════════════"
    echo " ANcpLua.NET.Sdk Canary - Fast Validation"
    echo "═══════════════════════════════════════════════════════════════"
    echo ""
}

print_time() {
    END_TIME=$(date +%s)
    DURATION=$((END_TIME - START_TIME))
    echo ""
    echo -e "${GREEN}✓ Completed in ${DURATION}s${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
    exit 1
}

# Parse arguments
BUILD_ONLY=false
TEST_ONLY=false
DIAGNOSTICS=false
BINLOG=false

for arg in "$@"; do
    case $arg in
        --build)   BUILD_ONLY=true ;;
        --test)    TEST_ONLY=true ;;
        --diag)    DIAGNOSTICS=true ;;
        --binlog)  BINLOG=true ;;
        --help|-h)
            echo "Usage: ./canary.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --build   Build only (fastest)"
            echo "  --test    Test only (requires prior build)"
            echo "  --diag    Enable ANcpLua diagnostics output"
            echo "  --binlog  Generate MSBuild binlog"
            exit 0
            ;;
    esac
done

print_header

# Build extra args
BUILD_ARGS="-c Release -v:q --nologo"
if [ "$DIAGNOSTICS" = true ]; then
    BUILD_ARGS="$BUILD_ARGS -p:ANcpLuaDiagnostics=true"
fi
if [ "$BINLOG" = true ]; then
    BUILD_ARGS="$BUILD_ARGS -bl:canary.binlog"
fi

# Build phase
if [ "$TEST_ONLY" = false ]; then
    echo "► Building canary project..."
    
    # Build net10.0
    echo "  → net10.0"
    if ! dotnet build "$PROJECT" -f net10.0 $BUILD_ARGS 2>&1; then
        print_error "net10.0 build failed"
    fi
    
    # Build netstandard2.0 (polyfill validation)
    echo "  → netstandard2.0"
    if ! dotnet build "$PROJECT" -f netstandard2.0 $BUILD_ARGS 2>&1; then
        print_error "netstandard2.0 build failed (polyfill issue)"
    fi
    
    echo -e "${GREEN}✓ Build passed${NC}"
fi

# Test phase
if [ "$BUILD_ONLY" = false ]; then
    echo ""
    echo "► Running canary tests..."
    
    # Run tests (net10.0 only - netstandard2.0 has no test runner)
    if ! dotnet test "$PROJECT" -f net10.0 --no-build -c Release --nologo -v:q 2>&1; then
        print_error "Tests failed"
    fi
    
    echo -e "${GREEN}✓ Tests passed${NC}"
fi

print_time

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo -e " ${GREEN}SDK CANARY PASSED${NC} - Safe to run full CI"
echo "═══════════════════════════════════════════════════════════════"
echo ""
