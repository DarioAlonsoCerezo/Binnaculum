# Visual Test Runner Guide

The Visual Test Runner provides an interactive MAUI-based UI for discovering, executing, and viewing results of device tests directly on the target platform.

## Overview

The Visual Test Runner is a complete XAML-based test execution environment that allows you to:

- **Discover Tests**: Automatically find all xUnit tests in loaded assemblies
- **Select Tests**: Choose individual tests, test classes, or entire assemblies to run
- **Execute Tests**: Run tests with real-time progress updates and cancellation support
- **View Results**: See detailed results including pass/fail status, execution times, and error details
- **Search & Filter**: Quickly find specific tests using text search
- **Navigate**: Browse tests hierarchically by Assembly → Class → Method

## Architecture

### Core Components

1. **VisualTestRunnerApp** - Main MAUI application entry point
2. **VisualTestRunnerShell** - Navigation shell with tabbed interface  
3. **TestDiscoveryPage** - Interactive test browser and selection UI
4. **TestExecutionPage** - Real-time test execution monitoring
5. **TestResultsPage** - Detailed results display

### ViewModels

- **TestRunnerViewModel** - Main orchestration and state management
- **TestAssemblyViewModel** - Assembly-level test organization
- **TestClassViewModel** - Class-level test grouping  
- **TestCaseViewModel** - Individual test representation

### Services

- **VisualDeviceRunner** - Core test discovery and execution engine

## Quick Start

### Launching the Visual Runner

```csharp
using Binnaculum.UI.DeviceTests.Runners;

// Simple launch
var app = VisualTestRunnerLauncher.LaunchVisualRunner();

// Or with advanced configuration
var builder = VisualTestRunnerLauncher.CreateMauiApp();
// Add your custom configuration here
var app = builder.Build();
```

### Using the UI

1. **Test Discovery**: The app automatically discovers tests when it starts or you can tap the refresh button
2. **Test Selection**: Use checkboxes to select tests at any level (assembly, class, or individual test)
3. **Search**: Use the search box to filter tests by name
4. **Execution**: Tap "Run Selected Tests" to execute chosen tests
5. **Monitor Progress**: Watch real-time progress in the execution tab
6. **View Results**: Check detailed results in the results tab

## Test Discovery

The Visual Runner automatically discovers tests by:

1. Scanning all loaded assemblies in the current app domain
2. Looking for classes with methods marked with `[Fact]` or `[Theory]` attributes
3. Building a hierarchical structure of Assembly → Class → Method
4. Supporting both parameterless `[Fact]` tests and parameterized `[Theory]` tests

### Example Discoverable Tests

```csharp
public class SampleTests
{
    [Fact]
    public void Simple_Test_ShouldPass()
    {
        Assert.True(true);
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 5, 10)]
    public void Parameterized_Test(int a, int b, int expected)
    {
        Assert.Equal(expected, a + b);
    }
}
```

## Test Execution

### Features

- **Real-time Progress**: Live updates showing which test is currently running
- **Cancellation**: Stop test execution at any time with proper cleanup
- **Error Capture**: Full exception details and stack traces for failed tests
- **Timing**: Execution duration for each test
- **Status Updates**: Visual indicators for pending, running, passed, failed, and skipped tests

### Execution Flow

1. Test selection validation
2. Progress reporting setup
3. Sequential test execution with reflection
4. Real-time status updates
5. Result aggregation and display
6. Final statistics and cleanup

## UI Components

### Test Discovery Page

- **Hierarchical Tree**: Expandable view of assemblies, classes, and methods
- **Selection Controls**: Checkboxes with cascading selection
- **Search Bar**: Real-time filtering of test names
- **Status Indicators**: Color-coded test status badges
- **Action Buttons**: Select All, Deselect All, Refresh, Run Selected

### Test Execution Page  

- **Progress Bar**: Visual execution progress
- **Status Messages**: Current test being executed
- **Control Buttons**: Stop/Cancel execution
- **Real-time Updates**: Live status changes

### Test Results Page

- **Results Summary**: Pass/fail/skip statistics
- **Detailed Results**: Individual test outcomes
- **Error Details**: Exception messages and stack traces
- **Execution Times**: Performance metrics

## Styling and Themes

The Visual Runner includes a complete Material Design-inspired theme with:

- **Color Palette**: Status-specific colors (green for pass, red for fail, etc.)
- **Typography**: Clear, readable text hierarchy
- **Icons**: Intuitive visual indicators
- **Layout**: Responsive design that works across device sizes
- **Animations**: Smooth expand/collapse and state transitions

## Integration with Existing Tests

The Visual Runner integrates seamlessly with existing xUnit test infrastructure:

- No changes required to existing test code
- Works with standard `[Fact]` and `[Theory]` attributes
- Supports existing assertion libraries
- Maintains full compatibility with command-line test execution

## Platform Support

The Visual Runner is designed to work across all MAUI target platforms:

- **Android**: Material Design compliance, hardware back button support
- **iOS**: Native navigation patterns, safe area handling
- **MacCatalyst**: Mac-specific menu and window behaviors
- **Windows**: Desktop window management, keyboard shortcuts

## Performance

- **Efficient Discovery**: Fast assembly scanning with minimal memory overhead
- **Responsive UI**: Non-blocking operations with async/await patterns
- **Memory Conscious**: Minimal reflection metadata storage
- **Scalable**: Handles large test suites (100+ tests) efficiently

## Advanced Features

### Search and Filtering

- Case-insensitive text matching
- Searches test names, display names, and full names
- Real-time filtering as you type
- Preserves selection state during filtering

### Selection Management

- Cascading selection from assembly to individual tests
- Bulk operations (Select All / Unselect All)
- Selection persistence during search operations
- Visual indication of partial selections

### Progress Reporting

- `IProgress<T>` based real-time updates
- Cancellation token support for clean stops
- Thread-safe progress reporting
- Detailed execution statistics

## Troubleshooting

### Common Issues

1. **No Tests Found**: Ensure test assemblies are properly loaded and contain `[Fact]` or `[Theory]` attributes
2. **Tests Not Running**: Check for proper test method signatures (public, parameterless for Facts)
3. **UI Not Responsive**: All operations are async - avoid blocking the UI thread
4. **Memory Issues**: Large test suites may require more memory - monitor device resources

### Logging

The Visual Runner includes comprehensive logging for debugging:

- Test discovery events
- Execution progress
- Error conditions
- Performance metrics

## Examples

See the `VisualTestRunnerTests.cs` and `VisualTestRunnerLauncher.cs` files for complete working examples of:

- Creating and configuring the runner
- Test discovery validation
- ViewModel functionality testing  
- Integration with existing test infrastructure

## Future Enhancements

Planned improvements include:

- Screenshot capture for UI test failures
- Test result export capabilities  
- Advanced filtering options
- Custom test runners
- Integration with CI/CD systems
- Performance profiling