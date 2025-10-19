

using System.ComponentModel;
using System.Text;
using ModelContextProtocol;

[McpServerToolType]
public static class BusinessRulesManager
{
    #region TickerCurrencySnapshot Rules

    [McpServerTool]
    [Description("Get comprehensive calculation rules for TickerCurrencySnapshot entity. Includes all formulas, constraints, edge cases, and implementation guidance.")]
    public static async Task<string> GetTickerCurrencySnapshotRules()
    {
        var rules = new StringBuilder();

        rules.AppendLine("# TickerCurrencySnapshot Calculation Rules");
        rules.AppendLine();
        rules.AppendLine("## Overview");
        rules.AppendLine();
        rules.AppendLine("**Entity:** `TickerCurrencySnapshot`");
        rules.AppendLine("**Database Table:** `TickerCurrencySnapshots`");
        rules.AppendLine("**Source Model:** `src/Core/Database/SnapshotsModel.fs`");
        rules.AppendLine("**Purpose:** Stores currency-specific financial metrics for a ticker at a specific point in time.");
        rules.AppendLine();
        rules.AppendLine("**Relationship:** One `TickerSnapshot` can have multiple `TickerCurrencySnapshot` records (one per currency).");
        rules.AppendLine("This enables multi-currency support when the same ticker is traded in different currencies.");
        rules.AppendLine();
        rules.AppendLine("---");
        rules.AppendLine();

        // Stored Fields
        rules.AppendLine("## Stored Fields (Source of Truth)");
        rules.AppendLine();
        rules.AppendLine("These fields are **persisted in the database** and represent the core factual data:");
        rules.AppendLine();
        rules.AppendLine("| Field | Type | Description | Constraints |");
        rules.AppendLine("|-------|------|-------------|-------------|");
        rules.AppendLine("| `TickerId` | `int` | Foreign key to Tickers table | Must exist in Tickers |");
        rules.AppendLine("| `CurrencyId` | `int` | Foreign key to Currencies table | Must exist in Currencies |");
        rules.AppendLine("| `TickerSnapshotId` | `int` | Foreign key to TickerSnapshots table | Must exist in TickerSnapshots |");
        rules.AppendLine("| `TotalShares` | `decimal` | Total shares held at snapshot date | >= 0 |");
        rules.AppendLine("| `Weight` | `decimal` | Percentage weight in portfolio | 0.0 to 100.0 |");
        rules.AppendLine("| `CostBasis` | `Money` | Original cost of shares purchased | >= 0 |");
        rules.AppendLine("| `RealCost` | `Money` | Adjusted cost basis after corporate actions | >= 0 |");
        rules.AppendLine("| `Dividends` | `Money` | Cumulative dividends received | >= 0 |");
        rules.AppendLine("| `Options` | `Money` | Cumulative options premiums received | Can be negative |");
        rules.AppendLine("| `TotalIncomes` | `Money` | Total income (dividends + options + other) | >= 0 |");
        rules.AppendLine("| `Realized` | `Money` | Cumulative realized gains/losses | Can be negative |");
        rules.AppendLine("| `LatestPrice` | `Money` | Market price at snapshot date | >= 0 |");
        rules.AppendLine("| `OpenTrades` | `bool` | Whether there are open positions | true/false |");
        rules.AppendLine();
        rules.AppendLine("---");
        rules.AppendLine();

        // Calculated Fields
        rules.AppendLine("## Calculated Fields (Derived Values)");
        rules.AppendLine();
        rules.AppendLine("These fields are **NOT stored** in the database. They are calculated on-the-fly from stored fields:");
        rules.AppendLine();
        rules.AppendLine("### 1. Unrealized Gains/Losses");
        rules.AppendLine();
        rules.AppendLine("**Description:** Current profit or loss on open positions that have not been sold.");
        rules.AppendLine();
        rules.AppendLine("**Formula:**");
        rules.AppendLine("```");
        rules.AppendLine("Unrealized = (LatestPrice × TotalShares) - CostBasis");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**Implementation Location:** `src/Core/Snapshots/SnapshotCalculations.fs` (to be created)");
        rules.AppendLine();
        rules.AppendLine("**Constraints:**");
        rules.AppendLine("- `TotalShares >= 0`");
        rules.AppendLine("- `LatestPrice >= 0`");
        rules.AppendLine("- `CostBasis >= 0`");
        rules.AppendLine();
        rules.AppendLine("**Edge Cases:**");
        rules.AppendLine("- **No Position (`TotalShares = 0`)**: Return `0`");
        rules.AppendLine("- **After Stock Split**: Cost basis adjusts proportionally (e.g., 2:1 split → cost basis halves per share)");
        rules.AppendLine("- **Partial Close**: Only unrealized portion is calculated (remaining shares × price - remaining cost basis)");
        rules.AppendLine();
        rules.AppendLine("**Example Calculations:**");
        rules.AppendLine("```");
        rules.AppendLine("Scenario 1: Profitable Position");
        rules.AppendLine("  TotalShares = 100");
        rules.AppendLine("  LatestPrice = 150.00");
        rules.AppendLine("  CostBasis = 10000.00");
        rules.AppendLine("  Unrealized = (150 × 100) - 10000 = 15000 - 10000 = 5000.00");
        rules.AppendLine();
        rules.AppendLine("Scenario 2: Loss Position");
        rules.AppendLine("  TotalShares = 50");
        rules.AppendLine("  LatestPrice = 80.00");
        rules.AppendLine("  CostBasis = 5000.00");
        rules.AppendLine("  Unrealized = (80 × 50) - 5000 = 4000 - 5000 = -1000.00");
        rules.AppendLine();
        rules.AppendLine("Scenario 3: No Position");
        rules.AppendLine("  TotalShares = 0");
        rules.AppendLine("  Unrealized = 0 (regardless of price)");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**F# Implementation:**");
        rules.AppendLine("```fsharp");
        rules.AppendLine("let calculateUnrealized (totalShares: decimal) (latestPrice: decimal) (costBasis: decimal) =");
        rules.AppendLine("    if totalShares = 0m then ");
        rules.AppendLine("        0m  // No position");
        rules.AppendLine("    else");
        rules.AppendLine("        (latestPrice * totalShares) - costBasis");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("---");
        rules.AppendLine();

        // Performance Percentage
        rules.AppendLine("### 2. Performance Percentage");
        rules.AppendLine();
        rules.AppendLine("**Description:** Total return percentage including both realized and unrealized gains.");
        rules.AppendLine();
        rules.AppendLine("**Formula:**");
        rules.AppendLine("```");
        rules.AppendLine("Performance = ((Unrealized + Realized) / CostBasis) × 100");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**Implementation Location:** `src/Core/Snapshots/SnapshotCalculations.fs` (to be created)");
        rules.AppendLine();
        rules.AppendLine("**Dependencies:**");
        rules.AppendLine("- Requires `Unrealized` calculation (see above)");
        rules.AppendLine("- Uses stored `Realized` field");
        rules.AppendLine("- Uses stored `CostBasis` field");
        rules.AppendLine();
        rules.AppendLine("**Constraints:**");
        rules.AppendLine("- `CostBasis > 0` for meaningful calculation");
        rules.AppendLine("- Result is a percentage (can be positive or negative)");
        rules.AppendLine();
        rules.AppendLine("**Edge Cases:**");
        rules.AppendLine("- **Zero Cost Basis (`CostBasis = 0`)**: Return `0` to avoid division by zero");
        rules.AppendLine("- **Fully Closed Position (`TotalShares = 0`)**: Use only `Realized` for performance");
        rules.AppendLine("- **Negative Cost Basis (Should Never Happen)**: Validate and throw error");
        rules.AppendLine();
        rules.AppendLine("**Example Calculations:**");
        rules.AppendLine("```");
        rules.AppendLine("Scenario 1: Profitable Open Position");
        rules.AppendLine("  Unrealized = 5000.00");
        rules.AppendLine("  Realized = 2000.00");
        rules.AppendLine("  CostBasis = 10000.00");
        rules.AppendLine("  Performance = ((5000 + 2000) / 10000) × 100 = 70.00%");
        rules.AppendLine();
        rules.AppendLine("Scenario 2: Loss Position");
        rules.AppendLine("  Unrealized = -1000.00");
        rules.AppendLine("  Realized = -500.00");
        rules.AppendLine("  CostBasis = 5000.00");
        rules.AppendLine("  Performance = ((-1000 + -500) / 5000) × 100 = -30.00%");
        rules.AppendLine();
        rules.AppendLine("Scenario 3: Fully Closed Position");
        rules.AppendLine("  Unrealized = 0 (TotalShares = 0)");
        rules.AppendLine("  Realized = 3000.00");
        rules.AppendLine("  CostBasis = 10000.00");
        rules.AppendLine("  Performance = ((0 + 3000) / 10000) × 100 = 30.00%");
        rules.AppendLine();
        rules.AppendLine("Scenario 4: Zero Cost Basis (Edge Case)");
        rules.AppendLine("  CostBasis = 0");
        rules.AppendLine("  Performance = 0 (avoid division by zero)");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**F# Implementation:**");
        rules.AppendLine("```fsharp");
        rules.AppendLine("let calculatePerformance (unrealized: decimal) (realized: decimal) (costBasis: decimal) =");
        rules.AppendLine("    if costBasis = 0m then ");
        rules.AppendLine("        0m  // Avoid division by zero");
        rules.AppendLine("    else");
        rules.AppendLine("        ((unrealized + realized) / costBasis) * 100m");
        rules.AppendLine();
        rules.AppendLine("// Convenience function for snapshots");
        rules.AppendLine("let calculateSnapshotPerformance (snapshot: TickerCurrencySnapshot) =");
        rules.AppendLine("    let unrealized = ");
        rules.AppendLine("        calculateUnrealized ");
        rules.AppendLine("            snapshot.TotalShares ");
        rules.AppendLine("            snapshot.LatestPrice.Value ");
        rules.AppendLine("            snapshot.CostBasis.Value");
        rules.AppendLine("    calculatePerformance unrealized snapshot.Realized.Value snapshot.CostBasis.Value");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("---");
        rules.AppendLine();

        // Cumulative Calculations
        rules.AppendLine("## Cumulative Field Calculations");
        rules.AppendLine();
        rules.AppendLine("These fields accumulate over time through trade processing:");
        rules.AppendLine();

        rules.AppendLine("### 3. Total Income");
        rules.AppendLine();
        rules.AppendLine("**Description:** Sum of all income sources for this ticker/currency combination.");
        rules.AppendLine();
        rules.AppendLine("**Formula:**");
        rules.AppendLine("```");
        rules.AppendLine("TotalIncomes = Dividends + Options + OtherIncome");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**Note:** This is currently stored, but could be calculated from component fields.");
        rules.AppendLine();

        rules.AppendLine("### 4. Realized Gains");
        rules.AppendLine();
        rules.AppendLine("**Description:** Cumulative profit/loss from closed positions.");
        rules.AppendLine();
        rules.AppendLine("**Calculation on Trade Close:**");
        rules.AppendLine("```");
        rules.AppendLine("RealizedGain = SaleProceeds - OriginalCostBasis - BuyCommission - SellCommission");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**Important:**");
        rules.AppendLine("- Uses **FIFO (First In, First Out)** lot matching");
        rules.AppendLine("- Adjusts for stock splits proportionally");
        rules.AppendLine("- Includes commissions from both buy and sell sides");
        rules.AppendLine();
        rules.AppendLine("**Example:**");
        rules.AppendLine("```");
        rules.AppendLine("Buy:  100 shares @ $50 + $10 commission = $5,010 cost");
        rules.AppendLine("Sell: 100 shares @ $75 + $10 commission = $7,490 proceeds");
        rules.AppendLine("Realized = $7,490 - $5,010 = $2,480");
        rules.AppendLine("```");
        rules.AppendLine();

        rules.AppendLine("### 5. Cost Basis Adjustments");
        rules.AppendLine();
        rules.AppendLine("**Original Cost Basis (`CostBasis`):**");
        rules.AppendLine("```");
        rules.AppendLine("CostBasis = Sum of (SharesPurchased × PurchasePrice + Commission)");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**Real Cost Basis (`RealCost`):**");
        rules.AppendLine("```");
        rules.AppendLine("RealCost = CostBasis adjusted for:");
        rules.AppendLine("  - Stock splits (proportional adjustment)");
        rules.AppendLine("  - Stock dividends");
        rules.AppendLine("  - Return of capital");
        rules.AppendLine("  - Spin-offs");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**Stock Split Example:**");
        rules.AppendLine("```");
        rules.AppendLine("Before 2:1 Split:");
        rules.AppendLine("  100 shares @ $100/share → CostBasis = $10,000");
        rules.AppendLine();
        rules.AppendLine("After 2:1 Split:");
        rules.AppendLine("  200 shares @ $50/share → RealCost = $10,000 (unchanged)");
        rules.AppendLine("  Cost per share = $10,000 / 200 = $50");
        rules.AppendLine("```");
        rules.AppendLine();

        // Portfolio Weight
        rules.AppendLine("### 6. Portfolio Weight");
        rules.AppendLine();
        rules.AppendLine("**Description:** Percentage of this position relative to total portfolio value.");
        rules.AppendLine();
        rules.AppendLine("**Formula:**");
        rules.AppendLine("```");
        rules.AppendLine("Weight = (PositionValue / TotalPortfolioValue) × 100");
        rules.AppendLine();
        rules.AppendLine("Where:");
        rules.AppendLine("  PositionValue = LatestPrice × TotalShares");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**Note:** Requires aggregation across all tickers to calculate total portfolio value.");
        rules.AppendLine();
        rules.AppendLine("---");
        rules.AppendLine();

        // Validation Rules
        rules.AppendLine("## Validation Rules");
        rules.AppendLine();
        rules.AppendLine("### Data Integrity Constraints");
        rules.AppendLine();
        rules.AppendLine("1. **Non-Negative Shares**: `TotalShares >= 0`");
        rules.AppendLine("   - Negative shares are invalid (use separate short position tracking if needed)");
        rules.AppendLine();
        rules.AppendLine("2. **Non-Negative Price**: `LatestPrice >= 0`");
        rules.AppendLine("   - Zero price allowed for delisted/worthless securities");
        rules.AppendLine();
        rules.AppendLine("3. **Non-Negative Cost Basis**: `CostBasis >= 0`");
        rules.AppendLine("   - Zero allowed for gifted shares or inherited positions");
        rules.AppendLine();
        rules.AppendLine("4. **Weight Range**: `0.0 <= Weight <= 100.0`");
        rules.AppendLine("   - Cannot exceed 100% of portfolio (leveraged positions handled separately)");
        rules.AppendLine();
        rules.AppendLine("5. **Foreign Key Integrity**:");
        rules.AppendLine("   - `TickerId` must exist in `Tickers` table");
        rules.AppendLine("   - `CurrencyId` must exist in `Currencies` table");
        rules.AppendLine("   - `TickerSnapshotId` must exist in `TickerSnapshots` table");
        rules.AppendLine();
        rules.AppendLine("6. **Open Trades Consistency**:");
        rules.AppendLine("   - **For Share Positions:**");
        rules.AppendLine("     - If `TotalShares = 0`, shares do NOT contribute to OpenTrades");
        rules.AppendLine("     - If `TotalShares > 0`, then `OpenTrades` should be `true`");
        rules.AppendLine("   - **For Option Positions:**");
        rules.AppendLine("     - OpenTrades depends on net position (netPosition) for each option (strike/expiration)");
        rules.AppendLine("     - netPosition = sum of: BuyToOpen(+1) + SellToOpen(-1) + BuyToClose(-1) + SellToClose(+1)");
        rules.AppendLine("     - If netPosition = 0 for ALL options, then options do NOT contribute to OpenTrades");
        rules.AppendLine("     - If netPosition ≠ 0 for ANY option group, then `OpenTrades` should be `true`");
        rules.AppendLine("   - **Overall Rule:**");
        rules.AppendLine("     - `OpenTrades = true` if (TotalShares > 0) OR (any option has netPosition ≠ 0)");
        rules.AppendLine("     - `OpenTrades = false` if (TotalShares = 0) AND (all options have netPosition = 0)");
        rules.AppendLine();

        // Database Schema
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## Database Schema Reference");
        rules.AppendLine();
        rules.AppendLine("**Table Name:** `TickerCurrencySnapshots`");
        rules.AppendLine();
        rules.AppendLine("**Primary Key:** `Id` (auto-increment)");
        rules.AppendLine();
        rules.AppendLine("**Foreign Keys:**");
        rules.AppendLine("- `TickerId` → `Tickers.Id` (CASCADE DELETE, CASCADE UPDATE)");
        rules.AppendLine("- `CurrencyId` → `Currencies.Id` (CASCADE DELETE, CASCADE UPDATE)");
        rules.AppendLine("- `TickerSnapshotId` → `TickerSnapshots.Id` (CASCADE DELETE, CASCADE UPDATE)");
        rules.AppendLine();
        rules.AppendLine("**Indexes:**");
        rules.AppendLine("- `idx_TickerCurrencySnapshots_TickerId` (for ticker-based queries)");
        rules.AppendLine("- `idx_TickerCurrencySnapshots_CurrencyId` (for currency filtering)");
        rules.AppendLine("- `idx_TickerCurrencySnapshots_TickerSnapshotId` (for parent relationship)");
        rules.AppendLine("- `idx_TickerCurrencySnapshots_Date` (for date-range queries)");
        rules.AppendLine("- `idx_TickerCurrencySnapshots_TickerId_Date` (composite for time-series analysis)");
        rules.AppendLine();

        // Implementation Notes
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## Implementation Notes");
        rules.AppendLine();
        rules.AppendLine("### Where to Add Calculation Functions");
        rules.AppendLine();
        rules.AppendLine("Create a new file: `src/Core/Snapshots/SnapshotCalculations.fs`");
        rules.AppendLine();
        rules.AppendLine("```fsharp");
        rules.AppendLine("module SnapshotCalculations =");
        rules.AppendLine();
        rules.AppendLine("    /// Calculate unrealized gains/losses for a position");
        rules.AppendLine("    let calculateUnrealized (totalShares: decimal) (latestPrice: decimal) (costBasis: decimal) =");
        rules.AppendLine("        if totalShares = 0m then 0m");
        rules.AppendLine("        else (latestPrice * totalShares) - costBasis");
        rules.AppendLine();
        rules.AppendLine("    /// Calculate total performance percentage");
        rules.AppendLine("    let calculatePerformance (unrealized: decimal) (realized: decimal) (costBasis: decimal) =");
        rules.AppendLine("        if costBasis = 0m then 0m");
        rules.AppendLine("        else ((unrealized + realized) / costBasis) * 100m");
        rules.AppendLine();
        rules.AppendLine("    /// Calculate performance for a snapshot");
        rules.AppendLine("    let calculateSnapshotPerformance (snapshot: TickerCurrencySnapshot) =");
        rules.AppendLine("        let unrealized = ");
        rules.AppendLine("            calculateUnrealized ");
        rules.AppendLine("                snapshot.TotalShares ");
        rules.AppendLine("                snapshot.LatestPrice.Value ");
        rules.AppendLine("                snapshot.CostBasis.Value");
        rules.AppendLine("        calculatePerformance unrealized snapshot.Realized.Value snapshot.CostBasis.Value");
        rules.AppendLine("```");
        rules.AppendLine();

        rules.AppendLine("### Converting Database Model to UI Model");
        rules.AppendLine();
        rules.AppendLine("In `src/Core/Models/DatabaseToModels.fs`, add calculations during conversion:");
        rules.AppendLine();
        rules.AppendLine("```fsharp");
        rules.AppendLine("let tickerCurrencySnapshotToModel (dbSnapshot: TickerCurrencySnapshot) =");
        rules.AppendLine("    let unrealized = ");
        rules.AppendLine("        SnapshotCalculations.calculateUnrealized ");
        rules.AppendLine("            dbSnapshot.TotalShares ");
        rules.AppendLine("            dbSnapshot.LatestPrice.Value ");
        rules.AppendLine("            dbSnapshot.CostBasis.Value");
        rules.AppendLine("    ");
        rules.AppendLine("    let performance = ");
        rules.AppendLine("        SnapshotCalculations.calculatePerformance ");
        rules.AppendLine("            unrealized ");
        rules.AppendLine("            dbSnapshot.Realized.Value ");
        rules.AppendLine("            dbSnapshot.CostBasis.Value");
        rules.AppendLine("    ");
        rules.AppendLine("    { Id = dbSnapshot.Base.Id");
        rules.AppendLine("      // ... other fields ...");
        rules.AppendLine("      Unrealized = unrealized  // Calculated, not stored");
        rules.AppendLine("      Performance = performance  // Calculated, not stored");
        rules.AppendLine("      // ... }");
        rules.AppendLine("```");
        rules.AppendLine();

        // Related Documentation
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## Related Documentation");
        rules.AppendLine();
        rules.AppendLine("- **Source Code**: `src/Core/Database/SnapshotsModel.fs` (lines 54-76)");
        rules.AppendLine("- **SQL Queries**: `src/Core/SQL/TickerCurrencySnapshotQuery.fs`");
        rules.AppendLine("- **Database Extensions**: `src/Core/Database/TickerCurrencySnapshotExtensions.fs`");
        rules.AppendLine("- **Batch Calculator**: `src/Core/Snapshots/TickerSnapshotBatchCalculator.fs`");
        rules.AppendLine("- **In-Memory Calculation**: `src/Core/Snapshots/TickerSnapshotCalculateInMemory.fs`");
        rules.AppendLine();

        return await Task.FromResult(rules.ToString());
    }

