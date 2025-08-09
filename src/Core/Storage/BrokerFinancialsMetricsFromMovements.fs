namespace Binnaculum.Core.Storage

open BrokerMovementExtensions
open TradeExtensions
open DividendExtensions
open DividendTaxExtensions
open OptionTradeExtensions
open Binnaculum.Core.Patterns

module internal BrokerFinancialsMetricsFromMovements =
    /// <summary>
    /// Calculates financial metrics from currency movement data using the extension methods.
    /// This function provides a centralized way to process all movement types and return
    /// a comprehensive financial metrics record that can be used across different scenarios.
    /// </summary>
    /// <param name="currencyMovements">The currency-specific movement data</param>
    /// <param name="currencyId">The currency ID for conversion impact calculations</param>
    /// <returns>CalculatedFinancialMetrics record with all financial calculations</returns>
    let internal calculate
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

