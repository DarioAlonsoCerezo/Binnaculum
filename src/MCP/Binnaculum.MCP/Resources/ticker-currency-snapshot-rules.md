# TickerCurrencySnapshot Calculation Rules

## Overview

**Entity:** `TickerCurrencySnapshot`
**Database Table:** `TickerCurrencySnapshots`
**Source Model:** `src/Core/Database/SnapshotsModel.fs`
**Purpose:** Stores currency-specific financial metrics for a ticker at a specific point in time.

**Relationship:** One `TickerSnapshot` can have multiple `TickerCurrencySnapshot` records (one per currency).
This enables multi-currency support when the same ticker is traded in different currencies.

---

## Stored Fields (Source of Truth)

These fields are **persisted in the database** and represent the core factual data:

| Field | Type | Description | Constraints |
|-------|------|-------------|-------------|
| `TickerId` | `int` | Foreign key to Tickers table | Must exist in Tickers |
| `CurrencyId` | `int` | Foreign key to Currencies table | Must exist in Currencies |
| `TickerSnapshotId` | `int` | Foreign key to TickerSnapshots table | Must exist in TickerSnapshots |
| `TotalShares` | `decimal` | Total shares held at snapshot date | >= 0 |
| `Weight` | `decimal` | Percentage weight in portfolio | 0.0 to 100.0 |
| `CostBasis` | `Money` | Original cost of shares purchased | >= 0 |
| `RealCost` | `Money` | Adjusted cost basis after corporate actions | >= 0 |
| `Dividends` | `Money` | Cumulative dividends received | >= 0 |
| `Options` | `Money` | Cumulative options premiums received | Can be negative |
| `TotalIncomes` | `Money` | Total income (dividends + options + other) | >= 0 |
| `Realized` | `Money` | Cumulative realized gains/losses | Can be negative |
| `LatestPrice` | `Money` | Market price at snapshot date | >= 0 |
| `OpenTrades` | `bool` | Whether there are open positions | true/false |

---

## Calculated Fields (Derived Values)

These fields are **NOT stored** in the database. They are calculated on-the-fly from stored fields:

### 1. Unrealized Gains/Losses

**Description:** Current profit or loss on open positions that have not been sold.

**Formula:**
```
Unrealized = (LatestPrice × TotalShares) - CostBasis
```

**Implementation Location:** `src/Core/Snapshots/SnapshotCalculations.fs` (to be created)

**Constraints:**
- `TotalShares >= 0`
- `LatestPrice >= 0`
- `CostBasis >= 0`

**Edge Cases:**
- **No Position (`TotalShares = 0`)**: Return `0`
- **After Stock Split**: Cost basis adjusts proportionally (e.g., 2:1 split → cost basis halves per share)
- **Partial Close**: Only unrealized portion is calculated (remaining shares × price - remaining cost basis)

**Example Calculations:**
```
Scenario 1: Profitable Position
  TotalShares = 100
  LatestPrice = 150.00
  CostBasis = 10000.00
  Unrealized = (150 × 100) - 10000 = 15000 - 10000 = 5000.00

Scenario 2: Loss Position
  TotalShares = 50
  LatestPrice = 80.00
  CostBasis = 5000.00
  Unrealized = (80 × 50) - 5000 = 4000 - 5000 = -1000.00

Scenario 3: No Position
  TotalShares = 0
  Unrealized = 0 (regardless of price)
```

**F# Implementation:**
```fsharp
let calculateUnrealized (totalShares: decimal) (latestPrice: decimal) (costBasis: decimal) =
    if totalShares = 0m then 
        0m  // No position
    else
        (latestPrice * totalShares) - costBasis
```

---

### 2. Performance Percentage

**Description:** Total return percentage including both realized and unrealized gains.

**Formula:**
```
Performance = ((Unrealized + Realized) / CostBasis) × 100
```

**Implementation Location:** `src/Core/Snapshots/SnapshotCalculations.fs` (to be created)

**Dependencies:**
- Requires `Unrealized` calculation (see above)
- Uses stored `Realized` field
- Uses stored `CostBasis` field

**Constraints:**
- `CostBasis > 0` for meaningful calculation
- Result is a percentage (can be positive or negative)

**Edge Cases:**
- **Zero Cost Basis (`CostBasis = 0`)**: Return `0` to avoid division by zero
- **Fully Closed Position (`TotalShares = 0`)**: Use only `Realized` for performance
- **Negative Cost Basis (Should Never Happen)**: Validate and throw error