    #endregion

    #region Option Trade Import & Storage Rules

    [McpServerTool]
    [Description("Get comprehensive rules for Option Trade Import & Storage. Defines data model consistency between UI and CSV import, FIFO matching algorithm, and IsOpen flag management.")]
    public static async Task<string> GetOptionTradeImportStorageRules()
    {
        var rules = new StringBuilder();

        rules.AppendLine("# Option Trade Import & Storage Rules");
        rules.AppendLine();
        rules.AppendLine("## Critical Principle: One Contract Per Record");
        rules.AppendLine();
        rules.AppendLine("**ALL option trades MUST be stored with Quantity = 1 in the database**, regardless of whether the trader is creating 1 contract or 11 contracts in a single action.");
        rules.AppendLine();
        rules.AppendLine("### Why?");
        rules.AppendLine("1. **Individual Contract Closing**: Traders need to close contracts independently (e.g., close 3 of 11)");
        rules.AppendLine("2. **FIFO Matching Simplicity**: Queue-based matching works cleanly with 1:1 records");
        rules.AppendLine("3. **Consistency**: Same data model across UI and import enables reliable FIFO logic");
        rules.AppendLine("4. **Auditability**: Each contract has its own lifecycle, timestamps, and closing relationship");
        rules.AppendLine();
        rules.AppendLine("---");
        rules.AppendLine();

        // UI Expansion
        rules.AppendLine("## UI-Based Option Trade Creation");
        rules.AppendLine();
        rules.AppendLine("**Location**: `src/Core/UI/Creator.fs`, function `SaveOptionsTrade` (lines 286-320)");
        rules.AppendLine();
        rules.AppendLine("**Implementation Pattern**: When saving option trades from the UI, ALWAYS expand Quantity > 1 into multiple records:");
        rules.AppendLine();
        rules.AppendLine("1. If trade.Quantity > 1:");
        rules.AppendLine("   - Divide NetPremium equally: `NetPremium_per_contract = Total / Quantity`");
        rules.AppendLine("   - Create N records with Quantity = 1 each");
        rules.AppendLine("   - All other fields remain identical");
        rules.AppendLine();
        rules.AppendLine("2. If trade.Quantity = 1: Store as-is");
        rules.AppendLine();
        rules.AppendLine("**Example**:");
        rules.AppendLine("```");
        rules.AppendLine("Input: SellToOpen 5 TSLL Calls with NetPremium=$50");
        rules.AppendLine("Output: 5 database records:");
        rules.AppendLine("  - Record 1: Quantity=1, NetPremium=$10");
        rules.AppendLine("  - Record 2: Quantity=1, NetPremium=$10");
        rules.AppendLine("  - Record 3: Quantity=1, NetPremium=$10");
        rules.AppendLine("  - Record 4: Quantity=1, NetPremium=$10");
        rules.AppendLine("  - Record 5: Quantity=1, NetPremium=$10");
        rules.AppendLine("```");
        rules.AppendLine();

        // CSV Import Expansion
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## CSV Import Processing");
        rules.AppendLine();
        rules.AppendLine("**Location**: `src/Core/Import/DatabasePersistence.fs`, function `createOptionTradeFromTransaction` (lines 210-270)");
        rules.AppendLine();
        rules.AppendLine("**REQUIREMENT**: CSV import MUST expand trades like the UI does.");
        rules.AppendLine();
        rules.AppendLine("**Current Behavior (❌ BUG)**:");
        rules.AppendLine("- Creates 1 record per CSV line");
        rules.AppendLine("- Preserves Quantity field from CSV");
        rules.AppendLine("- NetPremium not proportionally divided");
        rules.AppendLine();
        rules.AppendLine("**Required Behavior (✅ FIX)**:");
        rules.AppendLine("- If transaction.Quantity > 1:");
        rules.AppendLine("  - Calculate NetPremium per contract: `netPremiumPerContract = netPremium / Quantity`");
        rules.AppendLine("  - Create Quantity number of records, each with Quantity=1");
        rules.AppendLine("  - Return collection instead of single option");
        rules.AppendLine();
        rules.AppendLine("**Affected Code**:");
        rules.AppendLine("- Return type must change from `OptionTrade option` to `OptionTrade list`");
        rules.AppendLine("- Calling code (line ~612) must iterate through expanded trades");
        rules.AppendLine("- Each expanded trade must be persisted and potentially linked separately");
        rules.AppendLine();

        // FIFO Matching
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## FIFO Matching Algorithm");
        rules.AppendLine();
        rules.AppendLine("**Location**: `src/Core/Database/OptionTradeExtensions.fs`");
        rules.AppendLine("- Function `linkClosingTrade` (lines 199-228)");
        rules.AppendLine("- Function `tryFindOpenTradeForClosing` (lines 162-180)");
        rules.AppendLine();
        rules.AppendLine("**Matching Process**:");
        rules.AppendLine("1. For each closing trade (BuyToClose or SellToClose):");
        rules.AppendLine("   - Find matching opening trade by: TickerId, CurrencyId, BrokerAccountId, OptionType, Strike, Expiration");
        rules.AppendLine("   - Use FIFO order: ORDER BY TimeStamp, LIMIT 1");
        rules.AppendLine("2. Link the records:");
        rules.AppendLine("   - Set IsOpen = false on opening trade");
        rules.AppendLine("   - Set ClosedWith = closing_trade_id");
        rules.AppendLine("3. If no match found: Log error (non-critical), continue processing");
        rules.AppendLine();
        rules.AppendLine("**Why Expansion Fixes This**:");
        rules.AppendLine("- With Quantity=1: FIFO naturally matches 1 opening to 1 closing");
        rules.AppendLine("- Without expansion: Quantity mismatch prevents proper matching");
        rules.AppendLine();
        rules.AppendLine("**Example Fix**:");
        rules.AppendLine("```");
        rules.AppendLine("Before Expansion (BROKEN):");
        rules.AppendLine("  SellToOpen Quantity=5, SellToOpen Quantity=6");
        rules.AppendLine("  BuyToClose Quantity=11");
        rules.AppendLine("  Result: Only first SellToOpen (Qty=5) gets linked, second ignored");
        rules.AppendLine();
        rules.AppendLine("After Expansion (FIXED):");
        rules.AppendLine("  SellToOpen Qty=1 (record 1), ... (record 5)");
        rules.AppendLine("  SellToOpen Qty=1 (record 6), ... (record 11)");
        rules.AppendLine("  BuyToClose Qty=1 (record 12), ... (record 22)");
        rules.AppendLine("  Result: All 11 opening records get linked to closing records");
        rules.AppendLine("```");
        rules.AppendLine();

        // IsOpen Flag Management
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## IsOpen Flag Management");
        rules.AppendLine();
        rules.AppendLine("**Truth Table**:");
        rules.AppendLine();
        rules.AppendLine("| IsOpen | ClosedWith | Meaning | Valid? |");
        rules.AppendLine("|--------|-----------|---------|--------|");
        rules.AppendLine("| `true` | `null` | Position opened, not yet closed | ✅ YES |");
        rules.AppendLine("| `false` | `closing_trade_id` | Position closed and linked | ✅ YES |");
        rules.AppendLine("| `true` | not null | Marked open but linked to closing trade | ❌ NO |");
        rules.AppendLine("| `false` | `null` | Marked closed but not linked | ❌ NO |");
        rules.AppendLine();
        rules.AppendLine("**Update Process**:");
        rules.AppendLine("When a closing trade matches an opening trade:");
        rules.AppendLine("1. Set `IsOpen = false`");
        rules.AppendLine("2. Set `ClosedWith = closing_trade_id`");
        rules.AppendLine("3. Update Audit timestamp");
        rules.AppendLine("4. Persist to database");
        rules.AppendLine();

        // Edge Cases
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## Edge Cases & Validation");
        rules.AppendLine();
        rules.AppendLine("### 1. Partial Closing");
        rules.AppendLine("**Scenario**: Trader opens 5 contracts, closes 3");
        rules.AppendLine();
        rules.AppendLine("After expansion with Quantity=1:");
        rules.AppendLine("- 5 opening records created");
        rules.AppendLine("- 3 closing records created");
        rules.AppendLine("- 3 of 5 opening records get IsOpen=false");
        rules.AppendLine("- 2 opening records remain with IsOpen=true");
        rules.AppendLine();
        rules.AppendLine("### 2. Same Strike/Expiration, Different Times");
        rules.AppendLine("**Scenario**: Multiple SellToOpen at same timestamp, then BuyToClose");
        rules.AppendLine();
        rules.AppendLine("FIFO matching uses TimeStamp as primary sort, database insertion order as secondary.");
        rules.AppendLine("Ensures consistent order even with simultaneous trades.");
        rules.AppendLine();
        rules.AppendLine("### 3. Closing Without Opening");
        rules.AppendLine("**Scenario**: BuyToClose without corresponding SellToOpen (data corruption)");
        rules.AppendLine();
        rules.AppendLine("Behavior:");
        rules.AppendLine("- Closing trade is persisted ✓");
        rules.AppendLine("- ClosedWith remains null (no match found)");
        rules.AppendLine("- Error logged but import continues (non-critical)");
        rules.AppendLine();
        rules.AppendLine("### 4. Quantity Validation");
        rules.AppendLine("Rule: Reject if transaction.Quantity <= 0");
        rules.AppendLine();
        rules.AppendLine("### 5. Expired Options");
        rules.AppendLine("Rule: Do NOT automatically mark as closed based on expiration date");
        rules.AppendLine("Only explicit close transactions (BuyToClose, SellToClose) update IsOpen flag");
        rules.AppendLine();

        // Implementation Files
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## Implementation Locations");
        rules.AppendLine();
        rules.AppendLine("| File | Function | Change | Priority |");
        rules.AppendLine("|------|----------|--------|----------|");
        rules.AppendLine("| `src/Core/Import/DatabasePersistence.fs` | `createOptionTradeFromTransaction` | Expand Quantity > 1 | **HIGH** |");
        rules.AppendLine("| `src/Core/Import/DatabasePersistence.fs` | Calling code (line ~612) | Handle list of trades | **HIGH** |");
        rules.AppendLine("| `src/Core/Database/OptionTradeExtensions.fs` | `linkClosingTrade` | Already works with Qty=1 | Check only |");
        rules.AppendLine("| `src/Core/UI/Creator.fs` | `SaveOptionsTrade` | Verify expansion logic | Reference |");
        rules.AppendLine();

        // Test Data
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## Test Data & Known Issues");
        rules.AppendLine();
        rules.AppendLine("**Known Issue**: TSLL Import with Option Contracts");
        rules.AppendLine();
        rules.AppendLine("File: `src/Tests/Core.Platform.MauiTester/Resources/TestData/TsllImportTest.csv`");
        rules.AppendLine();
        rules.AppendLine("Lines 182-184: SellToOpen");
        rules.AppendLine("- Line 182: SellToOpen 5 TSLL 11/15/24 Call 17.00 @ 0.85");
        rules.AppendLine("- Line 184: SellToOpen 6 TSLL 11/15/24 Call 17.00 @ 0.85");
        rules.AppendLine();
        rules.AppendLine("Line 179: BuyToClose");
        rules.AppendLine("- Line 179: BuyToClose 11 TSLL 11/15/24 Call 17.00 @ 0.95");
        rules.AppendLine();
        rules.AppendLine("Current Issue: 5+6 opening records not fully linked to 11 closing record");
        rules.AppendLine("After Fix: All 11 opening records (1 per contract) will be properly linked");
        rules.AppendLine();

        // Reference Implementations
        rules.AppendLine("---");
        rules.AppendLine();
        rules.AppendLine("## Reference Implementations");
        rules.AppendLine();
        rules.AppendLine("**UI Expansion (Correct)**: `src/Core/UI/Creator.fs` lines 286-301");
        rules.AppendLine("```fsharp");
        rules.AppendLine("let expandedTrades =");
        rules.AppendLine("    optionTrades");
        rules.AppendLine("    |> List.collect (fun trade ->");
        rules.AppendLine("        if trade.Quantity > 1 then");
        rules.AppendLine("            let netPremium = trade.NetPremium / decimal trade.Quantity");
        rules.AppendLine("            [ for _ in 1 .. trade.Quantity ->");
        rules.AppendLine("                  { trade with");
        rules.AppendLine("                      Quantity = 1");
        rules.AppendLine("                      NetPremium = netPremium } ]");
        rules.AppendLine("        else");
        rules.AppendLine("            [ trade ])");
        rules.AppendLine("```");
        rules.AppendLine();
        rules.AppendLine("**FIFO Linking (Correct)**: `src/Core/Database/OptionTradeExtensions.fs` lines 184-198");
        rules.AppendLine("```fsharp");
        rules.AppendLine("let updatedTrade =");
        rules.AppendLine("    { openTrade with");
        rules.AppendLine("        IsOpen = false");
        rules.AppendLine("        ClosedWith = Some closingTradeId");
        rules.AppendLine("        Audit = updatedAudit }");
        rules.AppendLine("do! Do.save (updatedTrade)");
        rules.AppendLine("```");
        rules.AppendLine();

        return await Task.FromResult(rules.ToString());
    }

