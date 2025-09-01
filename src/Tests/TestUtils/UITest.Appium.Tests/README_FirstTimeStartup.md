# First-Time App Startup UI Tests

This directory contains UI tests that validate the first-time app startup experience, including database creation, loading indicators, and data population flow.

## Test Overview

### FirstTimeStartupTests.cs

**Purpose**: Validates the complete first-time user experience when launching Binnaculum for the first time.

**Key Test Scenarios**:

1. **`FirstTimeAppStartup_DatabaseCreation_LoadsOverviewWithIndicators()`**
   - Launches app in fresh state (no existing database)
   - Verifies loading indicators are initially visible
   - Waits for database creation and data population to complete
   - Captures screenshots of loading and loaded states
   - Validates final UI state

2. **`FirstTimeAppStartup_EmptyState_ShowsEmptyViews()`**
   - Tests how the app handles empty data scenarios
   - Verifies empty state UI elements display correctly
   - Ensures app doesn't crash when no accounts/movements exist

3. **`FirstTimeAppStartup_Performance_CompletesWithinTimeLimit()`**
   - Measures first-time startup performance
   - Ensures database creation completes within reasonable time
   - Validates startup time expectations

## Loading Indicators Monitored

Based on `OverviewPage.xaml` and `OverviewPage.xaml.cs`:

- **`CarouseIndicator`**: Shows while accounts/database initialization is in progress
  - Becomes invisible when `IsDatabaseInitialized` is true
- **`CollectionIndicator`**: Shows while movements/transactions are loading
  - Becomes invisible when `TransactionsLoaded` is true

## Prerequisites

### Appium Server Setup

These tests require a running Appium server. Start it manually before running tests:

```bash
appium --address 127.0.0.1 --port 4723 --relaxed-security
```

### Android Environment

Tests are configured for Android platform by default. Ensure you have:
- Android emulator running or Android device connected
- App installed and accessible via package name

## Running the Tests

### Individual Test Execution

```bash
# Run all first-time startup tests
dotnet test --filter "FirstTimeStartupTests"

# Run specific test
dotnet test --filter "FirstTimeAppStartup_DatabaseCreation_LoadsOverviewWithIndicators"
```

### Test Configuration

Tests use `AppResetStrategy.ReinstallApp` to ensure completely fresh state:

```csharp
var config = AppiumConfig.ForBinnaculumAndroid(
    resetStrategy: AppResetStrategy.ReinstallApp);
```

This strategy provides:
- Complete app data reset
- Fresh database creation on each test run
- Reliable test isolation

## Screenshots

Tests automatically capture screenshots during execution:

- **`first_startup_loading.png`**: Shows ActivityIndicators active during loading
- **`first_startup_loaded.png`**: Shows completed UI state after loading

Screenshots are saved to `Screenshots/` directory with timestamps for uniqueness.

## Expected Behavior

### Successful Test Flow

1. **App Launch**: Fresh installation starts successfully
2. **Initial State**: Both ActivityIndicators visible and running
3. **Database Creation**: `CarouseIndicator` disappears when database ready
4. **Data Population**: `CollectionIndicator` disappears when transactions loaded
5. **Final State**: Overview page displays with populated data or appropriate empty state

### Timing Expectations

- **App Launch**: ~5-10 seconds to reach ready state
- **Database Creation**: Additional 10-20 seconds for first-time setup
- **Total First-Time Startup**: Should complete within 60 seconds maximum

## Troubleshooting

### Common Issues

1. **Appium Server Not Running**
   - Error: Tests skip with message about Appium server
   - Solution: Start Appium server as shown in prerequisites

2. **App Not Installing**
   - Error: App package not found or installation fails
   - Solution: Check Android device/emulator connectivity and app package configuration

3. **Elements Not Found**
   - Error: Loading indicators or UI elements not located
   - Solution: Verify XAML element names match test expectations (`CarouseIndicator`, `CollectionIndicator`, etc.)

4. **Timeout During Loading**
   - Error: Loading indicators don't disappear within timeout
   - Solution: Increase timeout values or check for database/network connectivity issues

### Debug Information

Tests provide comprehensive console output:
- Phase-by-phase progress logging
- Element visibility status updates
- Screenshot capture confirmations
- Timing measurements
- Error details with context

## Integration with CI/CD

These tests are designed for environments where:
- Android emulators can be launched
- Appium server can be started automatically
- Screenshot artifacts can be collected and stored
- Sufficient timeout allowances for database operations

For headless CI environments, ensure proper Android emulator setup and consider extending timeouts for slower CI hardware.