**Example Calculations:**
```
Scenario 1: Profitable Open Position
  Unrealized = 5000.00
  Realized = 2000.00
  CostBasis = 10000.00
  Performance = ((5000 + 2000) / 10000) × 100 = 70.00%

Scenario 2: Loss Position
  Unrealized = -1000.00
  Realized = -500.00
  CostBasis = 5000.00
  Performance = ((-1000 + -500) / 5000) × 100 = -30.00%

Scenario 3: Fully Closed Position
  Unrealized = 0 (TotalShares = 0)
  Realized = 3000.00
  CostBasis = 10000.00
  Performance = ((0 + 3000) / 10000) × 100 = 30.00%

Scenario 4: Zero Cost Basis (Edge Case)
  CostBasis = 0
  Performance = 0 (avoid division by zero)
```

**F# Implementation:**
```fsharp
let calculatePerformance (unrealized: decimal) (realized: decimal) (costBasis: decimal) =
    if costBasis = 0m then 
        0m  // Avoid division by zero
    else
        ((unrealized + realized) / costBasis) * 100m

// Convenience function for snapshots
let calculateSnapshotPerformance (snapshot: TickerCurrencySnapshot) =
    let unrealized = 
        calculateUnrealized 
            snapshot.TotalShares 
            snapshot.LatestPrice.Value 
            snapshot.CostBasis.Value
    calculatePerformance unrealized snapshot.Realized.Value snapshot.CostBasis.Value
```

---

## Cumulative Field Calculations

These fields accumulate over time through trade processing:

### 3. Total Income

**Description:** Sum of all income sources for this ticker/currency combination.

**Formula:**
```
TotalIncomes = Dividends + Options + OtherIncome
```

**Note:** This is currently stored, but could be calculated from component fields.

### 4. Realized Gains

**Description:** Cumulative profit/loss from closed positions.

**Calculation on Trade Close:**
```
RealizedGain = SaleProceeds - OriginalCostBasis - BuyCommission - SellCommission
```

**Important:**
- Uses **FIFO (First In, First Out)** lot matching
- Adjusts for stock splits proportionally
- Includes commissions from both buy and sell sides

**Cumulative Calculation with Previous Snapshots:**

When calculating a new snapshot with a previous snapshot available, realized gains must be accumulated without double-counting:

```
Algorithm:
1. Calculate total realized from ALL closed trades up to the current snapshot date
2. Calculate total realized from ALL closed trades up to (and including) the previous snapshot date
3. New realized gains = Step 1 - Step 2 (trades that closed after previous snapshot)
4. Cumulative realized = PreviousSnapshot.Realized + NewRealizedGains
```

**Critical:** When filtering trades for the previous snapshot, include trades closing ON the previous snapshot date (use `<=` not `<`). Otherwise, trades on the previous snapshot date will be double-counted when added to the cumulative value.

**Example:**

```
Scenario: Calculating snapshot for 10/15/2024 with previous snapshot at 6/7/2024

Trade History:
- 6/7/2024: Put expiration closed = $13.86 realized
- 10/15/2024 to 10/21/2024: Other trades (not yet closed at 10/15)

Previous Snapshot (6/7/2024):
  Realized = $13.86

New Snapshot Calculation (10/15/2024):
  tradesUpToSnapshot = All closed trades up to 10/15 = [6/7 expiration] = $13.86
  tradesUpToPreviousDate = All closed trades up to 6/7 (INCLUSIVE) = [6/7 expiration] = $13.86
  newRealizedGains = $13.86 - $13.86 = $0.00
  Realized = $13.86 + $0.00 = $13.86 ✓
```

**Single Trade Example:**
```
Buy:  100 shares @ $50 + $10 commission = $5,010 cost
Sell: 100 shares @ $75 + $10 commission = $7,490 proceeds
Realized = $7,490 - $5,010 = $2,480
```

### 5. Cost Basis Adjustments

**Original Cost Basis (`CostBasis`):**
```
CostBasis = Sum of (SharesPurchased × PurchasePrice + Commission)
```

**Real Cost Basis (`RealCost`):**
```
RealCost = CostBasis adjusted for:
  - Stock splits (proportional adjustment)
  - Stock dividends
  - Return of capital
  - Spin-offs
```

**Stock Split Example:**
```
Before 2:1 Split:
  100 shares @ $100/share → CostBasis = $10,000

After 2:1 Split:
  200 shares @ $50/share → RealCost = $10,000 (unchanged)
  Cost per share = $10,000 / 200 = $50
```

### 6. Portfolio Weight

**Description:** Percentage of this position relative to total portfolio value.

**Formula:**
```
Weight = (PositionValue / TotalPortfolioValue) × 100

Where:
  PositionValue = LatestPrice × TotalShares
```

**Note:** Requires aggregation across all tickers to calculate total portfolio value.

---

## Validation Rules

### Data Integrity Constraints

1. **Non-Negative Shares**: `TotalShares >= 0`
   - Negative shares are invalid (use separate short position tracking if needed)

2. **Non-Negative Price**: `LatestPrice >= 0`
   - Zero price allowed for delisted/worthless securities

