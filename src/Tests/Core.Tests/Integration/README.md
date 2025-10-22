# Integration Testing Pattern

## Overview

This directory contains a reusable pattern for implementing signal-based integration tests that mirror the MAUI tester's validation approach. The pattern enables headless testing of reactive streams without platform dependencies.

## Architecture

```
Integration/
├── TestEnvironment.fs        (Test environment configuration)
├── TestContext.fs             (Test context wrapper)
├── StreamObserver.fs           (Signal observation and waiting)
├── TestActions.fs              (Common test actions)
├── TestVerifications.fs        (Reusable verification helpers)
├── TestSetup.fs                (Setup/teardown utilities)
├── TestFixtureBase.fs          (Base class for test fixtures) ← NEW
└── OverviewTests.fs            (Example test implementation)
```

## Quick Start: Creating a New Integration Test

### Step 1: Inherit from Base Class

```fsharp
[<TestFixture>]
type MyTests() =
    inherit TestFixtureBase()
    
    // Setup and teardown are inherited!
```

### Step 2: Write Test Methods

```fsharp
    [<Test>]
    [<Category("Integration")>]
    member this.``My integration test``() =
        async {
            let actions = this.Actions  // Access Actions property
            let ctx = this.Context      // Access Context property
            
            // Your test logic here
        }
```

### Step 3: Use Verification Helpers

```fsharp
    // Use TestVerifications module
    let (success, message) = TestVerifications.verifyBrokers 2
    Assert.That(success, Is.True, message)
    
    // Use TestSetup utilities
    TestSetup.printPhaseHeader 1 "Database Initialization"
    let! signalsReceived = 
        TestSetup.initializeDatabaseAndVerifySignals
            actions
            expectedSignals
            (TimeSpan.FromSeconds(10.0))
```

## Components

### TestFixtureBase

**Purpose:** Base class for all reactive test fixtures  
**Provides:**
- Automatic `Setup()` / `Teardown()` via inheritance
- `this.Actions` - Access to TestActions
- `this.Context` - Access to TestContext
- InMemory database mode
- Reactive stream observation

### TestSetup

**Purpose:** Reusable setup and database utilities  
**Functions:**
- `setupTestEnvironment()` - Configure test environment
- `teardownTestEnvironment()` - Cleanup
- `initializeDatabaseAndVerifySignals()` - DB init with signal verification
- `printPhaseHeader()` - Standardized phase reporting
- `printTestCompletionSummary()` - Standardized completion reporting

### TestVerifications

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

### StreamObserver

**Purpose:** Monitor reactive streams and emit signals  
**Key Types:**
- `ReactiveSignal` - Union type of stream signals
- `expectSignals()` - Set expected signals
- `waitForAllSignalsAsync()` - Wait for signals with timeout
- `getSignalStatus()` - Get signal verification status

## Pattern Example: OverviewTests

```fsharp
[<TestFixture>]
type OverviewTests() =
    inherit TestFixtureBase()

    [<Test>]
    [<Category("Integration")>]
    member this.``Overview reactive validation``() =
        async {
            let actions = this.Actions

            // Phase 1: Initialize and verify signals
            TestSetup.printPhaseHeader 1 "Database Initialization"
            let expectedSignals = [ Brokers_Updated; Currencies_Updated ]
            
            let! signalsReceived =
                TestSetup.initializeDatabaseAndVerifySignals
                    actions
                    expectedSignals
                    (TimeSpan.FromSeconds(10.0))
            
            Assert.That(signalsReceived, Is.True)

            // Phase 2: Verify collections
            TestSetup.printPhaseHeader 2 "Verify Collections"
            let verifications = TestVerifications.verifyFullDatabaseState()
            
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
StreamObserver.expectSignals([ Brokers_Updated ])
let! signalsReceived = StreamObserver.waitForAllSignalsAsync timeout
Assert.That(signalsReceived, Is.True)
```

### Verification Pattern

```fsharp
// ✅ Use built-in verifiers
let (success, message) = TestVerifications.verifyBrokers 2
Assert.That(success, Is.True, message)

// ✅ Use batched verifications
let verifications = TestVerifications.verifyFullDatabaseState()
for (success, message) in verifications do
    Assert.That(success, Is.True, message)
```

## Future Test Implementation

When implementing new reactive tests:

1. **Create new test class** inheriting from `TestFixtureBase`
2. **Use Actions property** for test operations
3. **Use TestSetup** for common operations
4. **Use TestVerifications** for assertions
5. **Follow naming convention** of existing tests

Example:

```fsharp
[<TestFixture>]
type BrokerAccountReactiveTests() =
    inherit TestFixtureBase()

    [<Test>]
    member this.``Create account fires signals``() =
        async {
            let actions = this.Actions
            TestSetup.printPhaseHeader 1 "Create Account"
            
            StreamObserver.expectSignals [ Accounts_Updated ]
            let! (ok, _, error) = actions.createBrokerAccount("Test")
            
            let! signalsReceived = StreamObserver.waitForAllSignalsAsync timeout
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
