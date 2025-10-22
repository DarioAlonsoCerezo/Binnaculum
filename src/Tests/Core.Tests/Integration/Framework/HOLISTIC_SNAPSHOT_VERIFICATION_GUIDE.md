# Holistic Snapshot Verification Guide

## Overview

The holistic snapshot verification functions provide type-safe, compile-time-checked comparison of entire snapshot records at once, replacing fragmented field-by-field verification with pure, reusable functions.

## Key Benefits

### ✅ Compile-Time Safety
Adding new fields to snapshot models immediately breaks tests at compile time:
```fsharp
// If BrokerFinancialSnapshot gets a new field:
type BrokerFinancialSnapshot = {
    // ... existing fields ...
    NewField: decimal  // ← NEW FIELD
}

// Tests using holistic verification will fail to compile:
// Error FS0764: No assignment given for field 'NewField'
```

### ✅ Better Error Messages
See ALL mismatches at once with clear diff output:
```
❌ Snapshot mismatch:
  ✅ Deposited         : 5000.00 = 5000.00
  ✅ Withdrawn         : 0.00 = 0.00
  ❌ Realized          : -28.67 ≠ -30.00
  ✅ Unrealized        : 83.04 = 83.04
  ✅ MovementCounter   : 16 = 16
```

### ✅ Less Code
Replace 6+ async function calls with one pure function:

**Old approach:**
```fsharp
let! (ok1, _, _) = actions.verifyDeposited(5000m)
let! (ok2, _, _) = actions.verifyWithdrawn(0m)
let! (ok3, _, _) = actions.verifyOptionsIncome(54.37m)
let! (ok4, _, _) = actions.verifyRealizedGains(-28.67m)
let! (ok5, _, _) = actions.verifyUnrealizedGains(83.04m)
let! (ok6, _, _) = actions.verifyMovementCounter(16)
Assert.That(ok1 && ok2 && ok3 && ok4 && ok5 && ok6, Is.True)
```

**New approach:**
```fsharp
let expected: BrokerFinancialSnapshot = { (* all fields *) }
let actual = getActualSnapshot()
let (allMatch, results) = verifyBrokerFinancialSnapshot expected actual
Assert.That(allMatch, Is.True)
```

### ✅ Pure Functions
- No I/O operations
- No async overhead
- Deterministic behavior
- Easy to test
- Composable

## Available Functions

### 1. verifyBrokerFinancialSnapshot

Compares two `BrokerFinancialSnapshot` records field-by-field.

**Signature:**
```fsharp
val verifyBrokerFinancialSnapshot :
    expected: BrokerFinancialSnapshot ->
    actual: BrokerFinancialSnapshot ->
    (bool * ValidationResult list)
```

**Fields verified:**
- Id, Date
- MovementCounter
- RealizedGains, RealizedPercentage
- UnrealizedGains, UnrealizedGainsPercentage
- Invested, Commissions, Fees
- Deposited, Withdrawn
- DividendsReceived, OptionsIncome, OtherIncome
- OpenTrades
- NetCashFlow

**Example:**
```fsharp
let actual = BrokerAccounts.GetLatestSnapshot(accountId).Financial

let expected: BrokerFinancialSnapshot = {
    Id = actual.Id
    Date = actual.Date
    Broker = actual.Broker
    BrokerAccount = actual.BrokerAccount
    Currency = actual.Currency
    MovementCounter = 16
    RealizedGains = -28.67m
    RealizedPercentage = 0m
    UnrealizedGains = 83.04m
    UnrealizedGainsPercentage = 0m
    Invested = 0m
    Commissions = 7.00m
    Fees = 1.65m
    Deposited = 5000m
    Withdrawn = 0m
    DividendsReceived = 0m
    OptionsIncome = 54.37m
    OtherIncome = 0m
    OpenTrades = false
    NetCashFlow = 5000m
}

let (allMatch, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual

if not allMatch then
    let formatted = TestVerifications.formatValidationResults results
    printfn "Mismatch:\n%s" formatted

Assert.That(allMatch, Is.True)
```