    #endregion

    #region General Rules

    [McpServerTool]
    [Description("Get a quick reference of all available business rule categories and entities in Binnaculum")]
    public static async Task<string> GetAvailableRules()
    {
        var rules = new StringBuilder();

        rules.AppendLine("# Binnaculum Business Rules - Quick Reference");
        rules.AppendLine();
        rules.AppendLine("## Available Rule Categories");
        rules.AppendLine();
        rules.AppendLine("### Snapshot Entities");
        rules.AppendLine();
        rules.AppendLine("1. **TickerCurrencySnapshot** - Currency-specific ticker financial metrics");
        rules.AppendLine("   - Use: `GetTickerCurrencySnapshotRules()`");
        rules.AppendLine("   - Covers: Unrealized gains, Performance%, Income calculations");
        rules.AppendLine();
        rules.AppendLine("2. **BrokerFinancialSnapshot** - Broker/account-level financial aggregations");
        rules.AppendLine("   - Use: `GetBrokerFinancialSnapshotRules()` (Coming Soon)");
        rules.AppendLine();
        rules.AppendLine("3. **TickerSnapshot** - Ticker-level snapshot parent entity");
        rules.AppendLine("   - Use: `GetTickerSnapshotRules()` (Coming Soon)");
        rules.AppendLine();
        rules.AppendLine("4. **BankAccountSnapshot** - Bank account balance tracking");
        rules.AppendLine("   - Use: `GetBankAccountSnapshotRules()` (Coming Soon)");
        rules.AppendLine();
        rules.AppendLine("### Data Import & Storage Rules");
        rules.AppendLine();
        rules.AppendLine("5. **Option Trade Import & Storage** - Data model consistency and FIFO matching");
        rules.AppendLine("   - Use: `GetOptionTradeImportStorageRules()`");
        rules.AppendLine("   - Covers: Trade expansion, FIFO matching, IsOpen flag management");
        rules.AppendLine("   - **CRITICAL**: Ensures UI and CSV import use consistent data models");
        rules.AppendLine();
        rules.AppendLine("### Trade Processing Rules");
        rules.AppendLine();
        rules.AppendLine("- **Trade Matching** - FIFO lot matching logic");
        rules.AppendLine("- **Commission Handling** - Buy/sell commission allocation");
        rules.AppendLine("- **Corporate Actions** - Stock splits, dividends, mergers");
        rules.AppendLine();
        rules.AppendLine("### Calculation Principles");
        rules.AppendLine();
        rules.AppendLine("- **Store Facts, Calculate Insights**: Database stores raw facts (shares, prices, costs)");
        rules.AppendLine("- **On-the-Fly Calculations**: Derived metrics (performance, unrealized) computed at read time");
        rules.AppendLine("- **Cumulative Tracking**: Realized gains, dividends, incomes accumulate through processing");
        rules.AppendLine();

        return await Task.FromResult(rules.ToString());
    }

