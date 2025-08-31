# MAUI Device Testing Infrastructure

This directory contains the MAUI device testing infrastructure for Binnaculum, providing device-level tests that run on actual Android, iOS, macOS, and Windows platforms.

## Project Structure

### UI.DeviceTests
- **Purpose**: Contains device-specific UI tests that run on actual devices or emulators
- **Target Frameworks**: 
  - `net9.0-android` (Android devices/emulators)
  - `net9.0-ios` (iOS devices/simulators) 
  - `net9.0-maccatalyst` (macOS)
  - `net9.0-windows10.0.19041.0` (Windows)
- **Output Type**: Executable MAUI application
- **Dependencies**: 
  - Core project (F# business logic)
  - XUnit testing framework
  - Microsoft.Maui packages

### UI.DeviceTests.Runners
- **Purpose**: Test runner infrastructure and utilities for coordinating device test execution
- **Target Frameworks**: Same as UI.DeviceTests
- **Output Type**: Library
- **Dependencies**: 
  - Core project
  - UI.DeviceTests project
  - XUnit testing framework

## Key Features

1. **Multi-Platform Support**: Tests can run on all MAUI-supported platforms
2. **XUnit Integration**: Uses XUnit as the testing framework for consistency with modern .NET testing practices
3. **Core Integration**: Direct access to F# core business logic for comprehensive testing
4. **MAUI Controls Testing**: Ability to test MAUI UI controls in their native platform environments
5. **Extensible Structure**: Organized folder structure for different test categories

## Folder Structure

```
UI.DeviceTests/
├── BasicDeviceTests.cs          # Basic infrastructure tests
├── Controls/                    # UI control tests
├── Pages/                       # Page-level integration tests
├── Services/                    # Platform service tests
├── GlobalUsings.cs              # Global using statements
└── Resources/                   # MAUI resources (app icons, splash screens, etc.)

UI.DeviceTests.Runners/
├── TestRunnerTests.cs           # Test runner verification tests
├── TestRunners/                 # Custom test runners
├── Configuration/               # Test configuration utilities
└── GlobalUsings.cs              # Global using statements
```

## Usage

### Building the Projects

On each platform, you can build the projects using:

```bash
# Android (available on all platforms)
dotnet build -f net9.0-android

# iOS (requires macOS)
dotnet build -f net9.0-ios

# macOS Catalyst (requires macOS)
dotnet build -f net9.0-maccatalyst

# Windows (requires Windows)
dotnet build -f net9.0-windows10.0.19041.0
```

### Running Device Tests

Device tests require the appropriate platform runtime and testing infrastructure:

```bash
# Running on Android emulator/device
dotnet test -f net9.0-android

# Running on iOS simulator/device (requires macOS + Xcode)
dotnet test -f net9.0-ios

# Running on Windows
dotnet test -f net9.0-windows10.0.19041.0
```

## Platform Requirements

- **Android**: Android SDK, Android emulator or physical device
- **iOS**: macOS, Xcode, iOS Simulator or physical device
- **macOS**: macOS 10.15+ for Mac Catalyst
- **Windows**: Windows 10 version 19041.0 or higher

## Development Notes

1. **Target Framework Conditional Compilation**: The projects use conditional target framework selection based on the host OS to avoid workload dependencies that aren't available on all platforms.

2. **Resource Management**: The DeviceTests project includes MAUI resources (icons, splash screens) while the Runners project is a library without UI resources.

3. **Test Categories**: Tests are organized into logical categories:
   - Infrastructure tests verify basic testing setup
   - Control tests focus on individual UI components
   - Page tests cover full-page scenarios
   - Service tests validate platform-specific functionality

4. **Future Enhancements**: The infrastructure is prepared for Microsoft.Maui.TestUtils integration when those packages become available and stable.

## Contributing

When adding new device tests:

1. Place tests in appropriate folders based on what they're testing
2. Use descriptive test names that indicate the platform feature being tested
3. Include both positive and negative test cases where appropriate
4. Consider platform-specific behavior and test accordingly
5. Document any special setup requirements for tests

## Troubleshooting

- **Build Issues**: Ensure you have the required platform workloads installed
- **Test Execution Issues**: Verify that target devices/emulators are available and properly configured
- **Resource Conflicts**: The Runners project intentionally doesn't include MAUI resources to avoid conflicts