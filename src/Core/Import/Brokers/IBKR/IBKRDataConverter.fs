namespace Binnaculum.Core.Import

open System
open IBKRModels
open Binnaculum.Core

/// <summary>
/// Convert IBKR-specific data structures to Binnaculum database models
/// Handles the mapping between IBKR CSV data and internal domain models
/// </summary>
module IBKRDataConverter =
    
    /// <summary>
    /// Convert IBKR trade to Binnaculum Trade model
    /// </summary>
    let convertTrade (ibkrTrade: IBKRTrade) (brokerAccountId: int) (tickerId: int) (currencyId: int) : Models.Trade =
        let tradeType = 
            if ibkrTrade.Quantity > 0m then Models.TradeType.Long
            else Models.TradeType.Short
        
        let absoluteQuantity = Math.Abs(ibkrTrade.Quantity)
        let tradePrice = ibkrTrade.TradePrice |> Option.defaultValue 0m
        let totalAmount = Math.Abs(ibkrTrade.Proceeds)
        
        {
            Id = 0 // Will be set by database
            BrokerAccount = { Id = brokerAccountId; Broker = { Id = 0; Name = ""; Image = ""; SupportedBroker = "" }; AccountNumber = "" }
            Ticker = { Id = tickerId; Symbol = ibkrTrade.Symbol; Image = None; Name = None }
            Currency = { Id = currencyId; Title = ""; Code = ibkrTrade.Currency; Symbol = "" }
            Quantity = absoluteQuantity
            Price = tradePrice
            Commission = Math.Abs(ibkrTrade.CommissionFee)
            TimeStamp = ibkrTrade.DateTime
            TradeType = tradeType
            TotalAmount = totalAmount
            Notes = ibkrTrade.Code |> Option.defaultValue ""
        }
    
    /// <summary>
    /// Convert IBKR forex trade to Binnaculum BrokerMovement model
    /// </summary>
    let convertForexTrade (forexTrade: IBKRForexTrade) (brokerAccountId: int) (currencyId: int) : Models.BrokerMovement =
        {
            Id = 0 // Will be set by database
            BrokerAccount = { Id = brokerAccountId; Broker = { Id = 0; Name = ""; Image = ""; SupportedBroker = "" }; AccountNumber = "" }
            Ticker = None
            Currency = { Id = currencyId; Title = ""; Code = forexTrade.QuoteCurrency; Symbol = "" }
            Amount = Math.Abs(forexTrade.Proceeds)
            Fee = Math.Abs(forexTrade.CommissionFee)
            TimeStamp = forexTrade.DateTime
            MovementType = Models.BrokerMovementType.Conversion
            Notes = Some $"Forex: {forexTrade.CurrencyPair} @ {forexTrade.TradePrice}"
        }
    
    /// <summary>
    /// Convert IBKR cash movement to Binnaculum BrokerMovement model
    /// </summary>
    let convertCashMovement (cashMovement: IBKRCashMovement) (brokerAccountId: int) (currencyId: int) : Models.BrokerMovement =
        let movementType = 
            match cashMovement.MovementType with
            | IBKRCashFlowType.Deposit -> Models.BrokerMovementType.Deposit
            | IBKRCashFlowType.Withdrawal -> Models.BrokerMovementType.Withdrawal
            | IBKRCashFlowType.Commission -> Models.BrokerMovementType.Fee
            | IBKRCashFlowType.Fee -> Models.BrokerMovementType.Fee
            | _ -> Models.BrokerMovementType.Deposit
        
        {
            Id = 0 // Will be set by database
            BrokerAccount = { Id = brokerAccountId; Broker = { Id = 0; Name = ""; Image = ""; SupportedBroker = "" }; AccountNumber = "" }
            Ticker = None
            Currency = { Id = currencyId; Title = ""; Code = cashMovement.Currency; Symbol = "" }
            Amount = Math.Abs(cashMovement.Amount)
            Fee = 0m
            TimeStamp = cashMovement.SettleDate
            MovementType = movementType
            Notes = Some cashMovement.Description
        }
    
    /// <summary>
    /// Convert IBKR cash flow to Binnaculum BrokerMovement model
    /// </summary>
    let convertCashFlow (cashFlow: IBKRCashFlow) (brokerAccountId: int) (currencyId: int) (timestamp: DateTime) : Models.BrokerMovement =
        let movementType = 
            match cashFlow.FlowType with
            | IBKRCashFlowType.FXTranslationGain | IBKRCashFlowType.FXTranslationLoss -> Models.BrokerMovementType.Conversion
            | IBKRCashFlowType.Commission -> Models.BrokerMovementType.Fee
            | IBKRCashFlowType.InterestPayment -> Models.BrokerMovementType.InterestsGained
            | IBKRCashFlowType.Fee -> Models.BrokerMovementType.Fee
            | _ -> Models.BrokerMovementType.Deposit
        
        {
            Id = 0 // Will be set by database
            BrokerAccount = { Id = brokerAccountId; Broker = { Id = 0; Name = ""; Image = ""; SupportedBroker = "" }; AccountNumber = "" }
            Ticker = None
            Currency = { Id = currencyId; Title = ""; Code = cashFlow.Currency; Symbol = "" }
            Amount = Math.Abs(cashFlow.Amount)
            Fee = 0m
            TimeStamp = timestamp
            MovementType = movementType
            Notes = Some cashFlow.Description
        }
    
    /// <summary>
    /// Convert IBKR instrument to Binnaculum Ticker model
    /// </summary>
    let convertInstrument (instrument: IBKRInstrument) : Models.Ticker =
        {
            Id = 0 // Will be set by database
            Symbol = instrument.Symbol
            Image = None
            Name = Some instrument.Description
        }
    
    /// <summary>
    /// Conversion result for batch processing
    /// </summary>
    type ConversionResult = {
        Trades: Models.Trade list
        BrokerMovements: Models.BrokerMovement list
        Tickers: Models.Ticker list
        Errors: string list
        Warnings: string list
    }
    
    /// <summary>
    /// Create empty conversion result
    /// </summary>
    let createEmptyConversionResult () = {
        Trades = []
        BrokerMovements = []
        Tickers = []
        Errors = []
        Warnings = []
    }
    
    /// <summary>
    /// Convert complete IBKR statement data to Binnaculum models
    /// Requires broker account ID and currency mappings to be resolved externally
    /// </summary>
    let convertStatementData 
        (data: IBKRStatementData) 
        (brokerAccountId: int) 
        (getCurrencyId: string -> int option)
        (getOrCreateTickerId: string -> int)
        : ConversionResult =
        
        let result = createEmptyConversionResult ()
        let errors = ResizeArray<string>(result.Errors)
        let warnings = ResizeArray<string>(result.Warnings)
        let trades = ResizeArray<Models.Trade>(result.Trades)
        let movements = ResizeArray<Models.BrokerMovement>(result.BrokerMovements)
        let tickers = ResizeArray<Models.Ticker>(result.Tickers)
        
        // Default statement date for cash flows without explicit dates
        let defaultDate = data.StatementDate |> Option.defaultValue DateTime.Today
        
        // Convert trades
        for ibkrTrade in data.Trades do
            match getCurrencyId ibkrTrade.Currency with
            | Some currencyId ->
                let tickerId = getOrCreateTickerId ibkrTrade.Symbol
                let trade = convertTrade ibkrTrade brokerAccountId tickerId currencyId
                trades.Add(trade)
            | None ->
                errors.Add($"Unknown currency: {ibkrTrade.Currency} for trade {ibkrTrade.Symbol}")
        
        // Convert forex trades to broker movements
        for forexTrade in data.ForexTrades do
            match getCurrencyId forexTrade.QuoteCurrency with
            | Some currencyId ->
                let movement = convertForexTrade forexTrade brokerAccountId currencyId
                movements.Add(movement)
            | None ->
                errors.Add($"Unknown currency: {forexTrade.QuoteCurrency} for forex trade {forexTrade.CurrencyPair}")
        
        // Convert cash movements
        for cashMovement in data.CashMovements do
            match getCurrencyId cashMovement.Currency with
            | Some currencyId ->
                let movement = convertCashMovement cashMovement brokerAccountId currencyId
                movements.Add(movement)
            | None ->
                errors.Add($"Unknown currency: {cashMovement.Currency} for cash movement")
        
        // Convert cash flows
        for cashFlow in data.CashFlows do
            match getCurrencyId cashFlow.Currency with
            | Some currencyId ->
                let movement = convertCashFlow cashFlow brokerAccountId currencyId defaultDate
                movements.Add(movement)
            | None ->
                errors.Add($"Unknown currency: {cashFlow.Currency} for cash flow")
        
        // Convert instruments to tickers (for auto-creation)
        for instrument in data.Instruments do
            let ticker = convertInstrument instrument
            tickers.Add(ticker)
        
        // Add warnings for unhandled sections
        if not data.OpenPositions.IsEmpty then
            warnings.Add($"Open positions data available but not converted to trades (count: {data.OpenPositions.Length})")
        
        if not data.ForexBalances.IsEmpty then
            warnings.Add($"Forex balances data available but not converted (count: {data.ForexBalances.Length})")
        
        {
            Trades = trades |> List.ofSeq
            BrokerMovements = movements |> List.ofSeq
            Tickers = tickers |> List.ofSeq
            Errors = errors |> List.ofSeq
            Warnings = warnings |> List.ofSeq
        }
    
    /// <summary>
    /// Extract unique currency codes from IBKR statement data
    /// Used to validate currency availability before conversion
    /// </summary>
    let extractCurrencyCodes (data: IBKRStatementData) : string list =
        let currencies = ResizeArray<string>()
        
        // From trades
        for trade in data.Trades do
            if not (currencies.Contains(trade.Currency)) then
                currencies.Add(trade.Currency)
        
        // From forex trades
        for forexTrade in data.ForexTrades do
            if not (currencies.Contains(forexTrade.BaseCurrency)) then
                currencies.Add(forexTrade.BaseCurrency)
            if not (currencies.Contains(forexTrade.QuoteCurrency)) then
                currencies.Add(forexTrade.QuoteCurrency)
        
        // From cash movements
        for cashMovement in data.CashMovements do
            if not (currencies.Contains(cashMovement.Currency)) then
                currencies.Add(cashMovement.Currency)
        
        // From cash flows
        for cashFlow in data.CashFlows do
            if not (currencies.Contains(cashFlow.Currency)) then
                currencies.Add(cashFlow.Currency)
        
        currencies |> List.ofSeq |> List.sort
    
    /// <summary>
    /// Extract unique ticker symbols from IBKR statement data
    /// Used for ticker auto-creation
    /// </summary>
    let extractTickerSymbols (data: IBKRStatementData) : string list =
        let symbols = ResizeArray<string>()
        
        // From trades
        for trade in data.Trades do
            if not (symbols.Contains(trade.Symbol)) then
                symbols.Add(trade.Symbol)
        
        // From open positions
        for position in data.OpenPositions do
            if not (symbols.Contains(position.Symbol)) then
                symbols.Add(position.Symbol)
        
        symbols |> List.ofSeq |> List.sort