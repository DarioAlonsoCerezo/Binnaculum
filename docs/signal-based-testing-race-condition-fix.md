# Signal-Based Testing Race Condition Fix

## 🎯 Problem Identified

The test was **timing out waiting for signals** because of a **race condition**. Here's what was happening:

### Timeline of the Bug:

```
11:06:36.450 - [Sync Step] Call ExpectSignals() ← Too late!
11:06:36.451 - [Async Step] Start CreateBrokerAccount() 
11:06:36.588 - Signal: 'Accounts_Updated' arrives ← Already past ExpectSignals!
11:06:36.588 - Signal: 'Snapshots_Updated' arrives ← Already past ExpectSignals!
11:06:36.698 - CreateBrokerAccount() completes
11:06:36.699 - [Async Step] Wait for signals (with fresh ExpectSignals) ← But signals already gone!
11:06:46.707 - ❌ TIMEOUT! Received: [], Missing: [Accounts_Updated, Snapshots_Updated]
```

**The signals arrived BEFORE the test started waiting for them!**

### Root Cause:
1. Test calls `ExpectSignals()` in the wait step (too late)
2. Signals are emitted during the action step
3. By the time the wait step runs, signals have already passed through
4. TaskCompletionSource was never populated because signals arrived before it was created

---

## ✅ Solution Implemented

### 1. **Call ExpectSignals() BEFORE the Action**

Added a new sync step that calls `ExpectSignals()` **before** the action step:

```csharp
.AddSyncStep("Prepare to Expect Account Creation Signals", () =>
{
    ReactiveTestVerifications.ExpectSignals("Accounts_Updated", "Snapshots_Updated");
    return (true, "ExpectSignals called - ready to capture signals");
})
.AddAsyncStep("Create BrokerAccount [Signal-Based]", () => testActions.CreateBrokerAccountAsync(...))
.AddSignalWaitStepOnly("Wait for Account Creation Signals", TimeSpan.FromSeconds(10), "Accounts_Updated", "Snapshots_Updated")
```

### 2. **Created AddSignalWaitStepOnly() Method**

New method that **only waits** without resetting expectations:

```csharp
/// Waits for previously expected signals (doesn't call ExpectSignals again)
public TestScenarioBuilder AddSignalWaitStepOnly(string stepName, TimeSpan timeout, params string[] expectedSignals)
{
    AddAsyncStep(stepName, async () =>
    {
        // Don't call ExpectSignals - was already called in prep step
        var success = await ReactiveTestVerifications.WaitForAllSignalsAsync(timeout);
        // ... handle result
    });
    return this;
}
```

### 3. **Updated Test Orchestration**

All signal-based test steps now follow this pattern:

```
Prep Step (Sync):   ExpectSignals() called here
    ↓
Action Step (Async): Signals emitted, captured by TaskCompletionSource
    ↓
Wait Step (Async):   Just waits, doesn't reset
```

This ensures:
- ✅ TaskCompletionSource is created BEFORE signals arrive
- ✅ Signals are captured as they arrive
- ✅ Wait mechanism detects the completion immediately

---

## 📝 Changes Made

### File: TestScenarioBuilder.cs
- **Added**: `AddSignalWaitStepOnly()` method (doesn't call ExpectSignals again)
- **Kept**: `AddSignalWaitStep()` for backward compatibility

### File: BuiltInTestScenarios.cs
- **Modified**: BrokerAccount creation test
  - Added "Prepare to Expect Account Creation Signals" sync step
  - Changed to use `AddSignalWaitStepOnly()`

- **Modified**: All movement creation tests (4 movements)
  - Each now has a "Prepare to Expect [X] Movement Signals" sync step
  - Each changed to use `AddSignalWaitStepOnly()`

### File: ReactiveTestVerifications.cs (From Previous Session)
- Already had proper TaskCompletionSource + lock-based synchronization
- Already had race condition handling in `ExpectSignals()`
- Logging already added to trace all activities

---

## 🔄 New Execution Flow

### Before (Broken):
```
1. CreateBrokerAccount [start]
   → Signals emitted (Accounts_Updated, Snapshots_Updated)
2. CreateBrokerAccount [end]
3. Wait for Signals
   → ExpectSignals() called (creates new TaskCompletionSource)
   → Waits 10 seconds... timeout! (signals already gone)
```

### After (Fixed):
```
1. Prepare Signals
   → ExpectSignals() called (creates TaskCompletionSource)
2. CreateBrokerAccount [start]
   → Signals emitted (captured immediately)
3. CreateBrokerAccount [end]
4. Wait for Signals
   → Just waits (TaskCompletionSource already set!)
   → Returns immediately
```

---

## ✅ Build Status
- ✅ Android build successful
- ✅ No compilation errors
- ✅ Ready for testing

## 🧪 Next Steps

Run the test again. Expected results:
- ✅ Signals should be captured when emitted
- ✅ Wait steps should complete immediately (not timeout)
- ✅ Test should pass without the 10-second timeout

The new `[RxTest]` logging will show:
```
[RxTest] 🚀 Expecting signals: Accounts_Updated, Snapshots_Updated
[RxTest] 🔔 Collection change detected from 'Accounts': ...
[RxTest] 📨 Signal received: 'Accounts_Updated'
[RxTest] 📨 Signal received: 'Snapshots_Updated'
[RxTest] ✅ All expected signals received! Setting completion.
```