    [McpServerTool]
    [Description("Get specific calculation formula with detailed examples and edge cases")]
    public static async Task<string> GetCalculationFormula(
        [Description("Calculation name: unrealized_gains, performance_percentage, realized_gains, total_income, portfolio_weight")]
        string calculationName)
    {
        var rules = new StringBuilder();

        switch (calculationName.ToLowerInvariant())
        {
            case "unrealized_gains":
            case "unrealized":
                rules.AppendLine("# Unrealized Gains Calculation");
                rules.AppendLine();
                rules.AppendLine("**Formula:** `(LatestPrice × TotalShares) - CostBasis`");
                rules.AppendLine();
                rules.AppendLine("**Description:** Current profit/loss on open positions that haven't been sold.");
                rules.AppendLine();
                rules.AppendLine("**F# Implementation:**");
                rules.AppendLine("```fsharp");
                rules.AppendLine("let calculateUnrealized totalShares latestPrice costBasis =");
                rules.AppendLine("    if totalShares = 0m then 0m");
                rules.AppendLine("    else (latestPrice * totalShares) - costBasis");
                rules.AppendLine("```");
                rules.AppendLine();
                rules.AppendLine("**Examples:** See `GetTickerCurrencySnapshotRules()` for detailed examples.");
                break;

            case "performance_percentage":
            case "performance":
                rules.AppendLine("# Performance Percentage Calculation");
                rules.AppendLine();
                rules.AppendLine("**Formula:** `((Unrealized + Realized) / CostBasis) × 100`");
                rules.AppendLine();
                rules.AppendLine("**Description:** Total return percentage including realized and unrealized gains.");
                rules.AppendLine();
                rules.AppendLine("**F# Implementation:**");
                rules.AppendLine("```fsharp");
                rules.AppendLine("let calculatePerformance unrealized realized costBasis =");
                rules.AppendLine("    if costBasis = 0m then 0m");
                rules.AppendLine("    else ((unrealized + realized) / costBasis) * 100m");
                rules.AppendLine("```");
                rules.AppendLine();
                rules.AppendLine("**Examples:** See `GetTickerCurrencySnapshotRules()` for detailed examples.");
                break;

            case "total_income":
                rules.AppendLine("# Total Income Calculation");
                rules.AppendLine();
                rules.AppendLine("**Formula:** `Dividends + Options + OtherIncome`");
                rules.AppendLine();
                rules.AppendLine("**Note:** Currently stored in database, but could be calculated from components.");
                break;

            case "portfolio_weight":
            case "weight":
                rules.AppendLine("# Portfolio Weight Calculation");
                rules.AppendLine();
                rules.AppendLine("**Formula:** `(PositionValue / TotalPortfolioValue) × 100`");
                rules.AppendLine();
                rules.AppendLine("Where: `PositionValue = LatestPrice × TotalShares`");
                break;

            default:
                rules.AppendLine($"# Unknown Calculation: {calculationName}");
                rules.AppendLine();
                rules.AppendLine("Available calculations:");
                rules.AppendLine("- unrealized_gains");
                rules.AppendLine("- performance_percentage");
                rules.AppendLine("- total_income");
                rules.AppendLine("- portfolio_weight");
                break;
        }

        return await Task.FromResult(rules.ToString());
    }

    #endregion
}