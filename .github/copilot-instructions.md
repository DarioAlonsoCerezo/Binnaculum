# Copilot Instructions for Binnaculum

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Project Overview
Binnaculum is a cross-platform investment tracking app built with .NET 9 and .NET MAUI. The solution includes F# and C# projects, focusing on investment, portfolio, and bank account management. It targets Android, iOS, MacCatalyst, and Windows platforms using SQLite for data persistence and ReactiveUI for MVVM patterns.

## Copilot Task Guidelines

### ? Ideal Tasks for Copilot Assignment
- **TestUtils Implementation**: Device testing infrastructure, assertion extensions, test data builders
- **UI Component Testing**: BrokerAccountTemplate tests, PercentageControl validation, cross-platform UI testing
- **Bug Fixes**: Specific compilation errors, test failures, package reference issues
- **Code Quality**: XAML formatting, code style compliance, adding missing tests
- **Documentation**: Update README files, add code comments, create examples
- **Performance Optimization**: Memory leak detection, Observable chain cleanup

### ? Tasks to Handle Manually (Complex/Critical)
- **Core Financial Logic**: Complex percentage calculations, portfolio balance algorithms
- **Database Schema Changes**: SQLite model updates, migration scripts
- **Architecture Decisions**: Multi-platform targeting, project structure changes
- **Security Concerns**: Authentication flows, sensitive data handling
- **Production Issues**: Critical bugs affecting user data

### ?? Task Scoping Best Practices
- **Be Specific**: "Implement BrokerAccountTemplate device tests for layout states" vs "Add some tests"  
- **Include Acceptance Criteria**: "Tests must run on Android, iOS, Windows, MacCatalyst"
- **Reference Examples**: "Follow patterns in Microsoft MAUI TestUtils: [URL]"
- **Specify Files**: "Update files in src/Tests/TestUtils/UI.DeviceTests/"

## Working Effectively

### Prerequisites and Setup
Install .NET 9 SDK (CRITICAL - project requires .NET 9):
```bash
./dotnet-install.sh --version 9.0.101 --install-dir ~/.dotnet
export PATH="$HOME/.dotnet:$PATH"
```

Install MAUI workloads (REQUIRED for UI builds):
```bash
dotnet workload install maui-android  # Takes ~66 seconds. NEVER CANCEL. Set timeout to 90+ seconds.
```
**Note**: iOS/macOS workloads are only available on macOS. On Linux/Windows, you can only build Android and Windows targets.

### Build and Test Commands
**NEVER CANCEL any builds or tests - they can take significant time but must complete.**

Bootstrap and build the repository:
```bash
dotnet restore                                    # 4-5 seconds for Core, may fail initially without workloads
dotnet build src/Core/Core.fsproj                # 13-14 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
dotnet build src/Tests/Core.Tests/Core.Tests.fsproj  # 11-12 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
```

Build MAUI UI (Android only on Linux):
```bash
dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj  # 95 seconds. NEVER CANCEL. Set timeout to 120+ seconds.
dotnet build src/Tests/TestUtils/UI.DeviceTests.Runners/UI.DeviceTests.Runners.csproj  # 7 seconds.
```

**Platform-Specific Builds**:
- **Android**: `dotnet build -f net9.0-android` (available on all platforms)
- **iOS**: `dotnet build -f net9.0-ios` (requires macOS + Xcode)
- **MacCatalyst**: `dotnet build -f net9.0-maccatalyst` (requires macOS)
- **Windows**: `dotnet build -f net9.0-windows10.0.19041.0` (requires Windows)

Run tests:
```bash
dotnet test src/Tests/Core.Tests/Core.Tests.fsproj  # 2-3 seconds. 80/87 tests pass (7 MAUI-dependent tests fail in headless environment - this is expected)
dotnet test --filter "BrokerFinancialSnapshotManager"  # Run specific performance tests - 51/52 tests pass
```

### Build Time Expectations
- **.NET 9 SDK installation**: ~66 seconds
- **MAUI workload installation**: ~66 seconds  
- **F# Core project**: 13-14 seconds
- **F# Core.Tests**: 11-12 seconds
- **Android DeviceTests**: 95 seconds (complex MAUI build with resource processing)
- **Test execution**: 2-3 seconds

## Validation and Quality Checks

Always validate changes with these steps:
1. **Build Core project**: Ensures F# business logic compiles
2. **Run Core tests**: Validates business logic correctness (expect 80/87 to pass)
3. **Build Android target**: Validates MAUI integration (if on supported platform)
4. **Run BrokerFinancialSnapshotManager tests**: Validates performance characteristics

XAML styling validation:
- Use `XAMLStylerConfiguration.json` for consistent XAML formatting
- Follow `.editorconfig` rules for C# code styling
- Ensure XAML follows project's attribute ordering and formatting rules

