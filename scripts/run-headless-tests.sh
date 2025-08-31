#!/bin/bash

# Binnaculum HeadlessRunner Script for CI/CD Integration
# This script provides a command-line interface to run the HeadlessRunner infrastructure

set -euo pipefail

# Default values
PLATFORM="android"
FILTER=""
OUTPUT_FORMAT="xml"
OUTPUT_PATH=""
HEADLESS="true"
COLLECT_ARTIFACTS="false"
ARTIFACT_PATH=""
VERBOSITY="normal"
TIMEOUT="300"
RETRY_COUNT="0"
PARALLEL="false"
TARGET_FRAMEWORK=""

# Help function
show_help() {
    cat << EOF
Binnaculum HeadlessRunner - CI/CD Integration Script

USAGE:
    $0 [OPTIONS]

OPTIONS:
    -p, --platform PLATFORM          Target platform (android, ios, windows, maccatalyst) [default: android]
    -f, --filter FILTER              Filter tests by name pattern (supports wildcards)
    -o, --output-format FORMAT       Output format (console, xml, json) [default: xml]
    --output-path PATH                Path to write output file
    --headless                        Run in headless mode [default: true]
    --collect-artifacts               Collect test artifacts (screenshots, logs)
    --artifact-path PATH              Path to store artifacts
    -v, --verbosity LEVEL             Verbosity (quiet, minimal, normal, detailed, diagnostic) [default: normal]
    -t, --timeout SECONDS            Timeout in seconds [default: 300]
    -r, --retry-count COUNT           Number of retries for failed tests [default: 0]
    --parallel                        Run tests in parallel
    --target-framework FRAMEWORK     Target framework (e.g., net9.0-android)
    -h, --help                        Show this help message

EXAMPLES:
    # Run all Android tests with XML output
    $0 --platform android --output-format xml --output-path results.xml

    # Run filtered tests with artifact collection
    $0 --platform ios --filter "*Core*" --collect-artifacts --artifact-path ./artifacts

    # Run performance tests in parallel with retries
    $0 --platform android --filter "*Performance*" --parallel --retry-count 2

ENVIRONMENT VARIABLES:
    DOTNET_ROOT                       Path to .NET SDK (auto-detected if not set)
    BINNACULUM_ROOT                   Path to Binnaculum repository (auto-detected if not set)

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--platform)
            PLATFORM="$2"
            shift 2
            ;;
        -f|--filter)
            FILTER="$2"
            shift 2
            ;;
        -o|--output-format)
            OUTPUT_FORMAT="$2"
            shift 2
            ;;
        --output-path)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --headless)
            HEADLESS="true"
            shift
            ;;
        --collect-artifacts)
            COLLECT_ARTIFACTS="true"
            shift
            ;;
        --artifact-path)
            ARTIFACT_PATH="$2"
            shift 2
            ;;
        -v|--verbosity)
            VERBOSITY="$2"
            shift 2
            ;;
        -t|--timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        -r|--retry-count)
            RETRY_COUNT="$2"
            shift 2
            ;;
        --parallel)
            PARALLEL="true"
            shift
            ;;
        --target-framework)
            TARGET_FRAMEWORK="$2"
            shift 2
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            echo "Use $0 --help for usage information" >&2
            exit 1
            ;;
    esac
done

# Detect repository root
if [[ -z "${BINNACULUM_ROOT:-}" ]]; then
    SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    BINNACULUM_ROOT="$(dirname "$SCRIPT_DIR")"
fi

# Validate repository structure
if [[ ! -f "$BINNACULUM_ROOT/src/Tests/TestUtils/UI.DeviceTests.Runners/UI.DeviceTests.Runners.csproj" ]]; then
    echo "ERROR: Cannot find HeadlessRunner project. Is BINNACULUM_ROOT correct?" >&2
    echo "BINNACULUM_ROOT: $BINNACULUM_ROOT" >&2
    exit 1
fi

# Determine target framework based on platform if not specified
if [[ -z "$TARGET_FRAMEWORK" ]]; then
    case "$PLATFORM" in
        android)
            TARGET_FRAMEWORK="net9.0-android"
            ;;
        ios)
            TARGET_FRAMEWORK="net9.0-ios"
            ;;
        maccatalyst)
            TARGET_FRAMEWORK="net9.0-maccatalyst"
            ;;
        windows)
            TARGET_FRAMEWORK="net9.0-windows10.0.19041.0"
            ;;
        *)
            echo "ERROR: Unknown platform: $PLATFORM" >&2
            echo "Supported platforms: android, ios, maccatalyst, windows" >&2
            exit 1
            ;;
    esac
fi

# Validate .NET installation
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: dotnet command not found. Please install .NET 9 SDK" >&2
    exit 1
fi

