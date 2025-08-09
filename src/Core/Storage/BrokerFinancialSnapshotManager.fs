namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open SnapshotManagerUtils
open BrokerFinancialSnapshotExtensions
open BrokerMovementExtensions
open TradeExtensions
open DividendExtensions
open DividendTaxExtensions
open OptionTradeExtensions
open TickerPriceExtensions

module internal BrokerFinancialSnapshotManager =
    
    /// <summary>
    /// Represents calculated financial metrics from movement data that can be combined with previous snapshots.
    /// This record encapsulates all the financial calculations needed to create or update a financial snapshot.
    /// </summary>
    type private CalculatedFinancialMetrics = {
        // Primary financial flows
        Deposited: Money
        Withdrawn: Money
        Invested: Money
        RealizedGains: Money
        
        // Income sources
        DividendsReceived: Money
        OptionsIncome: Money
        OtherIncome: Money
        
        // Costs
        Commissions: Money
        Fees: Money
        
        // Position tracking
        CurrentPositions: Map<int, decimal>
        CostBasisInfo: Map<int, decimal>
        HasOpenPositions: bool
        
        // Activity counters
        MovementCounter: int
    }

    /// <summary>
    /// Validates the input parameters for broker account snapshot operations.
    /// Throws detailed exceptions if any validation fails, does nothing if all validations pass.
    /// </summary>
    /// <param name="brokerAccountSnapshot">The broker account snapshot to validate</param>
    /// <param name="movementData">The movement data to validate</param>
    let private validateSnapshotAndMovementData
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (movementData: BrokerAccountMovementData)
        =
        // Validate brokerAccountSnapshot parameters
        if brokerAccountSnapshot.Base.Id <= 0 then
            failwithf "Invalid broker account snapshot ID: %d. ID must be greater than 0." brokerAccountSnapshot.Base.Id
        
        if brokerAccountSnapshot.BrokerAccountId <= 0 then
            failwithf "Invalid broker account ID: %d. Broker account ID must be greater than 0." brokerAccountSnapshot.BrokerAccountId
        
        // Validate movementData parameters
        if movementData.BrokerAccountId <= 0 then
            failwithf "Invalid movement data broker account ID: %d. Must be greater than 0." movementData.BrokerAccountId
        
        if movementData.BrokerAccountId <> brokerAccountSnapshot.BrokerAccountId then
            failwithf "Movement data broker account ID (%d) does not match snapshot broker account ID (%d)." 
                movementData.BrokerAccountId brokerAccountSnapshot.BrokerAccountId
        
        if movementData.FromDate.Value > brokerAccountSnapshot.Base.Date.Value then
            failwithf "Movement data FromDate (%A) cannot be later than snapshot date (%A)." 
                movementData.FromDate.Value brokerAccountSnapshot.Base.Date.Value

    /// <summary>
    /// Calculates unrealized gains for current positions based on market prices and cost basis.
    /// This helper function handles the complex logic of matching positions with market data.
    /// Market prices are retrieved for the target currency to ensure proper currency matching.
    /// </summary>
    /// <param name="currentPositions">Map of ticker ID to current position quantity</param>
    /// <param name="costBasisInfo">Map of ticker ID to average cost basis per share (in the target currency)</param>
    /// <param name="targetDate">Date to retrieve market prices for</param>
    /// <param name="targetCurrencyId">Currency ID to ensure price and cost basis alignment</param>
    /// <returns>Tuple of (UnrealizedGains as Money, UnrealizedGainsPercentage as decimal)</returns>
    let private calculateUnrealizedGains 
        (currentPositions: Map<int, decimal>) 
        (costBasisInfo: Map<int, decimal>) 
        (targetDate: DateTimePattern) 
        (targetCurrencyId: int) =
        task {
            let mutable totalMarketValue = 0m
            let mutable totalCostBasis = 0m
            
            // Process each ticker with current positions
            for KeyValue(tickerId, quantity) in currentPositions do
                // Only process if we have non-zero positions
                if quantity <> 0m then
                    // ✅ CURRENCY-SAFE: Get market price for this ticker on the target date in the correct currency
                    // This ensures that market price and cost basis are in the same currency for accurate comparison
                    let! marketPrice = TickerPriceExtensions.Do.getPriceByDateOrPreviousAndCurrencyId(tickerId, targetCurrencyId, targetDate.Value.ToString("yyyy-MM-dd"))
                    
                    // Get cost basis per share for this ticker (already in target currency from trade calculations)
                    let costBasisPerShare = costBasisInfo.TryFind(tickerId) |> Option.defaultValue 0m
                    
                    // Calculate market value and cost basis for this position
                    let positionMarketValue = marketPrice * abs(quantity)  // Use abs() to handle both long and short positions
                    let positionCostBasis = costBasisPerShare * abs(quantity)
                    
                    // For short positions, the unrealized gain/loss calculation is inverted
                    if quantity > 0m then
                        // Long position: gain when market price > cost basis
                        totalMarketValue <- totalMarketValue + positionMarketValue
                        totalCostBasis <- totalCostBasis + positionCostBasis
                    else
                        // Short position: gain when market price < cost basis (we sold high, can buy back low)
                        totalMarketValue <- totalMarketValue - positionMarketValue  // Negative market value for shorts
                        totalCostBasis <- totalCostBasis - positionCostBasis        // Negative cost basis for shorts
            
            // Calculate total unrealized gains: Market Value - Cost Basis
            let unrealizedGains = totalMarketValue - totalCostBasis
            
            // Calculate unrealized gains percentage
            let unrealizedGainsPercentage = 
                if totalCostBasis <> 0m then
                    (unrealizedGains / abs(totalCostBasis)) * 100m  // Use abs() for percentage calculation
                else 
                    0m
            
            return (Money.FromAmount unrealizedGains, unrealizedGainsPercentage)
        }

    /// <summary>
    /// Calculates financial metrics from currency movement data using the extension methods.
    /// This function provides a centralized way to process all movement types and return
    /// a comprehensive financial metrics record that can be used across different scenarios.
    /// </summary>
    /// <param name="currencyMovements">The currency-specific movement data</param>
    /// <param name="currencyId">The currency ID for conversion impact calculations</param>
    /// <returns>CalculatedFinancialMetrics record with all financial calculations</returns>
    let private calculateFinancialMetricsFromMovements 
        (currencyMovements: CurrencyMovementData) 
        (currencyId: int) =
        
        // Process broker movements using extension methods
        let brokerMovementSummary = 
            currencyMovements.BrokerMovements
            |> FinancialCalculations.calculateFinancialSummary
        
        // Calculate currency conversion impact
        let conversionImpact = 
            currencyMovements.BrokerMovements
            |> fun movements -> FinancialCalculations.calculateConversionImpact(movements, currencyId)
        
        // Apply conversion impact to deposits/withdrawals
        let (adjustedDeposited, adjustedWithdrawn) = 
            if conversionImpact.Value >= 0m then
                // Positive conversion impact: money was gained in this currency
                (brokerMovementSummary.TotalDeposited.Value + conversionImpact.Value, 
                 brokerMovementSummary.TotalWithdrawn.Value)
            else
                // Negative conversion impact: money was lost from this currency
                (brokerMovementSummary.TotalDeposited.Value, 
                 brokerMovementSummary.TotalWithdrawn.Value + abs(conversionImpact.Value))
        
        // Process trades using extension methods
        let tradingSummary = 
            currencyMovements.Trades
            |> TradeCalculations.calculateTradingSummary
        
        // Process dividends using extension methods
        let dividendSummary = 
            currencyMovements.Dividends
            |> DividendCalculations.calculateDividendSummary
        
        // Process dividend taxes using extension methods
        let dividendTaxSummary = 
            currencyMovements.DividendTaxes
            |> DividendTaxCalculations.calculateDividendTaxSummary
        
        // Process options using extension methods
        let optionsSummary = 
            currencyMovements.OptionTrades
            |> OptionTradeCalculations.calculateOptionsSummary
        
        // Calculate net dividend income after taxes
        let netDividendIncome = Money.FromAmount (dividendSummary.TotalDividendIncome.Value - dividendTaxSummary.TotalTaxWithheld.Value)
        
        // Adjust other income for interest paid
        let adjustedOtherIncome = Money.FromAmount (brokerMovementSummary.TotalOtherIncome.Value - brokerMovementSummary.TotalInterestPaid.Value)
        
        // Calculate total movement counter
        let totalMovementCounter = 
            brokerMovementSummary.MovementCount + 
            tradingSummary.TradeCount + 
            dividendSummary.DividendCount + 
            dividendTaxSummary.TaxEventCount + 
            optionsSummary.TradeCount
        
        // Return comprehensive metrics record
        {
            Deposited = Money.FromAmount adjustedDeposited
            Withdrawn = Money.FromAmount adjustedWithdrawn
            Invested = Money.FromAmount (tradingSummary.TotalInvested.Value + optionsSummary.OptionsInvestment.Value)
            RealizedGains = Money.FromAmount (tradingSummary.RealizedGains.Value + optionsSummary.RealizedGains.Value)
            DividendsReceived = netDividendIncome
            OptionsIncome = optionsSummary.OptionsIncome
            OtherIncome = adjustedOtherIncome
            Commissions = Money.FromAmount (brokerMovementSummary.TotalCommissions.Value + tradingSummary.TotalCommissions.Value + optionsSummary.TotalCommissions.Value)
            Fees = Money.FromAmount (brokerMovementSummary.TotalFees.Value + tradingSummary.TotalFees.Value + optionsSummary.TotalFees.Value)
            CurrentPositions = tradingSummary.CurrentPositions
            CostBasisInfo = tradingSummary.CostBasis
            HasOpenPositions = tradingSummary.HasOpenPositions || optionsSummary.HasOpenOptions
            MovementCounter = totalMovementCounter
        }

    /// <summary>
    /// Creates a financial snapshot by combining calculated metrics with optional previous snapshot values.
    /// This function centralizes the logic for creating cumulative financial snapshots, reducing duplication
    /// across the three scenario methods (initial, new, and update).
    /// </summary>
    /// <param name="targetDate">The date for the new snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="calculatedMetrics">The financial metrics calculated from movement data</param>
    /// <param name="previousSnapshot">Optional previous snapshot for cumulative calculations</param>
    /// <returns>Task that completes when the snapshot is calculated and saved</returns>
    let private createCumulativeFinancialSnapshot
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (calculatedMetrics: CalculatedFinancialMetrics)
        (previousSnapshot: BrokerFinancialSnapshot option)
        =
        task {
            // Calculate cumulative values by adding previous snapshot values (if any) to current metrics
            let cumulativeDeposited = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.Deposited.Value + calculatedMetrics.Deposited.Value)
                | None -> calculatedMetrics.Deposited
            
            let cumulativeWithdrawn = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.Withdrawn.Value + calculatedMetrics.Withdrawn.Value)
                | None -> calculatedMetrics.Withdrawn
            
            let cumulativeInvested = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.Invested.Value + calculatedMetrics.Invested.Value)
                | None -> calculatedMetrics.Invested
            
            let cumulativeRealizedGains = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.RealizedGains.Value + calculatedMetrics.RealizedGains.Value)
                | None -> calculatedMetrics.RealizedGains
            
            let cumulativeDividendsReceived = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.DividendsReceived.Value + calculatedMetrics.DividendsReceived.Value)
                | None -> calculatedMetrics.DividendsReceived
            
            let cumulativeOptionsIncome = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.OptionsIncome.Value + calculatedMetrics.OptionsIncome.Value)
                | None -> calculatedMetrics.OptionsIncome
            
            let cumulativeOtherIncome = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.OtherIncome.Value + calculatedMetrics.OtherIncome.Value)
                | None -> calculatedMetrics.OtherIncome
            
            let cumulativeCommissions = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.Commissions.Value + calculatedMetrics.Commissions.Value)
                | None -> calculatedMetrics.Commissions
            
            let cumulativeFees = 
                match previousSnapshot with
                | Some prev -> Money.FromAmount (prev.Fees.Value + calculatedMetrics.Fees.Value)
                | None -> calculatedMetrics.Fees
            
            let cumulativeMovementCounter = 
                match previousSnapshot with
                | Some prev -> prev.MovementCounter + calculatedMetrics.MovementCounter
                | None -> calculatedMetrics.MovementCounter
            
            // Calculate unrealized gains from current positions
            let! (unrealizedGains, unrealizedGainsPercentage) = 
                calculateUnrealizedGains calculatedMetrics.CurrentPositions calculatedMetrics.CostBasisInfo targetDate currencyId
            
            // Calculate realized percentage return
            let realizedPercentage = 
                if cumulativeInvested.Value > 0m then
                    (cumulativeRealizedGains.Value / cumulativeInvested.Value) * 100m
                else 
                    0m
            
            // Create the financial snapshot with all calculated values
            let newSnapshot = {
                Base = createBaseSnapshot targetDate
                BrokerId = 0 // Set to 0 for account-level snapshots
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = cumulativeMovementCounter
                BrokerSnapshotId = 0 // Set to 0 for account-level snapshots
                BrokerAccountSnapshotId = brokerAccountSnapshotId
                RealizedGains = cumulativeRealizedGains
                RealizedPercentage = realizedPercentage
                UnrealizedGains = unrealizedGains
                UnrealizedGainsPercentage = unrealizedGainsPercentage
                Invested = cumulativeInvested
                Commissions = cumulativeCommissions
                Fees = cumulativeFees
                Deposited = cumulativeDeposited
                Withdrawn = cumulativeWithdrawn
                DividendsReceived = cumulativeDividendsReceived
                OptionsIncome = cumulativeOptionsIncome
                OtherIncome = cumulativeOtherIncome
                OpenTrades = calculatedMetrics.HasOpenPositions
            }
            
            // Save the snapshot to database
            do! newSnapshot.save()
        }

    /// <summary>
    /// Updates an existing financial snapshot with recalculated values.
    /// This function handles the specific logic for updating existing snapshots while maintaining
    /// database identity and audit information.
    /// </summary>
    /// <param name="existingSnapshot">The existing snapshot to update</param>
    /// <param name="targetDate">The date for the snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="calculatedMetrics">The financial metrics calculated from movement data</param>
    /// <param name="previousSnapshot">The previous snapshot for baseline calculations</param>
    /// <returns>Task that completes when the snapshot is updated and saved</returns>
    let private updateExistingSnapshotWithMetrics
        (existingSnapshot: BrokerFinancialSnapshot)
        (targetDate: DateTimePattern)
        (currencyId: int)
        (calculatedMetrics: CalculatedFinancialMetrics)
        (previousSnapshot: BrokerFinancialSnapshot)
        =
        task {
            // Calculate cumulative values using previous snapshot as baseline
            let newDeposited = Money.FromAmount (previousSnapshot.Deposited.Value + calculatedMetrics.Deposited.Value)
            let newWithdrawn = Money.FromAmount (previousSnapshot.Withdrawn.Value + calculatedMetrics.Withdrawn.Value)
            let newInvested = Money.FromAmount (previousSnapshot.Invested.Value + calculatedMetrics.Invested.Value)
            let newRealizedGains = Money.FromAmount (previousSnapshot.RealizedGains.Value + calculatedMetrics.RealizedGains.Value)
            let newDividendsReceived = Money.FromAmount (previousSnapshot.DividendsReceived.Value + calculatedMetrics.DividendsReceived.Value)
            let newOptionsIncome = Money.FromAmount (previousSnapshot.OptionsIncome.Value + calculatedMetrics.OptionsIncome.Value)
            let newOtherIncome = Money.FromAmount (previousSnapshot.OtherIncome.Value + calculatedMetrics.OtherIncome.Value)
            let newCommissions = Money.FromAmount (previousSnapshot.Commissions.Value + calculatedMetrics.Commissions.Value)
            let newFees = Money.FromAmount (previousSnapshot.Fees.Value + calculatedMetrics.Fees.Value)
            let newMovementCounter = previousSnapshot.MovementCounter + calculatedMetrics.MovementCounter
            
            // Calculate unrealized gains from current positions
            let! (unrealizedGains, unrealizedGainsPercentage) = 
                calculateUnrealizedGains calculatedMetrics.CurrentPositions calculatedMetrics.CostBasisInfo targetDate currencyId
            
            // Calculate realized percentage return
            let realizedPercentage = 
                if newInvested.Value > 0m then
                    (newRealizedGains.Value / newInvested.Value) * 100m
                else 
                    0m
            
            // Update the existing snapshot with recalculated values
            // Keep the original ID and audit information to maintain data integrity
            let updatedSnapshot = {
                existingSnapshot with
                    MovementCounter = newMovementCounter
                    RealizedGains = newRealizedGains
                    RealizedPercentage = realizedPercentage
                    UnrealizedGains = unrealizedGains
                    UnrealizedGainsPercentage = unrealizedGainsPercentage
                    Invested = newInvested
                    Commissions = newCommissions
                    Fees = newFees
                    Deposited = newDeposited
                    Withdrawn = newWithdrawn
                    DividendsReceived = newDividendsReceived
                    OptionsIncome = newOptionsIncome
                    OtherIncome = newOtherIncome
                    OpenTrades = calculatedMetrics.HasOpenPositions
            }
            
            // Save the updated snapshot to database
            do! updatedSnapshot.save()
        }

    /// <summary>
    /// Updates an existing financial snapshot directly with new movements without a previous snapshot baseline.
    /// This is used for SCENARIO D: when new movements exist for a currency with an existing snapshot,
    /// but no previous snapshot is found. The existing snapshot itself serves as the baseline.
    /// This edge case may occur during data reprocessing, corrections, or when historical data is incomplete.
    /// </summary>
    /// <param name="targetDate">The date for the snapshot to update</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="currencyMovements">The currency-specific movements for calculations</param>
    /// <param name="existingSnapshot">The existing snapshot to update directly</param>
    /// <returns>Task that completes when the snapshot is updated and saved</returns>
    let private calculateDirectSnapshotUpdate
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        (existingSnapshot: BrokerFinancialSnapshot)
        =
        task {
            // =================================================================
            // VALIDATION AND CONSISTENCY CHECKS
            // =================================================================
            
            // Validate currency consistency
            if existingSnapshot.CurrencyId <> currencyId then
                failwithf "Existing snapshot currency (%d) does not match current currency (%d)" 
                    existingSnapshot.CurrencyId currencyId
            
            // Validate broker account consistency
            if existingSnapshot.BrokerAccountId <> brokerAccountId then
                failwithf "Existing snapshot broker account (%d) does not match expected account (%d)"
                    existingSnapshot.BrokerAccountId brokerAccountId
            
            // Validate date consistency
            if existingSnapshot.Base.Date <> targetDate then
                failwithf "Existing snapshot date (%A) does not match target date (%A)"
                    existingSnapshot.Base.Date.Value targetDate.Value
            
            // =================================================================
            // CALCULATE NEW MOVEMENTS AND UPDATE EXISTING SNAPSHOT
            // =================================================================
            
            // Calculate financial metrics from new movements
            let calculatedMetrics = calculateFinancialMetricsFromMovements currencyMovements currencyId
            
            // Since there's no previous snapshot, we add the new movement metrics directly to the existing snapshot
            // The existing snapshot serves as the baseline (which represents the state before these new movements)
            let newDeposited = Money.FromAmount (existingSnapshot.Deposited.Value + calculatedMetrics.Deposited.Value)
            let newWithdrawn = Money.FromAmount (existingSnapshot.Withdrawn.Value + calculatedMetrics.Withdrawn.Value)
            let newInvested = Money.FromAmount (existingSnapshot.Invested.Value + calculatedMetrics.Invested.Value)
            let newRealizedGains = Money.FromAmount (existingSnapshot.RealizedGains.Value + calculatedMetrics.RealizedGains.Value)
            let newDividendsReceived = Money.FromAmount (existingSnapshot.DividendsReceived.Value + calculatedMetrics.DividendsReceived.Value)
            let newOptionsIncome = Money.FromAmount (existingSnapshot.OptionsIncome.Value + calculatedMetrics.OptionsIncome.Value)
            let newOtherIncome = Money.FromAmount (existingSnapshot.OtherIncome.Value + calculatedMetrics.OtherIncome.Value)
            let newCommissions = Money.FromAmount (existingSnapshot.Commissions.Value + calculatedMetrics.Commissions.Value)
            let newFees = Money.FromAmount (existingSnapshot.Fees.Value + calculatedMetrics.Fees.Value)
            let newMovementCounter = existingSnapshot.MovementCounter + calculatedMetrics.MovementCounter
            
            // Calculate unrealized gains from current positions (including both existing and new positions)
            let! (unrealizedGains, unrealizedGainsPercentage) = 
                calculateUnrealizedGains calculatedMetrics.CurrentPositions calculatedMetrics.CostBasisInfo targetDate currencyId
            
            // Calculate realized percentage return
            let realizedPercentage = 
                if newInvested.Value > 0m then
                    (newRealizedGains.Value / newInvested.Value) * 100m
                else 
                    0m
            
            // Update the existing snapshot with the combined values
            // Keep the original ID and audit information to maintain data integrity
            let updatedSnapshot = {
                existingSnapshot with
                    MovementCounter = newMovementCounter
                    RealizedGains = newRealizedGains
                    RealizedPercentage = realizedPercentage
                    UnrealizedGains = unrealizedGains
                    UnrealizedGainsPercentage = unrealizedGainsPercentage
                    Invested = newInvested
                    Commissions = newCommissions
                    Fees = newFees
                    Deposited = newDeposited
                    Withdrawn = newWithdrawn
                    DividendsReceived = newDividendsReceived
                    OptionsIncome = newOptionsIncome
                    OtherIncome = newOtherIncome
                    OpenTrades = calculatedMetrics.HasOpenPositions
            }
            
            // Save the updated snapshot to database
            do! updatedSnapshot.save()
        }

    /// <summary>
    /// Creates an initial financial snapshot from movement data without requiring previous snapshots.
    /// This is used for SCENARIO B: when first movements occur in a new currency with no history.
    /// All financial metrics are calculated solely from the provided movement data.
    /// </summary>
    /// <param name="targetDate">The date for the new snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="currencyMovements">The currency-specific movements for calculations</param>
    /// <returns>Task that completes when the initial snapshot is calculated and saved</returns>
    let private calculateInitialFinancialSnapshot
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        =
        task {
            // Calculate financial metrics from movements
            let calculatedMetrics = calculateFinancialMetricsFromMovements currencyMovements currencyId
            
            // Create initial snapshot without previous baseline (pass None for previousSnapshot)
            do! createCumulativeFinancialSnapshot 
                    targetDate 
                    currencyId 
                    brokerAccountId 
                    brokerAccountSnapshotId 
                    calculatedMetrics 
                    None
        }

    /// <summary>
    /// Calculates a new financial snapshot based on currency movements and previous snapshot values.
    /// This is used for SCENARIO A: the most common case where we have new movements for a currency
    /// with existing historical data, creating a new snapshot for the target date.
    /// </summary>
    /// <param name="targetDate">The date for the new snapshot</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="currencyMovements">The currency-specific movements for calculations</param>
    /// <param name="previousSnapshot">The previous financial snapshot for cumulative calculations</param>
    /// <returns>Task that completes when the new snapshot is calculated and saved</returns>
    let private calculateNewFinancialSnapshot
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        (previousSnapshot: BrokerFinancialSnapshot)
        =
        task {
            // Validate previous snapshot currency matches current currency
            if previousSnapshot.CurrencyId <> currencyId then
                failwithf "Previous snapshot currency (%d) does not match current currency (%d)" 
                    previousSnapshot.CurrencyId currencyId
            
            // Calculate financial metrics from movements
            let calculatedMetrics = calculateFinancialMetricsFromMovements currencyMovements currencyId
            
            // Create new snapshot with previous snapshot as baseline
            do! createCumulativeFinancialSnapshot 
                    targetDate 
                    currencyId 
                    brokerAccountId 
                    brokerAccountSnapshotId 
                    calculatedMetrics 
                    (Some previousSnapshot)
        }
    
    /// <summary>
    /// Updates an existing financial snapshot with new movements while considering previous snapshot values.
    /// This is used for SCENARIO C: when new movements are added to a date that already has a snapshot,
    /// requiring a recalculation that combines previous baseline + all movements (existing + new).
    /// This ensures data consistency during corrections, reprocessing, or late movement additions.
    /// </summary>
    /// <param name="targetDate">The date for the snapshot to update</param>
    /// <param name="currencyId">The currency ID for this snapshot</param>
    /// <param name="brokerAccountId">The broker account ID</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID to associate with</param>
    /// <param name="currencyMovements">The currency-specific movements (includes both new and existing movements)</param>
    /// <param name="previousSnapshot">The previous financial snapshot for baseline calculations</param>
    /// <param name="existingSnapshot">The existing snapshot that needs to be updated</param>
    /// <returns>Task that completes when the snapshot is updated and saved</returns>
    let private updateExistingFinancialSnapshot
        (targetDate: DateTimePattern)
        (currencyId: int)
        (brokerAccountId: int)
        (brokerAccountSnapshotId: int)
        (currencyMovements: CurrencyMovementData)
        (previousSnapshot: BrokerFinancialSnapshot)
        (existingSnapshot: BrokerFinancialSnapshot)
        =
        task {
            // =================================================================
            // VALIDATION AND CONSISTENCY CHECKS
            // =================================================================
            
            // Validate currency consistency across all snapshots
            if previousSnapshot.CurrencyId <> currencyId then
                failwithf "Previous snapshot currency (%d) does not match current currency (%d)" 
                    previousSnapshot.CurrencyId currencyId
            
            if existingSnapshot.CurrencyId <> currencyId then
                failwithf "Existing snapshot currency (%d) does not match current currency (%d)" 
                    existingSnapshot.CurrencyId currencyId
            
            // Validate broker account consistency
            if existingSnapshot.BrokerAccountId <> brokerAccountId then
                failwithf "Existing snapshot broker account (%d) does not match expected account (%d)"
                    existingSnapshot.BrokerAccountId brokerAccountId
            
            // Validate date consistency
            if existingSnapshot.Base.Date <> targetDate then
                failwithf "Existing snapshot date (%A) does not match target date (%A)"
                    existingSnapshot.Base.Date.Value targetDate.Value
            
            // Validate chronological order of snapshots
            if previousSnapshot.Base.Date.Value >= targetDate.Value then
                failwithf "Previous snapshot date (%A) must be before target date (%A)"
                    previousSnapshot.Base.Date.Value targetDate.Value
            
            // =================================================================
            // RECALCULATE FINANCIAL METRICS FROM ALL MOVEMENTS
            // =================================================================
            
            // Calculate financial metrics from ALL movements for this date
            // The currencyMovements parameter should contain both existing and new movements
            // This ensures we don't miss any previously processed movements during the update
            let calculatedMetrics = calculateFinancialMetricsFromMovements currencyMovements currencyId
            
            // Update the existing snapshot using the recalculated metrics
            do! updateExistingSnapshotWithMetrics 
                    existingSnapshot 
                    targetDate 
                    currencyId 
                    calculatedMetrics 
                    previousSnapshot
        }

    /// <summary>
    /// Validates and updates broker account snapshots for a given day, processing all necessary currency movements.
    /// This handles both new movements and adjustments to existing snapshots for accuracy.
    /// </summary>
    /// <param name="brokerAccountSnapshot">The broker account snapshot for the target date</param>
    /// <param name="movementData">The movement data for updating snapshots</param>
    /// <param name="previousSnapshot">The optional previous snapshot for baseline calculations</param>
    /// <returns>Task that completes when the update is finished</returns>
    let brokerAccountOneDayUpdate
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (movementData: BrokerAccountMovementData)
        =
        task {
            // =================================================================
            // PHASE 1: INPUT VALIDATION & SETUP ✅ IMPLEMENTED
            // =================================================================
            
            // 1.1. ✅ Validate input parameters using the reusable validation method
            validateSnapshotAndMovementData brokerAccountSnapshot movementData
            
            // 1.2. ✅ Extract validated parameters for calculations
            let targetDate = brokerAccountSnapshot.Base.Date
            let brokerAccountId = brokerAccountSnapshot.BrokerAccountId
            
            // =================================================================
            // PHASE 2: RETRIEVE PREVIOUS FINANCIAL SNAPSHOTS ✅ IMPLEMENTED
            // =================================================================
            
            // 2.1. ✅ Get all existing financial snapshots for this broker account and target date
            // This handles the case where we might have multiple currency snapshots
            let! existingFinancialSnapshots = 
                BrokerFinancialSnapshotExtensions.Do.getAllByBrokerAccountIdBrokerAccountSnapshotIdAndDate(
                    brokerAccountId, 
                    brokerAccountSnapshot.Base.Id, 
                    targetDate)
            
            // 2.2. ✅ Get all previous financial snapshots for historical continuity
            // We need these as baseline for cumulative calculations (RealizedGains, Invested, etc.)
            // This gets ALL previous snapshots and we'll filter to find the most recent ones per currency
            let! allPreviousFinancialSnapshots = 
                BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerAccountIdGroupedByDate(brokerAccountId)
            
            // 2.3. ✅ Filter and group to get the most recent previous snapshot per currency
            // This finds the actual latest snapshot before target date for each currency,
            // regardless of whether it was yesterday, last week, or months ago
            let relevantPreviousSnapshots = 
                allPreviousFinancialSnapshots
                |> List.filter (fun snap -> snap.Base.Date.Value < targetDate.Value)
                |> List.groupBy (fun snap -> snap.CurrencyId)
                |> List.map (fun (currencyId, snaps) -> 
                    snaps |> List.maxBy (fun s -> s.Base.Date.Value))
            
            // =================================================================
            // PHASE 3: CURRENCY ANALYSIS & SNAPSHOT PLANNING ✅ IMPLEMENTED
            // =================================================================
            
            // 3.1. ✅ Determine which currencies have movements on target date
            let currenciesWithMovements = movementData.UniqueCurrencies
            
            // 3.2. ✅ Determine which currencies had previous snapshots
            let currenciesWithPreviousSnapshots = 
                relevantPreviousSnapshots 
                |> List.map (fun snap -> snap.CurrencyId) 
                |> Set.ofList
            
            // 3.3. ✅ Calculate which currencies need processing (union of movements and historical)
            let allRelevantCurrencies = 
                Set.union currenciesWithMovements currenciesWithPreviousSnapshots
            
            // Note: Financial snapshots are created within the scenario processing loop
            // unlike BrokerAccountSnapshotManager where account snapshots need pre-creation
            // Each scenario handles its own financial snapshot creation as needed
            // Existing snapshots are detected per-currency using List.tryFind during processing
            
            // =================================================================
            // PHASE 4: SCENARIO HANDLING & SNAPSHOT PROCESSING ✅ IMPLEMENTED
            // =================================================================
            
            // ✅ Process each currency that needs attention (framework implemented)
            for currencyId in allRelevantCurrencies do
                
                // 4.1. ✅ Get movement data for this specific currency
                let currencyMovementData = 
                    movementData.MovementsByCurrency.TryFind(currencyId)
                
                // 4.2. ✅ Get previous snapshot for this currency (for cumulative calculations)
                let previousSnapshot = 
                    relevantPreviousSnapshots 
                    |> List.tryFind (fun snap -> snap.CurrencyId = currencyId)
                
                // 4.3. ✅ Get existing snapshot for this currency and date (if any)
                let existingSnapshot = 
                    existingFinancialSnapshots 
                    |> List.tryFind (fun snap -> snap.CurrencyId = currencyId)
                
                // 4.4. ✅ SCENARIO DECISION TREE (framework implemented, calculations implemented)
                match currencyMovementData, previousSnapshot, existingSnapshot with
                
                // SCENARIO A: New movements, has previous snapshot, no existing snapshot
                | Some movements, Some previous, None ->
                    do! calculateNewFinancialSnapshot targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements previous
                
                // SCENARIO B: New movements, no previous snapshot, no existing snapshot  
                | Some movements, None, None ->
                    do! calculateInitialFinancialSnapshot targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements
                
                // SCENARIO C: New movements, has previous snapshot, has existing snapshot
                | Some movements, Some previous, Some existing ->
                    do! updateExistingFinancialSnapshot targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements previous existing

                // SCENARIO D: New movements, no previous snapshot, has existing snapshot
                | Some movements, None, Some existing ->
                    do! calculateDirectSnapshotUpdate targetDate currencyId brokerAccountId brokerAccountSnapshot.Base.Id movements existing
                
                // SCENARIO E: No movements, has previous snapshot, no existing snapshot
                | None, Some previous, None ->
                    // Carry forward previous snapshot values (no activity day)
                    // Create snapshot with same values as previous but new date
                    // 📋 TODO: Implement carry-forward logic from previous snapshot
                    // Scenario E handles passive days where no new movements occur, but a snapshot
                    // for the previous day exists.
                    //
                    // The goal is to create a new snapshot that mirrors the previous day's snapshot,
                    // effectively "freezing" the portfolio's state for the day without any changes.
                    //
                    // Steps to implement:
                    // - Retrieve the previous snapshot for the currency.
                    // - Create a new snapshot using the same values as the previous snapshot, with
                    //   the exception of the date, which should be updated to the target date.
                    // - Save the new snapshot to the database.
                    //
                    // Expected Outcome:
                    // - A new financial snapshot is created with the same values as the previous snapshot,
                    //   indicating no changes in the portfolio for the currency.
                    //
                    // This is useful for maintaining accurate daily snapshots even when no trading
                    // activity occurs.
                    // previous.Base.Date.AddDays(1) |> createBaseSnapshot |> ignore
                    ()
                
                // SCENARIO F: No movements, no previous snapshot, no existing snapshot
                | None, None, None ->
                    // ✅ No action needed - this currency has no history and no activity
                    // Skip processing for this currency
                    ()
                
                // SCENARIO G: No movements, has previous snapshot, has existing snapshot
                | None, Some previous, Some existing ->
                    // Verify existing snapshot matches previous (data consistency check)
                    // 📋 TODO: Implement consistency validation and correction if needed
                    // Scenario G is a consistency check where both a previous snapshot and an existing
                    // snapshot are found for a currency, but no new movements have occurred.
                    //
                    // The existing snapshot should theoretically match the previous snapshot, and this
                    // scenario serves to validate that consistency.
                    //
                    // Steps to implement:
                    // - Compare the existing snapshot with the previous snapshot for the currency.
                    // - If discrepancies are found, investigate and correct the existing snapshot to match
                    //   the previous snapshot.
                    // - This may involve reverting erroneous changes or resetting the snapshot to a known
                    //   good state.
                    //
                    // Expected Outcome:
                    // - The existing snapshot is validated or corrected to ensure it matches the previous
                    //   snapshot.
                    //
                    // This ensures data integrity and consistency within the financial snapshot records.
                    // TODO: Implement validation logic to compare existing and previous snapshots
                    ()
                
                // SCENARIO H: No movements, no previous snapshot, has existing snapshot  
                | None, None, Some existing ->
                    // Existing snapshot should be validated or cleaned up
                    // This might indicate data inconsistency
                    // 📋 TODO: Implement validation and cleanup logic
                    // Scenario H deals with the potential orphaning of snapshots where an existing
                    // snapshot is found without a corresponding previous snapshot or new movements.
                    //
                    // This may occur due to errors in data processing or could indicate that the
                    // snapshot belongs to a now-inactive currency.
                    //
                    // Steps to implement:
                    // - Validate the existing snapshot to ensure it contains all necessary data and
                    //   makes sense in the current context.
                    // - If the snapshot is determined to be orphaned or invalid, remove it or archive
                    //   it as necessary to prevent confusion.
                    //
                    // Expected Outcome:
                    // - The existing snapshot is either validated and kept or cleaned up if found
                    //   unnecessary or erroneous.
                    //
                    // This helps maintain a clean and accurate set of financial snapshots for the
                    // broker accounts.
                    // TODO: Implement logic to validate or remove orphaned snapshots
                    ()
            
            return()
        }

    /// <summary>
    /// Creates a default financial snapshot with zero values for initial setup.
    /// This is used when creating initial snapshots for brokers and accounts with no activity.
    /// </summary>
    /// <param name="snapshotDate">The date for the snapshot</param>
    /// <param name="brokerId">The broker ID (0 for account-level snapshots)</param>
    /// <param name="brokerAccountId">The broker account ID (0 for broker-level snapshots)</param>
    /// <param name="brokerSnapshotId">The broker snapshot ID (0 for account-level snapshots)</param>
    /// <param name="brokerAccountSnapshotId">The broker account snapshot ID (0 for broker-level snapshots)</param>
    /// <returns>Task that completes when the default snapshot is saved</returns>
    let private defaultFinancialSnapshot
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerAccountId: int)
        (brokerSnapshotId: int)
        (brokerAccountSnapshotId: int)
        =
        task {
            let! currencyId = getDefaultCurrency()
            let snapshot = {
                Base = createBaseSnapshot snapshotDate
                BrokerId = brokerId
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = 0
                BrokerSnapshotId = brokerSnapshotId
                BrokerAccountSnapshotId = brokerAccountSnapshotId
                RealizedGains = Money.FromAmount 0m
                RealizedPercentage = 0m
                UnrealizedGains = Money.FromAmount 0m
                UnrealizedGainsPercentage = 0m
                Invested = Money.FromAmount 0m
                Commissions = Money.FromAmount 0m
                Fees = Money.FromAmount 0m
                Deposited = Money.FromAmount 0m
                Withdrawn = Money.FromAmount 0m
                DividendsReceived = Money.FromAmount 0m
                OptionsIncome = Money.FromAmount 0m
                OtherIncome = Money.FromAmount 0m
                OpenTrades = false
            }
            do! snapshot.save()
        }

    /// <summary>
    /// Sets up the initial financial snapshot for a specific broker.
    /// This is used when a new broker is created or when initializing snapshots for existing brokers.
    /// </summary>
    let setupInitialFinancialSnapshotForBroker
        (snapshotDate: DateTimePattern)
        (brokerId: int)
        (brokerSnapshotId: int)
        =
        task {
            do! defaultFinancialSnapshot 
                    snapshotDate
                    brokerId
                    0 // BrokerAccountId set to 0 for broker-level snapshots
                    brokerSnapshotId
                    0 // BrokerAccountSnapshotId set to 0 for broker-level snapshots
        }

    /// <summary>
    /// Sets up the initial financial snapshot for a specific broker account.
    /// This is used when a new broker account is created or when initializing snapshots for existing accounts.
    /// Creates a single financial snapshot using the default currency, which is appropriate for initial setup
    /// since no movements exist yet. Multi-currency snapshots will be created later as movements are processed.
    /// </summary>
    let setupInitialFinancialSnapshotForBrokerAccount
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        =
        task {
            // Create initial snapshot with default currency
            // Multi-currency snapshots will be handled during movement processing
            // when brokerAccountOneDayUpdate and brokerAccountCascadeUpdate are implemented
            do! defaultFinancialSnapshot 
                    brokerAccountSnapshot.Base.Date
                    0 // BrokerId set to 0 for account-level snapshots
                    brokerAccountSnapshot.BrokerAccountId
                    0 // BrokerSnapshotId set to 0 for account-level snapshots
                    brokerAccountSnapshot.Base.Id // Use the snapshot's own ID as BrokerAccountSnapshotId
        }
    
    let brokerAccountCascadeUpdate
        (currentBrokerAccountSnapshot: BrokerAccountSnapshot)
        (snapshotsToUpdate: BrokerAccountSnapshot list)
        (movementData: BrokerAccountMovementData)
        =
        task {
            // Validate input parameters using the reusable validation method
            validateSnapshotAndMovementData currentBrokerAccountSnapshot movementData
            
            //TODO: Use the movement data for cascade calculations
            // Now has access to movementData.BrokerMovements, movementData.Trades, etc.
            // for cascade calculations across multiple snapshots without additional database calls
            return()
        }

    /// <summary>
    /// Handles changes to existing broker accounts, updating snapshots using a previous date.
    /// This function is used for one-day updates where the previous snapshot is used to calculate changes.
    /// </summary>
    let brokerAccountOneDayWithPrevious
        (brokerAccountSnapshot: BrokerAccountSnapshot)
        (previousSnapshot: BrokerAccountSnapshot)
        =
        task {
            //TODO
            return()
        }