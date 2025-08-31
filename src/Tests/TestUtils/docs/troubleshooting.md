# Troubleshooting Guide

This guide helps resolve common issues when working with Binnaculum's TestUtils infrastructure.

## Build and Setup Issues

### .NET 9 Installation Problems

**Problem**: `error NETSDK1178: workload packs that do not exist`
```
The project depends on the following workload packs that do not exist: Microsoft.iOS.Sdk.net9.0_18.5
```

**Solutions**:
```bash
# Install .NET 9 SDK (required version)
./dotnet-install.sh --version 9.0.101 --install-dir ~/.dotnet
export PATH="$HOME/.dotnet:$PATH"

# Verify installation
dotnet --version  # Should show 9.0.304 or later

# Install required workloads
dotnet workload install maui-android  # Required on all platforms
# iOS/MacCatalyst workloads only on macOS:
dotnet workload install maui-ios       # macOS only
dotnet workload install maui-maccatalyst # macOS only
```

**Platform Limitations**:
- Linux/Windows: Can only build Android and Windows targets
- macOS: Can build all targets (Android, iOS, MacCatalyst, Windows)

### MAUI Workload Installation Issues

**Problem**: Workload installation hangs or fails
```
Installing pack Microsoft.Android.Sdk.Linux version 35.0.92...
```

**Solutions**:
```bash
# Clear workload cache
dotnet workload clean

# Reinstall with verbose output
dotnet workload install maui-android --verbosity diagnostic

# If still failing, use global.json to pin SDK version
echo '{"sdk": {"version": "9.0.304"}}' > global.json
```

**Important**: MAUI workload installation takes ~66 seconds. Never cancel - always set appropriate timeout.

### Build Timeout Issues

**Problem**: Build processes timeout or appear to hang

**Solutions**:
```bash
# Increase build verbosity to see progress
dotnet build --verbosity detailed

# Use appropriate timeouts for different components:
# F# Core project: 30 seconds
dotnet build src/Core/Core.fsproj --timeout 30

# Android device tests: 120 seconds (complex MAUI build)
dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-android --timeout 120
```

**Expected Build Times**:
- F# Core: 13-14 seconds  
- F# Core.Tests: 11-12 seconds
- Android DeviceTests: 95 seconds (includes resource processing)
- Test Runners: 7 seconds

## Test Execution Issues

### Expected Test Failures in Headless Environment

**Problem**: Core.Tests shows 7 failing tests
```
Microsoft.Maui.ApplicationModel.NotImplementedInReferenceAssemblyException: 
This functionality is not implemented in the portable version of this assembly.
```

**Solution**: This is expected behavior. 80/87 tests pass in headless environments.

The 7 failing tests require platform-specific MAUI implementations:
- `SavedPreferencesTests` (requires platform storage)
- UI-dependent components that need actual device/emulator

**Validation**: Run this command to verify expected behavior:
```bash
dotnet test src/Tests/Core.Tests/Core.Tests.fsproj
# Expected output: 80/87 tests pass
```

### Device Test Discovery Problems

**Problem**: Tests not discovered in Visual Runner or headless execution

**Checklist**:
1. **Test Class Accessibility**: Ensure test classes are `public`
   ```csharp
   public class BrokerAccountTemplateTests  // Must be public
   {
       [Fact]
       public async Task TestMethod() { }   // Must be public
   }
   ```

2. **Proper Attributes**: Use `[Fact]` for xUnit (not `[Test]` for NUnit)
   ```csharp
   [Fact]  // Correct for device tests
   public async Task ComponentTest_Scenario_ExpectedResult() { }
   ```

3. **Assembly References**: Verify test project references
   ```xml
   <PackageReference Include="xunit" Version="2.4.2" />
   <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5" />
   ```

### Platform-Specific Test Failures

**Problem**: Tests fail on specific platforms but pass on others

