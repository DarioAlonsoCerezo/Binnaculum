# TestUtils Architecture

This document describes the architecture and design decisions behind Binnaculum's TestUtils infrastructure for investment app testing.

## Overview

The TestUtils architecture follows a modular, layered design that enables comprehensive testing of investment tracking functionality across multiple platforms. The system is built on Microsoft MAUI TestUtils patterns with investment-specific extensions.

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Test Applications                         │
│  ┌─────────────────┐  ┌─────────────────┐                  │
│  │ Visual Runner   │  │ Headless Runner │                  │  
│  │ (Interactive)   │  │ (CI/CD)         │                  │
│  └─────────────────┘  └─────────────────┘                  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Test Infrastructure                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Device Tests    │  │ Appium Tests    │  │ Core Tests   │ │
│  │ (UI Components) │  │ (E2E)           │  │ (UITest.Core)│ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│               Investment-Specific Extensions                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Assertion       │  │ Test Data       │  │ Test Helpers │ │
│  │ Extensions      │  │ Builders        │  │ & Utilities  │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      Core Foundation                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Binnaculum Core │  │ System.Reactive │  │ XUnit        │ │
│  │ (F# Domain)     │  │ (Observables)   │  │ (Testing)    │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Device Testing Framework (`UI.DeviceTests`)

The device testing framework provides the foundation for testing MAUI UI components on actual devices and emulators.

**Purpose:**
- Test UI components in their native platform environments
- Validate investment-specific controls and templates  
- Test F# business logic integration with C# UI components
- Perform memory leak detection for Observable chains

**Key Files:**
```
UI.DeviceTests/
├── BasicDeviceTestingFrameworkTests.cs     # Core framework validation
├── BinnaculumAssertionExtensions.cs        # Investment-specific assertions
├── TestDataBuilders.cs                     # Fluent test data builders
├── TestHelpers.cs                          # Utility methods
├── InvestmentTestData.cs                   # Realistic investment scenarios
└── Platform-Specific/                     # Platform assertion extensions
    ├── AssertionExtensions.Android.cs
    ├── AssertionExtensions.iOS.cs
    └── AssertionExtensions.Windows.cs
```

**Design Patterns:**
- **Builder Pattern**: Fluent APIs for creating realistic investment test data
- **Extension Methods**: Platform-specific assertion extensions
- **Dependency Injection**: Mock services for isolated component testing
- **Disposable Pattern**: Proper cleanup of Observable subscriptions

### 2. Test Runners (`UI.DeviceTests.Runners`)

The test runner infrastructure provides both interactive and headless test execution capabilities.

#### Visual Test Runner

An interactive XAML-based test runner for development and debugging:

```
VisualRunner/
├── VisualTestRunnerApp.cs              # MAUI app entry point
├── Shell/
│   └── VisualTestRunnerShell.xaml      # Navigation shell
├── ViewModels/
│   ├── TestRunnerViewModel.cs          # Main orchestration
│   ├── TestAssemblyViewModel.cs        # Assembly grouping
│   └── TestCaseViewModel.cs            # Individual test representation
└── Pages/
    ├── TestDiscoveryPage.xaml          # Test browser and selection
    ├── TestExecutionPage.xaml          # Real-time execution monitoring  
    └── TestResultsPage.xaml            # Detailed results display
```

**Features:**
- Hierarchical test discovery (Assembly → Class → Method)
- Real-time test execution with progress updates
- Interactive test selection and filtering
- Detailed results display with error information
- Search and navigation capabilities

#### Headless Test Runner

A command-line test runner optimized for CI/CD environments:

```
HeadlessRunner/
├── HeadlessTestRunner.cs               # Core execution engine
├── Configuration/
│   ├── CommandLineOptions.cs          # CLI argument parsing
│   └── TestExecutionConfig.cs         # Runtime configuration
├── ResultWriters/
│   ├── ConsoleResultsWriter.cs        # Console output
│   ├── XmlResultsWriter.cs            # JUnit XML format
│   └── JsonResultsWriter.cs           # Structured JSON results
└── Tests/
    └── HeadlessRunnerTests.cs          # Runner validation tests
```

**Features:**
- Command-line interface for automated testing
- Multiple output formats (Console, XML, JSON)
- Parallel test execution capability
- Filtering and retry logic
- Artifact collection for failed tests

### 3. Appium Integration (`UITest.Appium`)

Cross-platform UI automation using Appium for end-to-end testing.

```
UITest.Appium/
├── AppiumApp.cs                        # Platform-agnostic app interface
├── AppiumConfig.cs                     # Configuration management  
├── BinnaculumAppFactory.cs             # App instance creation
├── Apps/                               # Platform-specific app implementations
│   ├── BinnaculumAndroidApp.cs
│   ├── BinnaculumiOSApp.cs
│   ├── BinnaculumMacCatalystApp.cs
│   └── BinnaculumWindowsApp.cs
├── Components/                         # Page object model components
│   └── BrokerAccountTemplateComponent.cs
└── TestData/
    └── InvestmentTestData.cs           # Shared test data for E2E tests
```

**Design Patterns:**
- **Page Object Model**: Encapsulate page interactions and elements
- **Factory Pattern**: Create platform-appropriate app instances
- **Configuration Pattern**: Centralized Appium driver configuration

### 4. Core Testing Infrastructure (`UITest.Core`)

Foundational interfaces and utilities for cross-platform UI testing.

```
UITest.Core/
├── IApp.cs                             # Main app interaction interface
├── IUIElement.cs                       # Element interaction abstraction
├── TestDevice.cs                       # Device capabilities and management
├── Query/
│   ├── By.cs                          # Element selection strategies
│   └── IQuery.cs                      # Query interface definition
└── Configuration/
    ├── IConfig.cs                     # Test configuration interface
    └── TestPlatform.cs               # Platform enumeration
```

## Design Principles

### 1. Separation of Concerns

Each layer has a distinct responsibility:

- **UI.DeviceTests**: Component-level testing and assertions
- **UI.DeviceTests.Runners**: Test execution and orchestration  
- **UITest.Appium**: End-to-end automation
- **UITest.Core**: Platform abstraction and utilities

### 2. Platform Abstraction

The architecture provides consistent APIs across platforms while allowing platform-specific customization:

```csharp
// Platform-agnostic component testing
[Fact]
public async Task BrokerAccountTemplate_ProfitableInvestment_ShowsGreenIndicator()
{
    var template = new BrokerAccountTemplate();
    var account = InvestmentTestData.CreateProfitableAccount();
    
    await template.LoadAccountDataAsync(account);
    
    // Platform-specific assertions automatically applied
    template.AssertProfitColorIndicator(AppColors.Profit);
}
```

### 3. Realistic Test Data

Investment scenarios are based on real-world patterns:

```csharp
public static class InvestmentScenarios
{
    // Long-term growth investor
    public static BrokerAccount LongTermGrowth => new BrokerAccountBuilder()
        .WithTimeHorizon(years: 7)
        .WithAnnualReturn(8.5m)
        .WithDiversification(sectors: 8)
        .WithDividendFocus(yield: 2.1m)
        .Build();
        
    // Day trader high-frequency  
    public static BrokerAccount DayTrader => new BrokerAccountBuilder()
        .WithHighFrequency(transactionsPerDay: 25)
        .WithShortTermHolds(avgMinutes: 240)
        .WithHighVolatility(15.0m)
        .Build();
}
```

### 4. Memory and Performance Awareness

The architecture includes built-in performance and memory monitoring:

```csharp
[Fact]
public async Task ComponentTest_ObservableChain_NoMemoryLeaks()
{
    var subscription = viewModel.WhenAnyValue(x => x.Balance)
        .Subscribe(balance => UpdateUI(balance))
        .DisposeWith(disposables);
        
    // ... test logic ...
    
    subscription.AssertObservableMemoryLeak();
}
```

## Integration Points

### F# Core Integration

The testing infrastructure seamlessly integrates with F# business logic:

```csharp
// C# test calling F# domain logic
[Fact]
public async Task FinancialCalculations_ComplexScenario_F_SharpDomainIntegration()
{
    // Arrange - C# test data
    var portfolio = new BrokerAccountBuilder()
        .WithMultipleCurrencies()
        .Build();
    
    // Act - F# business logic
    var snapshot = await BrokerFinancialSnapshotManager.calculate(portfolio);
    
    // Assert - C# assertions
    snapshot.AssertFinancialAccuracy();
    snapshot.AssertCurrencyConsistency();
}
```

### Observable Chain Testing

Built-in support for testing ReactiveUI patterns:

```csharp
public static class ObservableTestExtensions
{
    public static void AssertObservableMemoryLeak<T>(this IObservable<T> observable)
    {
        // Memory leak detection implementation
        // Uses WeakReference to verify proper disposal
    }
    
    public static async Task AssertEventuallyEmits<T>(
        this IObservable<T> observable, 
        Func<T, bool> predicate, 
        TimeSpan timeout = default)
    {
        // Async assertion for eventual Observable emissions
    }
}
```

## Testing Strategies

### Component Testing Strategy

**Level 1: Individual Controls**
- Test single UI controls in isolation
- Validate formatting, styling, and basic interactions
- Use mocked dependencies for external services

**Level 2: Template and Composite Controls**
- Test complex controls like `BrokerAccountTemplate`
- Validate component interactions and data flow
- Test Observable chains and memory management

**Level 3: Page-Level Integration**
- Test complete pages with navigation
- Validate business logic integration
- Test real data binding scenarios

### End-to-End Testing Strategy

**Level 4: User Journey Testing**
- Complete user workflows (account creation, investment tracking)
- Cross-platform validation
- Performance testing with realistic data volumes

## Performance Considerations

### Mobile Device Constraints

The architecture is designed with mobile performance in mind:

- **Memory**: Tests include memory leak detection and GC monitoring
- **CPU**: Performance tests validate mobile device timing constraints  
- **Battery**: Tests avoid unnecessarily expensive operations
- **Network**: Mock external services for consistent test execution

### Scalability

The system scales from individual component tests to large integration test suites:

- **Parallel Execution**: Headless runner supports parallel test execution
- **Incremental Testing**: Smart test selection based on code changes
- **Resource Management**: Automatic cleanup of test resources and subscriptions

## Extension Points

### Custom Assertions

The architecture supports custom investment-specific assertions:

```csharp
public static class CustomInvestmentAssertions
{
    public static void AssertPortfolioBalance(
        this BrokerAccount account, 
        decimal expectedBalance, 
        decimal tolerance = 0.01m)
    {
        var actualBalance = account.CalculateCurrentBalance();
        var difference = Math.Abs(actualBalance - expectedBalance);
        
        Assert.True(difference <= tolerance,
            $"Portfolio balance {actualBalance} not within {tolerance} of expected {expectedBalance}");
    }
}
```

### Platform-Specific Extensions

Each platform can provide specialized testing capabilities:

```csharp
#if ANDROID
public static class AndroidInvestmentExtensions
{
    public static void AssertMaterialDesignCompliance(this View view)
    {
        // Android Material Design validation
    }
}
#endif
```

## Future Evolution

The architecture is designed to evolve with the investment app's needs:

### Planned Enhancements

1. **Screenshot Comparison Testing**: Visual regression testing for investment charts and graphs
2. **Accessibility Testing**: Comprehensive screen reader and accessibility validation
3. **Performance Profiling**: Detailed performance analysis for complex financial calculations
4. **Integration with External Services**: Testing with real market data APIs (in controlled environments)

### Extension Areas

- **Custom Test Runners**: Domain-specific test runners for particular investment scenarios
- **Advanced Reporting**: Investment-specific test reports with financial metrics
- **Continuous Performance Monitoring**: Integration with performance monitoring systems

This architecture provides a solid foundation for comprehensive testing of investment tracking applications while remaining flexible enough to evolve with changing requirements and new platform capabilities.