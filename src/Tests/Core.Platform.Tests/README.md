# Core.Platform.Tests

This project contains MAUI platform-dependent tests that require device or platform-specific implementations.

## Overview

These tests were separated from `Core.Tests` to eliminate "expected failures" in CI environments. The tests in this project:

- **Require MAUI platform services** (Android, iOS, Windows, MacCatalyst)
- **Cannot run in headless environments** (CI runners without platform implementations)
- **Test platform-specific functionality** like `Microsoft.Maui.Storage.Preferences`

## Test Categories

All tests in this project are marked with:
- `[<Category("RequiresMauiPlatform")>]` - Indicates MAUI platform dependency
- `[<Category("PlatformSpecific")>]` - Indicates platform-specific behavior

## Running Tests

### On Development Machines
```bash
# Run all platform tests (requires platform workloads)
dotnet test src/Tests/Core.Platform.Tests/Core.Platform.Tests.fsproj

# Run for specific platform
dotnet test src/Tests/Core.Platform.Tests/Core.Platform.Tests.fsproj -f net9.0-android
dotnet test src/Tests/Core.Platform.Tests/Core.Platform.Tests.fsproj -f net9.0-windows10.0.19041.0
```

### Platform Requirements
- **Android**: Requires `maui-android` workload
- **Windows**: Requires `maui-windows` workload  
- **iOS**: Requires `maui-ios` workload (macOS only)
- **MacCatalyst**: Requires `maui-maccatalyst` workload (macOS only)

### CI/CD Integration

These tests should **only run on device-capable runners**:

```yaml
- name: Run Platform Tests (Android)
  if: matrix.platform == 'android' || matrix.platform == 'device-runner'
  run: dotnet test src/Tests/Core.Platform.Tests/Core.Platform.Tests.fsproj -f net9.0-android
  timeout-minutes: 10

- name: Run Platform Tests (Windows) 
  if: matrix.platform == 'windows'
  run: dotnet test src/Tests/Core.Platform.Tests/Core.Platform.Tests.fsproj -f net9.0-windows10.0.19041.0
  timeout-minutes: 10
```

## Test Structure

### Platform Testing Helper (`PlatformTestEnvironment.fs`)
- **Environment detection** - CI, headless, platform availability
- **Automatic test skipping** - Skip tests when platform services unavailable
- **Platform initialization** - Setup platform services for testing

### SavedPreferencesTests (`SavedPreferencesTests.fs`)
Tests for `SavedPreferences` functionality that requires `Microsoft.Maui.Storage.Preferences`:
- Theme preference changes
- Language preference changes  
- Currency preference changes
- Account creation preferences
- Default ticker preferences
- Group option preferences with reload triggers

## Expected Results

- **On Platform-Capable Runners**: 6/6 tests pass
- **On Headless/CI Runners**: All tests skipped (not failed)
- **Integration**: Core.Tests now passes 81/81 (100% success rate)

## Development Guidelines

When adding new platform-dependent tests:
1. **Add to this project**, not `Core.Tests`
2. **Use `requiresMauiPlatform()`** wrapper for test actions
3. **Mark with appropriate categories** for CI filtering
4. **Test on actual devices/emulators** when possible