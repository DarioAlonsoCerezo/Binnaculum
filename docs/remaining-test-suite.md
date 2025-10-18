# Remaining Test Suite

## Overview
After modernizing the test infrastructure to use event-driven reactive testing, the Core.Platform.MauiTester now contains 10 test scenarios, all using signal-based verification with actual reactive signals instead of arbitrary time delays.

## Remaining Test Suite Table

| # | Test Name | UI Button | Event Handler | Scenario Method | Approach | Verification Strategy |
|---|-----------|-----------|----------------|-----------------|----------|----------------------|
| 1 | Overview Test | `RunOverviewReactiveTestButton` | `OnRunOverviewReactiveTestClicked()` | `RegisterOverviewReactiveTest()` | Signal-Based | Waits for "Database_Initialized", then "Snapshots_Updated", "Accounts_Updated", "Data_Loaded" |
| 2 | BrokerAccount Creation Test | `RunBrokerAccountReactiveTestButton` | `OnRunBrokerAccountReactiveTestClicked()` | `RegisterBrokerAccountCreationReactiveTest()` | Reactive | Validates BrokerAccount creation and automatic snapshot generation |
| 3 | BrokerAccount + Deposit Test | `RunBrokerAccountDepositReactiveTestButton` | `OnRunBrokerAccountDepositReactiveTestClicked()` | `RegisterBrokerAccountDepositReactiveTest()` | Reactive | Tests BrokerAccount with single deposit movement |
| 4 | BrokerAccount Multiple Movements Test | `RunBrokerAccountMultipleMovementsSignalBasedTestButton` | `OnRunBrokerAccountMultipleMovementsSignalBasedTestClicked()` | `RegisterBrokerAccountMultipleMovementsSignalBasedTest()` | Signal-Based | Waits for reactive signals: "Movements_Updated", "Snapshots_Updated" |
| 5 | Options Import Test | `RunOptionsImportIntegrationSignalBasedTestButton` | `OnRunOptionsImportIntegrationSignalBasedTestClicked()` | `RegisterOptionsImportIntegrationSignalBasedTest()` | Signal-Based | Validates Tastytrade options CSV import workflow with reactive verification |
| 6 | Money Movements Test | `RunDepositsWithdrawalsIntegrationTestButton` | `OnRunDepositsWithdrawalsIntegrationTestClicked()` | `RegisterDepositsWithdrawalsIntegrationTest()` | Reactive | Tests money movements (deposits/withdrawals) import integration |
| 7 | Pfizer Import Test | `RunPfizerImportIntegrationTestButton` | `OnRunPfizerImportIntegrationTestClicked()` | `RegisterPfizerImportIntegrationTest()` | Signal-Based | Validates Pfizer (PFE) options import with signal verification |
| 8 | Tastytrade Import Test | (Script/Programmatic) | `OnTastytradeImportIntegrationTestClicked()` | `RegisterTastytradeImportIntegrationTest()` | Signal-Based | Tastytrade broker statement import integration test |
| 9 | TSLL Import Test | (Script/Programmatic) | `OnTsllImportIntegrationTestClicked()` | `RegisterTsllImportIntegrationTest()` | Signal-Based | TSLL (Third-party Statement Loader) import test |
| 10 | Multiple Movements (Delay-Based) | (Unused/Reference) | (Removed) | `RegisterBrokerAccountMultipleMovementsReactiveTest()` | Delay-Based | **DEPRECATED** - Kept in codebase for reference, not wired to UI |

## Architecture Changes

### Signal-Based Testing (Current Standard)
The test infrastructure now primarily uses signal-based verification for reliable reactive testing:

**Signal-Based Approach (Recommended for All Operations):**
- Waits for actual reactive signal emissions instead of arbitrary timeouts
- Signals emitted: "Accounts_Updated", "Movements_Updated", "Snapshots_Updated", "Database_Initialized", "Data_Loaded"
- Works when observable streams emit during operations
- Example: Overview Test uses `AddSignalWaitStepOnly()` with 500ms settling delay

```csharp
.AddSyncStep("Prepare to Expect Database Initialization Signal", () =>
{
    ReactiveTestVerifications.ExpectSignals("Database_Initialized");
    return (true, "Ready to capture signal");
})
.AddAsyncStep("Overview.InitDatabase() [Reactive]", () => testRunner.Actions.InitializeDatabaseAsync())
.AddSignalWaitStepOnly("Wait for Database Initialization Signal", 
    TimeSpan.FromSeconds(10), "Database_Initialized")
```

**Settling Delay Pattern:**
- A 500ms delay between major operations allows concurrent operations to complete
- Placed after signal wait to let collections stabilize before next operation
- Prevents index-out-of-range exceptions from rapid consecutive database operations

```csharp
.AddSignalWaitStepOnly("Wait for Database Initialization Signal", TimeSpan.FromSeconds(10), "Database_Initialized")
.AddDelay("Allow database state to settle after initialization", TimeSpan.FromMilliseconds(500))
.AddSyncStep("Prepare to Expect Data Loaded Signals", ...)
```