**Android Issues**:
```bash
# Check Android SDK installation
echo $ANDROID_HOME
ls $ANDROID_HOME/platform-tools

# Verify emulator is running
adb devices

# Check AVD memory allocation (should be 4GB+ for investment tests)
```

**iOS Issues** (macOS only):
```bash
# Verify Xcode installation
xcode-select -p
xcodebuild -version

# Check iOS Simulator
xcrun simctl list devices
```

**Windows Issues**:
```bash
# Enable Developer Mode
# Settings → Update & Security → For developers → Developer mode: On

# Check Windows target framework
dotnet build -f net9.0-windows10.0.19041.0 --verbosity diagnostic
```

## Performance and Memory Issues

### Slow Build Performance

**Problem**: Builds are taking longer than expected

**Solutions**:
```bash
# Use incremental builds
dotnet build --no-restore

# Parallel build (if sufficient memory)
dotnet build -m:4

# Clear build artifacts if issues persist
dotnet clean
rm -rf bin/ obj/
dotnet restore
dotnet build
```

**Memory Requirements**:
- 8GB RAM minimum
- 16GB recommended for emulator usage
- Close unnecessary applications during builds

### Test Execution Memory Issues

**Problem**: Tests fail with out-of-memory errors

**Solutions**:
1. **Run tests in smaller batches**:
   ```bash
   # Filter to specific test categories
   dotnet test --filter "Category=Component"
   dotnet test --filter "Category=Performance"
   ```

2. **Monitor memory usage in tests**:
   ```csharp
   [Fact]
   public async Task MemoryIntensiveTest_LargeDataset_StaysWithinLimits()
   {
       var memoryBefore = GC.GetTotalMemory(false);
       
       // Test logic here
       
       GC.Collect();
       var memoryAfter = GC.GetTotalMemory(true);
       var memoryUsed = memoryAfter - memoryBefore;
       
       Assert.True(memoryUsed < 100 * 1024 * 1024, // 100MB limit
           $"Memory usage {memoryUsed / 1024 / 1024}MB exceeds limit");
   }
   ```

### Observable Memory Leaks

**Problem**: Tests show memory leak warnings for Observable chains

**Solution**: Always dispose subscriptions properly:
```csharp
public class ComponentTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    [Fact]
    public async Task ObservableTest_WithProperDisposal_NoLeaks()
    {
        var subscription = viewModel.WhenAnyValue(x => x.Property)
            .Subscribe(value => { /* handle value */ })
            .DisposeWith(_disposables);  // Critical: proper disposal
        
        // Test logic
        
        subscription.AssertObservableMemoryLeak();
    }
    
    public void Dispose()
    {
        _disposables?.Dispose();
    }
}
```

## Investment Data and Financial Calculation Issues

### Currency Formatting Problems

**Problem**: Currency formatting tests fail across different cultures

**Solutions**:
```csharp
// Use culture-specific testing
[Theory]
[InlineData("en-US", "$125,750.50")]
[InlineData("de-DE", "125.750,50 €")]
[InlineData("en-GB", "£125,750.50")]
public async Task CurrencyFormat_VariousCultures_FormatsCorrectly(
    string culture, string expected)
{
    using (new CultureScope(culture))
    {
        var formatted = account.FormatCurrency();
        formatted.AssertCurrencyFormat(expected);
    }
}

// Avoid culture-sensitive assertions
// Bad:
Assert.Equal("$125,750.50", formatted); // Fails in German culture

// Good:
formatted.AssertCurrencyFormat("$125,750.50"); // Culture-aware assertion
```

### Decimal Precision Issues

**Problem**: Financial calculations show precision errors

**Solutions**:
```csharp
// Always use decimal for financial calculations
decimal balance = 125750.50m;  // Correct
double balance = 125750.50;    // Wrong - floating point errors

// Use appropriate precision for comparisons
actualValue.AssertDecimalEquals(expectedValue, precision: 2);

// For percentage calculations
var percentage = (currentValue - originalValue) / originalValue * 100m;
percentage.AssertDecimalEquals(expectedPercentage, precision: 2);
```