### 2. verifyTickerCurrencySnapshot

Compares two `TickerCurrencySnapshot` records field-by-field.

**Signature:**
```fsharp
val verifyTickerCurrencySnapshot :
    expected: TickerCurrencySnapshot ->
    actual: TickerCurrencySnapshot ->
    (bool * ValidationResult list)
```

**Fields verified:**
- Id, Date
- TotalShares, Weight
- CostBasis, RealCost
- Dividends, Options, TotalIncomes
- Unrealized, Realized
- Performance, LatestPrice
- OpenTrades

**Example:**
```fsharp
let actual = Tickers.GetSnapshots(tickerId) |> Seq.head

let expected: TickerCurrencySnapshot = {
    Id = actual.Id
    Date = DateOnly(2023, 3, 31)
    Ticker = actual.Ticker
    Currency = actual.Currency
    TotalShares = 100m
    Weight = 0m
    CostBasis = 1000m
    RealCost = 1007m
    Dividends = 50m
    Options = 75m
    TotalIncomes = 125m
    Unrealized = 250.50m
    Realized = -50.25m
    Performance = 12.5m
    LatestPrice = 12.50m
    OpenTrades = false
}

let (allMatch, results) = TestVerifications.verifyTickerCurrencySnapshot expected actual
Assert.That(allMatch, Is.True)
```

### 3. formatValidationResults

Formats validation results as human-readable diff output.

**Signature:**
```fsharp
val formatValidationResults : results: ValidationResult list -> string
```

**Output format:**
```
  ✅ FieldName1           : expected = actual
  ❌ FieldName2           : expected ≠ actual
```

**Example:**
```fsharp
let (_, results) = verifyBrokerFinancialSnapshot expected actual
let formatted = formatValidationResults results
printfn "Comparison results:\n%s" formatted
```

## ValidationResult Type

```fsharp
type ValidationResult = {
    Field: string       // Field name (e.g., "Deposited")
    Expected: string    // Expected value formatted as string
    Actual: string      // Actual value formatted as string
    Match: bool        // True if values match
}
```

## Usage Patterns

### Pattern 1: Integration Test Verification

```fsharp
[<Test>]
member this.``Options import produces correct snapshot``() =
    async {
        // Setup and import
        let! (ok, _, _) = actions.initDatabase()
        let! (ok, _, _) = actions.createBrokerAccount("Test")
        let! (ok, _, _) = actions.importFile(brokerId, accountId, csvPath)
        
        // Get actual snapshot
        let actual = Collections.Snapshots.Items
                     |> Seq.filter (fun s -> s.Type = OverviewSnapshotType.BrokerAccount)
                     |> Seq.head
                     |> fun s -> s.BrokerAccount.Value.Financial
        
        // Define expected
        let expected: BrokerFinancialSnapshot = { (* ... *) }
        
        // Verify holistically
        let (allMatch, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual
        
        if not allMatch then
            let formatted = TestVerifications.formatValidationResults results
            printfn "❌ Mismatch:\n%s" formatted
        
        Assert.That(allMatch, Is.True)
    }
```

### Pattern 2: Unit Test for Pure Logic

```fsharp
[<Test>]
member _.``Calculation produces expected snapshot``() =
    // Arrange
    let movements = [ (* test movements *) ]
    
    // Act
    let actual = calculateSnapshot movements
    
    // Assert
    let expected: BrokerFinancialSnapshot = { (* expected result *) }
    let (allMatch, results) = TestVerifications.verifyBrokerFinancialSnapshot expected actual
    
    Assert.That(allMatch, Is.True, 
        sprintf "Snapshot should match:\n%s" 
            (TestVerifications.formatValidationResults results))
```

### Pattern 3: Historical Snapshot Validation

