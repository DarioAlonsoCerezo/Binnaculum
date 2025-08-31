---
applyTo: "src/Tests/TestUtils/**/*.cs"
---

# TestUtils Testing Guidelines

When working on MAUI TestUtils infrastructure, follow these specific patterns:

## XUnit Device Test Patterns
- Use `[Fact]` for device tests, not `[Test]` (NUnit is for F# Core.Tests only)
- Follow naming: `ComponentName_Scenario_ExpectedResult()` 
- Always test across all platforms (Android, iOS, MacCatalyst, Windows)

## BrokerAccountTemplate Testing
- Test layout states based on `_hasMovements` boolean
- Validate Observable chain cleanup with `DisposeWith(Disposables)`
- Test navigation to `BrokerMovementCreatorPage` and `BrokerAcccountPage`
- Use realistic investment test data (profitable/loss scenarios)

## Assertion Extensions
- Extend existing MAUI assertions, don't replace them
- Create investment-specific assertions like `AssertCurrencyFormat()`
- Follow Microsoft MAUI TestUtils patterns: https://github.com/dotnet/maui/tree/main/src/TestUtils/src
- Include memory leak detection for Observable chains

## Test Data Builders
- Create builders for Core F# models: `BrokerAccountBuilder`, `OverviewSnapshotBuilder`
- Use realistic financial data (currencies, percentages, dates)
- Support multiple scenarios: profitable, loss-making, mixed portfolios

## Platform-Specific Considerations
- Android: Test touch gestures and material design compliance
- iOS: Test tap recognition and safe area handling  
- Windows: Test mouse interactions and desktop behaviors
- MacCatalyst: Test hybrid touch/mouse interactions