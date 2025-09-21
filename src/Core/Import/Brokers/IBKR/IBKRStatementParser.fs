namespace Binnaculum.Core.Import

open System
open System.Globalization
open IBKRModels
open IBKRSectionFilter

/// <summary>
/// IBKR CSV statement parser with comprehensive section handling
/// Parses various IBKR statement sections while maintaining privacy compliance
/// </summary>
module IBKRStatementParser =
    
    /// <summary>
    /// Parse a DateTime from IBKR format with fallback handling
    /// </summary>
    let private parseDateTime (dateStr: string) : DateTime option =
        let formats = [|
            "yyyy-MM-dd, HH:mm:ss"
            "yyyy-MM-dd"
            "MM/dd/yyyy"
            "dd-MM-yyyy"
        |]
        
        let mutable result = DateTime.MinValue
        let success = formats |> Array.exists (fun format ->
            DateTime.TryParseExact(dateStr.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, &result)
        )
        
        if success then Some result else None
    
    /// <summary>
    /// Parse a decimal value with robust error handling
    /// </summary>
    let private parseDecimal (value: string) : decimal option =
        let cleanValue = value.Trim().Replace(",", "")
        match Decimal.TryParse(cleanValue, NumberStyles.Number, CultureInfo.InvariantCulture) with
        | true, result -> Some result
        | false, _ -> None
    
    /// <summary>
    /// Parse a decimal value with default fallback
    /// </summary>
    let private parseDecimalWithDefault (value: string) (defaultValue: decimal) : decimal =
        parseDecimal value |> Option.defaultValue defaultValue
    
    /// <summary>
    /// Split CSV line handling quoted values and commas within quotes
    /// </summary>
    let private splitCsvLine (line: string) : string[] =
        // Simple CSV parsing - for production, consider using a proper CSV library
        // This handles basic cases but might need enhancement for complex quoted fields
        line.Split(',') |> Array.map (fun s -> s.Trim().Trim('"'))
    
    /// <summary>
    /// Parse IBKR trade record from CSV fields
    /// </summary>
    let private parseTrade (fields: string[]) : IBKRTrade option =
        if fields.Length < 12 then None
        else
            try
                let dateTime = parseDateTime fields.[5]
                let quantity = parseDecimal fields.[6]
                let proceeds = parseDecimal fields.[8]
                let commission = parseDecimal fields.[9]
                
                match dateTime, quantity, proceeds, commission with
                | Some dt, Some qty, Some proc, Some comm ->
                    Some {
                        AssetCategory = fields.[2]
                        Currency = fields.[3]
                        Symbol = fields.[4]
                        DateTime = dt
                        Quantity = qty
                        TradePrice = parseDecimal fields.[7]
                        ClosePrice = parseDecimal fields.[8]
                        Proceeds = proc
                        CommissionFee = comm
                        Basis = parseDecimal fields.[10]
                        RealizedPnL = parseDecimal fields.[11]
                        RealizedPnLPercent = if fields.Length > 12 then parseDecimal fields.[12] else None
                        MTMPnL = if fields.Length > 13 then parseDecimal fields.[13] else None
                        Code = if fields.Length > 14 then Some fields.[14] else None
                    }
                | _ -> None
            with
            | _ -> None
    
    /// <summary>
    /// Parse IBKR forex trade record
    /// </summary>
    let private parseForexTrade (fields: string[]) : IBKRForexTrade option =
        if fields.Length < 10 then None
        else
            try
                let currencyPair = fields.[4]
                let dateTime = parseDateTime fields.[5]
                let quantity = parseDecimal fields.[6]
                let tradePrice = parseDecimal fields.[7]
                let proceeds = parseDecimal fields.[8]
                let commission = parseDecimal fields.[9]
                
                // Parse currency pair (e.g., "GBP.USD" -> base: GBP, quote: USD)
                let baseCurrency, quoteCurrency =
                    let parts = currencyPair.Split('.')
                    if parts.Length = 2 then parts.[0], parts.[1]
                    else currencyPair, ""
                
                match dateTime, quantity, tradePrice, proceeds, commission with
                | Some dt, Some qty, Some price, Some proc, Some comm ->
                    Some {
                        CurrencyPair = currencyPair
                        BaseCurrency = baseCurrency
                        QuoteCurrency = quoteCurrency
                        DateTime = dt
                        Quantity = qty
                        TradePrice = price
                        Proceeds = proc
                        CommissionFee = comm
                        Code = if fields.Length > 10 then Some fields.[10] else None
                    }
                | _ -> None
            with
            | _ -> None
    
    /// <summary>
    /// Parse IBKR cash movement record
    /// </summary>
    let private parseCashMovement (fields: string[]) : IBKRCashMovement option =
        if fields.Length < 5 then None
        else
            try
                let currency = fields.[2]
                let settleDate = parseDateTime fields.[3]
                let description = fields.[4]
                let amount = parseDecimal fields.[5]
                
                let movementType = 
                    if description.Contains("Electronic Fund Transfer") then Deposit
                    elif description.Contains("Withdrawal") then Withdrawal
                    elif description.Contains("Commission") then Commission
                    else TradeSettlement
                
                match settleDate, amount with
                | Some date, Some amt ->
                    Some {
                        Currency = currency
                        SettleDate = date
                        Description = description
                        Amount = amt
                        MovementType = movementType
                    }
                | _ -> None
            with
            | _ -> None
    
    /// <summary>
    /// Parse IBKR cash flow record from Cash Report section
    /// </summary>
    let private parseCashFlow (fields: string[]) : IBKRCashFlow option =
        if fields.Length < 6 then None
        else
            try
                let flowDescription = fields.[2]
                let amount = parseDecimal fields.[4]
                let amountBase = parseDecimal fields.[5]
                
                let flowType = 
                    match flowDescription with
                    | desc when desc.Contains("FX Translation Gain") -> FXTranslationGain
                    | desc when desc.Contains("FX Translation Loss") -> FXTranslationLoss
                    | desc when desc.Contains("Deposit") -> Deposit
                    | desc when desc.Contains("Commission") -> Commission
                    | desc when desc.Contains("Interest") -> InterestPayment
                    | desc when desc.Contains("Fee") -> Fee
                    | _ -> TradeSettlement
                
                match amount, amountBase with
                | Some amt, Some amtBase ->
                    Some {
                        FlowType = flowType
                        Currency = "USD" // Typically base currency
                        Amount = amt
                        AmountBase = amtBase
                        Description = flowDescription
                    }
                | _ -> None
            with
            | _ -> None
    
    /// <summary>
    /// Parse IBKR open position record
    /// </summary>
    let private parseOpenPosition (fields: string[]) : IBKROpenPosition option =
        if fields.Length < 12 then None
        else
            try
                let assetCategory = fields.[2]
                let currency = fields.[3]
                let symbol = fields.[4]
                let quantity = parseDecimal fields.[5]
                let multiplier = parseDecimalWithDefault fields.[6] 1.0m
                let costBasisPrice = parseDecimal fields.[7]
                let costBasisMoney = parseDecimal fields.[8]
                let closePrice = parseDecimal fields.[9]
                let value = parseDecimal fields.[10]
                let unrealizedPnL = parseDecimal fields.[11]
                let unrealizedPnLPercent = if fields.Length > 12 then parseDecimal fields.[12] else None
                
                match quantity, costBasisPrice, costBasisMoney, closePrice, value, unrealizedPnL with
                | Some qty, Some cbPrice, Some cbMoney, Some cPrice, Some valueAmount, Some upnl ->
                    Some {
                        AssetCategory = assetCategory
                        Currency = currency
                        Symbol = symbol
                        Quantity = qty
                        Multiplier = multiplier
                        CostBasisPrice = cbPrice
                        CostBasisMoney = cbMoney
                        ClosePrice = cPrice
                        Value = valueAmount
                        UnrealizedPnL = upnl
                        UnrealizedPnLPercent = unrealizedPnLPercent |> Option.defaultValue 0.0m
                    }
                | _ -> None
            with
            | _ -> None
    
    /// <summary>
    /// Parse IBKR financial instrument record
    /// </summary>
    let private parseInstrument (fields: string[]) : IBKRInstrument option =
        if fields.Length < 6 then None
        else
            try
                Some {
                    AssetCategory = fields.[2]
                    Symbol = fields.[3]
                    Description = fields.[4]
                    ConId = if fields.Length > 5 then Some fields.[5] else None
                    SecurityId = if fields.Length > 6 then Some fields.[6] else None
                    ListingExchange = if fields.Length > 7 then Some fields.[7] else None
                    Multiplier = if fields.Length > 8 then parseDecimal fields.[8] else None
                    InstrumentType = if fields.Length > 9 then Some fields.[9] else None
                    Code = if fields.Length > 10 then Some fields.[10] else None
                }
            with
            | _ -> None
    
    /// <summary>
    /// Parse IBKR exchange rate record
    /// </summary>
    let private parseExchangeRate (fields: string[]) : IBKRExchangeRate option =
        if fields.Length < 4 then None
        else
            try
                let currency = fields.[2]
                let rate = parseDecimal fields.[3]
                
                match rate with
                | Some r ->
                    Some {
                        Currency = currency
                        Rate = r
                    }
                | _ -> None
            with
            | _ -> None
    
    /// <summary>
    /// Parse lines for a specific section
    /// </summary>
    let private parseSection (section: IBKRSection) (lines: string list) (data: IBKRStatementData) : IBKRStatementData * string list =
        let errors = ResizeArray<string>()
        
        match section with
        | Trades ->
            let trades = lines |> List.choose (fun line ->
                let fields = splitCsvLine line
                // Distinguish between stock and forex trades
                if fields.Length > 2 && fields.[2] = "Forex" then
                    match parseForexTrade fields with
                    | Some forexTrade -> 
                        // Add to forex trades instead
                        None
                    | None -> 
                        errors.Add($"Failed to parse forex trade: {line}")
                        None
                else
                    match parseTrade fields with
                    | Some trade -> Some trade
                    | None -> 
                        errors.Add($"Failed to parse trade: {line}")
                        None
            )
            
            let forexTrades = lines |> List.choose (fun line ->
                let fields = splitCsvLine line
                if fields.Length > 2 && fields.[2] = "Forex" then
                    match parseForexTrade fields with
                    | Some forexTrade -> Some forexTrade
                    | None -> None
                else None
            )
            
            ({ data with Trades = data.Trades @ trades; ForexTrades = data.ForexTrades @ forexTrades }, errors |> List.ofSeq)
        
        | DepositsWithdrawals ->
            let movements = lines |> List.choose (fun line ->
                match parseCashMovement (splitCsvLine line) with
                | Some movement -> Some movement
                | None -> 
                    errors.Add($"Failed to parse cash movement: {line}")
                    None
            )
            ({ data with CashMovements = data.CashMovements @ movements }, errors |> List.ofSeq)
        
        | CashReport ->
            let flows = lines |> List.choose (fun line ->
                match parseCashFlow (splitCsvLine line) with
                | Some flow -> Some flow
                | None -> 
                    errors.Add($"Failed to parse cash flow: {line}")
                    None
            )
            ({ data with CashFlows = data.CashFlows @ flows }, errors |> List.ofSeq)
        
        | OpenPositions ->
            let positions = lines |> List.choose (fun line ->
                match parseOpenPosition (splitCsvLine line) with
                | Some position -> Some position
                | None -> 
                    errors.Add($"Failed to parse open position: {line}")
                    None
            )
            ({ data with OpenPositions = data.OpenPositions @ positions }, errors |> List.ofSeq)
        
        | FinancialInstruments ->
            let instruments = lines |> List.choose (fun line ->
                match parseInstrument (splitCsvLine line) with
                | Some instrument -> Some instrument
                | None -> 
                    errors.Add($"Failed to parse instrument: {line}")
                    None
            )
            ({ data with Instruments = data.Instruments @ instruments }, errors |> List.ofSeq)
        
        | ExchangeRates ->
            let rates = lines |> List.choose (fun line ->
                match parseExchangeRate (splitCsvLine line) with
                | Some rate -> Some rate
                | None -> 
                    errors.Add($"Failed to parse exchange rate: {line}")
                    None
            )
            ({ data with ExchangeRates = data.ExchangeRates @ rates }, errors |> List.ofSeq)
        
        | ForexBalances | CollateralBorrowing ->
            // These sections exist but are not yet implemented
            (data, [])
        
        | SkippedSection reason ->
            // Section was skipped for privacy or other reasons
            (data, [])
    
    /// <summary>
    /// Parse complete IBKR CSV file
    /// </summary>
    let parseCsvFile (filePath: string) : IBKRParseResult =
        try
            if not (System.IO.File.Exists(filePath)) then
                createFailureResult [$"File not found: {filePath}"]
            else
                let lines = System.IO.File.ReadAllLines(filePath) |> Array.toList
                let mutable data = createEmptyStatementData ()
                let errors = ResizeArray<string>()
                let warnings = ResizeArray<string>()
                let skippedSections = ResizeArray<string>()
                
                // Group lines by section
                let sections = ResizeArray<IBKRSection * string list>()
                let mutable currentSection = SkippedSection "Unknown"
                let mutable currentLines = ResizeArray<string>()
                
                for line in lines do
                    if not (String.IsNullOrWhiteSpace(line)) then
                        let fields = splitCsvLine line
                        if fields.Length >= 2 then
                            let sectionType = fields.[0].Trim()
                            let dataType = fields.[1].Trim()
                            
                            // Check if this is a new section header
                            if dataType = "Header" then
                                // Save previous section
                                if currentLines.Count > 0 then
                                    sections.Add((currentSection, currentLines |> List.ofSeq))
                                
                                // Start new section
                                currentSection <- classifySection sectionType
                                currentLines.Clear()
                                
                                // Log skipped sections
                                match getSkipReason currentSection with
                                | Some reason -> skippedSections.Add(reason)
                                | None -> ()
                            
                            elif dataType = "Data" && shouldProcessSection currentSection then
                                currentLines.Add(line)
                
                // Don't forget the last section
                if currentLines.Count > 0 then
                    sections.Add((currentSection, currentLines |> List.ofSeq))
                
                // Parse each section
                for (section, sectionLines) in sections do
                    if shouldProcessSection section then
                        let (updatedData, sectionErrors) = parseSection section sectionLines data
                        data <- updatedData
                        errors.AddRange(sectionErrors)
                
                // Validate privacy compliance
                let privacyErrors = validatePrivacyCompliance data
                errors.AddRange(privacyErrors)
                
                if errors.Count = 0 then
                    {
                        Success = true
                        Data = Some data
                        Errors = []
                        Warnings = warnings |> List.ofSeq
                        SkippedSections = skippedSections |> List.ofSeq
                    }
                else
                    createFailureResult (errors |> List.ofSeq)
        
        with
        | ex ->
            createFailureResult [$"Exception parsing file {filePath}: {ex.Message}"]
    
    /// <summary>
    /// Parse IBKR CSV from string content (useful for testing)
    /// </summary>
    let parseCsvContent (content: string) : IBKRParseResult =
        let tempFile = System.IO.Path.GetTempFileName()
        try
            System.IO.File.WriteAllText(tempFile, content)
            parseCsvFile tempFile
        finally
            if System.IO.File.Exists(tempFile) then
                System.IO.File.Delete(tempFile)