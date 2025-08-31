# Android Development Setup

Complete guide for setting up Android development for Binnaculum device tests.

## Prerequisites

- .NET 9 SDK installed
- Windows 10+, macOS 10.15+, or Ubuntu 20.04+
- 8GB RAM (16GB recommended)
- 10GB free disk space for Android SDK and emulators

## Step 1: Install Android Development Tools

### Option A: Android Studio (Recommended)

1. **Download Android Studio**
   - Visit [developer.android.com/studio](https://developer.android.com/studio)
   - Download for your platform

2. **Install with SDK Components**
   ```bash
   # During installation, ensure these are selected:
   # - Android SDK
   # - Android SDK Platform-Tools  
   # - Android Virtual Device (AVD)
   # - Intel x86 Emulator Accelerator (HAXM) - Intel systems
   # - Google APIs Intel x86 Atom_64 System Image
   ```

3. **Configure SDK Location**
   ```bash
   # Add to your shell profile (.bashrc, .zshrc, etc.)
   export ANDROID_HOME=$HOME/Android/Sdk
   export PATH=$PATH:$ANDROID_HOME/platform-tools
   export PATH=$PATH:$ANDROID_HOME/cmdline-tools/latest/bin
   ```

### Option B: Command Line Tools Only

```bash
# Download command line tools
wget https://dl.google.com/android/repository/commandlinetools-linux-9477386_latest.zip

# Extract and setup
mkdir -p $HOME/Android/Sdk/cmdline-tools
unzip commandlinetools-linux-9477386_latest.zip -d $HOME/Android/Sdk/cmdline-tools/
mv $HOME/Android/Sdk/cmdline-tools/cmdline-tools $HOME/Android/Sdk/cmdline-tools/latest

# Set environment variables
export ANDROID_HOME=$HOME/Android/Sdk
export PATH=$PATH:$ANDROID_HOME/cmdline-tools/latest/bin
export PATH=$PATH:$ANDROID_HOME/platform-tools
```

## Step 2: Install MAUI Android Workload

```bash
# Install Android workload (takes ~66 seconds)
dotnet workload install maui-android

# Verify installation
dotnet workload list
# Should show: maui-android 9.0.82/9.0.100
```

## Step 3: Configure Android SDK

### Accept Licenses
```bash
# Accept all Android SDK licenses (required)
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager --licenses
# Type 'y' for each license prompt
```

### Install Required SDK Components
```bash
# Install platform and build tools
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager "platforms;android-34"
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager "build-tools;34.0.0"
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager "platform-tools"

# Install Google APIs for testing
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager "add-ons;addon-google_apis-google-34"

# Install system images for emulator
$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager "system-images;android-34;google_apis;x86_64"
```

## Step 4: Create Android Virtual Device (AVD)

### Using Android Studio (GUI)
1. Open Android Studio
2. Tools → AVD Manager
3. Create Virtual Device
4. Select "Pixel 6" or similar modern device
5. Choose "API 34" system image with Google APIs
6. Configure AVD:
   - RAM: 4GB (minimum for investment tests)
   - VM Heap: 512MB
   - Internal Storage: 6GB
   - Enable Hardware Acceleration

### Using Command Line
```bash
# Create AVD for testing
$ANDROID_HOME/cmdline-tools/latest/bin/avdmanager create avd \
  -n "Binnaculum_Test_Device" \
  -k "system-images;android-34;google_apis;x86_64" \
  -g "google_apis" \
  -c "6144M"

# Configure AVD settings
echo "hw.ramSize=4096" >> ~/.android/avd/Binnaculum_Test_Device.avd/config.ini
echo "vm.heapSize=512" >> ~/.android/avd/Binnaculum_Test_Device.avd/config.ini
```

## Step 5: Validate Android Setup

### Test Device Connectivity
```bash
# Start emulator
$ANDROID_HOME/emulator/emulator -avd Binnaculum_Test_Device

# In another terminal, verify device connection
adb devices
# Should show: emulator-5554    device
```

### Build Binnaculum for Android
```bash
# Navigate to Binnaculum repository
cd /path/to/Binnaculum

# Build Core project first
dotnet build src/Core/Core.fsproj

# Build Android device tests (95 seconds - be patient!)
dotnet build src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-android

# Verify successful build
ls src/Tests/TestUtils/UI.DeviceTests/bin/Debug/net9.0-android/
# Should contain APK and related files
```

## Step 6: Investment App Testing Configuration

### Configure Test Device for Investment Testing

**Performance Settings** for reliable investment calculations:
```bash
# Disable animations for consistent test timing
adb shell settings put global window_animation_scale 0
adb shell settings put global transition_animation_scale 0  
adb shell settings put global animator_duration_scale 0

# Set appropriate time zone for financial markets
adb shell su -c "setprop persist.sys.timezone 'America/New_York'"

# Ensure sufficient memory for investment calculations
adb shell settings put global low_ram false
```

**Install Test APK**:
```bash
# Deploy test application to emulator/device
dotnet publish src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj \
  -f net9.0-android \
  -c Release

# APK will be deployed automatically, or manually install:
adb install src/Tests/TestUtils/UI.DeviceTests/bin/Release/net9.0-android/publish/com.binnaculum.devicetests-Signed.apk
```

## Step 7: Run Investment Device Tests

### Command Line Testing
```bash
# Run all device tests
dotnet test src/Tests/TestUtils/UI.DeviceTests/UI.DeviceTests.csproj -f net9.0-android

# Run specific investment tests
dotnet test -f net9.0-android --filter "BrokerAccountTemplate"

# Run with detailed logging
dotnet test -f net9.0-android --logger "console;verbosity=detailed"
```

### Visual Test Runner
```bash
# Build Visual Runner
dotnet build src/Tests/TestUtils/UI.DeviceTests.Runners/UI.DeviceTests.Runners.csproj -f net9.0-android

# Deploy to device for interactive testing
dotnet publish -f net9.0-android -c Debug
# Use Android Studio or adb to launch the Visual Runner app
```

## Step 8: Performance Optimization

### Emulator Performance
```bash
# Configure emulator for better performance
echo "hw.gpu.enabled=yes" >> ~/.android/avd/Binnaculum_Test_Device.avd/config.ini
echo "hw.gpu.mode=host" >> ~/.android/avd/Binnaculum_Test_Device.avd/config.ini

# Enable hardware acceleration (Intel systems)
echo "hw.cpu.arch=x86_64" >> ~/.android/avd/Binnaculum_Test_Device.avd/config.ini
```

### Memory Settings for Investment Tests
```bash
# Investment calculations can be memory-intensive
# Allocate sufficient heap space
echo "vm.heapSize=1024" >> ~/.android/avd/Binnaculum_Test_Device.avd/config.ini

# Increase RAM for large portfolios
echo "hw.ramSize=6144" >> ~/.android/avd/Binnaculum_Test_Device.avd/config.ini
```

## Android-Specific Testing Features

### Material Design Validation
```csharp
[Fact]
public async Task BrokerAccountTemplate_Android_MaterialDesignCompliance()
{
    // Skip on non-Android platforms
    if (DeviceInfo.Platform != DevicePlatform.Android)
        return;
    
    var template = new BrokerAccountTemplate();
    await template.LoadAsync(InvestmentTestData.CreateProfitableAccount());
    
    // Test Android-specific Material Design features
    await template.SimulateTouchAsync();
    
    template.AssertMaterialRippleEffect();
    template.AssertElevationShadow(expectedElevation: 4);
    template.AssertCornerRadius(MaterialDesign.CardCornerRadius);
    template.AssertTouchFeedbackTiming(MaterialDesignTiming.RippleDuration);
}
```

### Android Permissions Testing
```csharp
[Fact]
public async Task InvestmentDataExport_Android_RequestsStoragePermission()
{
    if (DeviceInfo.Platform != DevicePlatform.Android)
        return;
        
    var exportService = new InvestmentDataExportService();
    
    // Act - Request data export (should prompt for permission)
    var result = await exportService.ExportPortfolioAsync(testPortfolio);
    
    // Assert - Permission was requested and granted
    exportService.AssertStoragePermissionRequested();
    Assert.True(result.Success);
}
```

## Troubleshooting Android Issues

### Common Problems

**Emulator won't start**:
```bash
# Check virtualization support
egrep -c '(vmx|svm)' /proc/cpuinfo
# Should return > 0

# Enable KVM acceleration (Linux)
sudo apt install qemu-kvm libvirt-daemon-system libvirt-clients bridge-utils
sudo adduser $USER kvm
```

**Build failures**:
```bash
# Clear Android build cache
rm -rf ~/.android/build-cache/
rm -rf bin/ obj/

# Reinstall Android workload
dotnet workload uninstall maui-android
dotnet workload install maui-android
```

**Test deployment failures**:
```bash
# Check device connection
adb devices

# Restart ADB if needed
adb kill-server
adb start-server

# Check available space on device
adb shell df
```

**Performance issues**:
```bash
# Check emulator resource allocation
ps aux | grep emulator

# Monitor memory usage during tests
adb shell procrank

# Check for memory leaks in test app
adb shell dumpsys meminfo com.binnaculum.devicetests
```

### Android-Specific Configuration Files

**android.manifest** requirements for investment testing:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.VIBRATE" />
```

**proguard-rules.pro** to preserve investment classes:
```
-keep class Binnaculum.Core.Models.** { *; }
-keep class Binnaculum.UI.Controls.** { *; }
-keepattributes *Annotation*
```

## Next Steps

1. **Configure iOS Testing**: If on macOS, set up [iOS testing environment](ios-setup.md)
2. **Try Example Tests**: Follow [Component Test Examples](../../examples/component-tests/)
3. **Read Best Practices**: Review [Investment App Testing Best Practices](../../best-practices.md)
4. **Performance Testing**: Set up [Performance Test Examples](../../examples/performance-tests/)

## Additional Resources

- [Android Developer Documentation](https://developer.android.com/)
- [MAUI Android Development](https://docs.microsoft.com/en-us/dotnet/maui/android/)
- [Android Testing Fundamentals](https://developer.android.com/training/testing/)
- [Material Design Guidelines](https://material.io/design/)

This setup ensures comprehensive Android testing capabilities for Binnaculum's investment tracking functionality with proper device configuration, performance optimization, and platform-specific testing features.