```fsharp
[<Test>]
member _.``Verify snapshot evolution over time``() =
    let snapshots = BrokerAccounts.GetSnapshots(accountId)
    
    let expectedDay1: BrokerFinancialSnapshot = { (* ... *) }
    let expectedDay2: BrokerFinancialSnapshot = { (* ... *) }
    
    let (match1, _) = verifyBrokerFinancialSnapshot expectedDay1 snapshots.[0]
    let (match2, _) = verifyBrokerFinancialSnapshot expectedDay2 snapshots.[1]
    
    Assert.That(match1 && match2, Is.True)
```

### Pattern 4: Partial Verification

When you only care about specific fields, copy actual values for other fields:

```fsharp
let expected: BrokerFinancialSnapshot = {
    // Fields we care about
    Deposited = 5000m
    Withdrawn = 0m
    OptionsIncome = 54.37m
    
    // Copy actual values for fields we don't care about
    Id = actual.Id
    Date = actual.Date
    RealizedGains = actual.RealizedGains
    // ... etc
}
```

## Migration from Old Approach

### Step 1: Identify Tests Using Individual Verifications

Find tests calling:
- `actions.verifyDeposited()`
- `actions.verifyWithdrawn()`
- `actions.verifyOptionsIncome()`
- `actions.verifyRealizedGains()`
- `actions.verifyUnrealizedGains()`
- `actions.verifyMovementCounter()`

### Step 2: Fetch Actual Snapshot

Replace individual queries with single snapshot fetch:

```fsharp
// Old: Multiple queries through actions
let! (ok1, _, _) = actions.verifyDeposited(5000m)

// New: Single snapshot fetch
let actual = getActualSnapshot()
```

### Step 3: Build Expected Snapshot

Create expected snapshot with all required fields:

```fsharp
let expected: BrokerFinancialSnapshot = {
    // All fields must be specified
    // Compiler enforces this!
    Id = actual.Id
    Date = actual.Date
    // ... etc
}
```

### Step 4: Compare Holistically

Replace multiple assertions with single comparison:

```fsharp
// Old: Multiple assertions
Assert.That(ok1 && ok2 && ok3 && ok4 && ok5 && ok6, Is.True)

// New: Single assertion with better error messages
let (allMatch, results) = verifyBrokerFinancialSnapshot expected actual
Assert.That(allMatch, Is.True)
```

## Testing the Verifiers Themselves

The verification functions are pure and easy to test:

```fsharp
[<Test>]
member _.``verifyBrokerFinancialSnapshot detects mismatch``() =
    let expected = { Deposited = 5000m; (* ... *) }
    let actual = { Deposited = 4999m; (* ... *) }
    
    let (allMatch, results) = verifyBrokerFinancialSnapshot expected actual
    
    Assert.That(allMatch, Is.False)
    let depositedResult = results |> List.find (fun r -> r.Field = "Deposited")
    Assert.That(depositedResult.Match, Is.False)
```

## Best Practices

### ✅ DO
- Use domain models directly for type safety
- Format validation results for debugging
- Test controls when data is fetched
- Specify all fields (compiler enforces this)
- Use for both unit and integration tests

### ❌ DON'T
- Don't add async to verifiers (keep them pure)
- Don't query database from verifiers
- Don't use magic values without comments
- Don't skip fields (compiler won't let you anyway)

## Performance Considerations

Pure functions are **fast**:
- No database queries
- No async overhead
- Simple field comparisons
- Instant results

Suitable for:
- Unit tests (pure logic)
- Integration tests (after data fetch)
- Performance benchmarks
- Regression tests

## Examples

See complete working examples in:
- `src/Tests/Core.Tests/Examples/HolisticSnapshotVerificationExample.fs`
- `src/Tests/Core.Tests/Unit/Verifications/SnapshotVerificationTests.fs`

## Summary

Holistic snapshot verification provides:
- **Type Safety**: Compile-time checks for all fields
- **Better Errors**: See all mismatches at once
- **Less Code**: One call instead of many
- **Pure Functions**: Testable, deterministic, fast
- **Maintainability**: Changes to models break tests at compile time

This approach makes tests more robust, maintainable, and easier to understand.
