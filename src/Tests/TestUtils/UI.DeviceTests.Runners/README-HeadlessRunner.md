# Binnaculum Headless Test Runner

The Headless Test Runner provides a command-line interface for executing Binnaculum device tests without UI interaction, perfect for CI/CD integration.

## Overview

The headless runner builds on the existing Visual Test Runner infrastructure but provides:

- **Command-line interface** with comprehensive filtering options
- **Multiple output formats** (Console, XML, JSON) 
- **Platform-specific runners** for Android, iOS, Windows, and MacCatalyst
- **CI/CD integration** with proper exit codes and artifact collection
- **Parallel execution** and retry logic for flaky tests

## Architecture

```
HeadlessRunner/
├── CLI/                        # Command-line interface components
│   ├── HeadlessTestRunner.cs   # Main orchestration logic
│   ├── CommandLineOptions.cs   # Configuration model
│   ├── ArgumentParser.cs       # Command-line argument parsing
│   └── Program.cs             # Console application entry point
├── Platform/                   # Platform-specific runners
│   ├── AndroidHeadlessRunner.cs
│   ├── iOSHeadlessRunner.cs
│   ├── WindowsHeadlessRunner.cs
│   └── MacCatalystHeadlessRunner.cs
├── Results/                    # Output format writers
│   ├── IResultsWriter.cs       # Writer interface
│   ├── ConsoleResultsWriter.cs # Human-readable console output
│   ├── XmlResultsWriter.cs     # xUnit-compatible XML
│   └── JsonResultsWriter.cs    # Modern JSON format
├── Services/                   # Core services
│   ├── TestDiscoveryService.cs # Test discovery wrapper
│   ├── TestExecutionService.cs # Test execution wrapper
│   └── ArtifactCollectionService.cs # Screenshot/log collection
└── Tests/                      # Unit tests
    └── HeadlessRunnerTests.cs
```

## Usage Examples

### Basic Execution
```bash
# Run all tests on Android (default platform)
BinnaculumTestRunner

# Run tests on specific platform
BinnaculumTestRunner --platform ios
```

### Advanced Filtering
```bash
# Filter by test name patterns (supports wildcards)
BinnaculumTestRunner --filter "*BrokerAccount*"
BinnaculumTestRunner --filter "Core.Tests.*PerformanceTests"

# Run with different verbosity levels
BinnaculumTestRunner --verbosity detailed
BinnaculumTestRunner --verbosity quiet
```

### Output Formats
```bash
# Generate XML results (xUnit compatible)
BinnaculumTestRunner --output-format xml --output-path test-results.xml

# Generate JSON results
BinnaculumTestRunner --output-format json --output-path test-results.json

# Console output with high verbosity
BinnaculumTestRunner --output-format console --verbosity detailed
```

### CI/CD Integration
```bash
# Full CI mode with timeout, retries, and artifact collection
BinnaculumTestRunner \
  --headless \
  --timeout 600 \
  --retry-failed 2 \
  --collect-artifacts \
  --artifact-path ./test-artifacts \
  --output-format xml \
  --output-path ./test-results.xml
```

## Command-Line Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--platform` | `-p` | `android` | Target platform (android, ios, windows, maccatalyst) |
| `--filter` | `-f` | | Filter tests by name, category, or assembly (supports wildcards) |
| `--output-format` | `-o` | `console` | Output format (console, xml, json) |
| `--output-path` | `--out` | | Path to write output file (if not console) |
| `--headless` | | `true` | Run in headless mode (no UI) |
| `--parallel` | | `false` | Execute tests in parallel |
| `--timeout` | `-t` | `300` | Timeout in seconds for test execution |
| `--retry-failed` | `-r` | `0` | Number of times to retry failed tests |
| `--verbosity` | `-v` | `normal` | Verbosity level (quiet, minimal, normal, detailed, diagnostic) |
| `--collect-artifacts` | | `false` | Collect artifacts (screenshots, logs) for failed tests |
| `--artifact-path` | | | Path to store collected artifacts |

## Exit Codes

- `0`: All tests passed successfully
- `1`: One or more tests failed
- `2`: No tests were executed (potential configuration issue)
- `10-13`: Platform-specific error codes (Android=10, iOS=11, Windows=12, MacCatalyst=13)
- `130`: Execution cancelled (Ctrl+C)

## Output Formats

### Console Output
Human-readable output with color-coded status indicators:
- ✅ Passed tests
- ❌ Failed tests  
- ⏭️ Skipped tests
- 🏃 Running tests (during execution)

### XML Output (xUnit Compatible)
Standard xUnit XML format that integrates with most CI/CD systems:
```xml
<testsuites tests="42" failures="1" skipped="2" time="15.234">
  <testsuite name="Core.Tests" tests="20" failures="0" skipped="1">
    <testcase classname="Core.Tests.BrokerTests" name="ValidateAccountCreation" time="0.123"/>
    <testcase classname="Core.Tests.BrokerTests" name="FailingTest" time="0.045">
      <failure message="Assertion failed">Stack trace here</failure>
    </testcase>
  </testsuite>
</testsuites>
```

### JSON Output
Modern JSON format for analysis tools:
```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "summary": {
    "total": 42,
    "passed": 39,
    "failed": 1,
    "skipped": 2,
    "duration": 15234.5
  },
  "tests": [
    {
      "name": "Core.Tests.BrokerTests.ValidateAccountCreation",
      "status": "passed",
      "duration": 123.4,
      "errorMessage": null,
      "stackTrace": null
    }
  ]
}
```

## Platform Integration

### Android
- **Environment validation**: Checks for ADB availability and connected devices
- **Artifact collection**: Logcat logs, system information, memory usage
- **Emulator support**: Can detect and work with Android emulators

### iOS (macOS only)
- **Environment validation**: Requires macOS and Xcode installation
- **Simulator integration**: Works with iOS Simulator
- **Provisioning**: Handles device provisioning profiles

### Windows
- **Environment validation**: Windows-specific runtime checks
- **Desktop automation**: Integration with Windows desktop automation APIs
- **Multi-version support**: Compatible with different Windows SDK versions

### MacCatalyst (macOS only)
- **Environment validation**: macOS and Xcode with Catalyst support required
- **App lifecycle**: Proper MacCatalyst app lifecycle management
- **Signing**: Handles Mac app signing requirements

## Artifact Collection

When `--collect-artifacts` is enabled, the runner collects:

- **Screenshots**: Captures for failed UI tests (platform-specific)
- **Log files**: Application and system logs during test execution
- **Memory dumps**: For crashed or failed tests
- **System information**: Hardware and OS details
- **Execution timing**: Detailed performance metrics

Artifacts are organized in timestamped directories:
```
test-artifacts/
├── artifacts-summary.txt
├── screenshots/
├── logs/
├── memory-dumps/
└── system-info/
```

## Reusing Existing Infrastructure

The headless runner leverages the existing `VisualDeviceRunner` service for:
- **Test discovery**: Same reflection-based discovery as visual runner
- **Test execution**: Identical test execution engine
- **Progress reporting**: Compatible progress and cancellation handling
- **Error handling**: Consistent error capture and reporting

This ensures compatibility and reduces code duplication while adding headless capabilities.