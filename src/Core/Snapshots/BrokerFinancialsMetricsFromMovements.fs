namespace Binnaculum.Core.Storage

open BrokerMovementExtensions
open TradeExtensions
open DividendExtensions
open DividendTaxExtensions
open OptionTradeExtensions
open Binnaculum.Core.Patterns
open Binnaculum.Core.Logging

module internal BrokerFinancialsMetricsFromMovements =
    /// <summary>
    /// Calculates financial metrics from currency movement data using the extension methods.
    /// This function provides a centralized way to process all movement types and return
    /// a comprehensive financial metrics record that can be used across different scenarios.
    /// </summary>
    /// <param name="currencyMovements">The currency-specific movement data</param>
    /// <param name="currencyId">The currency ID for conversion impact calculations</param>
    /// <param name="targetDate">The target date for the snapshot (used for option expiration calculations)</param>
    /// <returns>CalculatedFinancialMetrics record with all financial calculations</returns>
    let internal calculate (currencyMovements: CurrencyMovementData) (currencyId: int) (targetDate: DateTimePattern) =

        CoreLogger.logDebug "BrokerFinancialsMetricsFromMovements" $"Starting calculate for currency {currencyId}"

        CoreLogger.logDebug
            "BrokerFinancialsMetricsFromMovements"
            $"Input movements - BrokerMovements: {currencyMovements.BrokerMovements.Length}, Trades: {currencyMovements.Trades.Length}, Dividends: {currencyMovements.Dividends.Length}"

        // Process broker movements using extension methods
        let brokerMovementSummary =
            currencyMovements.BrokerMovements
            |> FinancialCalculations.calculateFinancialSummary

        CoreLogger.logDebug
            "BrokerFinancialsMetricsFromMovements"
            $"BrokerMovement summary - TotalDeposited: {brokerMovementSummary.TotalDeposited.Value}, TotalWithdrawn: {brokerMovementSummary.TotalWithdrawn.Value}"

        // Calculate currency conversion impact
        let conversionImpact =
            currencyMovements.BrokerMovements
            |> fun movements -> FinancialCalculations.calculateConversionImpact (movements, currencyId)

        CoreLogger.logDebug "BrokerFinancialsMetricsFromMovements" $"Conversion impact: {conversionImpact.Value}"

        // Apply conversion impact to deposits/withdrawals
        let (adjustedDeposited, adjustedWithdrawn) =
            if conversionImpact.Value >= 0m then
                // Positive conversion impact: money was gained in this currency
                (brokerMovementSummary.TotalDeposited.Value + conversionImpact.Value,
                 brokerMovementSummary.TotalWithdrawn.Value)
            else
                // Negative conversion impact: money was lost from this currency
                (brokerMovementSummary.TotalDeposited.Value,
                 brokerMovementSummary.TotalWithdrawn.Value + abs (conversionImpact.Value))

        CoreLogger.logDebug
            "BrokerFinancialsMetricsFromMovements"
            $"After conversion adjustment - Deposited: {adjustedDeposited}, Withdrawn: {adjustedWithdrawn}"

        // Process trades using extension methods
        let tradingSummary =
            currencyMovements.Trades |> TradeCalculations.calculateTradingSummary

        // Process dividends using extension methods
        let dividendSummary =
            currencyMovements.Dividends |> DividendCalculations.calculateDividendSummary

        // Process dividend taxes using extension methods
        let dividendTaxSummary =
            currencyMovements.DividendTaxes
            |> DividendTaxCalculations.calculateDividendTaxSummary

        // Process options using extension methods with target date for expiration calculations
        let targetDateValue = targetDate.Value

        let optionsSummaryCurrent =
            OptionTradeCalculations.calculateOptionsSummary (currencyMovements.OptionTrades, targetDateValue)

        let optionsSummaryPrevious =
            OptionTradeCalculations.calculateOptionsSummary (
                currencyMovements.OptionTrades,
                targetDateValue.AddDays(-1.0)
            )

        // Derive daily deltas for option-related metrics to avoid double counting when accumulating snapshots
        let dailyOptionsIncome =
            Money.FromAmount(
                optionsSummaryCurrent.OptionsIncome.Value
                - optionsSummaryPrevious.OptionsIncome.Value
            )

        let dailyOptionsInvestment =
            Money.FromAmount(
                optionsSummaryCurrent.OptionsInvestment.Value
                - optionsSummaryPrevious.OptionsInvestment.Value
            )

        let dailyNetOptionsIncome =
            Money.FromAmount(
                optionsSummaryCurrent.NetOptionsIncome.Value
                - optionsSummaryPrevious.NetOptionsIncome.Value
            )

        let dailyOptionCommissions =
            Money.FromAmount(
                optionsSummaryCurrent.TotalCommissions.Value
                - optionsSummaryPrevious.TotalCommissions.Value
            )

        let dailyOptionFees =
            Money.FromAmount(optionsSummaryCurrent.TotalFees.Value - optionsSummaryPrevious.TotalFees.Value)

        let dailyOptionRealized =
            Money.FromAmount(
                optionsSummaryCurrent.RealizedGains.Value
                - optionsSummaryPrevious.RealizedGains.Value
            )

        let optionUnrealized = optionsSummaryCurrent.UnrealizedGains
        let optionHasOpenPositions = optionsSummaryCurrent.HasOpenOptions
        let optionOpenPositions = optionsSummaryCurrent.OpenPositions
        let optionTradeCountForDay = optionsSummaryCurrent.TradeCount

        CoreLogger.logDebug
            "BrokerFinancialsMetricsFromMovements"
            $"Options summary - TradesToday: {optionTradeCountForDay}, DailyIncome: {dailyOptionsIncome.Value}, DailyNetIncome: {dailyNetOptionsIncome.Value}, DailyInvestment: {dailyOptionsInvestment.Value}, DailyRealized: {dailyOptionRealized.Value}, Unrealized: {optionUnrealized.Value}, HasOpen: {optionHasOpenPositions}"

        // Calculate net dividend income after taxes
        let netDividendIncome =
            Money.FromAmount(
                dividendSummary.TotalDividendIncome.Value
                - dividendTaxSummary.TotalTaxWithheld.Value
            )

        // Adjust other income for interest paid
        let adjustedOtherIncome =
            Money.FromAmount(
                brokerMovementSummary.TotalOtherIncome.Value
                - brokerMovementSummary.TotalInterestPaid.Value
            )

        // Calculate total movement counter
        let totalMovementCounter =
            brokerMovementSummary.MovementCount
            + tradingSummary.TradeCount
            + dividendSummary.DividendCount
            + dividendTaxSummary.TaxEventCount
            + optionTradeCountForDay

        let result =
            { Deposited = Money.FromAmount adjustedDeposited
              Withdrawn = Money.FromAmount adjustedWithdrawn
              Invested = Money.FromAmount(tradingSummary.TotalInvested.Value + dailyOptionsInvestment.Value)
              RealizedGains = Money.FromAmount(tradingSummary.RealizedGains.Value + dailyOptionRealized.Value)
              DividendsReceived = netDividendIncome
              OptionsIncome = dailyOptionsIncome
              OtherIncome = adjustedOtherIncome
              Commissions =
                Money.FromAmount(
                    brokerMovementSummary.TotalCommissions.Value
                    + tradingSummary.TotalCommissions.Value
                    + dailyOptionCommissions.Value
                )
              Fees =
                Money.FromAmount(
                    brokerMovementSummary.TotalFees.Value
                    + tradingSummary.TotalFees.Value
                    + dailyOptionFees.Value
                )
              CurrentPositions = tradingSummary.CurrentPositions
              CostBasisInfo = tradingSummary.CostBasis
              HasOpenPositions = tradingSummary.HasOpenPositions || optionHasOpenPositions
              OptionUnrealizedGains = optionUnrealized
              MovementCounter = totalMovementCounter }

        CoreLogger.logDebug
            "BrokerFinancialsMetricsFromMovements"
            $"Final calculated metrics - Deposited: {result.Deposited.Value}, Invested: {result.Invested.Value}, RealizedGains: {result.RealizedGains.Value}, OptionsIncome: {result.OptionsIncome.Value}, MovementCounter: {result.MovementCounter}"

        // Return comprehensive metrics record
        result
