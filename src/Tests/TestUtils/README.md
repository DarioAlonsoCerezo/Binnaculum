# Binnaculum TestUtils - Investment App Testing Infrastructure

A comprehensive testing framework for Binnaculum's cross-platform investment tracking application. This infrastructure provides device-level testing, investment-specific assertions, and platform-specific utilities for Android, iOS, MacCatalyst, and Windows.

## 🎯 Overview

The TestUtils framework enables thorough testing of investment functionality across platforms, including:

- **Investment-Specific Testing**: Currency formatting, percentage calculations, portfolio balance validation
- **Multi-Platform Support**: Native testing on Android, iOS, MacCatalyst, and Windows  
- **Realistic Test Data**: Investment scenarios based on real-world trading patterns
- **Memory Leak Detection**: Observable chain testing for ReactiveUI components
- **F# Integration**: Seamless testing of F# business logic from C# device tests
- **Visual & Headless Runners**: Interactive development testing and automated CI/CD execution

## 🏗️ Project Architecture

### Core Components

#### UI.DeviceTests
- **Purpose**: Component-level testing of MAUI UI controls and investment templates
- **Target Frameworks**: `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, `net9.0-windows10.0.19041.0`
- **Key Features**:
  - Investment-specific assertion extensions (`AssertCurrencyFormat`, `AssertPercentageCalculation`)
  - Fluent test data builders for realistic investment scenarios
  - Observable memory leak detection for ReactiveUI components
  - Platform-specific testing extensions

#### UI.DeviceTests.Runners  
- **Purpose**: Test execution infrastructure for both interactive and automated testing
- **Components**:
  - **Visual Runner**: Interactive XAML-based test discovery and execution
  - **Headless Runner**: Command-line runner optimized for CI/CD environments
- **Features**: Real-time progress, result export (XML/JSON), parallel execution

#### UITest.Appium
- **Purpose**: Cross-platform end-to-end UI automation using Appium
- **Coverage**: Complete user workflows across Android, iOS, MacCatalyst, and Windows
- **Pattern**: Page Object Model with investment-specific components

#### UITest.Core
- **Purpose**: Foundational interfaces and utilities for cross-platform UI testing
- **Abstraction**: Platform-agnostic APIs with platform-specific implementations

## 📚 Documentation

### Quick Start
- **[Writing Your First Device Test](docs/getting-started/first-test.md)** - Step-by-step tutorial for investment component testing
- **[Developer Setup Guide](docs/getting-started/setup-guide.md)** - Complete environment setup instructions
- **[Platform-Specific Setup](docs/getting-started/platform-setup/)** - Detailed platform configuration guides

### Examples & Templates  
- **[Component Test Examples](docs/examples/component-tests/)** - Real investment control testing scenarios
- **[End-to-End Test Examples](docs/examples/e2e-tests/)** - Complete user workflow testing
- **[Performance Test Examples](docs/examples/performance-tests/)** - Mobile performance validation

### Best Practices & Architecture
- **[Investment App Testing Best Practices](docs/best-practices.md)** - Guidelines for financial application testing
- **[TestUtils Architecture](docs/architecture.md)** - System design and component relationships
- **[API Documentation](docs/api/)** - Detailed API reference for all components

### Support
- **[Troubleshooting Guide](docs/troubleshooting.md)** - Common issues and platform-specific solutions

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