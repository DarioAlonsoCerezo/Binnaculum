

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