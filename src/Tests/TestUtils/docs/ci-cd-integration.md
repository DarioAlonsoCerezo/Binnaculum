# CI/CD Integration Guide

Complete guide for integrating Binnaculum's TestUtils infrastructure with CI/CD pipelines, focusing on GitHub Actions and local automation.

## GitHub Actions Integration

### Basic Workflow Setup

Create `.github/workflows/device-tests.yml`:

```yaml
name: Device Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  android-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 30
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET 9
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Install MAUI workloads
      run: |
        dotnet workload install maui-android
      timeout-minutes: 5
    
    - name: Restore dependencies
      run: dotnet restore
      timeout-minutes: 2
    
    - name: Build Core project
      run: |
        dotnet build src/Core/Core.fsproj --no-restore
      timeout-minutes: 2
    
    - name: Run Core tests
      run: |
        dotnet test src/Tests/Core.Tests/Core.Tests.fsproj --no-build --verbosity normal
        # Expected: 80/87 tests pass (7 MAUI-dependent tests fail in headless environment)
      continue-on-error: false
    
    - name: Build Android device tests
      run: |
        dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-android --no-restore
      timeout-minutes: 8  # Android builds take ~95 seconds
    
    - name: Run headless device tests
      run: |
        ./scripts/run-headless-tests.sh \
          --platform android \
          --output-format xml \
          --output-path test-results.xml \
          --collect-artifacts \
          --artifact-path ./test-artifacts
      timeout-minutes: 10
    
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: android-test-results
        path: |
          test-results.xml
          test-artifacts/
    
    - name: Publish test results
      uses: dorny/test-reporter@v1
      if: always()
      with:
        name: Android Device Tests
        path: test-results.xml
        reporter: java-junit

  windows-tests:
    runs-on: windows-latest
    timeout-minutes: 25
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET 9
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Install MAUI workloads
      run: |
        dotnet workload install maui-windows
      timeout-minutes: 5
    
    - name: Build and test Windows target
      run: |
        dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-windows10.0.19041.0 --no-restore
        ./scripts/run-headless-tests.sh --platform windows --output-format xml --output-path windows-test-results.xml
      timeout-minutes: 10
    
    - name: Upload Windows test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: windows-test-results
        path: windows-test-results.xml

  ios-tests:
    runs-on: macos-latest
    timeout-minutes: 35
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET 9
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Install MAUI workloads
      run: |
        dotnet workload install maui-ios maui-maccatalyst
      timeout-minutes: 8
    
    - name: Build iOS tests
      run: |
        dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-ios --no-restore
      timeout-minutes: 10
    
    - name: Build MacCatalyst tests  
      run: |
        dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-maccatalyst --no-restore
      timeout-minutes: 10
    
    - name: Run iOS Simulator tests
      run: |
        ./scripts/run-headless-tests.sh --platform ios --output-format xml --output-path ios-test-results.xml
      timeout-minutes: 12
    
    - name: Upload iOS test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: ios-test-results
        path: ios-test-results.xml
```

### Advanced Pipeline with Performance Monitoring

