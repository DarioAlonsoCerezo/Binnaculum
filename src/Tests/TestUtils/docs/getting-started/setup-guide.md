# Developer Setup Guide

This guide will help you set up your development environment for writing and running Binnaculum device tests.

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10+ (19041.0+), macOS 10.15+, or Ubuntu 20.04+
- **.NET 9 SDK**: Required for Binnaculum's investment tracking functionality
- **Memory**: 8GB RAM minimum, 16GB recommended for emulator usage
- **Storage**: 10GB free space for workloads and emulators

### Platform-Specific Requirements
- **Android**: Android SDK, emulator or physical device
- **iOS/MacCatalyst**: macOS with Xcode (macOS only)
- **Windows**: Windows 10 version 19041.0+ for Windows target

## Step 1: Install .NET 9 SDK

### Windows/Linux
```bash
# Download and install from Microsoft
# Or use the provided script in repository root:
./dotnet-install.sh --version 9.0.101 --install-dir ~/.dotnet
export PATH="$HOME/.dotnet:$PATH"
```

### macOS
```bash
# Via Homebrew
brew install dotnet@9

# Or download from Microsoft .NET website
```

Verify installation:
```bash
dotnet --version
# Should show: 9.0.304 or later
```

## Step 2: Install MAUI Workloads

**Critical**: MAUI workload installation takes ~66 seconds. Set timeout appropriately and never cancel.

### Android Workload (Required)
```bash
dotnet workload install maui-android
# Takes approximately 66 seconds - DO NOT CANCEL
```

### iOS/MacCatalyst Workloads (macOS Only)
```bash
# Only available on macOS
dotnet workload install maui-ios
dotnet workload install maui-maccatalyst
```

### Windows Workload (Windows Only)
```bash
# Windows development machine
dotnet workload install maui-windows
```

### Verify Workload Installation
```bash
dotnet workload list
# Should show installed workloads: maui-android (and others based on platform)
```

## Step 3: Clone and Build Binnaculum

### Clone Repository
```bash
git clone https://github.com/DarioAlonsoCerezo/Binnaculum.git
cd Binnaculum
```

### Build Core Components
**Important**: Builds can take significant time. Never cancel build processes.

```bash
# Restore packages (4-5 seconds)
dotnet restore

# Build F# Core project (13-14 seconds)
dotnet build src/Core/Core.fsproj

# Build Core Tests (11-12 seconds)  
dotnet build src/Tests/Core.Tests/Core.Tests.fsproj

# Run Core Tests to verify setup (2-3 seconds)
# Expected: 80/87 tests pass (7 MAUI-dependent tests fail in headless environment)
dotnet test src/Tests/Core.Tests/Core.Tests.fsproj
```

### Build Device Test Infrastructure
```bash
# Build Android device tests (95 seconds - NEVER CANCEL)
dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-android

# Build test runners (7 seconds)
dotnet build src/Tests/TestUtils/UI.DeviceTests.Runners/UI.DeviceTests.Runners.csproj -f net9.0-android
```

## Step 4: Development Environment Setup

### IDE Configuration

#### Visual Studio 2022 (Windows/Mac)
- Install "MAUI" workload through Visual Studio Installer
- Install "F# Development Tools"
- Recommended extensions:
  - F# Power Tools
  - XAML Styler (uses `XAMLStylerConfiguration.json` in repo root)

#### Visual Studio Code
```bash
# Install recommended extensions
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime
code --install-extension ionide.ionide-fsharp
```

#### JetBrains Rider
- F# support is built-in
- Enable MAUI development plugin

### Git Configuration
```bash
# Configure editor for commit messages
git config core.editor "code --wait"

# Use .editorconfig for consistent code style
# (Already included in repository)
```

## Step 5: Platform-Specific Setup

### Android Development
```bash
# Accept Android licenses (required for emulator)
# This may prompt for several license acceptances
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses
```

Create Android Virtual Device:
- Open Android Studio or use command line
- Create AVD with API 34 (Android 14)
- Allocate 4GB+ RAM for investment app testing

### iOS Development (macOS Only)
```bash
# Install Xcode from App Store
xcode-select --install

# Accept Xcode license
sudo xcodebuild -license accept

# Start iOS Simulator
open -a Simulator
```

### Windows Development
```bash
# Enable Developer Mode in Windows Settings
# Settings → Update & Security → For developers → Developer mode: On

# Install Visual Studio Build Tools (if not using full Visual Studio)
```

## Step 6: Validate Setup

### Run Basic Tests
```bash
# Test F# Core functionality
dotnet test src/Tests/Core.Tests/Core.Tests.fsproj --filter "BrokerFinancialSnapshotManager"
# Expected: 51/52 tests pass

# Test build for Android target  
dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-android
# Should complete without errors
```

### Test Visual Runner (Interactive)
```bash
# Build Visual Test Runner
dotnet build src/Tests/TestUtils/UI.DeviceTests.Runners/UI.DeviceTests.Runners.csproj -f net9.0-android

# Deploy to emulator/device for interactive testing
# (Cannot run in headless CI environment)
```

## Step 7: Configure Development Workflow

### Recommended Project Structure
```
YourProject/
├── Tests/
│   ├── ComponentTests/       # Individual control tests
│   ├── IntegrationTests/     # Page-level tests  
│   ├── PerformanceTests/     # Investment calculation performance
│   └── TestData/            # Reusable test data builders
└── YourProject.csproj
```

### Build Scripts
Create build scripts for common tasks:

#### build-android.sh
```bash
#!/bin/bash
dotnet build -f net9.0-android --configuration Release
```

#### run-device-tests.sh
```bash
#!/bin/bash
dotnet test -f net9.0-android --filter "Category!=Performance" --logger "console;verbosity=detailed"
```

### Environment Variables
```bash
# Optional: Set environment variables for consistent behavior
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Android (if using Android Studio)
export ANDROID_HOME=/path/to/android/sdk
export PATH=$PATH:$ANDROID_HOME/platform-tools
```

## Development Best Practices

1. **Build Time Management**: F# compilation can be slow. Use `dotnet build --no-restore` for incremental builds
2. **Memory Management**: Close unused emulators to free memory for builds
3. **Test Organization**: Group tests by component or feature area  
4. **Platform Testing**: Test on multiple platforms regularly, not just local development machine
5. **Performance Monitoring**: Use performance tests to catch regressions in investment calculations

## Troubleshooting Setup Issues

### Common Build Errors

**Error**: `workload packs that do not exist`
```bash
# Solution: Install required workloads
dotnet workload restore
dotnet workload install maui-android
```

**Error**: `Microsoft.Maui.ApplicationModel.NotImplementedInReferenceAssemblyException`
```bash
# Expected in headless environments for 7/87 Core.Tests
# These tests require platform-specific MAUI implementation
```

**Error**: F# compilation timeout
```bash
# Increase timeout and ensure sufficient memory
dotnet build --verbosity detailed --no-incremental
```

### Performance Issues
- **Slow builds**: Use SSD storage, increase available RAM
- **Emulator lag**: Allocate more RAM to Android AVD
- **Test timeouts**: Increase test timeouts for device tests (they're slower than unit tests)

## Next Steps

1. Review [Platform-Specific Setup Guides](platform-setup/) for detailed platform configuration
2. Try [Writing Your First Device Test](first-test.md) tutorial
3. Explore [Example Test Suites](../examples/) for common testing patterns  
4. Read [Best Practices](../best-practices.md) for investment app testing guidelines

For additional help, see the [Troubleshooting Guide](../troubleshooting.md) or consult the existing README files in the TestUtils directory.