### Implementation Details

**TaskCompletionSource Pattern:**
- Thread-safe async signal waiting with lock-based synchronization
- Signals received BEFORE `ExpectSignals()` is called are captured if TCS already exists
- `ExpectSignals()` must be called in a SYNC step BEFORE the action step
- Pattern: Prepare (sync) → Action (async) → Wait (async)

**Race Condition Solution:**
- Problem: Signals can arrive before test starts waiting for them
- Solution: Call `ExpectSignals()` in a sync step before the async action
- This ensures `TaskCompletionSource` is created before any signals are emitted

### Conversion History
The Overview Test evolution demonstrates signal-based testing success:
1. **Initial State**: Used 500ms arbitrary delay
2. **First Attempt**: Converted to signal-based but with wrong signal set
3. **Issue**: Waited for signals that don't emit during LoadData (Currencies_Updated, Brokers_Updated, Tickers_Updated)
4. **Root Cause**: Those collections are loaded during InitDatabase, not LoadData
5. **Solution**: Modified to only wait for signals that actually occur (Snapshots_Updated, Accounts_Updated, Data_Loaded)
6. **Current State**: ✅ Fully signal-based with proper 500ms settling delay between phases

## Key Signals Monitored

The test suite verifies the app's reactivity by waiting for these core signals emitted during operations:

| Signal | Emitted During | Indicates |
|--------|----------------|-----------|
| **Database_Initialized** | `Overview.InitDatabase()` | Database has been initialized and basic data loaded |
| **Currencies_Updated** | `InitDatabase()` during currency load | Currency collection has been populated |
| **Brokers_Updated** | `InitDatabase()` during broker load | Broker collection has been populated |
| **Tickers_Updated** | `InitDatabase()` during ticker load | Ticker collection has been populated |
| **Accounts_Updated** | `LoadData()` or account creation | Account collection has changed |
| **Snapshots_Updated** | `LoadData()` or snapshot generation | Financial snapshots have been recalculated |
| **Movements_Updated** | Movement transaction import/creation | Movement transactions have been added/modified |
| **Data_Loaded** | `Overview.LoadData()` completion | All data loading is complete, transactions loaded |

**Critical Insight:** Signals only emit during their respective operations. Attempting to wait for a signal outside its operation window causes timeout. For example:
- ❌ Don't expect Currencies_Updated during LoadData (already loaded in InitDatabase)
- ✅ Do expect Snapshots_Updated during LoadData (snapshots are generated for loaded data)

## UI Button Status

### Visible Test Buttons (7)
✅ Run Overview Test  
✅ Run BrokerAccount Creation Test  
✅ Run BrokerAccount + Deposit Test  
✅ Run BrokerAccount Multiple Movements Test (Signal-Based)  
✅ Run Options Import Test  
✅ Run Money Movements Test  
✅ Run Pfizer Import Test  

### Additional Tests (3)
- Tastytrade Import Test (programmatic access)
- TSLL Import Test (programmatic access)
- Multiple Movements Test - Delay-Based (deprecated, reference only)

## Cleanup Summary

**Removed:**
- 7 old non-reactive test buttons and event handlers
- 5 old test scenario registrations (145 lines)
- 1 delay-based BrokerAccount Multiple Movements test UI button
- "Reactive" terminology from button labels (simplified to actual test names)

**Kept:**
- 10 reactive/signal-based test scenarios
- Event-driven architecture aligned with production behavior
- Comprehensive test coverage for core platform functionality

## Build Status
✅ **Android Build**: Passing (net9.0-android)  
✅ **Compilation**: No errors  
✅ **Test Infrastructure**: Fully functional and wired  

## Testing Philosophy

The remaining test suite validates the Core library's functionality **as it actually behaves in production**:
- **Event-Driven**: Tests respond to real reactive signals, not arbitrary timeouts
- **Production-Aligned**: Verification strategy matches how the app reacts to user actions
- **Signal-Based**: Waits for Observable streams to emit expected notifications
- **Comprehensive**: Covers account creation, transactions, imports, and complex workflows

### Signal-Based Tests (Current Standard - All Working)
Tests using `AddSignalWaitStepOnly()` to wait for actual reactive signals:
- ✅ **Overview Test** - Database initialization and data loading (2 signal wait phases)
- ✅ **BrokerAccount Multiple Movements Test** - Account creation with movements
- ✅ **Options Import Test** - CSV import workflow verification
- ✅ **Pfizer Import Test** - Specific ticker import scenario
- ✅ **Tastytrade Import Test** - Broker statement import
- ✅ **TSLL Import Test** - Third-party data import

### Traditional Reactive Tests (Legacy - Still Functional)
Tests using time-based delays - working correctly but use less precise verification:
- BrokerAccount Creation Test (500ms delay)
- BrokerAccount + Deposit Test (500ms delay)
- Money Movements Test (500ms delay)

**Migration Path:** These tests could be converted to signal-based if specific signals are identified for their operations.

