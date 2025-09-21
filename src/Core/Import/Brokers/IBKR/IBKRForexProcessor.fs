namespace Binnaculum.Core.Import

open System
open IBKRModels

/// <summary>
/// Specialized processor for IBKR forex trades and currency pair handling
/// Handles complex forex scenarios including currency pair parsing and conversion logic
/// </summary>
module IBKRForexProcessor =
    
    /// <summary>
    /// Parsed currency pair information
    /// </summary>
    type CurrencyPairInfo = {
        Original: string
        BaseCurrency: string
        QuoteCurrency: string
        IsValid: bool
        ParsedCorrectly: bool
    }
    
    /// <summary>
    /// Enhanced forex trade with processing metadata
    /// </summary>
    type ProcessedForexTrade = {
        OriginalTrade: IBKRForexTrade
        PairInfo: CurrencyPairInfo
        ConversionDirection: string
        EffectiveRate: decimal
        BaseCurrencyAmount: decimal
        QuoteCurrencyAmount: decimal
        ProcessingNotes: string list
    }
    
    /// <summary>
    /// Forex processing result
    /// </summary>
    type ForexProcessingResult = {
        ProcessedTrades: ProcessedForexTrade list
        CurrencyExposure: Map<string, decimal>
        NetConversions: Map<string, decimal>
        Errors: string list
        Warnings: string list
    }
    
    /// <summary>
    /// Parse IBKR currency pair format (e.g., "GBP.USD", "EUR.USD")
    /// </summary>
    let parseCurrencyPair (pairString: string) : CurrencyPairInfo =
        let cleanPair = pairString.Trim()
        
        if cleanPair.Contains(".") then
            let parts = cleanPair.Split('.')
            if parts.Length = 2 then
                let baseCurrency = parts.[0].Trim().ToUpper()
                let quoteCurrency = parts.[1].Trim().ToUpper()
                
                // Validate currency codes (basic check for 3-letter codes)
                let isValidBase = baseCurrency.Length = 3 && baseCurrency |> Seq.forall Char.IsLetter
                let isValidQuote = quoteCurrency.Length = 3 && quoteCurrency |> Seq.forall Char.IsLetter
                
                {
                    Original = cleanPair
                    BaseCurrency = baseCurrency
                    QuoteCurrency = quoteCurrency
                    IsValid = isValidBase && isValidQuote
                    ParsedCorrectly = true
                }
            else
                {
                    Original = cleanPair
                    BaseCurrency = cleanPair
                    QuoteCurrency = ""
                    IsValid = false
                    ParsedCorrectly = false
                }
        else
            // Handle cases where currency pair doesn't follow expected format
            {
                Original = cleanPair
                BaseCurrency = cleanPair
                QuoteCurrency = ""
                IsValid = false
                ParsedCorrectly = false
            }
    
    /// <summary>
    /// Determine conversion direction based on quantity sign
    /// </summary>
    let determineConversionDirection (quantity: decimal) (baseCurrency: string) (quoteCurrency: string) : string =
        if quantity > 0m then
            $"Buy {baseCurrency} with {quoteCurrency}"
        else
            $"Sell {baseCurrency} for {quoteCurrency}"
    
    /// <summary>
    /// Calculate effective exchange rate from trade data
    /// </summary>
    let calculateEffectiveRate (trade: IBKRForexTrade) : decimal =
        if trade.Quantity <> 0m then
            Math.Abs(trade.Proceeds / trade.Quantity)
        else
            trade.TradePrice
    
    /// <summary>
    /// Process individual forex trade with enhanced analysis
    /// </summary>
    let private processForexTrade (forexTrade: IBKRForexTrade) : ProcessedForexTrade =
        let pairInfo = parseCurrencyPair forexTrade.CurrencyPair
        let conversionDirection = determineConversionDirection forexTrade.Quantity pairInfo.BaseCurrency pairInfo.QuoteCurrency
        let effectiveRate = calculateEffectiveRate forexTrade
        
        let processingNotes = ResizeArray<string>()
        
        // Calculate amounts in both currencies
        let baseCurrencyAmount = Math.Abs(forexTrade.Quantity)
        let quoteCurrencyAmount = Math.Abs(forexTrade.Proceeds)
        
        // Add processing notes
        if not pairInfo.IsValid then
            processingNotes.Add($"Invalid currency pair format: {forexTrade.CurrencyPair}")
        
        if Math.Abs(effectiveRate - forexTrade.TradePrice) > 0.0001m then
            processingNotes.Add($"Effective rate ({effectiveRate:F4}) differs from trade price ({forexTrade.TradePrice:F4})")
        
        if forexTrade.CommissionFee > quoteCurrencyAmount * 0.01m then
            processingNotes.Add($"High commission rate: {forexTrade.CommissionFee:F4} on {quoteCurrencyAmount:F2}")
        
        {
            OriginalTrade = forexTrade
            PairInfo = pairInfo
            ConversionDirection = conversionDirection
            EffectiveRate = effectiveRate
            BaseCurrencyAmount = baseCurrencyAmount
            QuoteCurrencyAmount = quoteCurrencyAmount
            ProcessingNotes = processingNotes |> List.ofSeq
        }
    
    /// <summary>
    /// Process multiple forex trades with comprehensive analysis
    /// </summary>
    let processForexTrades (forexTrades: IBKRForexTrade list) : ForexProcessingResult =
        let errors = ResizeArray<string>()
        let warnings = ResizeArray<string>()
        
        // Process each trade
        let processedTrades = 
            forexTrades
            |> List.map processForexTrade
        
        // Calculate currency exposure
        let currencyExposure = ResizeArray<string * decimal>()
        
        for trade in processedTrades do
            if trade.PairInfo.IsValid then
                // Base currency exposure (negative if selling)
                let baseExposure = if trade.OriginalTrade.Quantity > 0m then trade.BaseCurrencyAmount else -trade.BaseCurrencyAmount
                currencyExposure.Add((trade.PairInfo.BaseCurrency, baseExposure))
                
                // Quote currency exposure (opposite of base)
                let quoteExposure = if trade.OriginalTrade.Quantity > 0m then -trade.QuoteCurrencyAmount else trade.QuoteCurrencyAmount
                currencyExposure.Add((trade.PairInfo.QuoteCurrency, quoteExposure))
        
        let currencyExposureMap = 
            currencyExposure
            |> Seq.groupBy fst
            |> Seq.map (fun (currency, amounts) -> currency, amounts |> Seq.sumBy snd)
            |> Map.ofSeq
        
        // Calculate net conversions
        let netConversions = 
            processedTrades
            |> List.filter (fun pt -> pt.PairInfo.IsValid)
            |> List.groupBy (fun pt -> pt.PairInfo.BaseCurrency)
            |> List.map (fun (currency, trades) -> 
                let netAmount = trades |> List.sumBy (fun t -> 
                    if t.OriginalTrade.Quantity > 0m then t.BaseCurrencyAmount else -t.BaseCurrencyAmount)
                currency, netAmount)
            |> Map.ofList
        
        // Collect processing warnings
        for trade in processedTrades do
            for note in trade.ProcessingNotes do
                warnings.Add($"{trade.PairInfo.Original}: {note}")
        
        // Validate trades
        let invalidTrades = processedTrades |> List.filter (fun pt -> not pt.PairInfo.IsValid)
        if not invalidTrades.IsEmpty then
            errors.Add($"Found {invalidTrades.Length} forex trades with invalid currency pairs")
        
        // Check for unusual rates
        let unusualRates = 
            processedTrades
            |> List.filter (fun pt -> pt.EffectiveRate < 0.1m || pt.EffectiveRate > 100.0m)
        
        if not unusualRates.IsEmpty then
            warnings.Add($"Found {unusualRates.Length} forex trades with unusual exchange rates")
        
        {
            ProcessedTrades = processedTrades
            CurrencyExposure = currencyExposureMap
            NetConversions = netConversions
            Errors = errors |> List.ofSeq
            Warnings = warnings |> List.ofSeq
        }
    
    /// <summary>
    /// Analyze forex trading patterns and provide insights
    /// </summary>
    let analyzeForexPatterns (processedTrades: ProcessedForexTrade list) : Map<string, obj> =
        let analysis = ResizeArray<string * obj>()
        
        // Most traded currency pairs
        let pairFrequency = 
            processedTrades
            |> List.filter (fun pt -> pt.PairInfo.IsValid)
            |> List.groupBy (fun pt -> pt.PairInfo.Original)
            |> List.map (fun (pair, trades) -> pair, trades.Length)
            |> List.sortByDescending snd
        
        analysis.Add("MostTradedPairs", box pairFrequency)
        
        // Average effective rates by pair
        let averageRates = 
            processedTrades
            |> List.filter (fun pt -> pt.PairInfo.IsValid)
            |> List.groupBy (fun pt -> pt.PairInfo.Original)
            |> List.map (fun (pair, trades) -> 
                let avgRate = trades |> List.averageBy (fun t -> float t.EffectiveRate)
                pair, avgRate)
        
        analysis.Add("AverageRatesByPair", box averageRates)
        
        // Total commission paid
        let totalCommission = 
            processedTrades
            |> List.sumBy (fun pt -> pt.OriginalTrade.CommissionFee)
        
        analysis.Add("TotalCommission", box totalCommission)
        
        // Trading time patterns
        let tradingHours = 
            processedTrades
            |> List.map (fun pt -> pt.OriginalTrade.DateTime.Hour)
            |> List.groupBy id
            |> List.map (fun (hour, trades) -> hour, trades.Length)
            |> List.sortBy fst
        
        analysis.Add("TradingHourDistribution", box tradingHours)
        
        analysis |> Map.ofSeq
    
    /// <summary>
    /// Validate forex trade data integrity
    /// </summary>
    let validateForexIntegrity (forexTrades: IBKRForexTrade list) : string list =
        let errors = ResizeArray<string>()
        
        // Check for zero quantities
        let zeroQuantityTrades = 
            forexTrades
            |> List.filter (fun ft -> ft.Quantity = 0m)
        
        if not zeroQuantityTrades.IsEmpty then
            errors.Add($"Found {zeroQuantityTrades.Length} forex trades with zero quantity")
        
        // Check for zero proceeds
        let zeroProceedsTrades = 
            forexTrades
            |> List.filter (fun ft -> ft.Proceeds = 0m && ft.Quantity <> 0m)
        
        if not zeroProceedsTrades.IsEmpty then
            errors.Add($"Found {zeroProceedsTrades.Length} forex trades with zero proceeds but non-zero quantity")
        
        // Check for missing trade prices
        let missingPriceTrades = 
            forexTrades
            |> List.filter (fun ft -> ft.TradePrice = 0m && ft.Quantity <> 0m)
        
        if not missingPriceTrades.IsEmpty then
            errors.Add($"Found {missingPriceTrades.Length} forex trades with missing trade prices")
        
        // Check for future dates
        let futureTrades = 
            forexTrades
            |> List.filter (fun ft -> ft.DateTime > DateTime.Now.AddHours(24))
        
        if not futureTrades.IsEmpty then
            errors.Add($"Found {futureTrades.Length} forex trades with future dates")
        
        errors |> List.ofSeq
    
    /// <summary>
    /// Extract supported currency pairs from forex trades
    /// Useful for validating system currency support
    /// </summary>
    let extractSupportedPairs (forexTrades: IBKRForexTrade list) : string list =
        forexTrades
        |> List.map (fun ft -> parseCurrencyPair ft.CurrencyPair)
        |> List.filter (fun pair -> pair.IsValid)
        |> List.map (fun pair -> pair.Original)
        |> List.distinct
        |> List.sort