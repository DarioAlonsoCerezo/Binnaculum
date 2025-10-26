# Implementation Plan for AutoImportOperation.Invested Property

## Overview

This document outlines the implementation plan for adding a new `Invested` property to `AutoImportOperation` and refactoring broker snapshot calculations to use operation-based invested amounts instead of cumulative snapshots.

## Problem Statement

The current cumulative approach to calculating `Invested` in broker snapshots has several issues:
- **Historical baggage**: Errors in previous snapshots perpetuate indefinitely
- **Multi-ticker interference**: Position changes in one ticker affect others
- **Incorrect sells handling**: Sell trades create negative investments instead of position closure
- **Zero position issue**: When no shares are held, `Invested` should be 0 but shows incorrect values

## Solution

Add `Invested` property to `AutoImportOperation` calculated as `TotalShares × CostBasis`, then sum all open operations for broker snapshot `Invested` calculation.

---

## Phase 1: Database Schema Changes

### 1.1 Database Model Updates
- **File**: `src/Core/Database/DatabaseModel.fs`
- **Action**: Add `Invested: Money` field to `AutoImportOperation` record
- **Location**: After existing fields like `CapitalDeployed`

### 1.2 Database Extensions
- **File**: `src/Core/Database/AutoImportOperationExtensions.fs`
- **Action**: Update SQL insert/update queries to include `Invested` field
- **Action**: Add parameter mapping for new field

### 1.3 SQL Schema
- **File**: `src/Core/SQL/` (relevant SQL files)
- **Action**: Add `Invested` column to AutoImportOperation table
- **Action**: Update create/insert statements

---

## Phase 2: Core Business Logic

### 2.1 Operation Manager Updates
- **File**: `src/Core/Snapshots/AutoImportOperationManager.fs`
- **Action**: Add `calculateInvestedAmount` function
- **Formula**: `SharesHeld × CostBasis` (for stock operations)
- **Action**: Update operation creation/update logic to populate `Invested`

### 2.2 Broker Financial Calculation
- **File**: `src/Core/Snapshots/BrokerFinancialsMetricsFromMovements.fs`
- **Action**: Replace `tradingSummary.TotalInvested` calculation
- **Action**: Add `calculateInvestedFromOperations` function
- **Logic**: Sum `Invested` from all open operations for the broker account

### 2.3 Snapshot Calculation Updates
- **File**: `src/Core/Snapshots/BrokerFinancialCalculateInMemory.fs`
- **Action**: Remove cumulative `Invested` calculation
- **Action**: Replace with direct operation-based calculation
- **Action**: Remove `cumulativeInvested` logic

---

## Phase 3: Model Updates

### 3.1 Domain Models
- **File**: `src/Core/Models/Models.fs` (if needed)
- **Action**: Ensure `Money` type compatibility

### 3.2 Database-to-Model Conversion
- **File**: `src/Core/Models/DatabaseToModels.fs`
- **Action**: Add `Invested` field mapping in conversion functions

---

## Phase 4: Test Data Updates

### 4.1 Expected Snapshots - TSLL
- **File**: `src/Tests/Core.Tests/Integration/Import/TsllImportExpectedSnapshots.fs`
- **Action**: Update all broker snapshots with correct `Invested = 0m` (since no stock positions)
- **Count**: ~72 broker snapshots

### 4.2 Expected Snapshots - Options
- **File**: `src/Tests/Core.Tests/Integration/Import/OptionsImportExpectedSnapshots.fs`
- **Action**: Update broker snapshots with correct `Invested` values
- **Logic**: Operations with stock positions vs options-only

### 4.3 Expected Snapshots - Pfizer
- **File**: `src/Tests/Core.Tests/Integration/Import/PfizerImportExpectedSnapshots.fs`
- **Action**: Update broker snapshots with stock-based `Invested` calculations

### 4.4 Expected Operations
- **All integration test files**
- **Action**: Add `Invested` field to all `AutoImportOperation` expected data
- **Calculation**: Per operation based on shares and cost basis

---

## Phase 5: Unit Tests

### 5.1 New Tests
- **File**: `src/Tests/Core.Tests/Unit/Managers/AutoImportOperationInvestedTests.fs` (new)
- **Tests**: Invested calculation logic, multi-ticker scenarios, position trimming

### 5.2 Update Existing Tests
- **Files**: All financial calculation tests
- **Action**: Update assertions to expect operation-based `Invested` values

---

## Phase 6: Migration & Validation

### 6.1 Data Migration
- **Consider**: Existing database data migration script
- **Action**: Calculate `Invested` for existing operations retroactively

### 6.2 Integration Testing
- **Action**: Run full test suite to ensure no regressions
- **Focus**: Multi-ticker scenarios, position management, snapshot consistency

---

## Phase 7: Documentation

### 7.1 Code Documentation
- **Action**: Update XML docs for new calculation method
- **Action**: Document the architectural change from cumulative to operation-based

---

## Implementation Details

### New Calculation Logic

#### AutoImportOperation.Invested
```fsharp
// For stock operations
Invested = SharesHeld × CostBasis

// For options-only operations  
Invested = 0m
```

#### Broker Snapshot.Invested
```fsharp
// Sum all open operations for the broker account
brokerSnapshot.Invested = Σ(operation.Invested where operation.IsOpen = true)
```

### Benefits

1. **Accuracy**: Direct calculation from current positions, not cumulative history
2. **Simplicity**: No complex cumulative logic or historical baggage
3. **Multi-ticker support**: Each operation contributes independently
4. **Auto-zero**: When all operations close, sum = 0 automatically
5. **Ticker isolation**: Operations don't interfere with each other

### Data Flow
```
Trade Activity → AutoImportOperation.Invested (shares × cost basis)
                ↓
All Operations → Broker Snapshot.Invested (sum of open operations)
```

---

## Risk Mitigation

1. **Backward Compatibility**: Ensure existing data continues to work
2. **Performance**: Operation queries should be efficient for snapshot calculation
3. **Data Consistency**: Validate that sum of operation.Invested matches expected portfolio values
4. **Test Coverage**: Comprehensive testing of edge cases (partial sales, multiple tickers)

---

## Validation Checkpoints

1. **TSLL Test**: All snapshots show `Invested = 0m` (options-only portfolio)
2. **Multi-ticker Test**: Each ticker's operations contribute correctly to total
3. **Partial Sale Test**: Selling 50% of position correctly updates `Invested`
4. **Zero Position Test**: Closing all positions results in `Invested = 0m`

---

## Expected Outcomes

- **TSLL Import**: `Invested = 0m` for all snapshots (no stock positions)
- **Options Import**: Correct stock vs options separation
- **Multi-ticker Portfolios**: Accurate per-ticker tracking
- **Position Management**: Proper handling of partial sales and closures

---

## Timeline Considerations

- **Database changes** require careful migration planning
- **Test data updates** are extensive but mechanical
- **Core logic changes** should be implemented incrementally
- **Full integration testing** is critical before deployment

This architectural change moves from a fragile cumulative model to a robust operation-based calculation, providing the foundation for accurate multi-ticker investment tracking.