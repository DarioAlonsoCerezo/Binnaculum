# Core Platform MAUI Tester

## Overview

This MAUI application is designed to validate Core Platform functionality in a real MAUI environment, addressing the limitation where `PublicApiIntegrationTests.fs` fails due to platform service dependencies (`NotImplementedInReferenceAssemblyException`) when running in headless test environments.

## Problem Statement

The test `Overview InitDatabase and LoadData work without errors` in `PublicApiIntegrationTests.fs` fails because:
- MAUI.Essentials services require platform-specific implementations
- Test runners don't provide proper MAUI application context  
- Critical functionality like `FileSystem.AppDataDirectory` and `Preferences` are not available
- Tests are skipped due to platform service unavailability

## Solution

This interactive MAUI application provides:
- **Real MAUI Environment**: Runs with full platform services
- **Visual Test Execution**: Step-by-step progress and results display
- **Comprehensive Validation**: Recreates exact logic from `PublicApiIntegrationTests.fs`
- **Error Handling**: Detailed error reporting and logging
- **Cross-Platform**: Supports Android, iOS, macOS, and Windows

## Features

### Test Validation Steps
1. ✅ **Initialize MAUI Platform Services** - Verify platform services are available
2. ✅ **Overview.InitDatabase()** - Call Core database initialization
3. ✅ **Overview.LoadData()** - Call Core data loading
4. ✅ **Verify Database Initialized** - Check `Overview.Data.Value.IsDatabaseInitialized`
5. ✅ **Verify Data Loaded** - Check `Overview.Data.Value.TransactionsLoaded`
6. ✅ **Verify Currencies Collection** - Ensure `Collections.Currencies.Items.Count > 0`
7. ✅ **Verify USD Currency Exists** - Confirm USD currency is loaded
8. ✅ **Overall Result** - Display final pass/fail status

### User Interface
```
┌─────────────────────────────────────┐
│         Core Platform Tester        │
├─────────────────────────────────────┤
│  [Run Overview Test] (Primary Btn)  │
│  Status: Ready/Running/Complete     │
│                                     │
│  ┌─────────────────────────────────┐ │
│  │         Test Results            │ │
│  │ ✅ Platform Services: Available │ │
│  │ ✅ Database Initialized: True   │ │
│  │ ✅ Data Loaded: True            │ │
│  │ ✅ Currencies: 150              │ │
│  │ ✅ USD Found: True              │ │
│  │ Overall: ✅ PASSED              │ │
│  └─────────────────────────────────┘ │
│                                     │
│  [View Details] [Clear Results]     │
└─────────────────────────────────────┘
```

## Building and Running

### Prerequisites
- .NET 9 SDK
- MAUI workloads installed
- Platform-specific requirements (Android SDK, Xcode, etc.)

### Build Commands
```bash
# Android (available on all platforms)
dotnet build -f net9.0-android

# iOS (requires macOS + Xcode)
dotnet build -f net9.0-ios

# macOS (requires macOS)
dotnet build -f net9.0-maccatalyst

# Windows (requires Windows)
dotnet build -f net9.0-windows10.0.19041.0
```

### Running the App
The app must be deployed to a physical device or emulator since it requires real platform services.

## Project Structure

```
Core.Platform.MauiTester/
├── Models/
│   └── TestResult.cs          # Data models for test results
├── Services/
│   ├── TestRunner.cs          # Main test execution logic
│   └── LogService.cs          # Detailed logging service
├── MainPage.xaml              # Primary test interface
├── MainPage.xaml.cs           # UI interaction logic
└── MauiProgram.cs             # Dependency injection setup
```

## Test Logic Recreation

The `TestRunner.ExecuteOverviewTestAsync()` method recreates the exact logic from `PublicApiIntegrationTests.fs`:

```csharp
// Original F# test logic:
do! Overview.InitDatabase()
do! Overview.LoadData()

let isInitialized = Overview.Data.Value.IsDatabaseInitialized
let isLoaded = Overview.Data.Value.TransactionsLoaded

Assert.That(Collections.Currencies.Items.Count, Is.GreaterThan(0))
let usdExists = Collections.Currencies.Items |> Seq.exists (fun c -> c.Code = "USD")
```

```csharp
// MAUI Test App equivalent:
await Overview.InitDatabase();
await Overview.LoadData();

var isInitialized = Overview.Data.Value.IsDatabaseInitialized;
var isLoaded = Overview.Data.Value.TransactionsLoaded;

var currencyCount = Collections.Currencies.Items.Count; // > 0
var usdExists = Collections.Currencies.Items.Any(c => c.Code == "USD");
```

## Expected Results

When running successfully, the app should display:
- ✅ **Platform Services Available** - Confirms MAUI platform services work
- ✅ **Database Initialized** - `Overview.InitDatabase()` completed
- ✅ **Data Loaded** - `Overview.LoadData()` completed  
- ✅ **Currencies Collection Populated** - Shows count > 0
- ✅ **USD Currency Found** - Confirms USD exists in collection
- ✅ **Overall: PASSED** - All validation steps completed

## Troubleshooting

### Common Issues
1. **Platform services not available** - Ensure running on actual device/emulator
2. **Database errors** - Check file system permissions and storage access
3. **Build failures** - Verify correct target framework and workload installation

### Debugging
- Use **View Details** button to see full execution log
- Check debug output for detailed error messages
- Compare results with expected Core library behavior

## Development Notes

### Architecture Patterns
- **MVVM** with data binding
- **Dependency Injection** for services
- **Async/await** for all Core operations
- **Progress reporting** for UI responsiveness

### Performance Considerations
- **Mobile-optimized** for device constraints
- **Chunked processing** for large datasets
- **Memory-efficient** collection handling
- **Responsive UI** with progress indicators

## Future Enhancements

Potential additions for expanded testing:
- **SavedPreferences** functionality testing
- **ReactiveSnapshotManager** validation  
- **Database schema** validation tests
- **Performance benchmark** tests
- **Automated test suite** runner
- **Comparison** with previous test runs

## Related Files

- `src/Tests/Core.Platform.Tests/PublicApiIntegrationTests.fs` - Original failing test
- `src/Core/UI/Overview.fs` - Core Overview module being tested
- `src/Core/UI/Collections.fs` - Collections being validated
- `src/Core/Storage/DataLoader.fs` - Data loading functionality