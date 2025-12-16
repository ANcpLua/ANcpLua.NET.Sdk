#!/bin/bash
# run-tests.sh - Run tests with proper environment setup
#
# Usage:
#   ./run-tests.sh                    # Run all tests
#   ./run-tests.sh --filter "Name"    # Run filtered tests
#   ./run-tests.sh --quick            # Run only fast tests (exclude SDK integration tests)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Build packages first if not already built
if [ ! -d "artifacts" ] || [ -z "$(ls -A artifacts/*.nupkg 2>/dev/null)" ]; then
    echo "Building SDK packages..."
    dotnet pack src -c Release -o artifacts -p:Version=999.9.9
fi

# Set environment variables for PackageFixture
export CI=true
export NUGET_DIRECTORY="$SCRIPT_DIR/artifacts"
export PACKAGE_VERSION=999.9.9

# Parse arguments
FILTER=""
QUICK=false
EXTRA_ARGS=()

while [[ $# -gt 0 ]]; do
    case $1 in
        --filter)
            FILTER="$2"
            shift 2
            ;;
        --quick)
            QUICK=true
            shift
            ;;
        *)
            EXTRA_ARGS+=("$1")
            shift
            ;;
    esac
done

# Build filter
if [ "$QUICK" = true ]; then
    # Exclude heavy SDK integration tests
    FILTER="FullyQualifiedName!~Sdk10_0"
fi

# Run tests
echo "Running tests with CI=$CI NUGET_DIRECTORY=$NUGET_DIRECTORY"
echo ""

TEST_CMD="dotnet test tests/ANcpLua.Sdk.Tests"

if [ -n "$FILTER" ]; then
    TEST_CMD="$TEST_CMD --filter \"$FILTER\""
fi

if [ ${#EXTRA_ARGS[@]} -gt 0 ]; then
    TEST_CMD="$TEST_CMD ${EXTRA_ARGS[*]}"
fi

echo "Command: $TEST_CMD"
echo ""

eval "$TEST_CMD"