### Manual Testing Scenarios
Since the MAUI app cannot run in headless environments, validate UI changes by:
1. **Build successfully**: Ensure compilation without errors
2. **Review XAML structure**: Check pages in `src/UI/Pages/` and controls in `src/UI/Controls/`
3. **Verify resource usage**: Ensure images, fonts, and styles are properly referenced
4. **Test platform targets**: Build for target platforms (Android on Linux, all on macOS/Windows)

## Platform Requirements and Limitations

### Development Environment Constraints
- **Linux/Windows**: Can build Android and Windows targets only
- **macOS**: Can build all targets (iOS, MacCatalyst, Android, Windows)
- **Headless environments**: Cannot run MAUI apps but can build and test core logic

### Multi-Platform Considerations
The UI project uses OS-aware conditional compilation:
```xml
<TargetFrameworks>net9.0-android</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">net9.0-android;net9.0-windows10.0.19041.0</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('osx'))">net9.0-android;net9.0-ios;net9.0-maccatalyst</TargetFrameworks>
```

**Platform Support Matrix:**
| Operating System | Available Targets | MAUI Workloads Required |
|-----------------|-------------------|-------------------------|
| **Linux/Ubuntu** | Android | `maui-android` |
| **Windows** | Android, Windows | `maui-android`, `maui-windows` |
| **macOS** | Android, iOS, macOS | `maui-android`, `maui-ios`, `maui-maccatalyst` |

## Project Structure and Key Components

### Core Architecture
- **`src/Core/`**: F# core logic with business rules, database models, and financial calculations
- **`src/UI/`**: C# MAUI UI project with pages, controls, and platform-specific implementations
- **`src/Tests/Core.Tests/`**: F# unit tests with comprehensive performance testing
- **`src/Tests/TestUtils/`**: MAUI device testing infrastructure

### Key Directories
- **Database Layer**: `src/Core/Database/` - SQLite data access and models
- **Business Logic**: `src/Core/Snapshots/` - Financial calculation engines
- **UI Pages**: `src/UI/Pages/` - XAML pages (AccountCreator, BrokerAccount, Calendar, Overview, Settings, etc.)
- **UI Controls**: `src/UI/Controls/` - Custom MAUI controls and templates
- **Resources**: `src/UI/Resources/` - Images, fonts, styles, and XAML resources

### Testing Infrastructure
- **Performance Tests**: `BrokerFinancialSnapshotManagerPerformanceTests.fs` - Mobile-optimized performance validation
- **Device Tests**: Multi-platform MAUI testing with xUnit
- **Integration Tests**: Comprehensive business logic validation

## Code Standards and Development Guidelines

### Language Usage
- **F# for core logic**: Business rules, database operations, financial calculations
- **C# for UI and platform code**: MAUI pages, controls, platform-specific implementations
- **Follow .NET 9 patterns**: Use modern .NET features, avoid legacy Xamarin.Forms patterns

### Error Handling Strategy
- **No Try-Catch in Core Logic**: Allow exceptions to bubble up to UI layer
- **Use CatchCoreError in UI**: Handle exceptions from core operations in C# UI code
- **Fail Fast with failwith**: Use descriptive F# error messages for exceptional cases
- **UI-Driven Error Display**: Show errors through MAUI popups/alerts

### Code Quality
- **Follow .editorconfig**: C# formatting and style rules are enforced
- **Use XAML Styler**: Maintain consistent XAML formatting with `XAMLStylerConfiguration.json`
- **Write comprehensive tests**: Update Core.Tests for any business logic changes
- **Document complex logic**: Especially financial calculations and database operations

## Common Development Tasks

### Adding New Features
1. **Core Logic**: Add to appropriate `src/Core/` subdirectory (Database, Models, Snapshots, etc.)
2. **UI Implementation**: Create pages in `src/UI/Pages/` and controls in `src/UI/Controls/`
3. **Testing**: Add tests to `src/Tests/Core.Tests/` for F# logic
4. **Validation**: Run full build and test suite

### Database Changes
- **Models**: Update `src/Core/Models/Models.fs`
- **Extensions**: Modify database extensions in `src/Core/Database/`
- **Queries**: Update SQL queries in `src/Core/SQL/`
- **Always test**: Ensure existing functionality is preserved

### Performance Considerations
- **Mobile Targets**: Consider memory and CPU constraints on mobile devices
- **Chunked Processing**: Use established patterns for large dataset handling
- **GC Pressure**: Monitor garbage collection impact in performance tests
- **Async Operations**: Use F# async workflows for concurrent operations

This project has comprehensive test coverage including performance benchmarks specifically designed for mobile constraints. The BrokerFinancialSnapshotManager includes tests for memory pressure, concurrent processing, and mobile CPU simulation.