```yaml
name: Advanced Device Tests with Performance

on:
  push:
    branches: [ main ]
  schedule:
    - cron: '0 2 * * *'  # Daily performance monitoring at 2 AM

jobs:
  performance-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 45
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup environment
      uses: ./.github/actions/setup-dotnet-maui
      with:
        dotnet-version: '9.0.x'
    
    - name: Run performance benchmarks
      run: |
        ./scripts/run-performance-tests.sh \
          --platform android \
          --benchmark-suite investment-calculations \
          --output-format json \
          --output-path performance-results.json
      timeout-minutes: 20
    
    - name: Analyze performance regression
      run: |
        python scripts/analyze-performance-regression.py \
          --current performance-results.json \
          --baseline performance-baseline.json \
          --threshold 10  # 10% regression threshold
    
    - name: Upload performance results
      uses: actions/upload-artifact@v3
      with:
        name: performance-results
        path: |
          performance-results.json
          performance-analysis.html
    
    - name: Update performance baselines
      if: github.ref == 'refs/heads/main'
      run: |
        cp performance-results.json performance-baseline.json
        git config --local user.email "action@github.com"
        git config --local user.name "GitHub Action"
        git add performance-baseline.json
        git commit -m "Update performance baseline [skip ci]" || exit 0
        git push

  flaky-test-detection:
    runs-on: ubuntu-latest
    timeout-minutes: 60
    strategy:
      matrix:
        run: [1, 2, 3, 4, 5]  # Run tests 5 times to detect flaky tests
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup environment
      uses: ./.github/actions/setup-dotnet-maui
    
    - name: Run tests (attempt ${{ matrix.run }})
      run: |
        ./scripts/run-headless-tests.sh \
          --platform android \
          --retry-count 0 \
          --output-format json \
          --output-path test-results-run-${{ matrix.run }}.json
      continue-on-error: true
    
    - name: Upload test results
      uses: actions/upload-artifact@v3
      with:
        name: flaky-test-results-run-${{ matrix.run }}
        path: test-results-run-${{ matrix.run }}.json
  
  analyze-flaky-tests:
    runs-on: ubuntu-latest
    needs: flaky-test-detection
    if: always()
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Download all test results
      uses: actions/download-artifact@v3
      with:
        path: flaky-test-results/
    
    - name: Analyze flaky tests
      run: |
        python scripts/analyze-flaky-tests.py \
          --results-dir flaky-test-results/ \
          --threshold 0.8  # Tests that pass <80% of time are flaky
    
    - name: Create issue for flaky tests
      if: steps.analyze.outputs.flaky-tests-found == 'true'
      uses: actions/github-script@v6
      with:
        script: |
          const flakyTests = JSON.parse(process.env.FLAKY_TESTS);
          const issueBody = `## Flaky Tests Detected\n\n${flakyTests.map(test => `- ${test.name} (${test.successRate}% success rate)`).join('\n')}`;
          github.rest.issues.create({
            owner: context.repo.owner,
            repo: context.repo.repo,
            title: 'Flaky Tests Detected in Device Tests',
            body: issueBody,
            labels: ['bug', 'flaky-test', 'testing']
          });
```

## Local CI/CD Scripts

### Headless Test Runner Script

Create `scripts/run-headless-tests.sh`:

```bash
#!/bin/bash
set -e

# Binnaculum TestUtils Headless Runner
# Usage: ./run-headless-tests.sh --platform android --collect-artifacts

PLATFORM="android"
OUTPUT_FORMAT="console"
OUTPUT_PATH=""
COLLECT_ARTIFACTS=false
ARTIFACT_PATH="./artifacts"
PARALLEL=false
RETRY_COUNT=1
FILTER=""
VERBOSE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --platform)
            PLATFORM="$2"
            shift 2
            ;;
        --output-format)
            OUTPUT_FORMAT="$2"
            shift 2
            ;;
        --output-path)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --collect-artifacts)
            COLLECT_ARTIFACTS=true
            shift
            ;;
        --artifact-path)
            ARTIFACT_PATH="$2"
            shift 2
            ;;
        --parallel)
            PARALLEL=true
            shift
            ;;
        --retry-count)
            RETRY_COUNT="$2"
            shift 2
            ;;
        --filter)
            FILTER="$2"
            shift 2
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

# Set platform-specific target framework
case $PLATFORM in
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
        echo "Unsupported platform: $PLATFORM"
        echo "Supported platforms: android, ios, maccatalyst, windows"
        exit 1
        ;;
esac

echo "🚀 Starting Binnaculum TestUtils Headless Runner"
echo "Platform: $PLATFORM ($TARGET_FRAMEWORK)"
echo "Output Format: $OUTPUT_FORMAT"
echo "Artifacts: $([ "$COLLECT_ARTIFACTS" = true ] && echo "enabled" || echo "disabled")"