3. **Non-Negative Cost Basis**: `CostBasis >= 0`
   - Zero allowed for gifted shares or inherited positions

4. **Weight Range**: `0.0 <= Weight <= 100.0`
   - Cannot exceed 100% of portfolio (leveraged positions handled separately)

5. **Foreign Key Integrity**:
   - `TickerId` must exist in `Tickers` table
   - `CurrencyId` must exist in `Currencies` table
   - `TickerSnapshotId` must exist in `TickerSnapshots` table

6. **Open Trades Consistency**:
   - **For Share Positions:**
     - If `TotalShares = 0`, shares do NOT contribute to OpenTrades
     - If `TotalShares > 0`, then `OpenTrades` should be `true`
   - **For Option Positions:**
     - OpenTrades depends on net position (netPosition) for each option (strike/expiration)
     - netPosition = sum of: BuyToOpen(+1) + SellToOpen(-1) + BuyToClose(-1) + SellToClose(+1)
     - If netPosition = 0 for ALL options, then options do NOT contribute to OpenTrades
     - If netPosition ≠ 0 for ANY option group, then `OpenTrades` should be `true`
   - **Overall Rule:**
     - `OpenTrades = true` if (TotalShares > 0) OR (any option has netPosition ≠ 0)
     - `OpenTrades = false` if (TotalShares = 0) AND (all options have netPosition = 0)

---

## Database Schema Reference

**Table Name:** `TickerCurrencySnapshots`

**Primary Key:** `Id` (auto-increment)

**Foreign Keys:**
- `TickerId` → `Tickers.Id` (CASCADE DELETE, CASCADE UPDATE)
- `CurrencyId` → `Currencies.Id` (CASCADE DELETE, CASCADE UPDATE)
- `TickerSnapshotId` → `TickerSnapshots.Id` (CASCADE DELETE, CASCADE UPDATE)

**Indexes:**
- `idx_TickerCurrencySnapshots_TickerId` (for ticker-based queries)
- `idx_TickerCurrencySnapshots_CurrencyId` (for currency filtering)
- `idx_TickerCurrencySnapshots_TickerSnapshotId` (for parent relationship)
- `idx_TickerCurrencySnapshots_Date` (for date-range queries)
- `idx_TickerCurrencySnapshots_TickerId_Date` (composite for time-series analysis)

---

## Implementation Notes

### Where to Add Calculation Functions

Create a new file: `src/Core/Snapshots/SnapshotCalculations.fs`

```fsharp
module SnapshotCalculations =

    /// Calculate unrealized gains/losses for a position
    let calculateUnrealized (totalShares: decimal) (latestPrice: decimal) (costBasis: decimal) =
        if totalShares = 0m then 0m
        else (latestPrice * totalShares) - costBasis

    /// Calculate total performance percentage
    let calculatePerformance (unrealized: decimal) (realized: decimal) (costBasis: decimal) =
        if costBasis = 0m then 0m
        else ((unrealized + realized) / costBasis) * 100m

    /// Calculate performance for a snapshot
    let calculateSnapshotPerformance (snapshot: TickerCurrencySnapshot) =
        let unrealized = 
            calculateUnrealized 
                snapshot.TotalShares 
                snapshot.LatestPrice.Value 
                snapshot.CostBasis.Value
        calculatePerformance unrealized snapshot.Realized.Value snapshot.CostBasis.Value
```

### Converting Database Model to UI Model

In `src/Core/Models/DatabaseToModels.fs`, add calculations during conversion:

```fsharp
let tickerCurrencySnapshotToModel (dbSnapshot: TickerCurrencySnapshot) =
    let unrealized = 
        SnapshotCalculations.calculateUnrealized 
            dbSnapshot.TotalShares 
            dbSnapshot.LatestPrice.Value 
            dbSnapshot.CostBasis.Value
    
    let performance = 
        SnapshotCalculations.calculatePerformance 
            unrealized 
            dbSnapshot.Realized.Value 
            dbSnapshot.CostBasis.Value
    
    { Id = dbSnapshot.Base.Id
      // ... other fields ...
      Unrealized = unrealized  // Calculated, not stored
      Performance = performance  // Calculated, not stored
      // ... }
```

---

## Related Documentation

- **Source Code**: `src/Core/Database/SnapshotsModel.fs` (lines 54-76)
- **SQL Queries**: `src/Core/SQL/TickerCurrencySnapshotQuery.fs`
- **Database Extensions**: `src/Core/Database/TickerCurrencySnapshotExtensions.fs`
- **Batch Calculator**: `src/Core/Snapshots/TickerSnapshotBatchCalculator.fs`
- **In-Memory Calculation**: `src/Core/Snapshots/TickerSnapshotCalculateInMemory.fs`
