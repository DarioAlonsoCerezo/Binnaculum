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
            // =================================================================
            // BROKER MOVEMENTS CALCULATION
            // =================================================================
            // Process broker movements (deposits, withdrawals, fees, conversions, etc.)
            let mutable deposited = 0m
            let mutable withdrawn = 0m  
            let mutable fees = 0m
            let mutable commissions = 0m
            let mutable otherIncome = 0m
            let mutable interestPaid = 0m
            let mutable conversionImpact = 0m
            
            for movement in currencyMovements.BrokerMovements do
                let movementType = movement.MovementType
                if movementType = BrokerMovementType.Deposit then
                    deposited <- deposited + movement.Amount.Value
                elif movementType = BrokerMovementType.Withdrawal then
                    withdrawn <- withdrawn + movement.Amount.Value
                elif movementType = BrokerMovementType.Fee then
                    fees <- fees + movement.Amount.Value
                elif movementType = BrokerMovementType.InterestsGained then
                    otherIncome <- otherIncome + movement.Amount.Value
                elif movementType = BrokerMovementType.InterestsPaid then
                    interestPaid <- interestPaid + movement.Amount.Value
                elif movementType = BrokerMovementType.Conversion then
                    // For conversions, we need to check if this currency gained or lost money
                    if movement.CurrencyId = currencyId then
                        // This currency received money (positive impact)
                        conversionImpact <- conversionImpact + movement.Amount.Value
                    match movement.FromCurrencyId with
                    | Some fromCurrencyId when fromCurrencyId = currencyId ->
                        // This currency lost money (negative impact)
                        match movement.AmountChanged with
                        | Some amountChanged -> conversionImpact <- conversionImpact - amountChanged.Value
                        | None -> ()
                    | _ -> ()
                else
                    // Handle other broker movement types as needed
                    otherIncome <- otherIncome + movement.Amount.Value
                    
                // Add movement commissions and fees to totals
                commissions <- commissions + movement.Commissions.Value
                fees <- fees + movement.Fees.Value
            
            // Apply conversion impact to appropriate categories
            let (adjustedDeposited, adjustedWithdrawn) = 
                if conversionImpact >= 0m then
                    // Money was gained in this currency
                    (deposited + conversionImpact, withdrawn)
                else
                    // Money was lost from this currency
                    (deposited, withdrawn + abs(conversionImpact))
            
            // =================================================================
            // TRADES CALCULATION
            // =================================================================
            let mutable invested = 0m
            let mutable tradeCommissions = 0m
            let mutable tradeFees = 0m
            let mutable realizedGains = 0m
            let mutable hasOpenTrades = false
            let mutable currentPositions = Map.empty<int, decimal>
            let mutable costBasisInfo = Map.empty<int, decimal>
            
            // Group trades by ticker for position tracking
            let tradesByTicker = currencyMovements.Trades |> List.groupBy (fun t -> t.TickerId)
            
            for (tickerId, trades) in tradesByTicker do
                let mutable position = 0m
                let mutable totalInvested = 0m
                let mutable totalShares = 0m
                
                for trade in trades do
                    tradeCommissions <- tradeCommissions + trade.Commissions.Value
                    tradeFees <- tradeFees + trade.Fees.Value
                    
                    let code = trade.TradeCode
                    if code = TradeCode.BuyToOpen then
                        position <- position + trade.Quantity
                        totalInvested <- totalInvested + (trade.Quantity * trade.Price.Value)
                        totalShares <- totalShares + trade.Quantity
                        invested <- invested + (trade.Quantity * trade.Price.Value)
                    elif code = TradeCode.SellToClose then
                        let soldQuantity = min trade.Quantity position
                        position <- position - soldQuantity
                        // Calculate realized gains for sold portion
                        if totalShares > 0m then
                            let avgCostBasis = totalInvested / totalShares
                            let soldProceeds = soldQuantity * trade.Price.Value
                            let soldCostBasis = soldQuantity * avgCostBasis
                            realizedGains <- realizedGains + (soldProceeds - soldCostBasis)
                            
                            // Update remaining cost basis
                            totalInvested <- totalInvested - soldCostBasis
                            totalShares <- totalShares - soldQuantity
                    elif code = TradeCode.SellToOpen then
                        // Short selling - negative position
                        position <- position - trade.Quantity
                        totalInvested <- totalInvested + (trade.Quantity * trade.Price.Value)
                        invested <- invested + (trade.Quantity * trade.Price.Value)
                    elif code = TradeCode.BuyToClose then
                        // Closing short position
                        let coveredQuantity = min trade.Quantity (abs position)
                        position <- position + coveredQuantity
                        // Calculate realized gains for covered portion
                        if position < 0m then
                            let avgShortPrice = totalInvested / abs(position + coveredQuantity)
                            let coveringCost = coveredQuantity * trade.Price.Value
                            let shortProceeds = coveredQuantity * avgShortPrice
                            realizedGains <- realizedGains + (shortProceeds - coveringCost)
                
                // Track current positions and cost basis
                if position <> 0m then
                    hasOpenTrades <- true
                    currentPositions <- currentPositions.Add(tickerId, position)
                    if totalShares > 0m then
                        let avgCostBasis = totalInvested / totalShares
                        costBasisInfo <- costBasisInfo.Add(tickerId, avgCostBasis)
            
            // =================================================================
            // DIVIDENDS CALCULATION
            // =================================================================
            let mutable dividendIncome = 0m
            
            for dividend in currencyMovements.Dividends do
                dividendIncome <- dividendIncome + dividend.DividendAmount.Value
            
            // =================================================================
            // DIVIDEND TAXES CALCULATION
            // =================================================================
            let mutable dividendTaxWithheld = 0m
            
            for dividendTax in currencyMovements.DividendTaxes do
                dividendTaxWithheld <- dividendTaxWithheld + dividendTax.DividendTaxAmount.Value
            
            // Calculate net dividend income after taxes
            let netDividendIncome = dividendIncome - dividendTaxWithheld
            
            // =================================================================
            // OPTIONS CALCULATION
            // =================================================================
            let mutable optionsIncome = 0m
            let mutable optionsInvestment = 0m
            let mutable optionsCommissions = 0m
            let mutable optionsFees = 0m
            let mutable optionsRealizedGains = 0m
            let mutable hasOpenOptions = false
            
            for optionTrade in currencyMovements.OptionTrades do
                optionsCommissions <- optionsCommissions + optionTrade.Commissions.Value
                optionsFees <- optionsFees + optionTrade.Fees.Value
                
                let code = optionTrade.Code
                if code = OptionCode.SellToOpen then
                    // Selling options generates income
                    optionsIncome <- optionsIncome + optionTrade.NetPremium.Value
                elif code = OptionCode.BuyToOpen then
                    // Buying options is an investment
                    optionsInvestment <- optionsInvestment + abs(optionTrade.NetPremium.Value)
                elif code = OptionCode.BuyToClose || code = OptionCode.SellToClose then
                    // Closing positions affect realized gains
                    optionsRealizedGains <- optionsRealizedGains + optionTrade.NetPremium.Value
                
                // Check if option is still open (not expired)
                if optionTrade.ExpirationDate.Value > targetDate.Value then
                    hasOpenOptions <- true
            
            // =================================================================
            // CALCULATE TOTALS AND METRICS
            // =================================================================
            let totalDeposited = Money.FromAmount adjustedDeposited
            let totalWithdrawn = Money.FromAmount adjustedWithdrawn
            let totalFees = Money.FromAmount (fees + tradeFees + optionsFees)
            let totalCommissions = Money.FromAmount (commissions + tradeCommissions + optionsCommissions)
            let totalOtherIncome = Money.FromAmount (otherIncome - interestPaid)
            let totalInvested = Money.FromAmount (invested + optionsInvestment)
            let totalRealizedGains = Money.FromAmount (realizedGains + optionsRealizedGains)
            let totalDividendsReceived = Money.FromAmount netDividendIncome
            let totalOptionsIncome = Money.FromAmount optionsIncome
            
            // Calculate movement counter
            let movementCounter = currencyMovements.TotalCount
            
            // Calculate unrealized gains from current positions
            let! (unrealizedGains, unrealizedGainsPercentage) = 
                calculateUnrealizedGains currentPositions costBasisInfo targetDate currencyId
            
            // Calculate realized percentage return
            let realizedPercentage = 
                if totalInvested.Value > 0m then
                    (totalRealizedGains.Value / totalInvested.Value) * 100m
                else 
                    0m
            
            // =================================================================
            // CREATE AND SAVE SNAPSHOT
            // =================================================================
            let newSnapshot = {
                Base = createBaseSnapshot targetDate
                BrokerId = 0 // Set to 0 for account-level snapshots
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = movementCounter
                BrokerSnapshotId = 0 // Set to 0 for account-level snapshots
                BrokerAccountSnapshotId = brokerAccountSnapshotId
                RealizedGains = totalRealizedGains
                RealizedPercentage = realizedPercentage
                UnrealizedGains = unrealizedGains
                UnrealizedGainsPercentage = unrealizedGainsPercentage
                Invested = totalInvested
                Commissions = totalCommissions
                Fees = totalFees
                Deposited = totalDeposited
                Withdrawn = totalWithdrawn
                DividendsReceived = totalDividendsReceived
                OptionsIncome = totalOptionsIncome
                OtherIncome = totalOtherIncome
                OpenTrades = hasOpenTrades || hasOpenOptions
            }
            
            // Save the snapshot to database
            do! newSnapshot.save()
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
            // Extract previous snapshot values as baseline for cumulative calculations
            // We need these totals to build upon for the new snapshot date
            let previousRealizedGains = previousSnapshot.RealizedGains
            let previousInvested = previousSnapshot.Invested
            let previousDeposited = previousSnapshot.Deposited
            let previousWithdrawn = previousSnapshot.Withdrawn
            let previousDividendsReceived = previousSnapshot.DividendsReceived
            let previousOptionsIncome = previousSnapshot.OptionsIncome
            let previousOtherIncome = previousSnapshot.OtherIncome
            let previousCommissions = previousSnapshot.Commissions
            let previousFees = previousSnapshot.Fees
            let previousMovementCounter = previousSnapshot.MovementCounter
            
            // Validate previous snapshot currency matches current currency
            if previousSnapshot.CurrencyId <> currencyId then
                failwithf "Previous snapshot currency (%d) does not match current currency (%d)" 
                    previousSnapshot.CurrencyId currencyId
            
            // Process broker movements using the new extension methods
            // These calculations provide clean, reusable financial aggregations
            let brokerMovementSummary = 
                currencyMovements.BrokerMovements
                |> FinancialCalculations.calculateFinancialSummary
            
            // Extract calculated values from broker movements
            let currentDeposited = brokerMovementSummary.TotalDeposited
            let currentWithdrawn = brokerMovementSummary.TotalWithdrawn
            let currentFees = brokerMovementSummary.TotalFees
            let currentCommissions = brokerMovementSummary.TotalCommissions
            let currentOtherIncome = brokerMovementSummary.TotalOtherIncome
            let currentInterestPaid = brokerMovementSummary.TotalInterestPaid
            let brokerMovementCount = brokerMovementSummary.MovementCount
            
            // Calculate currency conversion impact for this specific currency
            // This accounts for money gained (positive) or lost (negative) through conversions
            // Example: Converting $1000 USD to €900 EUR:
            // - USD currency: conversionImpact = -$1000 (money lost)
            // - EUR currency: conversionImpact = +€900 (money gained)
            let conversionImpact = 
                currencyMovements.BrokerMovements
                |> fun movements -> FinancialCalculations.calculateConversionImpact(movements, currencyId)
            
            // Process trade movements for investment tracking and realized gains calculation
            // Trades directly impact invested amounts, commissions, and realized gains when positions are closed
            let tradingSummary = 
                currencyMovements.Trades
                |> TradeCalculations.calculateTradingSummary
            
            // Extract calculated values from trades
            let currentInvested = tradingSummary.TotalInvested
            let currentTradeCommissions = tradingSummary.TotalCommissions
            let currentTradeFees = tradingSummary.TotalFees
            let currentRealizedGains = tradingSummary.RealizedGains
            let hasOpenTrades = tradingSummary.HasOpenPositions
            let currentPositions = tradingSummary.CurrentPositions
            let costBasisInfo = tradingSummary.CostBasis
            let tradeCount = tradingSummary.TradeCount
            
            // Calculate currency conversion impact for trade-related cash flows
            // Similar to broker movements, we need to account for any gains or losses due to currency conversion
            
            // Process dividend income from holdings
            // Dividends provide additional income beyond trading gains
            let dividendSummary = 
                currencyMovements.Dividends
                |> DividendCalculations.calculateDividendSummary
            
            // Process dividend tax withholdings
            // Tax withholdings reduce the net dividend income received
            let dividendTaxSummary = 
                currencyMovements.DividendTaxes
                |> DividendTaxCalculations.calculateDividendTaxSummary
            
            // Extract calculated values from dividends and taxes
            let currentDividendIncome = dividendSummary.TotalDividendIncome
            let currentTaxWithheld = dividendTaxSummary.TotalTaxWithheld
            let dividendCount = dividendSummary.DividendCount
            let dividendTaxCount = dividendTaxSummary.TaxEventCount
            
            // Calculate net dividend income after tax withholdings
            // This represents the actual cash received after all tax deductions
            // Formula: Net Income = Gross Dividends - Tax Withholdings
            let netDividendIncome = Money.FromAmount (currentDividendIncome.Value - currentTaxWithheld.Value)
            
            // Process options trading for premium income and options-related costs
            // Options can generate income (selling) or costs (buying) and affect realized gains
            let optionsSummary = 
                currencyMovements.OptionTrades
                |> OptionTradeCalculations.calculateOptionsSummary
            
            // Extract calculated values from options trading
            let currentOptionsIncome = optionsSummary.OptionsIncome
            let currentOptionsInvestment = optionsSummary.OptionsInvestment
            let netOptionsIncome = optionsSummary.NetOptionsIncome
            let currentOptionsCommissions = optionsSummary.TotalCommissions
            let currentOptionsFees = optionsSummary.TotalFees
            let currentOptionsRealizedGains = optionsSummary.RealizedGains
            let hasOpenOptions = optionsSummary.HasOpenOptions
            let currentOptionPositions = optionsSummary.OpenPositions
            let optionsTradeCount = optionsSummary.TradeCount
            
            // Calculate cumulative values by combining previous snapshot with current movements
            // Most financial metrics are cumulative and build upon previous totals
            
            // Apply conversion impact to appropriate categories based on whether money was gained or lost
            let (adjustedDeposited, adjustedWithdrawn) = 
                if conversionImpact.Value >= 0m then
                    // Positive conversion impact: money was gained in this currency (e.g., USD -> EUR conversion)
                    (currentDeposited.Value + conversionImpact.Value, currentWithdrawn.Value)
                else
                    // Negative conversion impact: money was lost from this currency (e.g., EUR -> USD conversion)
                    (currentDeposited.Value, currentWithdrawn.Value + abs(conversionImpact.Value))
            
            let newDeposited = Money.FromAmount (previousDeposited.Value + adjustedDeposited)
            let newWithdrawn = Money.FromAmount (previousWithdrawn.Value + adjustedWithdrawn)
            let newCommissions = Money.FromAmount (previousCommissions.Value + currentCommissions.Value + currentTradeCommissions.Value + currentOptionsCommissions.Value)
            let newFees = Money.FromAmount (previousFees.Value + currentFees.Value + currentTradeFees.Value + currentOptionsFees.Value)
            let newOtherIncome = Money.FromAmount (previousOtherIncome.Value + currentOtherIncome.Value)
            let newInvested = Money.FromAmount (previousInvested.Value + currentInvested.Value + currentOptionsInvestment.Value)
            let newRealizedGains = Money.FromAmount (previousRealizedGains.Value + currentRealizedGains.Value + currentOptionsRealizedGains.Value)
            let newDividendsReceived = Money.FromAmount (previousDividendsReceived.Value + netDividendIncome.Value)
            let newOptionsIncome = Money.FromAmount (previousOptionsIncome.Value + currentOptionsIncome.Value)
            
            // Handle interest paid (typically reduces other income or increases fees)
            let adjustedOtherIncome = Money.FromAmount (newOtherIncome.Value - currentInterestPaid.Value)
            
            // Update movement counter to track activity level over time
            let newMovementCounter = previousMovementCounter + brokerMovementCount + tradeCount + dividendCount + dividendTaxCount + optionsTradeCount
            // All major movement types are now included in the activity counter
            
            // ✅ IMPLEMENTED: Calculate unrealized gains which requires current market prices and position tracking
            // This is complex as it requires knowing current positions and market values
            // Note: currentPositions and costBasisInfo are already currency-filtered from currencyMovements.Trades
            let! (unrealizedGains, unrealizedGainsPercentage) = 
                calculateUnrealizedGains currentPositions costBasisInfo targetDate currencyId
            
            // Calculate percentage metrics for performance analysis
            // Performance percentages help evaluate investment success over time
            
            // Calculate realized percentage return on invested capital
            let realizedPercentage = 
                if newInvested.Value > 0m then
                    (newRealizedGains.Value / newInvested.Value) * 100m
                else 
                    0m
            
            // Create the final BrokerFinancialSnapshot with all calculated values
            // This snapshot represents the complete financial state for this currency on the target date
            let newSnapshot = {
                Base = createBaseSnapshot targetDate
                BrokerId = 0 // Set to 0 for account-level snapshots
                BrokerAccountId = brokerAccountId
                CurrencyId = currencyId
                MovementCounter = newMovementCounter
                BrokerSnapshotId = 0 // Set to 0 for account-level snapshots
                BrokerAccountSnapshotId = brokerAccountSnapshotId
                RealizedGains = newRealizedGains
                RealizedPercentage = realizedPercentage
                UnrealizedGains = unrealizedGains // ✅ Now calculated from market prices
                UnrealizedGainsPercentage = unrealizedGainsPercentage // ✅ Now calculated based on unrealized gains / cost basis
                Invested = newInvested
                Commissions = newCommissions
                Fees = newFees
                Deposited = newDeposited
                Withdrawn = newWithdrawn
                DividendsReceived = newDividendsReceived
                OptionsIncome = newOptionsIncome
                OtherIncome = adjustedOtherIncome
                OpenTrades = hasOpenTrades || hasOpenOptions
            }
            
            // Save the snapshot to database with proper error handling
            do! newSnapshot.save()
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
            // RETRIEVE ALL MOVEMENTS FOR TARGET DATE
            // =================================================================
            
            // For accurate recalculation, we need ALL movements from the movement data
            // The currencyMovements parameter should contain both existing and new movements
            // This ensures we don't miss any previously processed movements during the update
            
            // Process broker movements (deposits, withdrawals, fees, conversions, etc.)
            let mutable deposited = 0m
            let mutable withdrawn = 0m  
            let mutable fees = 0m
            let mutable commissions = 0m
            let mutable otherIncome = 0m
            let mutable interestPaid = 0m
            let mutable conversionImpact = 0m
            
            // Process all broker movements for accurate recalculation
            for movement in currencyMovements.BrokerMovements do
                let movementType = movement.MovementType
                if movementType = BrokerMovementType.Deposit then
                    deposited <- deposited + movement.Amount.Value
                elif movementType = BrokerMovementType.Withdrawal then
                    withdrawn <- withdrawn + movement.Amount.Value
                elif movementType = BrokerMovementType.Fee then
                    fees <- fees + movement.Amount.Value
                elif movementType = BrokerMovementType.InterestsGained then
                    otherIncome <- otherIncome + movement.Amount.Value
                elif movementType = BrokerMovementType.InterestsPaid then
                    interestPaid <- interestPaid + movement.Amount.Value
                elif movementType = BrokerMovementType.Conversion then
                    // Handle currency conversion impacts
                    if movement.CurrencyId = currencyId then
                        conversionImpact <- conversionImpact + movement.Amount.Value
                    match movement.FromCurrencyId with
                    | Some fromCurrencyId when fromCurrencyId = currencyId ->
                        match movement.AmountChanged with
                        | Some amountChanged -> conversionImpact <- conversionImpact - amountChanged.Value
                        | None -> ()
                    | _ -> ()
                else
                    otherIncome <- otherIncome + movement.Amount.Value
                
                commissions <- commissions + movement.Commissions.Value
                fees <- fees + movement.Fees.Value
            
            // Apply conversion impact to deposits/withdrawals
            let (currentDeposited, currentWithdrawn) = 
                if conversionImpact >= 0m then
                    (deposited + conversionImpact, withdrawn)
                else
                    (deposited, withdrawn + abs(conversionImpact))
            
            // =================================================================
            // TRADES RECALCULATION
            // =================================================================
            let mutable invested = 0m
            let mutable tradeCommissions = 0m
            let mutable tradeFees = 0m
            let mutable realizedGains = 0m
            let mutable hasOpenTrades = false
            let mutable currentPositions = Map.empty<int, decimal>
            let mutable costBasisInfo = Map.empty<int, decimal>
            
            // Recalculate all trade positions and realized gains from scratch
            let tradesByTicker = currencyMovements.Trades |> List.groupBy (fun t -> t.TickerId)
            
            for (tickerId, trades) in tradesByTicker do
                let mutable position = 0m
                let mutable totalInvested = 0m
                let mutable totalShares = 0m
                
                for trade in trades do
                    tradeCommissions <- tradeCommissions + trade.Commissions.Value
                    tradeFees <- tradeFees + trade.Fees.Value
                    
                    let code = trade.TradeCode
                    if code = TradeCode.BuyToOpen then
                        position <- position + trade.Quantity
                        totalInvested <- totalInvested + (trade.Quantity * trade.Price.Value)
                        totalShares <- totalShares + trade.Quantity
                        invested <- invested + (trade.Quantity * trade.Price.Value)
                    elif code = TradeCode.SellToClose then
                        let soldQuantity = min trade.Quantity position
                        position <- position - soldQuantity
                        if totalShares > 0m then
                            let avgCostBasis = totalInvested / totalShares
                            let soldProceeds = soldQuantity * trade.Price.Value
                            let soldCostBasis = soldQuantity * avgCostBasis
                            realizedGains <- realizedGains + (soldProceeds - soldCostBasis)
                            totalInvested <- totalInvested - soldCostBasis
                            totalShares <- totalShares - soldQuantity
                    elif code = TradeCode.SellToOpen then
                        position <- position - trade.Quantity
                        totalInvested <- totalInvested + (trade.Quantity * trade.Price.Value)
                        invested <- invested + (trade.Quantity * trade.Price.Value)
                    elif code = TradeCode.BuyToClose then
                        let coveredQuantity = min trade.Quantity (abs position)
                        position <- position + coveredQuantity
                        if position < 0m then
                            let avgShortPrice = totalInvested / abs(position + coveredQuantity)
                            let coveringCost = coveredQuantity * trade.Price.Value
                            let shortProceeds = coveredQuantity * avgShortPrice
                            realizedGains <- realizedGains + (shortProceeds - coveringCost)
                
                // Track current positions and cost basis
                if position <> 0m then
                    hasOpenTrades <- true
                    currentPositions <- currentPositions.Add(tickerId, position)
                    if totalShares > 0m then
                        let avgCostBasis = totalInvested / totalShares
                        costBasisInfo <- costBasisInfo.Add(tickerId, avgCostBasis)
            
            // =================================================================
            // DIVIDENDS AND OPTIONS RECALCULATION
            // =================================================================
            
            // Recalculate dividend income
            let mutable dividendIncome = 0m
            for dividend in currencyMovements.Dividends do
                dividendIncome <- dividendIncome + dividend.DividendAmount.Value
            
            // Recalculate dividend taxes
            let mutable dividendTaxWithheld = 0m
            for dividendTax in currencyMovements.DividendTaxes do
                dividendTaxWithheld <- dividendTaxWithheld + dividendTax.DividendTaxAmount.Value
            
            let netDividendIncome = dividendIncome - dividendTaxWithheld
            
            // Recalculate options trading
            let mutable optionsIncome = 0m
            let mutable optionsInvestment = 0m
            let mutable optionsCommissions = 0m
            let mutable optionsFees = 0m
            let mutable optionsRealizedGains = 0m
            let mutable hasOpenOptions = false
            
            for optionTrade in currencyMovements.OptionTrades do
                optionsCommissions <- optionsCommissions + optionTrade.Commissions.Value
                optionsFees <- optionsFees + optionTrade.Fees.Value
                
                let code = optionTrade.Code
                if code = OptionCode.SellToOpen then
                    optionsIncome <- optionsIncome + optionTrade.NetPremium.Value
                elif code = OptionCode.BuyToOpen then
                    optionsInvestment <- optionsInvestment + abs(optionTrade.NetPremium.Value)
                elif code = OptionCode.BuyToClose || code = OptionCode.SellToClose then
                    optionsRealizedGains <- optionsRealizedGains + optionTrade.NetPremium.Value
                
                if optionTrade.ExpirationDate.Value > targetDate.Value then
                    hasOpenOptions <- true
            
            // =================================================================
            // CALCULATE CUMULATIVE TOTALS WITH PREVIOUS SNAPSHOT BASELINE
            // =================================================================
            
            // Combine recalculated current period values with previous snapshot baseline
            // This ensures cumulative metrics are accurate after the update
            let newDeposited = Money.FromAmount (previousSnapshot.Deposited.Value + currentDeposited)
            let newWithdrawn = Money.FromAmount (previousSnapshot.Withdrawn.Value + currentWithdrawn)
            let newCommissions = Money.FromAmount (previousSnapshot.Commissions.Value + commissions + tradeCommissions + optionsCommissions)
            let newFees = Money.FromAmount (previousSnapshot.Fees.Value + fees + tradeFees + optionsFees)
            let newOtherIncome = Money.FromAmount (previousSnapshot.OtherIncome.Value + otherIncome - interestPaid)
            let newInvested = Money.FromAmount (previousSnapshot.Invested.Value + invested + optionsInvestment)
            let newRealizedGains = Money.FromAmount (previousSnapshot.RealizedGains.Value + realizedGains + optionsRealizedGains)
            let newDividendsReceived = Money.FromAmount (previousSnapshot.DividendsReceived.Value + netDividendIncome)
            let newOptionsIncome = Money.FromAmount (previousSnapshot.OptionsIncome.Value + optionsIncome)
            
            // Calculate movement counter (total movements including this update)
            let newMovementCounter = previousSnapshot.MovementCounter + currencyMovements.TotalCount
            
            // Calculate unrealized gains from current positions
            let! (unrealizedGains, unrealizedGainsPercentage) = 
                calculateUnrealizedGains currentPositions costBasisInfo targetDate currencyId
            
            // Calculate realized percentage return
            let realizedPercentage = 
                if newInvested.Value > 0m then
                    (newRealizedGains.Value / newInvested.Value) * 100m
                else 
                    0m
            
            // =================================================================
            // UPDATE EXISTING SNAPSHOT
            // =================================================================
            
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
                    OpenTrades = hasOpenTrades || hasOpenOptions
            }
            
            // Save the updated snapshot to database
            do! updatedSnapshot.save()
            
            // Optional: Log the update for monitoring and debugging
            // This helps track when snapshots are recalculated due to new movements
            // System.Diagnostics.Debug.WriteLine($"Updated financial snapshot for currency {currencyId} on {targetDate.Value:yyyy-MM-dd}")
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
            // PHASE 4: SCENARIO HANDLING & SNAPSHOT PROCESSING 📋 TODO
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
                
                // 4.4. ✅ SCENARIO DECISION TREE (framework implemented, calculations pending)
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
                    // Update existing snapshot with new movements (rare edge case)
                    // 📋 TODO: Implement update logic using movements and existing snapshot
                    // Scenario D is an edge case where new movements are available for a currency
                    // that already has an existing snapshot, but no previous snapshot is found.
                    //
                    // This may occur in situations where movements are reprocessed or adjusted
                    // without a corresponding update to the previous snapshot.
                    //
                    // Steps to implement:
                    // - Retrieve the existing snapshot for the currency and the new movements data.
                    // - Update the existing snapshot's metrics (e.g., Deposited, Withdrawn) based on
                    //   the new movements.
                    // - Since there's no previous snapshot to compare against, ensure that all metric
                    //   updates are based solely on the new movement data.
                    // - Save the updated snapshot to the database.
                    //
                    // This scenario is relatively simple as it mainly involves updating the existing
                    // snapshot with the new data without the need for complex calculations or merges.
                    //
                    // Expected Outcome:
                    // - The existing financial snapshot is updated with the new movement data,
                    //   accurately reflecting the latest broker account state.
                    //
                    // This ensures that the financial information is current, although the absence of
                    // a previous snapshot may limit some historical calculations.
                    // TODO: Implement logic for updating existing snapshot with new movements only
                    ()
                
                // SCENARIO E: No movements, has previous snapshot, no existing snapshot
                | None, Some previous, None ->
                    // Carry forward previous snapshot values (no activity day)
                    // Create snapshot with same values as previous day but new date
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
                    // This scenario is straightforward as it primarily involves duplicating the
                    // previous snapshot with a new date.
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
            
            // =================================================================
            // PHASE 5: FINANCIAL CALCULATION HELPERS 📋 TODO
            // =================================================================
            
            // 📋 5.1. Calculate cumulative values (RealizedGains, Invested, etc.)
            // These should accumulate from previous snapshot + current movements
            
            // 📋 5.2. Calculate period-specific values (Commissions, Fees for this date)
            // These should sum only movements from current date
            
            // 📋 5.3. Calculate portfolio metrics (UnrealizedGains, OpenTrades status)
            // These require current positions and market prices
            
            // 📋 5.4. Update MovementCounter (increment from previous or set based on activity)
            
            // =================================================================
            // PHASE 6: EDGE CASE HANDLING 📋 TODO
            // =================================================================
            
            // 📋 6.1. Handle currency conversion movements (multi-currency impact)
            // A single conversion movement affects two currencies simultaneously
            
            // 📋 6.2. Handle ACAT transfers (securities and money transfers)
            // These may not have direct cash impact but affect positions
            
            // 📋 6.3. Handle option exercises and assignments
            // These create complex position and cash flow changes
            
            // 📋 6.4. Handle dividend tax withholdings
            // These are negative cash flows that need proper categorization
            
            // 📋 6.5. Handle corporate actions (splits, mergers, spinoffs)
            // These affect position quantities and cost basis
            
            // =================================================================
            // PHASE 7: DATA VALIDATION & PERSISTENCE 📋 TODO
            // =================================================================
            
            // 📋 7.1. Validate all calculated snapshot values for consistency
            // - Ensure monetary values are reasonable
            // - Validate percentages are within expected ranges
            // - Check that cumulative values increase monotonically (where applicable)
            
            // 📋 7.2. Save all new/updated financial snapshots to database
            // Use batch operations if multiple currencies were processed
            
            // 📋 7.3. Log processing summary for monitoring and debugging
            // Include counts of processed currencies and any edge cases encountered
            
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