## CI/CD Integration Issues

### GitHub Actions Failures

**Problem**: Tests pass locally but fail in CI/CD

**Common Issues**:

1. **Timeout Issues**: CI environments are slower
   ```yaml
   # In GitHub Actions
   - name: Build Android Tests
     run: dotnet build -f net9.0-android
     timeout-minutes: 10  # Increase timeout for CI
   ```

2. **Platform Workload Availability**:
   ```yaml
   # Only Android workloads available in Linux runners
   - name: Install workloads
     run: dotnet workload install maui-android
     # Don't install iOS workloads on Linux runners
   ```

3. **Headless Environment Limitations**:
   ```bash
   # Expected in CI: 80/87 Core.Tests pass
   # UI-dependent tests will fail - this is normal
   dotnet test src/Tests/Core.Tests/Core.Tests.fsproj
   ```

### Artifact Collection Issues

**Problem**: Test artifacts not collected properly

**Solutions**:
```bash
# Use HeadlessRunner with artifact collection
./scripts/run-headless-tests.sh \
  --platform android \
  --collect-artifacts \
  --artifact-path ./test-results \
  --output-format xml
```

## Development Environment Issues

### IDE Configuration Problems

**Visual Studio 2022**:
- Install "MAUI" workload through Visual Studio Installer
- Install "F# Development Tools"
- Enable XAML Hot Reload for faster development

**Visual Studio Code**:
```bash
# Install required extensions
code --install-extension ms-dotnettools.csharp
code --install-extension ionide.ionide-fsharp
```

**JetBrains Rider**:
- Enable F# support (built-in)
- Install MAUI development plugin

### Version Compatibility Issues

**Problem**: Package version conflicts

**Solutions**:
```xml
<!-- Use Directory.Packages.props for version management -->
<PackageVersion Include="Microsoft.Maui.Controls" Version="9.0.82" />
<PackageVersion Include="xunit" Version="2.4.2" />

<!-- Ensure consistent versions across projects -->
<PackageReference Include="Microsoft.Maui.Controls" />
<!-- Version comes from Directory.Packages.props -->
```

## Getting Help

### Debug Information Collection

When reporting issues, collect this information:

```bash
# System information
dotnet --info
dotnet workload list

# Project-specific information
dotnet build --verbosity diagnostic > build.log 2>&1
dotnet test --verbosity diagnostic > test.log 2>&1

# Memory and performance information
dotnet test --collect:"XPlat Code Coverage" --logger trx
```

### Common Log Analysis

**Build Logs**: Look for these indicators:
- `error NETSDK1178`: Workload missing
- `MSB4018`: Build task failed
- `timeout`: Increase build timeout

**Test Logs**: Key patterns:
- `80 passed, 7 failed`: Normal for Core.Tests in headless environment
- `NotImplementedInReferenceAssemblyException`: Expected for MAUI-dependent tests
- Memory usage > 100MB: Consider optimization

### Support Resources

1. **Documentation**: Start with [Getting Started Guide](docs/getting-started/setup-guide.md)
2. **Examples**: Check [Component Test Examples](docs/examples/component-tests/) for patterns
3. **Architecture**: Review [TestUtils Architecture](docs/architecture.md) for system understanding
4. **Community**: Consult existing GitHub issues and discussions

### Platform-Specific Support

**Android Development**:
- Android Studio documentation
- Android SDK troubleshooting guides
- Emulator performance optimization

**iOS Development** (macOS only):
- Xcode documentation and release notes
- iOS Simulator troubleshooting
- Apple Developer documentation

**Windows Development**:
- Windows App SDK documentation
- Visual Studio MAUI troubleshooting
- Windows platform requirements

This troubleshooting guide covers the most common issues encountered when working with Binnaculum's TestUtils infrastructure. For issues not covered here, check the component-specific README files or create a detailed issue report with the debug information outlined above.