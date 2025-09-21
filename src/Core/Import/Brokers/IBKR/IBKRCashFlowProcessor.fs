namespace Binnaculum.Core.Import

open System
open IBKRModels

/// <summary>
/// Specialized processor for IBKR multi-currency cash flows and FX transactions
/// Handles complex scenarios like FX translation gains/losses and currency conversions
/// </summary>
module IBKRCashFlowProcessor =
    
    /// <summary>
    /// Processed cash flow with enhanced metadata
    /// </summary>
    type ProcessedCashFlow = {
        OriginalFlow: IBKRCashFlow
        ProcessedType: IBKRCashFlowType
        BaseCurrencyAmount: decimal
        ForeignCurrencyAmount: decimal option
        ExchangeRate: decimal option
        ProcessingNotes: string list
    }
    
    /// <summary>
    /// Cash flow processing result
    /// </summary>
    type CashFlowProcessingResult = {
        ProcessedFlows: ProcessedCashFlow list
        TotalBaseCurrencyFlow: decimal
        CurrencyBreakdown: Map<string, decimal>
        Errors: string list
        Warnings: string list
    }
    
    /// <summary>
    /// Identify FX translation gains and losses
    /// </summary>
    let private classifyFXTranslation (description: string) (amount: decimal) : IBKRCashFlowType =
        if description.Contains("FX Translation Gain") || (description.Contains("FX Translation") && amount > 0m) then
            FXTranslationGain
        elif description.Contains("FX Translation Loss") || (description.Contains("FX Translation") && amount < 0m) then
            FXTranslationLoss
        else
            TradeSettlement
    
    /// <summary>
    /// Process individual cash flow with enhanced classification
    /// </summary>
    let private processCashFlow (cashFlow: IBKRCashFlow) (exchangeRates: Map<string, decimal>) : ProcessedCashFlow =
        let processedType = 
            match cashFlow.FlowType with
            | FXTranslationGain | FXTranslationLoss -> 
                classifyFXTranslation cashFlow.Description cashFlow.Amount
            | flowType -> flowType
        
        let exchangeRate = 
            if cashFlow.Currency <> "USD" then
                exchangeRates |> Map.tryFind cashFlow.Currency
            else
                Some 1.0m
        
        let baseCurrencyAmount = cashFlow.AmountBase
        let foreignCurrencyAmount = 
            if cashFlow.Currency <> "USD" then Some cashFlow.Amount
            else None
        
        let processingNotes = ResizeArray<string>()
        
        // Add processing notes
        if processedType <> cashFlow.FlowType then
            processingNotes.Add($"Reclassified from {cashFlow.FlowType} to {processedType}")
        
        if exchangeRate.IsNone && cashFlow.Currency <> "USD" then
            processingNotes.Add($"Missing exchange rate for {cashFlow.Currency}")
        
        if Math.Abs(baseCurrencyAmount) < 0.01m && processedType <> FXTranslationGain && processedType <> FXTranslationLoss then
            processingNotes.Add("Small amount - may be rounding adjustment")
        
        {
            OriginalFlow = cashFlow
            ProcessedType = processedType
            BaseCurrencyAmount = baseCurrencyAmount
            ForeignCurrencyAmount = foreignCurrencyAmount
            ExchangeRate = exchangeRate
            ProcessingNotes = processingNotes |> List.ofSeq
        }
    
    /// <summary>
    /// Process IBKR cash flows with multi-currency handling
    /// </summary>
    let processCashFlows (cashFlows: IBKRCashFlow list) (exchangeRates: IBKRExchangeRate list) : CashFlowProcessingResult =
        let errors = ResizeArray<string>()
        let warnings = ResizeArray<string>()
        
        // Build exchange rate map
        let rateMap = 
            exchangeRates
            |> List.map (fun rate -> rate.Currency, rate.Rate)
            |> Map.ofList
        
        // Process each cash flow
        let processedFlows = 
            cashFlows
            |> List.map (fun flow -> processCashFlow flow rateMap)
        
        // Calculate totals
        let totalBaseCurrencyFlow = 
            processedFlows
            |> List.sumBy (fun pf -> pf.BaseCurrencyAmount)
        
        // Build currency breakdown
        let currencyBreakdown = 
            processedFlows
            |> List.choose (fun pf -> 
                match pf.ForeignCurrencyAmount with
                | Some amount -> Some (pf.OriginalFlow.Currency, amount)
                | None -> Some ("USD", pf.BaseCurrencyAmount))
            |> List.groupBy fst
            |> List.map (fun (currency, amounts) -> currency, amounts |> List.sumBy snd)
            |> Map.ofList
        
        // Collect processing warnings
        for pf in processedFlows do
            for note in pf.ProcessingNotes do
                warnings.Add(note)
        
        // Validate consistency
        let fxGains = processedFlows |> List.filter (fun pf -> pf.ProcessedType = FXTranslationGain) |> List.sumBy (fun pf -> pf.BaseCurrencyAmount)
        let fxLosses = processedFlows |> List.filter (fun pf -> pf.ProcessedType = FXTranslationLoss) |> List.sumBy (fun pf -> pf.BaseCurrencyAmount)
        let netFX = fxGains + fxLosses
        
        if Math.Abs(netFX) > 0.01m then
            warnings.Add($"Net FX translation amount: {netFX:F4} (may indicate currency fluctuations)")
        
        {
            ProcessedFlows = processedFlows
            TotalBaseCurrencyFlow = totalBaseCurrencyFlow
            CurrencyBreakdown = currencyBreakdown
            Errors = errors |> List.ofSeq
            Warnings = warnings |> List.ofSeq
        }
    
    /// <summary>
    /// Reconcile cash flows with deposits and withdrawals
    /// Helps identify missing transactions or data inconsistencies
    /// </summary>
    let reconcileCashFlows 
        (cashFlows: IBKRCashFlow list) 
        (cashMovements: IBKRCashMovement list) 
        (exchangeRates: IBKRExchangeRate list) : string list =
        
        let warnings = ResizeArray<string>()
        
        // Process cash flows
        let flowResult = processCashFlows cashFlows exchangeRates
        
        // Calculate expected totals from movements
        let depositTotal = 
            cashMovements
            |> List.filter (fun m -> m.MovementType = Deposit)
            |> List.sumBy (fun m -> m.Amount)
        
        let withdrawalTotal = 
            cashMovements
            |> List.filter (fun m -> m.MovementType = Withdrawal)
            |> List.sumBy (fun m -> -m.Amount)
        
        let expectedNet = depositTotal + withdrawalTotal
        
        // Compare with cash report
        let reportedDeposits = 
            flowResult.ProcessedFlows
            |> List.filter (fun pf -> pf.ProcessedType = Deposit)
            |> List.sumBy (fun pf -> pf.BaseCurrencyAmount)
        
        let reportedWithdrawals = 
            flowResult.ProcessedFlows
            |> List.filter (fun pf -> pf.ProcessedType = Withdrawal)
            |> List.sumBy (fun pf -> pf.BaseCurrencyAmount)
        
        let reportedNet = reportedDeposits + reportedWithdrawals
        
        // Check for discrepancies
        let difference = Math.Abs(expectedNet - reportedNet)
        if difference > 0.01m then
            warnings.Add($"Cash flow reconciliation difference: {difference:F4} (Expected: {expectedNet:F4}, Reported: {reportedNet:F4})")
        
        // Check for missing exchange rates
        let missingRates = 
            flowResult.ProcessedFlows
            |> List.filter (fun pf -> pf.ExchangeRate.IsNone && pf.OriginalFlow.Currency <> "USD")
            |> List.map (fun pf -> pf.OriginalFlow.Currency)
            |> List.distinct
        
        for currency in missingRates do
            warnings.Add($"Missing exchange rate for {currency} - amounts may not be accurate")
        
        warnings |> List.ofSeq
    
    /// <summary>
    /// Extract FX translation impact summary
    /// Useful for understanding currency exposure and translation effects
    /// </summary>
    let analyzeFXTranslationImpact (cashFlows: IBKRCashFlow list) : Map<string, decimal> * decimal =
        let fxFlows = 
            cashFlows
            |> List.filter (fun cf -> cf.FlowType = FXTranslationGain || cf.FlowType = FXTranslationLoss)
        
        let currencyImpact = 
            fxFlows
            |> List.groupBy (fun cf -> cf.Currency)
            |> List.map (fun (currency, flows) -> 
                currency, flows |> List.sumBy (fun f -> f.AmountBase))
            |> Map.ofList
        
        let totalImpact = 
            fxFlows
            |> List.sumBy (fun cf -> cf.AmountBase)
        
        (currencyImpact, totalImpact)
    
    /// <summary>
    /// Validate cash flow data integrity
    /// Checks for common data quality issues
    /// </summary>
    let validateCashFlowIntegrity (cashFlows: IBKRCashFlow list) : string list =
        let errors = ResizeArray<string>()
        
        // Check for zero amounts (suspicious)
        let zeroAmountFlows = 
            cashFlows
            |> List.filter (fun cf -> cf.Amount = 0m && cf.AmountBase = 0m)
        
        if not zeroAmountFlows.IsEmpty then
            errors.Add($"Found {zeroAmountFlows.Length} cash flows with zero amounts")
        
        // Check for missing base currency amounts
        let missingBaseAmounts = 
            cashFlows
            |> List.filter (fun cf -> cf.Amount <> 0m && cf.AmountBase = 0m)
        
        if not missingBaseAmounts.IsEmpty then
            errors.Add($"Found {missingBaseAmounts.Length} cash flows with missing base currency amounts")
        
        // Check for unusual FX ratios (potential data errors)
        let unusualRatios = 
            cashFlows
            |> List.filter (fun cf -> cf.Amount <> 0m && cf.AmountBase <> 0m)
            |> List.filter (fun cf -> 
                let ratio = Math.Abs(cf.AmountBase / cf.Amount)
                ratio < 0.1m || ratio > 10.0m)
        
        if not unusualRatios.IsEmpty then
            errors.Add($"Found {unusualRatios.Length} cash flows with unusual FX ratios (may indicate data errors)")
        
        errors |> List.ofSeq