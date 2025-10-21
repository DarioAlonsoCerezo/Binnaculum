# Reactive Integration Testing Pattern

## Overview

This directory contains a reusable pattern for implementing signal-based integration tests that mirror the MAUI tester's reactive validation approach. The pattern enables headless testing of reactive streams without platform dependencies.

## Architecture

```
Integration/
├── ReactiveTestEnvironment.fs        (Test environment configuration)
├── ReactiveTestContext.fs             (Test context wrapper)
├── ReactiveStreamObserver.fs           (Signal observation and waiting)
├── ReactiveTestActions.fs              (Common test actions)
├── ReactiveTestVerifications.fs        (Reusable verification helpers)
├── ReactiveTestSetup.fs                (Setup/teardown utilities)
├── ReactiveTestFixtureBase.fs          (Base class for test fixtures) ← NEW
└── ReactiveOverviewTests.fs            (Example test implementation)
```

## Quick Start: Creating a New Reactive Test

### Step 1: Inherit from Base Class

```fsharp
[<TestFixture>]
type MyReactiveTests() =
    inherit ReactiveTestFixtureBase()
    
    // Setup and teardown are inherited!
```

### Step 2: Write Test Methods

```fsharp
    [<Test>]
    [<Category("Integration")>]
    member this.``My reactive test``() =
        async {
            let actions = this.Actions  // Access Actions property
            let ctx = this.Context      // Access Context property
            
            // Your test logic here
        }
```

### Step 3: Use Verification Helpers

```fsharp
    // Use ReactiveTestVerifications module
    let (success, message) = ReactiveTestVerifications.verifyBrokers 2
    Assert.That(success, Is.True, message)
    
    // Use ReactiveTestSetup utilities
    ReactiveTestSetup.printPhaseHeader 1 "Database Initialization"
    let! signalsReceived = 
        ReactiveTestSetup.initializeDatabaseAndVerifySignals
            actions
            expectedSignals
            (TimeSpan.FromSeconds(10.0))
```

## Components

### ReactiveTestFixtureBase

**Purpose:** Base class for all reactive test fixtures  
**Provides:**
- Automatic `Setup()` / `Teardown()` via inheritance
- `this.Actions` - Access to ReactiveTestActions
- `this.Context` - Access to ReactiveTestContext
- InMemory database mode
- Reactive stream observation

### ReactiveTestSetup

**Purpose:** Reusable setup and database utilities  
**Functions:**
- `setupTestEnvironment()` - Configure test environment
- `teardownTestEnvironment()` - Cleanup
- `initializeDatabaseAndVerifySignals()` - DB init with signal verification
- `printPhaseHeader()` - Standardized phase reporting
- `printTestCompletionSummary()` - Standardized completion reporting

### ReactiveTestVerifications

**Purpose:** Reusable collection verification helpers  
**Functions:**
- `verifyBrokers(minCount)` - Verify broker count
- `verifyCurrencies(minCount)` - Verify currency count
- `verifyTickers(minCount)` - Verify ticker count
- `verifySnapshots(minCount)` - Verify snapshot count
- `verifyAccounts(minCount)` - Verify account count
- `verifyCurrencyExists(code)` - Check specific currency
- `verifyStandardCurrencies()` - Verify USD and EUR
- `verifyCollectionsState()` - Get formatted state summary
- `verifyFullDatabaseState()` - Run all standard verifications

### ReactiveStreamObserver

**Purpose:** Monitor reactive streams and emit signals  
**Key Types:**
- `ReactiveSignal` - Union type of stream signals
- `expectSignals()` - Set expected signals
- `waitForAllSignalsAsync()` - Wait for signals with timeout
- `getSignalStatus()` - Get signal verification status

## Pattern Example: ReactiveOverviewTests

```fsharp
[<TestFixture>]
type ReactiveOverviewTests() =
    inherit ReactiveTestFixtureBase()

    [<Test>]
    [<Category("Integration")>]
    member this.``Overview reactive validation``() =
        async {
            let actions = this.Actions

            // Phase 1: Initialize and verify signals
            ReactiveTestSetup.printPhaseHeader 1 "Database Initialization"
            let expectedSignals = [ Brokers_Updated; Currencies_Updated ]
            
            let! signalsReceived =
                ReactiveTestSetup.initializeDatabaseAndVerifySignals
                    actions
                    expectedSignals
                    (TimeSpan.FromSeconds(10.0))
            
            Assert.That(signalsReceived, Is.True)

            // Phase 2: Verify collections
            ReactiveTestSetup.printPhaseHeader 2 "Verify Collections"
            let verifications = ReactiveTestVerifications.verifyFullDatabaseState()
            
            for (success, message) in verifications do
                Assert.That(success, Is.True, message)
        }
```

## Testing Approach

### Signal-Based Testing

Instead of arbitrary delays:
```fsharp
// ❌ DON'T: Thread.Sleep(1000)

// ✅ DO: Wait for signals
ReactiveStreamObserver.expectSignals([ Brokers_Updated ])
let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync timeout
Assert.That(signalsReceived, Is.True)
```

### Verification Pattern

```fsharp
// ✅ Use built-in verifiers
let (success, message) = ReactiveTestVerifications.verifyBrokers 2
Assert.That(success, Is.True, message)

// ✅ Use batched verifications
let verifications = ReactiveTestVerifications.verifyFullDatabaseState()
for (success, message) in verifications do
    Assert.That(success, Is.True, message)
```

## Future Test Implementation

When implementing new reactive tests:

1. **Create new test class** inheriting from `ReactiveTestFixtureBase`
2. **Use Actions property** for test operations
3. **Use ReactiveTestSetup** for common operations
4. **Use ReactiveTestVerifications** for assertions
5. **Follow naming convention** of existing tests

Example:

```fsharp
[<TestFixture>]
type BrokerAccountReactiveTests() =
    inherit ReactiveTestFixtureBase()

    [<Test>]
    member this.``Create account fires signals``() =
        async {
            let actions = this.Actions
            ReactiveTestSetup.printPhaseHeader 1 "Create Account"
            
            ReactiveStreamObserver.expectSignals [ Accounts_Updated ]
            let! (ok, _, error) = actions.createBrokerAccount("Test")
            
            let! signalsReceived = ReactiveStreamObserver.waitForAllSignalsAsync timeout
            Assert.That(signalsReceived, Is.True)
        }
```

## Benefits

✅ **DRY** - No duplicate setup/teardown code  
✅ **Consistent** - All tests follow same pattern  
✅ **Reusable** - Utilities work across all tests  
✅ **Maintainable** - Change once, applies everywhere  
✅ **Scalable** - Easy to add new tests  

## Notes

- Tests run in **InMemory mode** to avoid platform dependencies
- All tests use **signal-based verification** instead of timing-based waits
- The pattern mirrors **Core.Platform.MauiTester** architecture
- Tests are **headless compatible** and run in CI/CD environments
