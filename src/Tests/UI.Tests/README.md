# Binnaculum UI Tests

Simple UI tests for the Binnaculum MAUI app using Appium.

## Prerequisites

1. **Android Emulator**: Start an Android emulator
2. **Install App**: Install the Binnaculum app on the emulator
3. **Appium Server**: Run Appium server on port 4723

## Setup

### 1. Start Android Emulator
```bash
# List available emulators
emulator -list-avds

# Start an emulator (replace with your emulator name)
emulator -avd Pixel_8_API_34
```

### 2. Install Binnaculum App
```bash
# Build and install the Android app
dotnet build src/UI/Binnaculum.csproj -f net9.0-android
adb install src/UI/bin/Debug/net9.0-android/com.darioalonso.binnacle-Signed.apk
```

### 3. Start Appium Server
```bash
# Install Appium if not already installed
npm install -g appium

# Start Appium server
appium --address 127.0.0.1 --port 4723
```

## Run Tests

```bash
# Run all UI tests
dotnet test src/Tests/UI.Tests/

# Run a specific test
dotnet test src/Tests/UI.Tests/ --filter "App_Launches_And_Shows_OverviewPage"
```

## Test Structure

- **BasicAppTests.cs**: Contains simple tests that verify:
  - App launches successfully
  - OverviewTitle element is visible
  - App doesn't crash on startup
  - Screenshots can be taken

## Configuration

The tests are configured for the Binnaculum Android app with these settings:
- Package: `com.darioalonso.binnacle`
- Activity: `crc64f728827fec74e9c3.MainActivity`
- No app reset for faster test execution

Adjust these in `BasicAppTests.cs` if your app package differs.

## Troubleshooting

1. **"Failed to create Android driver"**: 
   - Verify emulator is running: `adb devices`
   - Check app is installed: `adb shell pm list packages | grep binnacle`
   - Ensure Appium server is accessible: `curl http://127.0.0.1:4723/status`

2. **"Element not found"**: 
   - Verify AutomationId="OverviewTitle" exists in your XAML
   - Check if app takes longer to load (increase timeout)

3. **Tests are slow**: 
   - This is expected for mobile UI tests (30+ seconds)
   - Consider using `noReset=true` to avoid app data clearing