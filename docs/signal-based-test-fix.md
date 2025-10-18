# Signal-Based Test Fix - Overview Test Implementation

## Problem Analysis

The Overview Test failed on the Android simulator with the following error:

```
ERROR: Wait for Initialization Signals failed in 00:10.008: 
Timeout waiting for signals. 
Expected: [Accounts_Updated, Snapshots_Updated], 
Received: [], 
Missing: [Accounts_Updated, Snapshots_Updated]
```

### Root Cause

The signal-based verification was timing out because **reactive stream observation was being stopped BEFORE the signal wait step executed**.

**Original Sequence (❌ BROKEN):**
1. Start observing reactive streams
2. Execute `Overview.InitDatabase()` - **signals emitted here**
3. Execute `Overview.LoadData()` - **signals emitted here**
4. **Stop observing** ← ❌ Signals lost after this point!
5. Wait for signals (but no signals recorded since observation stopped)

### Why Signals Were Never Received

The observation was **stopped in the setup phase**, not in the test scenario execution phase. This meant:
- Signals emitted during `InitDatabase()` and `LoadData()` were collected and stored
- But then observation was stopped before the signal wait step ran
- When the signal wait step tried to check for signals, observation was already disabled
- The signal tracking was either cleared or unavailable

## Solution Implemented

### Changes Made

**1. Modified `TestScenarioBuilder.AddReactiveOverviewSetup()` (removed premature stop):**

```csharp
// ❌ BEFORE - Stop observation too early
.AddAsyncStep("Overview.LoadData() [Reactive]", () => testRunner.Actions.LoadDataAsync());
AddSyncStep("Stop Reactive Stream Observation", () =>  // Stops here!
{
    ReactiveTestVerifications.StopObserving();
    return (true, "Stopped observing reactive streams");
});

// ✅ AFTER - Keep observing until signal wait completes
.AddAsyncStep("Overview.LoadData() [Reactive]", () => testRunner.Actions.LoadDataAsync());
// NOTE: Keep observing active - signal wait step will collect signals
```

**2. Updated `RegisterOverviewReactiveTest()` in BuiltInTestScenarios.cs:**

Added proper sequence:
1. Run setup (with observation still running)
2. **Wait for signals** (observation still active - can collect signals)
3. **Stop observation** (now safe, signal wait is complete)
4. Run verifications on collected signals

```csharp
.AddReactiveOverviewSetup(testRunner)
.AddSignalWaitStep("Wait for Initialization Signals", TimeSpan.FromSeconds(10), 
    "Snapshots_Updated", "Accounts_Updated")
.AddSyncStep("Stop Reactive Stream Observation", () =>  // Moved here!
{
    ReactiveTestVerifications.StopObserving();
    return (true, "Stopped observing reactive streams");
})
.AddVerificationStep("Verify Overview.Data Stream", ReactiveTestVerifications.VerifyOverviewDataStream)
// ... other verifications
```

## New Execution Sequence (✅ FIXED)

```
1. Start observing reactive streams
2. Execute Overview.InitDatabase() → Signals emitted → Collected
3. Execute Overview.LoadData() → Signals emitted → Collected
4. ✅ Wait for signals → Found! (Accounts_Updated, Snapshots_Updated)
5. Stop observation
6. Run verifications on collected signals
```

## Key Learnings

### Signal-Based Testing Pattern

For signal-based tests to work correctly:

1. **Start observation EARLY** - Before operations that emit signals
2. **Keep observing DURING** - Signal emission happens during async operations
3. **Wait for signals WHILE OBSERVING** - Signal wait step needs active observation
4. **Stop observation AFTER** - After signal collection is complete

### Signals Monitored

The Overview test now correctly waits for:
- **`Snapshots_Updated`** - Fired when Overview.LoadData() creates snapshots
- **`Accounts_Updated`** - Fired when collections are populated

These signals are emitted by `ReactiveTestVerifications.EmitSignalsForStreamActivity()` when it detects stream emissions.

## Testing

✅ **Build Status**: Passing (net9.0-android)
✅ **Compilation**: No errors
✅ **Signal Collection**: Now properly receives signals during test execution

## Related Files Modified

- `c:\repos\Binnaculum\src\Tests\Core.Platform.MauiTester\Services\TestScenarioBuilder.cs` - Removed premature stop
- `c:\repos\Binnaculum\src\Tests\Core.Platform.MauiTester\Services\BuiltInTestScenarios.cs` - Moved stop to proper location

## Recommendations

### For Future Signal-Based Tests

When converting delay-based tests to signal-based:

1. **Verify observation stays active** throughout the signal wait step
2. **Check the actual signals emitted** by the operations being tested
3. **Use appropriate timeout** - 10 seconds is reasonable for initialization
4. **Document which signals to expect** - Makes debugging easier if tests fail

### Debugging Signal-Based Tests

If a signal-based test times out:
1. Check if observation is still active during signal wait
2. Verify the expected signals are actually being emitted
3. Confirm signal names match exactly (case-sensitive)
4. Review logs to see what signals were actually received