DOTNET_VERSION=$(dotnet --version 2>/dev/null || echo "unknown")
if [[ ! "$DOTNET_VERSION" =~ ^9\. ]]; then
    echo "WARNING: .NET 9 not detected (found: $DOTNET_VERSION). Tests may fail." >&2
fi

echo "🚀 Binnaculum HeadlessRunner Starting..."
echo "   Platform: $PLATFORM"
echo "   Target Framework: $TARGET_FRAMEWORK"
echo "   Filter: ${FILTER:-'All tests'}"
echo "   Output Format: $OUTPUT_FORMAT"
echo "   Repository: $BINNACULUM_ROOT"
echo ""

# Change to repository root
cd "$BINNACULUM_ROOT"

# Build the HeadlessRunner project
echo "🔧 Building HeadlessRunner..."
if ! dotnet build src/Tests/TestUtils/UI.DeviceTests.Runners/UI.DeviceTests.Runners.csproj \
    --framework "$TARGET_FRAMEWORK" \
    --configuration Release \
    --verbosity minimal; then
    echo "ERROR: Failed to build HeadlessRunner project" >&2
    exit 1
fi

echo "✅ Build successful"

# For now, since we can't directly run the HeadlessRunner as an executable,
# we'll create a simple test runner using the existing dotnet test infrastructure
# but with enhanced reporting and artifact collection

echo "🧪 Running tests with HeadlessRunner integration..."

# Prepare output file
if [[ -n "$OUTPUT_PATH" ]]; then
    mkdir -p "$(dirname "$OUTPUT_PATH")"
    TEST_LOGGER="trx;LogFileName=$OUTPUT_PATH"
else
    TEST_LOGGER="console;verbosity=$VERBOSITY"
fi

# Prepare filter arguments
FILTER_ARGS=""
if [[ -n "$FILTER" ]]; then
    FILTER_ARGS="--filter \"$FILTER\""
fi

# Prepare artifact collection
if [[ "$COLLECT_ARTIFACTS" == "true" && -n "$ARTIFACT_PATH" ]]; then
    mkdir -p "$ARTIFACT_PATH"
    echo "📦 Artifacts will be collected to: $ARTIFACT_PATH"
fi

# Run device tests with enhanced configuration
# Note: This is a placeholder implementation that runs device tests in a HeadlessRunner-compatible way
# The actual HeadlessRunner would be invoked here once it's fully integrated
echo "Running device tests for $PLATFORM ($TARGET_FRAMEWORK)..."

TEST_COMMAND="dotnet test src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj \
    --framework $TARGET_FRAMEWORK \
    --configuration Release \
    --logger \"$TEST_LOGGER\" \
    --verbosity $VERBOSITY"

if [[ -n "$FILTER" ]]; then
    TEST_COMMAND="$TEST_COMMAND --filter \"$FILTER\""
fi

# Execute the test command
echo "Executing: $TEST_COMMAND"
if eval "$TEST_COMMAND"; then
    echo "✅ Tests completed successfully"
    EXIT_CODE=0
else
    echo "⚠️  Some tests failed or encountered issues"
    EXIT_CODE=1
fi

# Collect artifacts if requested
if [[ "$COLLECT_ARTIFACTS" == "true" && -n "$ARTIFACT_PATH" ]]; then
    echo "📦 Collecting artifacts..."
    
    # Create artifact summary
    cat > "$ARTIFACT_PATH/test-summary.txt" << EOF
Binnaculum HeadlessRunner Test Summary
Generated: $(date -u +"%Y-%m-%d %H:%M:%S UTC")

Configuration:
- Platform: $PLATFORM
- Target Framework: $TARGET_FRAMEWORK  
- Filter: ${FILTER:-'All tests'}
- Output Format: $OUTPUT_FORMAT
- Verbosity: $VERBOSITY
- Timeout: $TIMEOUT seconds
- Retry Count: $RETRY_COUNT
- Parallel: $PARALLEL

Test Results:
- Exit Code: $EXIT_CODE
- Output Path: ${OUTPUT_PATH:-'Console'}
- Artifacts Path: $ARTIFACT_PATH
EOF

    # Collect any screenshots or logs that may have been generated
    find . -name "*.png" -path "*/TestResults/*" -exec cp {} "$ARTIFACT_PATH/" \; 2>/dev/null || true
    find . -name "*.log" -path "*/TestResults/*" -exec cp {} "$ARTIFACT_PATH/" \; 2>/dev/null || true
    
    echo "✅ Artifacts collected"
fi

echo ""
echo "🎉 HeadlessRunner execution completed"
echo "   Exit Code: $EXIT_CODE"
if [[ -n "$OUTPUT_PATH" && -f "$OUTPUT_PATH" ]]; then
    echo "   Results: $OUTPUT_PATH"
fi
if [[ "$COLLECT_ARTIFACTS" == "true" && -n "$ARTIFACT_PATH" ]]; then
    echo "   Artifacts: $ARTIFACT_PATH"
fi

exit $EXIT_CODE