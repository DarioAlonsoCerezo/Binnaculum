# UI Test Screenshots

This directory contains screenshots automatically captured during UI test execution.

## Screenshot Files
- `OverviewTitle_IsDisplayed.png` - Screenshot from the OverviewTitle test
- `App_LaunchesSuccessfully.png` - Screenshot from the app launch test

## How Screenshots Work
- Screenshots are automatically captured at the end of each test method
- Files use consistent naming based on the test method name
- Screenshots are **overwritten** on each test run to keep the repository clean
- All screenshots are tracked in Git for visual regression detection

## Viewing Screenshots
After running tests, you can view the latest screenshots to verify:
- UI elements are properly displayed
- App layout is correct across different screen sizes  
- Visual regression hasn't occurred

## CI/CD Integration
Screenshots are automatically attached to NUnit test results and can be viewed in:
- Test result reports
- CI/CD pipeline artifacts
- Visual Studio Test Explorer (when available)