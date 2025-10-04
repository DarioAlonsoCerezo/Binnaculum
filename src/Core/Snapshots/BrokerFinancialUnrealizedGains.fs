namespace Binnaculum.Core.Storage

open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging

module BrokerFinancialUnrealizedGains =

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
    let internal calculateUnrealizedGains
        (currentPositions: Map<int, decimal>)
        (costBasisInfo: Map<int, decimal>)
        (targetDate: DateTimePattern)
        (targetCurrencyId: int)
        =
        task {
            System.Diagnostics.Debug.WriteLine(
                sprintf
                    "[BrokerFinancialUnrealizedGains] Starting calculation - PositionCount:%d CostBasisCount:%d TargetDate:%s CurrencyId:%d"
                    currentPositions.Count
                    costBasisInfo.Count
                    (targetDate.Value.ToString("yyyy-MM-dd"))
                    targetCurrencyId
            )

            let mutable totalMarketValue = 0m
            let mutable totalCostBasis = 0m

            // Process each ticker with current positions
            for KeyValue(tickerId, quantity) in currentPositions do
                // Only process if we have non-zero positions
                if quantity <> 0m then
                    System.Diagnostics.Debug.WriteLine(
                        sprintf
                            "[BrokerFinancialUnrealizedGains] Processing ticker %d with quantity %M"
                            tickerId
                            quantity
                    )

                    // ✅ CURRENCY-SAFE: Get market price for this ticker on the target date in the correct currency
                    // This ensures that market price and cost basis are in the same currency for accurate comparison
                    let! marketPrice =
                        TickerPriceExtensions.Do.getPriceByDateOrPreviousAndCurrencyId (
                            tickerId,
                            targetCurrencyId,
                            targetDate.Value.ToString("yyyy-MM-dd")
                        )

                    // Get cost basis per share for this ticker (already in target currency from trade calculations)
                    let costBasisPerShare = costBasisInfo.TryFind(tickerId) |> Option.defaultValue 0m

                    // Calculate market value and cost basis for this position
                    let positionMarketValue = marketPrice * abs (quantity) // Use abs() to handle both long and short positions
                    let positionCostBasis = costBasisPerShare * abs (quantity)

                    System.Diagnostics.Debug.WriteLine(
                        sprintf
                            "[BrokerFinancialUnrealizedGains]   MarketPrice:%M CostBasisPerShare:%M MarketValue:%M CostBasis:%M"
                            marketPrice
                            costBasisPerShare
                            positionMarketValue
                            positionCostBasis
                    )

                    // For short positions, the unrealized gain/loss calculation is inverted
                    if quantity > 0m then
                        // Long position: gain when market price > cost basis
                        totalMarketValue <- totalMarketValue + positionMarketValue
                        totalCostBasis <- totalCostBasis + positionCostBasis
                    else
                        // Short position: gain when market price < cost basis (we sold high, can buy back low)
                        totalMarketValue <- totalMarketValue - positionMarketValue // Negative market value for shorts
                        totalCostBasis <- totalCostBasis - positionCostBasis // Negative cost basis for shorts

            // Calculate total unrealized gains: Market Value - Cost Basis
            let unrealizedGains = totalMarketValue - totalCostBasis

            // Calculate unrealized gains percentage
            let unrealizedGainsPercentage =
                if totalCostBasis <> 0m then
                    (unrealizedGains / abs (totalCostBasis)) * 100m // Use abs() for percentage calculation
                else
                    0m

            System.Diagnostics.Debug.WriteLine(
                sprintf
                    "[BrokerFinancialUnrealizedGains] Completed calculation - TotalMarketValue:%M TotalCostBasis:%M UnrealizedGains:%M Unrealized%%:%M"
                    totalMarketValue
                    totalCostBasis
                    unrealizedGains
                    unrealizedGainsPercentage
            )

            return (Money.FromAmount unrealizedGains, unrealizedGainsPercentage)
        }