# Ensure build directory exists and is clean
BUILD_DIR="src/Tests/TestUtils/UI.DeviceTests"
if [ "$COLLECT_ARTIFACTS" = true ]; then
    mkdir -p "$ARTIFACT_PATH"
    rm -rf "$ARTIFACT_PATH"/*
fi

# Build tests if not already built
echo "📦 Building device tests for $PLATFORM..."
BUILD_START=$(date +%s)

dotnet build "$BUILD_DIR/UI.DeviceTests.csproj" \
    -f "$TARGET_FRAMEWORK" \
    -c Release \
    --no-restore \
    $([ "$VERBOSE" = true ] && echo "--verbosity detailed" || echo "--verbosity minimal")

BUILD_END=$(date +%s)
BUILD_TIME=$((BUILD_END - BUILD_START))
echo "✅ Build completed in ${BUILD_TIME}s"

# Prepare test execution parameters
TEST_PARAMS=()
TEST_PARAMS+=("-f" "$TARGET_FRAMEWORK")
TEST_PARAMS+=("-c" "Release")
TEST_PARAMS+=("--no-build")
TEST_PARAMS+=("--logger" "console;verbosity=normal")

if [ -n "$FILTER" ]; then
    TEST_PARAMS+=("--filter" "$FILTER")
fi

if [ "$OUTPUT_FORMAT" = "xml" ] && [ -n "$OUTPUT_PATH" ]; then
    TEST_PARAMS+=("--logger" "junit;LogFilePath=$OUTPUT_PATH")
elif [ "$OUTPUT_FORMAT" = "trx" ] && [ -n "$OUTPUT_PATH" ]; then
    TEST_PARAMS+=("--logger" "trx;LogFileName=$OUTPUT_PATH")
fi

# Execute tests with retry logic
echo "🧪 Running device tests..."
TEST_START=$(date +%s)

for attempt in $(seq 1 $RETRY_COUNT); do
    echo "Attempt $attempt of $RETRY_COUNT"
    
    if dotnet test "$BUILD_DIR/UI.DeviceTests.csproj" "${TEST_PARAMS[@]}"; then
        echo "✅ Tests passed on attempt $attempt"
        break
    elif [ $attempt -eq $RETRY_COUNT ]; then
        echo "❌ Tests failed after $RETRY_COUNT attempts"
        
        # Collect failure artifacts
        if [ "$COLLECT_ARTIFACTS" = true ]; then
            echo "📁 Collecting failure artifacts..."
            
            # Copy build logs
            find . -name "*.binlog" -exec cp {} "$ARTIFACT_PATH/" \; 2>/dev/null || true
            
            # Copy test logs
            find . -name "*.trx" -exec cp {} "$ARTIFACT_PATH/" \; 2>/dev/null || true
            find . -name "TestResults" -type d -exec cp -r {} "$ARTIFACT_PATH/" \; 2>/dev/null || true
            
            # System information
            {
                echo "=== System Information ==="
                echo "Date: $(date)"
                echo "Platform: $PLATFORM"
                echo "Target Framework: $TARGET_FRAMEWORK"
                echo ".NET Version: $(dotnet --version)"
                echo "OS: $(uname -a)"
                echo ""
                echo "=== Environment Variables ==="
                env | grep -E "(DOTNET_|ANDROID_|JAVA_)" || true
                echo ""
                echo "=== Disk Space ==="
                df -h
                echo ""
                echo "=== Memory Usage ==="
                free -h 2>/dev/null || vm_stat 2>/dev/null || true
            } > "$ARTIFACT_PATH/system-info.txt"
            
            echo "📁 Artifacts collected in: $ARTIFACT_PATH"
        fi
        
        exit 1
    else
        echo "⚠️  Tests failed on attempt $attempt, retrying..."
        sleep 5
    fi
done

TEST_END=$(date +%s)
TEST_TIME=$((TEST_END - TEST_START))
echo "✅ Tests completed in ${TEST_TIME}s"

# Generate summary report
if [ "$OUTPUT_FORMAT" = "json" ] || [ "$COLLECT_ARTIFACTS" = true ]; then
    SUMMARY_FILE="${ARTIFACT_PATH:-./}/test-summary.json"
    
    {
        echo "{"
        echo "  \"timestamp\": \"$(date -Iseconds)\","
        echo "  \"platform\": \"$PLATFORM\","
        echo "  \"targetFramework\": \"$TARGET_FRAMEWORK\","
        echo "  \"buildTime\": $BUILD_TIME,"
        echo "  \"testTime\": $TEST_TIME,"
        echo "  \"totalTime\": $((BUILD_TIME + TEST_TIME)),"
        echo "  \"success\": true,"
        echo "  \"retryAttempts\": $attempt"
        echo "}"
    } > "$SUMMARY_FILE"
    
    echo "📊 Summary report: $SUMMARY_FILE"
fi

echo "🎉 Headless test execution completed successfully!"
```

### Performance Testing Script

Create `scripts/run-performance-tests.sh`:

```bash
#!/bin/bash
set -e

# Binnaculum Performance Testing Script
# Focuses on investment calculation performance and mobile constraints

PLATFORM="android"
BENCHMARK_SUITE="all"
OUTPUT_FORMAT="console"
OUTPUT_PATH=""
ITERATIONS=5
WARMUP_ITERATIONS=2

while [[ $# -gt 0 ]]; do
    case $1 in
        --platform)
            PLATFORM="$2"
            shift 2
            ;;
        --benchmark-suite)
            BENCHMARK_SUITE="$2"
            shift 2
            ;;
        --output-format)
            OUTPUT_FORMAT="$2"
            shift 2
            ;;
        --output-path)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        --iterations)
            ITERATIONS="$2"
            shift 2
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

echo "🏃 Starting Binnaculum Performance Tests"
echo "Platform: $PLATFORM"
echo "Benchmark Suite: $BENCHMARK_SUITE"
echo "Iterations: $ITERATIONS (+ $WARMUP_ITERATIONS warmup)"

# Build performance test project
echo "📦 Building performance tests..."
dotnet build src/Tests/Core.Tests/Core.Tests.fsproj -c Release

# Run specific benchmark suites
case $BENCHMARK_SUITE in
    "investment-calculations"|"all")
        echo "🧮 Running investment calculation benchmarks..."
        dotnet test src/Tests/Core.Tests/Core.Tests.fsproj \
            --filter "BrokerFinancialSnapshotManager" \
            --configuration Release \
            --logger "console;verbosity=detailed" \
            -- RunConfiguration.TestSessionTimeout=300000
        ;;
    "ui-performance"|"all")
        echo "📱 Running UI performance benchmarks..."
        ./scripts/run-headless-tests.sh \
            --platform "$PLATFORM" \
            --filter "Performance" \
            --output-format json \
            --output-path "ui-performance-results.json"
        ;;
    "memory-stress"|"all")
        echo "🧠 Running memory stress tests..."
        ./scripts/run-headless-tests.sh \
            --platform "$PLATFORM" \
            --filter "MemoryLeak|LargeDataset" \
            --output-format json \
            --output-path "memory-stress-results.json"
        ;;
esac

# Generate performance report
if [ "$OUTPUT_FORMAT" = "json" ] && [ -n "$OUTPUT_PATH" ]; then
    echo "📊 Generating performance report..."
    
    {
        echo "{"
        echo "  \"timestamp\": \"$(date -Iseconds)\","
        echo "  \"platform\": \"$PLATFORM\","
        echo "  \"benchmarkSuite\": \"$BENCHMARK_SUITE\","
        echo "  \"iterations\": $ITERATIONS,"
        echo "  \"results\": {"
        
        # Include specific performance metrics
        if [ -f "ui-performance-results.json" ]; then
            echo "    \"uiPerformance\": $(cat ui-performance-results.json),"
        fi
        
        if [ -f "memory-stress-results.json" ]; then
            echo "    \"memoryStress\": $(cat memory-stress-results.json),"
        fi
        
        echo "    \"systemInfo\": {"
        echo "      \"dotnetVersion\": \"$(dotnet --version)\","
        echo "      \"os\": \"$(uname -s)\","
        echo "      \"architecture\": \"$(uname -m)\""
        echo "    }"
        echo "  }"
        echo "}"
    } > "$OUTPUT_PATH"
    
    echo "📊 Performance report saved: $OUTPUT_PATH"
fi

echo "✅ Performance testing completed"
```

## Local Development Workflow

### Pre-commit Hooks

Create `.githooks/pre-commit`:

```bash
#!/bin/bash
# Pre-commit hook for Binnaculum TestUtils

echo "🔍 Running pre-commit checks for TestUtils..."

# Check if TestUtils files were modified
TESTUTILS_MODIFIED=$(git diff --cached --name-only | grep "src/Tests/TestUtils" || true)

if [ -z "$TESTUTILS_MODIFIED" ]; then
    echo "✅ No TestUtils changes detected, skipping device tests"
    exit 0
fi

echo "📱 TestUtils changes detected, running quick validation..."

# Run Core tests (quick validation of F# business logic)
echo "🧪 Running Core tests..."
if ! dotnet test src/Tests/Core.Tests/Core.Tests.fsproj --verbosity minimal; then
    echo "❌ Core tests failed. Please fix before committing."
    exit 1
fi

# Build Android tests (compilation check)
echo "📦 Building Android tests..."
if ! dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-android --verbosity minimal; then
    echo "❌ Android build failed. Please fix before committing."
    exit 1
fi

# Run TestUtils framework validation tests
echo "🧪 Running TestUtils framework tests..."
if ! dotnet test src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj \
    --filter "CoreDeviceTestingFrameworkTests" \
    -f net9.0-android \
    --verbosity minimal; then
    echo "❌ TestUtils framework tests failed. Please fix before committing."
    exit 1
fi

echo "✅ Pre-commit checks passed!"
```

### Development Task Automation

Create `scripts/dev-setup.sh`:

```bash
#!/bin/bash
# Development environment setup for Binnaculum TestUtils

echo "🛠️  Setting up Binnaculum TestUtils development environment..."

# Install .NET 9 if not present
if ! command -v dotnet &> /dev/null || [[ "$(dotnet --version)" != 9.* ]]; then
    echo "📥 Installing .NET 9 SDK..."
    ./dotnet-install.sh --version 9.0.101 --install-dir ~/.dotnet
    export PATH="$HOME/.dotnet:$PATH"
fi

# Install MAUI workloads based on platform
echo "📦 Installing MAUI workloads..."
dotnet workload install maui-android

if [[ "$OSTYPE" == "darwin"* ]]; then
    echo "🍎 macOS detected, installing iOS workloads..."
    dotnet workload install maui-ios maui-maccatalyst
fi

if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    echo "🪟 Windows detected, installing Windows workloads..."
    dotnet workload install maui-windows
fi

# Restore dependencies
echo "📦 Restoring NuGet packages..."
dotnet restore

# Build Core project
echo "🏗️  Building F# Core project..."
dotnet build src/Core/Core.fsproj

# Install git hooks
echo "🪝 Installing git hooks..."
mkdir -p .git/hooks
cp .githooks/* .git/hooks/
chmod +x .git/hooks/*

# Create development directories
mkdir -p .dev-tools/logs
mkdir -p .dev-tools/artifacts

echo "✅ Development environment setup complete!"
echo ""
echo "📋 Quick commands:"
echo "  • Build Android: dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-android"
echo "  • Run Core tests: dotnet test src/Tests/Core.Tests/Core.Tests.fsproj"
echo "  • Run device tests: ./scripts/run-headless-tests.sh --platform android"
echo "  • Performance tests: ./scripts/run-performance-tests.sh --platform android"
```

## Monitoring and Alerting

### Performance Regression Detection

Create `scripts/analyze-performance-regression.py`:

```python
#!/usr/bin/env python3
"""
Performance regression analysis for Binnaculum TestUtils
Compares current performance metrics against established baselines
"""

import json
import sys
import argparse
from datetime import datetime
from typing import Dict, List, Tuple

def load_performance_data(file_path: str) -> Dict:
    """Load performance data from JSON file"""
    try:
        with open(file_path, 'r') as f:
            return json.load(f)
    except FileNotFoundError:
        print(f"❌ Performance data file not found: {file_path}")
        sys.exit(1)
    except json.JSONDecodeError:
        print(f"❌ Invalid JSON in performance data file: {file_path}")
        sys.exit(1)

def analyze_regression(current: Dict, baseline: Dict, threshold: float) -> List[Dict]:
    """Analyze performance regression between current and baseline"""
    regressions = []
    
    # Investment calculation performance
    if 'investmentCalculations' in current and 'investmentCalculations' in baseline:
        current_calc = current['investmentCalculations']
        baseline_calc = baseline['investmentCalculations']
        
        for test_name in current_calc:
            if test_name in baseline_calc:
                current_time = current_calc[test_name]['executionTimeMs']
                baseline_time = baseline_calc[test_name]['executionTimeMs']
                
                regression_pct = ((current_time - baseline_time) / baseline_time) * 100
                
                if regression_pct > threshold:
                    regressions.append({
                        'test': test_name,
                        'category': 'Investment Calculations',
                        'currentTime': current_time,
                        'baselineTime': baseline_time,
                        'regressionPercent': regression_pct
                    })
    
    # UI performance
    if 'uiPerformance' in current and 'uiPerformance' in baseline:
        current_ui = current['uiPerformance']
        baseline_ui = baseline['uiPerformance']
        
        for test_name in current_ui:
            if test_name in baseline_ui:
                current_time = current_ui[test_name]['averageResponseTimeMs']
                baseline_time = baseline_ui[test_name]['averageResponseTimeMs']
                
                regression_pct = ((current_time - baseline_time) / baseline_time) * 100
                
                if regression_pct > threshold:
                    regressions.append({
                        'test': test_name,
                        'category': 'UI Performance',
                        'currentTime': current_time,
                        'baselineTime': baseline_time,
                        'regressionPercent': regression_pct
                    })
    
    # Memory usage
    if 'memoryUsage' in current and 'memoryUsage' in baseline:
        current_memory = current['memoryUsage']
        baseline_memory = baseline['memoryUsage']
        
        for test_name in current_memory:
            if test_name in baseline_memory:
                current_mem = current_memory[test_name]['peakMemoryMB']
                baseline_mem = baseline_memory[test_name]['peakMemoryMB']
                
                regression_pct = ((current_mem - baseline_mem) / baseline_mem) * 100
                
                if regression_pct > threshold:
                    regressions.append({
                        'test': test_name,
                        'category': 'Memory Usage',
                        'currentMemory': current_mem,
                        'baselineMemory': baseline_mem,
                        'regressionPercent': regression_pct
                    })
    
    return regressions

def generate_report(regressions: List[Dict], output_file: str = None):
    """Generate HTML performance regression report"""
    if not regressions:
        print("✅ No performance regressions detected!")
        return
    
    print(f"⚠️  {len(regressions)} performance regressions detected:")
    
    html_content = f"""
    <!DOCTYPE html>
    <html>
    <head>
        <title>Binnaculum Performance Regression Report</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 20px; }}
            .regression {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; margin: 10px 0; border-radius: 5px; }}
            .severe {{ background: #f8d7da; border: 1px solid #f5c6cb; }}
            .category {{ font-weight: bold; color: #495057; }}
            .metrics {{ font-family: monospace; background: #f8f9fa; padding: 5px; }}
        </style>
    </head>
    <body>
        <h1>Performance Regression Report</h1>
        <p>Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
        <p>Total regressions found: {len(regressions)}</p>
    """
    
    for regression in regressions:
        severity_class = "severe" if regression['regressionPercent'] > 25 else "regression"
        
        html_content += f"""
        <div class="{severity_class}">
            <div class="category">{regression['category']}</div>
            <strong>{regression['test']}</strong><br/>
            <div class="metrics">
                Regression: {regression['regressionPercent']:.1f}%<br/>
        """
        
        if 'currentTime' in regression:
            html_content += f"Time: {regression['baselineTime']}ms → {regression['currentTime']}ms<br/>"
        
        if 'currentMemory' in regression:
            html_content += f"Memory: {regression['baselineMemory']}MB → {regression['currentMemory']}MB<br/>"
        
        html_content += "</div></div>"
    
    html_content += "</body></html>"
    
    if output_file:
        with open(output_file, 'w') as f:
            f.write(html_content)
        print(f"📊 Detailed report saved: {output_file}")
    
    # Print summary to console
    for regression in regressions:
        print(f"  • {regression['category']}: {regression['test']} ({regression['regressionPercent']:.1f}% slower)")

def main():
    parser = argparse.ArgumentParser(description='Analyze Binnaculum performance regressions')
    parser.add_argument('--current', required=True, help='Current performance results JSON file')
    parser.add_argument('--baseline', required=True, help='Baseline performance results JSON file')
    parser.add_argument('--threshold', type=float, default=10.0, help='Regression threshold percentage (default: 10%)')
    parser.add_argument('--output', help='Output HTML report file')
    
    args = parser.parse_args()
    
    current_data = load_performance_data(args.current)
    baseline_data = load_performance_data(args.baseline)
    
    regressions = analyze_regression(current_data, baseline_data, args.threshold)
    generate_report(regressions, args.output)
    
    # Exit with error code if regressions found
    if regressions:
        sys.exit(1)

if __name__ == '__main__':
    main()
```

This CI/CD integration guide provides:

1. **Complete GitHub Actions Workflows**: Basic and advanced pipelines with performance monitoring
2. **Local Development Scripts**: Headless test runner, performance testing, and setup automation  
3. **Quality Gates**: Pre-commit hooks, flaky test detection, and performance regression analysis
4. **Monitoring**: Automated performance baseline updates and regression alerting
5. **Platform Support**: Android, iOS, Windows, and MacCatalyst CI/CD configuration

The integration ensures consistent, reliable testing of Binnaculum's investment functionality across all supported platforms while maintaining development velocity and code quality.